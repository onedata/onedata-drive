using NLog;
using OnedataDrive;
using OnedataDrive.ErrorHandling;
using OnedataDrive.JSON_Object;
using System.Diagnostics;
using System.Text.Json;

namespace OnedataDriveGUI
{
    public partial class ConnectForm : Form
    {
        private const string ROOT_DIR = "OnedataDrive";

        private string loggerPath;
        private Logger logger;
        private bool connectClicked = false;
        private string userProfilePath { get; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        private string defaultRootPath { get => userProfilePath + "\\" + ROOT_DIR; }
        private CustomSettings userSettings = new();

        public ConnectForm()
        {
            this.Text = CloudSync.APP_NAME;
            loggerPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\Logs";
            NLog.GlobalDiagnosticsContext.Set("logdir", loggerPath);
            logger = LogManager.GetCurrentClassLogger();
            logger.Info("APP GUI LAUNCHED - version: " + CloudSync.VERSION);

            InitializeComponent();

            InitGuiValuesDefaults();

            LoadLastConfig();

            CloudSync.OnMessageGenerated += PrintStatusMessage;
        }

        private void PrintStatusMessage(string message)
        {
            secondaryStatusMessage.Text = message;
        }

        private void InitGuiValuesDefaults()
        {
            disconect_button.Enabled = false;
            rootFolder_textBox.PlaceholderText = defaultRootPath;
            rootFolder_folderBrowserDialog.InitialDirectory = userProfilePath;
            version_label.Text = CloudSync.VERSION;
            oneproviderTokenKeep_checkBox.Checked = true;
            disableRefresh_checkBox.Checked = false;
            readOnly_checkBox.Checked = true;
            rootFolderDelete_checkBox.Checked = false;
        }

        private void LoadLastConfig()
        {
            onezone_comboBox.Text = userSettings.Onezone;
            oneproviderToken_textBox.Text = userSettings.OneproviderToken;
            rootFolder_textBox.Text = userSettings.RootFolderPath;

            oneproviderTokenKeep_checkBox.Checked = userSettings.OneproviderTokenKeep;
            rootFolderDelete_checkBox.Checked = userSettings.RootFolderDeleteCheckBox;
            disableRefresh_checkBox.Checked = userSettings.DisableRefreshCheckbox;
            readOnly_checkBox.Checked = userSettings.ReadOnlyCheckbox;
        }

        private void SaveLastConfig()
        {
            userSettings.RootFolderDeleteCheckBox = rootFolderDelete_checkBox.Checked;
            userSettings.Onezone = onezone_comboBox.Text;
            userSettings.RootFolderPath = rootFolder_textBox.Text;
            userSettings.OneproviderTokenKeep = oneproviderTokenKeep_checkBox.Checked;
            userSettings.DisableRefreshCheckbox = disableRefresh_checkBox.Checked;
            userSettings.ReadOnlyCheckbox = readOnly_checkBox.Checked;
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

        public static async Task<CloudSyncReturnCodes> LaunchCloudSyncAsync(Config config)
        {
            CloudSyncReturnCodes status;
            status = await CloudSync.RunAsync(config);

            if (status == CloudSyncReturnCodes.ROOT_FOLDER_NOT_EMPTY && !config.deleteExistingRootDir)
            {
                string message = "Can not connect, because Root Folder "
                + config.root_path
                + " is not empty. In order to connect this directory needs to be empty. Do you want to delete contents of this directory?";
                if (MessageBox.Show(message, "Onedata Drive", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    config.deleteExistingRootDir = true;
                    status = await CloudSync.RunAsync(config);
                }
            }
            return status;
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

        /* ---------- EVENT FUNCTIONS ---------- */
        /*          |                 |          */
        /*          V                 V          */

        // Form events

        private void ConnectForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            SetDisplayStatus(Status.DISCONNECTING);
            statusMessage.Text = "Disconnecting";
            if (CloudSync.Running)
            {
                CloudSync.Stop();
            }
            logger.Info("APP GUI STOPPED");
        }

        private void ConnectForm_Closing(object sender, FormClosingEventArgs e)
        {
            string message = $"Are you sure you want to quit? This will disconnect {CloudSync.APP_NAME}";
            if (statusImageGreen.Visible &&
                MessageBox.Show(message, CloudSync.APP_NAME, MessageBoxButtons.YesNo) == DialogResult.No)
            {
                e.Cancel = true;
            }
        }


        // Button clicked events //

        private async void connect_button_Click(object sender, EventArgs e)
        {
            // prohibit double click
            if (connectClicked)
            {
                return;
            }
            connectClicked = true;

            SetDisplayStatus(Status.CONNECTING);
            statusMessage.Text = "In progress";
            SaveLastConfig();
            Config config = new();
            config.Init(
                path: rootFolder_textBox.Text.Length == 0 ? defaultRootPath : rootFolder_textBox.Text,
                token: oneproviderToken_textBox.Text,
                host: onezone_comboBox.Text);
            config.deleteExistingRootDir = rootFolderDelete_checkBox.Checked;
            config.enableRefresh = !disableRefresh_checkBox.Checked;
            config.readOnly = readOnly_checkBox.Checked;

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
                case CloudSyncReturnCodes.STARTUP_CANCELED:
                    statusMessage.Text = "Startup Canceled";
                    break;
                case CloudSyncReturnCodes.ROOT_FOLDER_EXCEPTION:
                    statusMessage.Text = "Can not create root folder";
                    break;
                default:
                    SetDisplayStatus(Status.ERROR);
                    statusMessage.Text = "Unknown Error";
                    break;
            }

            // prohibit double click
            connectClicked = false;
        }

        private async void disconect_button_ClickAsync(object sender, EventArgs e)
        {
            SetDisplayStatus(Status.DISCONNECTING);
            statusMessage.Text = "Disconnecting";
            await CloudSync.Stop();
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
            config_openFileDialog.Filter = "JSON files (*.json)|*.json";
            config_openFileDialog.FilterIndex = 1;
            config_openFileDialog.FileName = "config.json";
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

        private void saveToFile_button_Click(object sender, EventArgs e)
        {
            config_saveFileDialog.Filter = "JSON files (*.json)|*.json";
            config_saveFileDialog.FilterIndex = 1;
            config_saveFileDialog.FileName = "config.json";
            if (config_saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                Config config = new();
                try
                {
                    config.onezone = onezone_comboBox.Text;
                    config.provider_token = oneproviderToken_textBox.Text;
                    config.root_path = rootFolder_textBox.Text;
                    string content = JsonSerializer.Serialize(config);


                    string filePath = config_saveFileDialog.FileName;

                    File.WriteAllText(config_saveFileDialog.FileName, content);
                }
                catch (Exception)
                {
                    statusMessage.Text = "Failed to save file";
                }
            }
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

        private void rootFolderErase_button_Click(object sender, EventArgs e)
        {
            rootFolder_textBox.Text = "";
        }

        private void openLogFolder_button_Click(object sender, EventArgs e)
        {
            string logPath = loggerPath;
            Process.Start("explorer.exe", logPath);
        }

        private void removeSyncRoot_button_Click(object sender, EventArgs e)
        {
            try
            {
                logger.Info("Unregister SyncRoot START, CloudSync running: {0}", CloudSync.Running);
                if (!CloudSync.Running)
                {
                    string message = "Use this only after crash or when you can not remove SyncRoot by using Disconnect. Do you want to proceed?";
                    if (MessageBox.Show(message, CloudSync.APP_NAME, MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        CloudProvider.UnregisterSafely();
                        statusMessage.Text = "Unregister SyncRoot OK";
                    }
                }
            }
            catch (Exception exception)
            {
                statusMessage.Text = "Unregister SyncRoot FAIL";
                logger.Error("Unregister SyncRoot", exception);
            }
        }

        private void createToken_linkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string host = onezone_comboBox.Text?.Trim() ?? "";
            string url;

            if (string.IsNullOrEmpty(host))
            {
                MessageBox.Show("Select/write a Onezone, to generate token creation link", CloudSync.APP_NAME, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                // remove any scheme if the user included it
                if (host.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    host = host.Substring("http://".Length);
                }
                else if (host.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    host = host.Substring("https://".Length);
                }

                // remove trailing slashes
                host = host.TrimEnd('/');

                // build token creation path based on selected onezone host
                url = $"https://{host}/ozw/onezone/i#/onedata/tokens/new?options=";
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
                MessageBox.Show("Failed to open web browser.", CloudSync.APP_NAME, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}