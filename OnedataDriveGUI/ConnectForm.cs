using OnedataDriveGUI.Properties;
using OnedataDrive.ErrorHandling;
using OnedataDrive;
using OnedataDrive.JSON_Object;

namespace OnedataDriveGUI
{
    public partial class ConnectForm : Form
    {
        private bool connectClicked = false;
        private const string ROOT_DIR = "OnedataDrive";
        //private string exePath { get; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
        private string userProfilePath { get; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        private string defaultRootPath
        {
            get
            {
                return userProfilePath + "\\" + ROOT_DIR;
            }
        }
        private string textBoxErrBC = "#d38787";
        private string textBoxBC = "#ffffff";

        public ConnectForm()
        {
            InitializeComponent();
            disconect_button.Enabled = false;
            rootFolder_textBox.PlaceholderText = defaultRootPath;
            rootFolder_folderBrowserDialog.InitialDirectory = userProfilePath;

            rootFolderDelete_checkBox.Checked = Settings.Default.RootFolderDeleteCheckBox;

            LoadLastConfig();
        }

        private async void connect_button_Click(object sender, EventArgs e)
        {
            // prohibit double click
            if (connectClicked)
            {
                return;
            }
            connectClicked = true;

            SetDisplayStatus(Status.CONNECTING);
            SaveLastConfig();
            Config config = new();
            config.Init(
                path: rootFolder_textBox.Text.Length == 0 ? defaultRootPath : rootFolder_textBox.Text,
                token: oneproviderToken_textBox.Text,
                host: onezone_comboBox.Text);
            statusMessage.Text = "In progress";

            CloudSyncReturnCodes returnCode = await LaunchCloudSyncAsync(config);

            if (returnCode == CloudSyncReturnCodes.SUCCESS)
            {
                SetDisplayStatus(Status.CONNECTED);
            }
            else
            {
                SetDisplayStatus(Status.ERROR);
            }

            switch (returnCode)
            {
                case CloudSyncReturnCodes.SUCCESS:
                    statusMessage.Text = "Connected";
                    break;
                case CloudSyncReturnCodes.ERROR:
                    statusMessage.Text = "Failed to connect";
                    break;
                case CloudSyncReturnCodes.ROOT_FOLDER_NO_ACCESS_RIGHT:
                    statusMessage.Text = "Does not have sufficient access rights for Root folder";
                    break;
                case CloudSyncReturnCodes.ONEZONE_FAIL:
                    statusMessage.Text = "Invalid Onezone";
                    break;
                case CloudSyncReturnCodes.TOKEN_FAIL:
                    statusMessage.Text = "Invalid Token";
                    break;
                case CloudSyncReturnCodes.INVALID_TOKEN_TYPE:
                    statusMessage.Text = "Invalid Token - should support REST/CDMI";
                    break;
                case CloudSyncReturnCodes.ROOT_FOLDER_NOT_EMPTY:
                    statusMessage.Text = "Root Folder not empty";
                    break;
                default:
                    SetDisplayStatus(Status.ERROR);
                    statusMessage.Text = "Unknown Error";
                    break;
            }

            // prohibit double click
            connectClicked = false;
        }

        private async Task<CloudSyncReturnCodes> LaunchCloudSyncAsync(Config config)
        {
            CloudSyncReturnCodes status;

            status = await Task.Run(() => CloudSync.Run(config, delete: rootFolderDelete_checkBox.Checked));

            if (status == CloudSyncReturnCodes.ROOT_FOLDER_NOT_EMPTY && !rootFolderDelete_checkBox.Checked)
            {
                string message = "Can not connect, because Root Folder "
                + config.root_path
                + " is not empty. Do you want to delete contents of this folder?";
                if (MessageBox.Show(message, ROOT_DIR, MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    status = await Task.Run(() => CloudSync.Run(config, delete: true));
                }
            }
            return status;
        }

        private void disconect_button_Click(object sender, EventArgs e)
        {
            SetDisplayStatus(Status.DISCONNECTING);
            statusMessage.Text = "Disconnecting";
            CloudSync.Stop();
            statusMessage.Text = "Disconected";
            SetDisplayStatus(Status.NOT_CONNECTED);
        }

        private void folderBrowser_button_Click(object sender, EventArgs e)
        {
            if (rootFolder_folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                rootFolder_textBox.Text = rootFolder_folderBrowserDialog.SelectedPath
                    + "\\" + ROOT_DIR;
            }
        }

        private void loadFromFile_button_Click(object sender, EventArgs e)
        {
            if (config_openFileDialog.ShowDialog() == DialogResult.OK)
            {
                Config config = new();
                try
                {
                    config.Init(config_openFileDialog.FileName);
                    onezone_comboBox.Text = config.onezone;
                    oneproviderToken_textBox.Text = config.provider_token;
                    rootFolder_textBox.Text = config.root_path;

                    statusMessage.Text = "file read OK";
                }
                catch (Exception)
                {
                    statusMessage.Text = "Failed to read file";
                }
            }
        }

        private void ConnectForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            SetDisplayStatus(Status.DISCONNECTING);
            statusMessage.Text = "Disconnecting";
            CloudSync.Stop();
        }

        private void advanced_button_Click(object sender, EventArgs e)
        {
            if (advanced_panel.Visible)
            {
                advanced_panel.Hide();
            }
            else
            {
                advanced_panel.Show();
            }
        }

        private void ConnectForm_Closing(object sender, FormClosingEventArgs e)
        {
            string message = $"Are you sure you want to quit? This will disconnect {ROOT_DIR}";
            if (statusImageGreen.Visible &&
                MessageBox.Show(message, ROOT_DIR, MessageBoxButtons.YesNo) == DialogResult.No)
            {
                e.Cancel = true;
            }
        }

        private void SetDisplayStatus(Status status)
        {
            Dictionary<Status, bool> mask = new()
            {
                { Status.CONNECTED, false },
                { Status.NOT_CONNECTED, false },
                { Status.ERROR, false },
                { Status.CONNECTING, false },
                { Status.DISCONNECTING, false },
            };

            mask[status] = true;

            statusImageBlue.Visible = mask[Status.CONNECTING] || mask[Status.DISCONNECTING];
            statusImageGrey.Visible = mask[Status.NOT_CONNECTED];
            statusImageGreen.Visible = mask[Status.CONNECTED];
            statusImageRed.Visible = mask[Status.ERROR];

            advanced_panel.Enabled = form_panel.Enabled = mask[Status.NOT_CONNECTED] || mask[Status.ERROR];

            connect_button.Enabled = mask[Status.NOT_CONNECTED] || mask[Status.ERROR];
            disconect_button.Enabled = !connect_button.Enabled;
        }

        private void rootFolderErase_button_Click(object sender, EventArgs e)
        {
            rootFolder_textBox.Text = "";
        }

        private void LoadLastConfig()
        {
            onezone_comboBox.Text = Settings.Default.Onezone;
            oneproviderToken_textBox.Text = Settings.Default.OneproviderToken;
            rootFolder_textBox.Text = Settings.Default.RootFolderPath;

            oneproviderTokenKeep_checkBox.Checked = Settings.Default.OneproviderTokenKeep;
            rootFolderDelete_checkBox.Checked = Settings.Default.RootFolderDeleteCheckBox;
        }

        private void SetForm(Config config)
        {
            rootFolder_textBox.Text = config.root_path;
            onezone_comboBox.Text = config.onezone;
            oneproviderToken_textBox.Text = config.provider_token;
        }

        private void SaveLastConfig()
        {
            Settings.Default.RootFolderDeleteCheckBox = rootFolderDelete_checkBox.Checked;
            Settings.Default.Onezone = onezone_comboBox.Text;
            Settings.Default.RootFolderPath = rootFolder_textBox.Text;
            Settings.Default.OneproviderTokenKeep = oneproviderTokenKeep_checkBox.Checked;
            if (oneproviderTokenKeep_checkBox.Checked)
            {
                Settings.Default.OneproviderToken = oneproviderToken_textBox.Text;
            }
            else
            {
                Settings.Default.OneproviderToken = "";
            }

            Settings.Default.Save();
        }
    }
}
