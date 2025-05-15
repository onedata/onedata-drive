using OnedataDrive.ErrorHandling;
using OnedataDrive;
using OnedataDrive.JSON_Object;
using NLog;
using System.Diagnostics;

namespace OnedataDriveGUI
{
    public partial class ConnectForm : Form
    {
        private const string ROOT_DIR = "OnedataDrive";

        public static Logger logger = LogManager.GetCurrentClassLogger();
        private bool connectClicked = false;
        private string userProfilePath { get; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        private string defaultRootPath { get => userProfilePath + "\\" + ROOT_DIR; }
        private CustomSettings userSettings = new();
        CancellationTokenSource cts;
        Task? refreshTask = null;
        public ConnectForm()
        {
            logger.Info("APP GUI LAUNCHED - version: " + CloudSync.VERSION);
            InitializeComponent();

            InitGuiValuesDefaults();

            LoadLastConfig();

            cts = new();
        }

        private void RefreshMonitor(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Thread.Sleep(100);
                if (PipeServer.refreshMsg != "")
                {
                    refreshStatusMessage.Text = " | " + PipeServer.refreshMsg;
                }
            }
            refreshStatusMessage.Text = "";
        }

        private void InitGuiValuesDefaults()
        {
            disconect_button.Enabled = false;
            rootFolder_textBox.PlaceholderText = defaultRootPath;
            rootFolder_folderBrowserDialog.InitialDirectory = userProfilePath;
            version_label.Text = CloudSync.VERSION;
        }

        private void LoadLastConfig()
        {
            onezone_comboBox.Text = userSettings.Onezone;
            oneproviderToken_textBox.Text = userSettings.OneproviderToken;
            rootFolder_textBox.Text = userSettings.RootFolderPath;

            oneproviderTokenKeep_checkBox.Checked = userSettings.OneproviderTokenKeep;
            rootFolderDelete_checkBox.Checked = userSettings.RootFolderDeleteCheckBox;
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

        private void SaveLastConfig()
        {
            userSettings.RootFolderDeleteCheckBox = rootFolderDelete_checkBox.Checked;
            userSettings.Onezone = onezone_comboBox.Text;
            userSettings.RootFolderPath = rootFolder_textBox.Text;
            userSettings.OneproviderTokenKeep = oneproviderTokenKeep_checkBox.Checked;
            if (oneproviderTokenKeep_checkBox.Checked)
            {
                userSettings.OneproviderToken = oneproviderToken_textBox.Text;
            }
            else
            {
                userSettings.OneproviderToken = "";
            }

            userSettings.Save();
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

            EnableDisableControl(advanced_panel, mask[Status.NOT_CONNECTED] || mask[Status.ERROR]);
            EnableDisableControl(form_panel, mask[Status.NOT_CONNECTED] || mask[Status.ERROR]);
            advanced_button.Enabled = true;
            openLogFolder_button.Enabled = true;

            connect_button.Enabled = mask[Status.NOT_CONNECTED] || mask[Status.ERROR];
            disconect_button.Enabled = !connect_button.Enabled;
        }

        private void EnableDisableControl(Control controlPanel, bool enabled)
        {
            foreach (Control control in controlPanel.Controls)
            {
                control.Enabled = enabled;
            }
        }

        /* ---------- EVENT FUNCTIONS ---------- */
        /*          |                 |          */
        /*          V                 V          */
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
                    cts = new();
                    refreshTask = Task.Run(() => RefreshMonitor(cts.Token), cts.Token);
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

        private void disconect_button_Click(object sender, EventArgs e)
        {
            SetDisplayStatus(Status.DISCONNECTING);
            statusMessage.Text = "Disconnecting";
            cts.Cancel();
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
            if (CloudSync.running)
            {
                CloudSync.Stop();
            }
            logger.Info("APP GUI STOPPED");
        }

        private void advanced_button_Click(object sender, EventArgs e)
        {
            if (advanced_panel.Visible)
            {
                advanced_panel.Hide();
                this.Size = new System.Drawing.Size(this.Size.Width, this.Size.Height - advanced_panel.Size.Height);
            }
            else
            {
                this.Size = new System.Drawing.Size(this.Size.Width, this.Size.Height + advanced_panel.Size.Height);
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

        private void rootFolderErase_button_Click(object sender, EventArgs e)
        {
            rootFolder_textBox.Text = "";
        }

        private void openLogFolder_button_Click(object sender, EventArgs e)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string logPath = Path.Join(appDataPath, "OnedataDrive\\logs");
            Process.Start("explorer.exe", logPath);
        }

        /// <summary>
        /// Creates border around advanced_panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void advanced_panel_paint(object sender, PaintEventArgs e)
        {
            if (advanced_panel.BorderStyle == BorderStyle.None)
            {
                int thickness = 20;//it's up to you
                int halfThickness = thickness / 2;
                using (Pen p = new Pen(SystemColors.Control, thickness))
                {
                    e.Graphics.DrawLine(p, new Point(halfThickness, 0), new Point(halfThickness, advanced_panel.ClientSize.Height));
                    e.Graphics.DrawLine(
                        p, 
                        new Point(advanced_panel.ClientSize.Width - halfThickness, 0), 
                        new Point(advanced_panel.ClientSize.Width - halfThickness, advanced_panel.ClientSize.Height));
                }
            }
        }
    }
}
