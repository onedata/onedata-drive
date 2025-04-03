namespace OnedataDriveGUI
{
    partial class ConnectForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConnectForm));
            header_panel = new Panel();
            version_label = new Label();
            pictureBox1 = new PictureBox();
            onezone_label = new Label();
            rootFolder_textBox = new TextBox();
            rootFolder_label = new Label();
            oneproviderToken_textBox = new TextBox();
            oneproviderToken_label = new Label();
            folderBrowser_button = new Button();
            rootFolder_folderBrowserDialog = new FolderBrowserDialog();
            rootFolderDelete_checkBox = new CheckBox();
            connect_button = new Button();
            disconect_button = new Button();
            loadFromFile_button = new Button();
            config_openFileDialog = new OpenFileDialog();
            form_panel = new Panel();
            onezone_comboBox = new ComboBox();
            oneproviderTokenKeep_checkBox = new CheckBox();
            advanced_button = new Button();
            rootFolderErase_button = new Button();
            advanced_panel = new Panel();
            openLogFolder_button = new Button();
            controls_panel = new Panel();
            statusStrip = new StatusStrip();
            statusImageGrey = new ToolStripStatusLabel();
            statusImageBlue = new ToolStripStatusLabel();
            statusImageRed = new ToolStripStatusLabel();
            statusImageGreen = new ToolStripStatusLabel();
            statusLabel = new ToolStripStatusLabel();
            statusMessage = new ToolStripStatusLabel();
            connectForm_toolTip = new ToolTip(components);
            scrollPanel = new Panel();
            refreshStatusMessage = new ToolStripStatusLabel();
            header_panel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            form_panel.SuspendLayout();
            advanced_panel.SuspendLayout();
            controls_panel.SuspendLayout();
            statusStrip.SuspendLayout();
            scrollPanel.SuspendLayout();
            SuspendLayout();
            // 
            // header_panel
            // 
            header_panel.BackColor = Color.FromArgb(54, 54, 54);
            header_panel.Controls.Add(version_label);
            header_panel.Controls.Add(pictureBox1);
            header_panel.Dock = DockStyle.Top;
            header_panel.Location = new Point(0, 0);
            header_panel.Margin = new Padding(2, 3, 2, 3);
            header_panel.Name = "header_panel";
            header_panel.Size = new Size(767, 119);
            header_panel.TabIndex = 3;
            // 
            // version_label
            // 
            version_label.AutoSize = true;
            version_label.ForeColor = Color.White;
            version_label.Location = new Point(292, 89);
            version_label.Name = "version_label";
            version_label.Size = new Size(39, 19);
            version_label.TabIndex = 2;
            version_label.Text = "0.0.0";
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(18, 0);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(268, 119);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 1;
            pictureBox1.TabStop = false;
            // 
            // onezone_label
            // 
            onezone_label.AutoSize = true;
            onezone_label.Location = new Point(17, 20);
            onezone_label.Margin = new Padding(2, 0, 2, 0);
            onezone_label.Name = "onezone_label";
            onezone_label.Size = new Size(64, 19);
            onezone_label.TabIndex = 4;
            onezone_label.Text = "Onezone";
            // 
            // rootFolder_textBox
            // 
            rootFolder_textBox.Location = new Point(178, 19);
            rootFolder_textBox.Margin = new Padding(2, 3, 2, 3);
            rootFolder_textBox.Name = "rootFolder_textBox";
            rootFolder_textBox.ReadOnly = true;
            rootFolder_textBox.Size = new Size(474, 25);
            rootFolder_textBox.TabIndex = 8;
            // 
            // rootFolder_label
            // 
            rootFolder_label.AutoSize = true;
            rootFolder_label.Location = new Point(35, 23);
            rootFolder_label.Margin = new Padding(2, 0, 2, 0);
            rootFolder_label.Name = "rootFolder_label";
            rootFolder_label.Size = new Size(80, 19);
            rootFolder_label.TabIndex = 7;
            rootFolder_label.Text = "Root Folder";
            // 
            // oneproviderToken_textBox
            // 
            oneproviderToken_textBox.Location = new Point(161, 86);
            oneproviderToken_textBox.Margin = new Padding(2, 3, 2, 3);
            oneproviderToken_textBox.Name = "oneproviderToken_textBox";
            oneproviderToken_textBox.Size = new Size(474, 25);
            oneproviderToken_textBox.TabIndex = 11;
            // 
            // oneproviderToken_label
            // 
            oneproviderToken_label.AutoSize = true;
            oneproviderToken_label.Location = new Point(17, 87);
            oneproviderToken_label.Margin = new Padding(2, 0, 2, 0);
            oneproviderToken_label.Name = "oneproviderToken_label";
            oneproviderToken_label.Size = new Size(127, 19);
            oneproviderToken_label.TabIndex = 10;
            oneproviderToken_label.Text = "Oneprovider Token";
            // 
            // folderBrowser_button
            // 
            folderBrowser_button.Location = new Point(656, 20);
            folderBrowser_button.Margin = new Padding(2, 3, 2, 3);
            folderBrowser_button.Name = "folderBrowser_button";
            folderBrowser_button.Size = new Size(31, 24);
            folderBrowser_button.TabIndex = 14;
            folderBrowser_button.Text = "...";
            folderBrowser_button.UseVisualStyleBackColor = true;
            folderBrowser_button.Click += folderBrowser_button_Click;
            // 
            // rootFolderDelete_checkBox
            // 
            rootFolderDelete_checkBox.AutoSize = true;
            rootFolderDelete_checkBox.Location = new Point(179, 64);
            rootFolderDelete_checkBox.Margin = new Padding(2, 3, 2, 3);
            rootFolderDelete_checkBox.Name = "rootFolderDelete_checkBox";
            rootFolderDelete_checkBox.Size = new Size(186, 23);
            rootFolderDelete_checkBox.TabIndex = 15;
            rootFolderDelete_checkBox.Text = "delete existing root folder";
            rootFolderDelete_checkBox.UseVisualStyleBackColor = true;
            // 
            // connect_button
            // 
            connect_button.BackColor = SystemColors.Window;
            connect_button.Location = new Point(528, 29);
            connect_button.Margin = new Padding(2, 3, 2, 3);
            connect_button.Name = "connect_button";
            connect_button.Size = new Size(118, 49);
            connect_button.TabIndex = 16;
            connect_button.Text = "Connect";
            connect_button.UseVisualStyleBackColor = false;
            connect_button.Click += connect_button_Click;
            // 
            // disconect_button
            // 
            disconect_button.BackColor = SystemColors.Window;
            disconect_button.Location = new Point(391, 29);
            disconect_button.Margin = new Padding(2, 3, 2, 3);
            disconect_button.Name = "disconect_button";
            disconect_button.Size = new Size(118, 49);
            disconect_button.TabIndex = 17;
            disconect_button.Text = "Disconnect";
            disconect_button.UseVisualStyleBackColor = false;
            disconect_button.Click += disconect_button_Click;
            // 
            // loadFromFile_button
            // 
            loadFromFile_button.BackColor = SystemColors.Window;
            loadFromFile_button.Location = new Point(177, 103);
            loadFromFile_button.Margin = new Padding(2, 3, 2, 3);
            loadFromFile_button.Name = "loadFromFile_button";
            loadFromFile_button.Size = new Size(194, 33);
            loadFromFile_button.TabIndex = 19;
            loadFromFile_button.Text = "Load configuration from file";
            loadFromFile_button.UseVisualStyleBackColor = false;
            loadFromFile_button.Click += loadFromFile_button_Click;
            // 
            // config_openFileDialog
            // 
            config_openFileDialog.FileName = "openFileDialog1";
            config_openFileDialog.Filter = "JSON|*.json";
            // 
            // form_panel
            // 
            form_panel.Controls.Add(onezone_comboBox);
            form_panel.Controls.Add(oneproviderTokenKeep_checkBox);
            form_panel.Controls.Add(advanced_button);
            form_panel.Controls.Add(onezone_label);
            form_panel.Controls.Add(oneproviderToken_label);
            form_panel.Controls.Add(oneproviderToken_textBox);
            form_panel.Dock = DockStyle.Top;
            form_panel.Location = new Point(0, 119);
            form_panel.Name = "form_panel";
            form_panel.Size = new Size(767, 181);
            form_panel.TabIndex = 20;
            // 
            // onezone_comboBox
            // 
            onezone_comboBox.FormattingEnabled = true;
            onezone_comboBox.Items.AddRange(new object[] { "datahub.egi.eu", "onedata.e-infra.cz", "onezone.onedata.org" });
            onezone_comboBox.Location = new Point(161, 16);
            onezone_comboBox.Name = "onezone_comboBox";
            onezone_comboBox.Size = new Size(473, 27);
            onezone_comboBox.TabIndex = 17;
            // 
            // oneproviderTokenKeep_checkBox
            // 
            oneproviderTokenKeep_checkBox.AutoSize = true;
            oneproviderTokenKeep_checkBox.Location = new Point(161, 120);
            oneproviderTokenKeep_checkBox.Name = "oneproviderTokenKeep_checkBox";
            oneproviderTokenKeep_checkBox.Size = new Size(204, 23);
            oneproviderTokenKeep_checkBox.TabIndex = 16;
            oneproviderTokenKeep_checkBox.Text = "Remeber Oneprovider Token";
            oneproviderTokenKeep_checkBox.UseVisualStyleBackColor = true;
            // 
            // advanced_button
            // 
            advanced_button.FlatAppearance.BorderSize = 0;
            advanced_button.FlatStyle = FlatStyle.Flat;
            advanced_button.Image = (Image)resources.GetObject("advanced_button.Image");
            advanced_button.ImageAlign = ContentAlignment.MiddleRight;
            advanced_button.Location = new Point(15, 148);
            advanced_button.Name = "advanced_button";
            advanced_button.Size = new Size(110, 28);
            advanced_button.TabIndex = 15;
            advanced_button.Text = "Advanced";
            advanced_button.TextAlign = ContentAlignment.MiddleLeft;
            advanced_button.UseVisualStyleBackColor = true;
            advanced_button.Click += advanced_button_Click;
            // 
            // rootFolderErase_button
            // 
            rootFolderErase_button.Location = new Point(691, 20);
            rootFolderErase_button.Margin = new Padding(2, 3, 2, 3);
            rootFolderErase_button.Name = "rootFolderErase_button";
            rootFolderErase_button.Size = new Size(31, 24);
            rootFolderErase_button.TabIndex = 16;
            rootFolderErase_button.Text = "X";
            connectForm_toolTip.SetToolTip(rootFolderErase_button, "This will erase Root Folder path \r\nand set it to default value");
            rootFolderErase_button.UseVisualStyleBackColor = true;
            rootFolderErase_button.Click += rootFolderErase_button_Click;
            // 
            // advanced_panel
            // 
            advanced_panel.BackColor = Color.FromArgb(225, 225, 225);
            advanced_panel.Controls.Add(openLogFolder_button);
            advanced_panel.Controls.Add(rootFolderErase_button);
            advanced_panel.Controls.Add(loadFromFile_button);
            advanced_panel.Controls.Add(rootFolderDelete_checkBox);
            advanced_panel.Controls.Add(rootFolder_label);
            advanced_panel.Controls.Add(rootFolder_textBox);
            advanced_panel.Controls.Add(folderBrowser_button);
            advanced_panel.Dock = DockStyle.Top;
            advanced_panel.Location = new Point(0, 300);
            advanced_panel.Name = "advanced_panel";
            advanced_panel.Size = new Size(767, 200);
            advanced_panel.TabIndex = 21;
            advanced_panel.Visible = false;
            advanced_panel.Paint += advanced_panel_paint;
            // 
            // openLogFolder_button
            // 
            openLogFolder_button.BackColor = SystemColors.Window;
            openLogFolder_button.Location = new Point(177, 151);
            openLogFolder_button.Margin = new Padding(2, 3, 2, 3);
            openLogFolder_button.Name = "openLogFolder_button";
            openLogFolder_button.Size = new Size(194, 33);
            openLogFolder_button.TabIndex = 20;
            openLogFolder_button.Text = "Open folder with logs";
            openLogFolder_button.UseVisualStyleBackColor = false;
            openLogFolder_button.Click += openLogFolder_button_Click;
            // 
            // controls_panel
            // 
            controls_panel.Controls.Add(connect_button);
            controls_panel.Controls.Add(disconect_button);
            controls_panel.Dock = DockStyle.Top;
            controls_panel.Location = new Point(0, 500);
            controls_panel.Name = "controls_panel";
            controls_panel.Size = new Size(767, 97);
            controls_panel.TabIndex = 22;
            // 
            // statusStrip
            // 
            statusStrip.Items.AddRange(new ToolStripItem[] { statusImageGrey, statusImageBlue, statusImageRed, statusImageGreen, statusLabel, statusMessage, refreshStatusMessage });
            statusStrip.Location = new Point(0, 464);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(784, 22);
            statusStrip.TabIndex = 23;
            statusStrip.Text = "statusStrip1";
            // 
            // statusImageGrey
            // 
            statusImageGrey.Image = (Image)resources.GetObject("statusImageGrey.Image");
            statusImageGrey.Name = "statusImageGrey";
            statusImageGrey.Size = new Size(16, 17);
            // 
            // statusImageBlue
            // 
            statusImageBlue.Image = (Image)resources.GetObject("statusImageBlue.Image");
            statusImageBlue.Name = "statusImageBlue";
            statusImageBlue.Size = new Size(16, 17);
            statusImageBlue.Visible = false;
            // 
            // statusImageRed
            // 
            statusImageRed.Image = (Image)resources.GetObject("statusImageRed.Image");
            statusImageRed.Name = "statusImageRed";
            statusImageRed.Size = new Size(16, 17);
            statusImageRed.Visible = false;
            // 
            // statusImageGreen
            // 
            statusImageGreen.Image = (Image)resources.GetObject("statusImageGreen.Image");
            statusImageGreen.Name = "statusImageGreen";
            statusImageGreen.Size = new Size(16, 17);
            statusImageGreen.Visible = false;
            // 
            // statusLabel
            // 
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(45, 17);
            statusLabel.Text = "Status: ";
            // 
            // statusMessage
            // 
            statusMessage.Name = "statusMessage";
            statusMessage.Size = new Size(0, 17);
            // 
            // scrollPanel
            // 
            scrollPanel.AutoScroll = true;
            scrollPanel.Controls.Add(controls_panel);
            scrollPanel.Controls.Add(advanced_panel);
            scrollPanel.Controls.Add(form_panel);
            scrollPanel.Controls.Add(header_panel);
            scrollPanel.Dock = DockStyle.Fill;
            scrollPanel.Location = new Point(0, 0);
            scrollPanel.Name = "scrollPanel";
            scrollPanel.Size = new Size(784, 464);
            scrollPanel.TabIndex = 24;
            // 
            // refreshStatusMessage
            // 
            refreshStatusMessage.Name = "refreshStatusMessage";
            refreshStatusMessage.Size = new Size(0, 17);
            // 
            // ConnectForm
            // 
            AutoScaleDimensions = new SizeF(8F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoScroll = true;
            ClientSize = new Size(784, 486);
            Controls.Add(scrollPanel);
            Controls.Add(statusStrip);
            Font = new Font("Microsoft YaHei", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(2, 3, 2, 3);
            MaximizeBox = false;
            MaximumSize = new Size(900, 1080);
            MinimumSize = new Size(800, 525);
            Name = "ConnectForm";
            Text = "OnedataDrive";
            FormClosing += ConnectForm_Closing;
            FormClosed += ConnectForm_FormClosed;
            header_panel.ResumeLayout(false);
            header_panel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            form_panel.ResumeLayout(false);
            form_panel.PerformLayout();
            advanced_panel.ResumeLayout(false);
            advanced_panel.PerformLayout();
            controls_panel.ResumeLayout(false);
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            scrollPanel.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Panel header_panel;
        private Label onezone_label;
        private TextBox rootFolder_textBox;
        private Label rootFolder_label;
        private TextBox oneproviderToken_textBox;
        private Label oneproviderToken_label;
        private Button folderBrowser_button;
        private FolderBrowserDialog rootFolder_folderBrowserDialog;
        private CheckBox rootFolderDelete_checkBox;
        private Button connect_button;
        private Button disconect_button;
        private Label status_label;
        private Button loadFromFile_button;
        private FolderBrowserDialog config_folderBrowserDialog;
        private OpenFileDialog config_openFileDialog;
        private Panel form_panel;
        private Panel advanced_panel;
        private Panel controls_panel;
        private Button advanced_button;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripStatusLabel statusMessage;
        private ToolStripStatusLabel statusImageRed;
        private ToolStripStatusLabel statusImageGreen;
        private Button rootFolderErase_button;
        private ToolTip connectForm_toolTip;
        private Panel scrollPanel;
        private CheckBox oneproviderTokenKeep_checkBox;
        private ToolStripStatusLabel statusImageGrey;
        private ToolStripStatusLabel statusImageBlue;
        private PictureBox pictureBox1;
        private ComboBox onezone_comboBox;
        private Label version_label;
        private Button openLogFolder_button;
        private ToolStripStatusLabel refreshStatusMessage;
    }
}
