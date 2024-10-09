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
            Config config = new();
            config.Init(
                path: rootPath_textBox.Text,
                token: providerToken_textBox.Text,
                host: zoneHost_textBox.Text);
            label1.Text = "In progress";
            await LaunchCloudSyncAsync(config);
        }

        private async Task LaunchCloudSyncAsync(Config config)
        {
            int status = await Task.Run(() => CloudSync.Run(config, delete: deleteRoot_checkBox.Checked));
            if (status == 0)
            {
                label1.Text = "Connected";
            }
            else
            {
                label1.Text = "Something went wrong";
            }
        }

        private void disconect_button_Click(object sender, EventArgs e)
        {
            CloudSync.Stop();
            label1.Text = "Disconected";
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
                    label1.Text = "file read OK";
                }
                catch (Exception)
                {
                    label1.Text = "Failed to read file";
                }

            }
        }

        private void ConnectForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            CloudSync.Stop();
        }
    }
}
