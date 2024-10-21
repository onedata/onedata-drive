using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Windows.Media.Audio;

namespace WinForms
{
    public partial class ConnectForm : Form
    {
        private const string ROOT_DIR = "Onedata Drive";
        private string exePath { get; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
        private string appDataPath { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private string defaultRootPath 
        {
            get
            {
                return appDataPath + "\\" + "Onedata Drive";
            }
        }
        private string lastConfigPath
        {
            get
            {
                return exePath + "\\" + "last_form.json";
            }
        }

        public ConnectForm()
        {
            InitializeComponent();
            disconect_button.Enabled = false;
            rootFolder_textBox.PlaceholderText = defaultRootPath;
            rootFolder_folderBrowserDialog.InitialDirectory = appDataPath;
            LoadLastConfig();
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

            if (valid)
            {
                Config config = new();
                config.Init(
                    path: rootFolder_textBox.Text.Length == 0 ? defaultRootPath : rootFolder_textBox.Text,
                    token: oneproviderToken_textBox.Text,
                    host: onezone_textBox.Text);
                statusMessage.Text = "In progress";
                SaveLastConfig();
                if (await LaunchCloudSyncAsync(config) == 0)
                {
                    statusMessage.Text = "Connected";
                    SetStatus(running: true);
                }
                else
                {
                    statusMessage.Text = "Failed to connect";
                }
            }
            else
            {
                statusMessage.Text = "Invalid values";
            }
        }

        private async Task<int> LaunchCloudSyncAsync(Config config)
        {
            int status = await Task.Run(() => CloudSync.Run(config, delete: deleteRoot_checkBox.Checked));
            return status;
        }

        private void disconect_button_Click(object sender, EventArgs e)
        {
            CloudSync.Stop();
            statusMessage.Text = "Disconected";
            SetStatus(running: false);
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
            string message = "Are you sure you want to quit? This will disconect Onedata Drive";
            if (statusImageGreen.Visible &&
                MessageBox.Show(message, "Onedata Drive", MessageBoxButtons.YesNo) == DialogResult.No)
            {
                e.Cancel = true;
            }
        }

        private void SetStatus(bool running)
        {
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
        }

        private void rootFolderErase_button_Click(object sender, EventArgs e)
        {
            rootFolder_textBox.Text = "";
        }

        private void LoadLastConfig()
        {
            if (File.Exists(lastConfigPath))
            {
                try
                {
                    Config lastConfig = new();
                    lastConfig.Init(lastConfigPath);
                    SetForm(lastConfig);
                }
                catch { return; }
            }
        }

        private void SetForm(Config config)
        {
            rootFolder_textBox.Text = config.root_path;
            onezone_textBox.Text = config.zone_host;
            oneproviderToken_textBox.Text = config.provider_token;
        }

        private void SaveLastConfig()
        {
            try
            {
                Config config = new();
                config.Init(
                    host: onezone_textBox.Text,
                    token: oneproviderToken_textBox.Text,
                    path: rootFolder_textBox.Text);
                string json = JsonSerializer.Serialize(config);
                File.WriteAllText(lastConfigPath, json);
            }
            catch { return; }
        }
    }
}
