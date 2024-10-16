using System.Diagnostics;

namespace WinForms
{
    public partial class ConnectForm : Form
    {
        private const string ROOT_DIR = "Onedata Drive";
        public ConnectForm()
        {
            InitializeComponent();
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            rootFolder_textBox.Text = appData + "\\" + ROOT_DIR;
            rootPath_folderBrowserDialog.InitialDirectory = appData;
            Debug.WriteLine("User Home Directory: " + appData);
        }

        private async void connect_button_Click(object sender, EventArgs e)
        {
            bool valid = true;
            err_rootFolder_label.Visible = false;
            err_oneproviderToken_label.Visible = false;
            err_onezone_label.Visible = false;

            if (oneproviderToken_textBox.Text == "")
            {
                err_oneproviderToken_label.Visible = true;
                valid = false;
            }
            if (onezone_textBox.Text == "")
            {
                err_onezone_label.Visible = true;
                valid = false;
            }
            if (rootFolder_textBox.Text == "")
            {
                err_rootFolder_label.Visible = true;
                valid = false;
            }

            if (valid)
            {
                Config config = new();
                config.Init(
                    path: rootFolder_textBox.Text,
                    token: oneproviderToken_textBox.Text,
                    host: onezone_textBox.Text);
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
                rootFolder_textBox.Text = rootPath_folderBrowserDialog.SelectedPath
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
                    rootFolder_textBox.Text = config.root_path;
                    oneproviderToken_textBox.Text = config.provider_token;
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
