namespace WinForms
{
    public partial class ConnectForm : Form
    {
        public ConnectForm()
        {
            InitializeComponent();
        }

        private async void connect_button_Click(object sender, EventArgs e)
        {
            bool valid = true;
            err_rootPath_label.Visible = false;
            err_providerToken_label.Visible = false;
            err_zoneHost_label.Visible = false;

            if (providerToken_textBox.Text == "")
            {
                err_providerToken_label.Visible = true;
                valid = false;
            }
            if (zoneHost_textBox.Text == "")
            {
                err_zoneHost_label.Visible = true;
                valid = false;
            }
            if (rootPath_textBox.Text == "")
            {
                err_rootPath_label.Visible = true;
                valid = false;
            }

            if (valid)
            {
                Config config = new();
                config.Init(
                    path: rootPath_textBox.Text,
                    token: providerToken_textBox.Text,
                    host: zoneHost_textBox.Text);
                statusValue_label.Text = "In progress";
                await LaunchCloudSyncAsync(config);
            }
            else 
            {
                statusValue_label.Text = "Invalid values";
            }
        }

        private async Task LaunchCloudSyncAsync(Config config)
        {
            int status = await Task.Run(() => CloudSync.Run(config, delete: deleteRoot_checkBox.Checked));
            if (status == 0)
            {
                statusValue_label.Text = "Connected";
            }
            else
            {
                statusValue_label.Text = "Something went wrong";
            }
        }

        private void disconect_button_Click(object sender, EventArgs e)
        {
            CloudSync.Stop();
            statusValue_label.Text = "Disconected";
        }

        private void folderBrowser_button_Click(object sender, EventArgs e)
        {
            if (rootPath_folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                rootPath_textBox.Text = rootPath_folderBrowserDialog.SelectedPath;
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
                    zoneHost_textBox.Text = config.zone_host;
                    rootPath_textBox.Text = config.root_path;
                    providerToken_textBox.Text = config.provider_token;
                    statusValue_label.Text = "file read OK";
                }
                catch (Exception)
                {
                    statusValue_label.Text = "Failed to read file";
                }

            }
        }

        private void ConnectForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            CloudSync.Stop();
        }
    }
}
