using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Windows.Media.Audio;
using OnedataDriveGUI.Properties;
using OnedataDrive.CloudSync.Exceptions;
using OnedataDrive.CloudSync.ErrorHandling;

namespace OnedataDriveGUI
{
    public partial class ConnectForm : Form
    {
        private bool connectClicked = false;
        private const string ROOT_DIR = "Onedata Drive";
        //private string exePath { get; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
        private string myDocumentsPath { get; } = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private string defaultRootPath
        {
            get
            {
                return myDocumentsPath + "\\" + "Onedata Drive";
            }
        }
        private string textBoxErrBC = "#d38787";
        private string textBoxBC = "#ffffff";

        public ConnectForm()
        {
            InitializeComponent();
            disconect_button.Enabled = false;
            rootFolder_textBox.PlaceholderText = defaultRootPath;
            rootFolder_folderBrowserDialog.InitialDirectory = myDocumentsPath;

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

            bool valid = true;
            oneproviderToken_textBox.BackColor = ColorTranslator.FromHtml(textBoxBC);
            onezone_textBox.BackColor = ColorTranslator.FromHtml(textBoxBC);

            if (oneproviderToken_textBox.Text == "")
            {
                oneproviderToken_textBox.BackColor = ColorTranslator.FromHtml(textBoxErrBC);
                valid = false;
            }
            if (onezone_textBox.Text == "")
            {
                onezone_textBox.BackColor = ColorTranslator.FromHtml(textBoxErrBC);
                valid = false;
            }

            if (valid)
            {
                SetDisplayStatus(Status.CONNECTING);
                Config config = new();
                config.Init(
                    path: rootFolder_textBox.Text.Length == 0 ? defaultRootPath : rootFolder_textBox.Text,
                    token: oneproviderToken_textBox.Text,
                    host: onezone_textBox.Text);
                statusMessage.Text = "In progress";
                SaveLastConfig();
                if (await LaunchCloudSyncAsync(config) == ReturnCodesEnum.SUCCESS)
                {
                    statusMessage.Text = "Connected";
                    SetDisplayStatus(Status.CONNECTED);
                }
                else
                {
                    statusMessage.Text = "Failed to connect";
                    SetDisplayStatus(Status.ERROR);
                }
            }
            else
            {
                statusMessage.Text = "Invalid values";
                SetDisplayStatus(Status.ERROR);
            }

            // prohibit double click
            connectClicked = false;
        }

        private async Task<ReturnCodesEnum> LaunchCloudSyncAsync(Config config)
        {
            ReturnCodesEnum status;

            status = await Task.Run(() => CloudSync.Run(config, delete: rootFolderDelete_checkBox.Checked));

            if (status == ReturnCodesEnum.ROOT_FOLDER_NOT_EMPTY && !rootFolderDelete_checkBox.Checked)
            {
                string message = "Can not connect, because Root Folder "
                + config.root_path
                + " is not empty. Do you want to delete contents of this folder?";
                if (MessageBox.Show(message, "Onedata Drive", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    status = await Task.Run(() => CloudSync.Run(config, delete: true));
                }
            }
            return status;
        }

        private void disconect_button_Click(object sender, EventArgs e)
        {
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
                    onezone_textBox.Text = config.zone_host;
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
            string message = "Are you sure you want to quit? This will disconnect Onedata Drive";
            if (statusImageGreen.Visible &&
                MessageBox.Show(message, "Onedata Drive", MessageBoxButtons.YesNo) == DialogResult.No)
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
            };

            mask[status] = true;

            statusImageBlue.Visible = mask[Status.CONNECTING];
            statusImageGrey.Visible = mask[Status.NOT_CONNECTED];
            statusImageGreen.Visible = mask[Status.CONNECTED];
            statusImageRed.Visible = mask[Status.ERROR];

            advanced_panel.Enabled = form_panel.Enabled = mask[Status.NOT_CONNECTED] || mask[Status.ERROR];

            connect_button.Enabled = mask[Status.NOT_CONNECTED] || mask[Status.ERROR];
            disconect_button.Enabled = !connect_button.Enabled;

            /*
            statusImageGreen.Visible = running;
            statusImageRed.Visible = !running;
            
            disconect_button.Enabled = running;
            connect_button.Enabled = !running;

            oneproviderToken_textBox.Enabled = !running;
            onezone_textBox.Enabled = !running;
            rootFolder_textBox.Enabled = !running;
            folderBrowser_button.Enabled = !running;
            rootFolderErase_button.Enabled = !running;
            advanced_panel.Enabled = !running;
            */
        }

        private void rootFolderErase_button_Click(object sender, EventArgs e)
        {
            rootFolder_textBox.Text = "";
        }

        private void LoadLastConfig()
        {
            onezone_textBox.Text = Settings.Default.Onezone;
            oneproviderToken_textBox.Text = Settings.Default.OneproviderToken;
            rootFolder_textBox.Text = Settings.Default.RootFolderPath;

            oneproviderTokenKeep_checkBox.Checked = Settings.Default.OneproviderTokenKeep;
            rootFolderDelete_checkBox.Checked = Settings.Default.RootFolderDeleteCheckBox;
        }

        private void SetForm(Config config)
        {
            rootFolder_textBox.Text = config.root_path;
            onezone_textBox.Text = config.zone_host;
            oneproviderToken_textBox.Text = config.provider_token;
        }

        private void SaveLastConfig()
        {
            Settings.Default.RootFolderDeleteCheckBox = rootFolderDelete_checkBox.Checked;
            Settings.Default.Onezone = onezone_textBox.Text;
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
