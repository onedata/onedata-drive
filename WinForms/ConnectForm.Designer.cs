namespace WinForms
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
            headingLabel = new Label();
            logoPictureBox = new PictureBox();
            onezone_label = new Label();
            onezone_textBox = new TextBox();
            err_onezone_label = new Label();
            err_rootFolder_label = new Label();
            rootFolder_textBox = new TextBox();
            rootFolder_label = new Label();
            err_oneproviderToken_label = new Label();
            oneproviderToken_textBox = new TextBox();
            oneproviderToken_label = new Label();
            folderBrowser_button = new Button();
            rootFolder_folderBrowserDialog = new FolderBrowserDialog();
            deleteRoot_checkBox = new CheckBox();
            connect_button = new Button();
            disconect_button = new Button();
            loadFromFile_button = new Button();
            config_openFileDialog = new OpenFileDialog();
            form_panel = new Panel();
            advanced_button = new Button();
            rootFolderErase_button = new Button();
            advanced_panel = new Panel();
            controls_panel = new Panel();
            statusStrip = new StatusStrip();
            statusImageRed = new ToolStripStatusLabel();
            statusImageGreen = new ToolStripStatusLabel();
            statusLabel = new ToolStripStatusLabel();
            statusMessage = new ToolStripStatusLabel();
            connectForm_toolTip = new ToolTip(components);
            scrollPanel = new Panel();
            header_panel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)logoPictureBox).BeginInit();
            form_panel.SuspendLayout();
            advanced_panel.SuspendLayout();
            controls_panel.SuspendLayout();
            statusStrip.SuspendLayout();
            scrollPanel.SuspendLayout();
            SuspendLayout();
            // 
            // header_panel
            // 
            header_panel.BackColor = Color.FromArgb(64, 64, 64);
            header_panel.Controls.Add(headingLabel);
            header_panel.Controls.Add(logoPictureBox);
            header_panel.Dock = DockStyle.Top;
            header_panel.Location = new Point(0, 0);
            header_panel.Margin = new Padding(2, 3, 2, 3);
            header_panel.Name = "header_panel";
            header_panel.Size = new Size(767, 119);
            header_panel.TabIndex = 3;
            // 
            // headingLabel
            // 
            headingLabel.AutoSize = true;
            headingLabel.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            headingLabel.ForeColor = Color.FromArgb(224, 224, 224);
            headingLabel.Location = new Point(128, 39);
            headingLabel.Margin = new Padding(2, 0, 2, 0);
            headingLabel.Name = "headingLabel";
            headingLabel.Size = new Size(202, 37);
            headingLabel.TabIndex = 1;
            headingLabel.Text = "Onedata Drive";
            headingLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // logoPictureBox
            // 
            logoPictureBox.Image = (Image)resources.GetObject("logoPictureBox.Image");
            logoPictureBox.Location = new Point(15, 15);
            logoPictureBox.Margin = new Padding(2, 3, 2, 3);
            logoPictureBox.Name = "logoPictureBox";
            logoPictureBox.Size = new Size(81, 91);
            logoPictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
            logoPictureBox.TabIndex = 0;
            logoPictureBox.TabStop = false;
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
            // onezone_textBox
            // 
            onezone_textBox.Location = new Point(161, 16);
            onezone_textBox.Margin = new Padding(2, 3, 2, 3);
            onezone_textBox.Name = "onezone_textBox";
            onezone_textBox.Size = new Size(474, 25);
            onezone_textBox.TabIndex = 5;
            // 
            // err_onezone_label
            // 
            err_onezone_label.AutoSize = true;
            err_onezone_label.ForeColor = Color.FromArgb(192, 0, 0);
            err_onezone_label.Location = new Point(161, 46);
            err_onezone_label.Margin = new Padding(2, 0, 2, 0);
            err_onezone_label.Name = "err_onezone_label";
            err_onezone_label.Size = new Size(48, 19);
            err_onezone_label.TabIndex = 6;
            err_onezone_label.Text = "Empty";
            err_onezone_label.Visible = false;
            // 
            // err_rootFolder_label
            // 
            err_rootFolder_label.AutoSize = true;
            err_rootFolder_label.ForeColor = Color.FromArgb(192, 0, 0);
            err_rootFolder_label.Location = new Point(160, 44);
            err_rootFolder_label.Margin = new Padding(2, 0, 2, 0);
            err_rootFolder_label.Name = "err_rootFolder_label";
            err_rootFolder_label.Size = new Size(48, 19);
            err_rootFolder_label.TabIndex = 9;
            err_rootFolder_label.Text = "Empty";
            err_rootFolder_label.Visible = false;
            // 
            // rootFolder_textBox
            // 
            rootFolder_textBox.Location = new Point(160, 14);
            rootFolder_textBox.Margin = new Padding(2, 3, 2, 3);
            rootFolder_textBox.Name = "rootFolder_textBox";
            rootFolder_textBox.ReadOnly = true;
            rootFolder_textBox.Size = new Size(474, 25);
            rootFolder_textBox.TabIndex = 8;
            // 
            // rootFolder_label
            // 
            rootFolder_label.AutoSize = true;
            rootFolder_label.Location = new Point(17, 18);
            rootFolder_label.Margin = new Padding(2, 0, 2, 0);
            rootFolder_label.Name = "rootFolder_label";
            rootFolder_label.Size = new Size(80, 19);
            rootFolder_label.TabIndex = 7;
            rootFolder_label.Text = "Root Folder";
            // 
            // err_oneproviderToken_label
            // 
            err_oneproviderToken_label.AutoSize = true;
            err_oneproviderToken_label.ForeColor = Color.FromArgb(192, 0, 0);
            err_oneproviderToken_label.Location = new Point(161, 116);
            err_oneproviderToken_label.Margin = new Padding(2, 0, 2, 0);
            err_oneproviderToken_label.Name = "err_oneproviderToken_label";
            err_oneproviderToken_label.Size = new Size(48, 19);
            err_oneproviderToken_label.TabIndex = 12;
            err_oneproviderToken_label.Text = "Empty";
            err_oneproviderToken_label.Visible = false;
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
            folderBrowser_button.Location = new Point(638, 15);
            folderBrowser_button.Margin = new Padding(2, 3, 2, 3);
            folderBrowser_button.Name = "folderBrowser_button";
            folderBrowser_button.Size = new Size(31, 24);
            folderBrowser_button.TabIndex = 14;
            folderBrowser_button.Text = "...";
            folderBrowser_button.UseVisualStyleBackColor = true;
            folderBrowser_button.Click += folderBrowser_button_Click;
            // 
            // deleteRoot_checkBox
            // 
            deleteRoot_checkBox.AutoSize = true;
            deleteRoot_checkBox.Checked = true;
            deleteRoot_checkBox.CheckState = CheckState.Checked;
            deleteRoot_checkBox.Location = new Point(161, 80);
            deleteRoot_checkBox.Margin = new Padding(2, 3, 2, 3);
            deleteRoot_checkBox.Name = "deleteRoot_checkBox";
            deleteRoot_checkBox.Size = new Size(186, 23);
            deleteRoot_checkBox.TabIndex = 15;
            deleteRoot_checkBox.Text = "delete existing root folder";
            deleteRoot_checkBox.UseVisualStyleBackColor = true;
            // 
            // connect_button
            // 
            connect_button.BackColor = Color.FromArgb(236, 60, 60);
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
            disconect_button.BackColor = Color.White;
            disconect_button.Location = new Point(391, 29);
            disconect_button.Margin = new Padding(2, 3, 2, 3);
            disconect_button.Name = "disconect_button";
            disconect_button.Size = new Size(118, 49);
            disconect_button.TabIndex = 17;
            disconect_button.Text = "Disconect";
            disconect_button.UseVisualStyleBackColor = false;
            disconect_button.Click += disconect_button_Click;
            // 
            // loadFromFile_button
            // 
            loadFromFile_button.BackColor = SystemColors.Window;
            loadFromFile_button.Location = new Point(159, 119);
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
            form_panel.Controls.Add(advanced_button);
            form_panel.Controls.Add(onezone_textBox);
            form_panel.Controls.Add(onezone_label);
            form_panel.Controls.Add(err_onezone_label);
            form_panel.Controls.Add(oneproviderToken_label);
            form_panel.Controls.Add(err_oneproviderToken_label);
            form_panel.Controls.Add(oneproviderToken_textBox);
            form_panel.Dock = DockStyle.Top;
            form_panel.Location = new Point(0, 119);
            form_panel.Name = "form_panel";
            form_panel.Size = new Size(767, 181);
            form_panel.TabIndex = 20;
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
            rootFolderErase_button.Location = new Point(673, 15);
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
            advanced_panel.BackColor = SystemColors.Control;
            advanced_panel.BorderStyle = BorderStyle.FixedSingle;
            advanced_panel.Controls.Add(rootFolderErase_button);
            advanced_panel.Controls.Add(loadFromFile_button);
            advanced_panel.Controls.Add(deleteRoot_checkBox);
            advanced_panel.Controls.Add(rootFolder_label);
            advanced_panel.Controls.Add(err_rootFolder_label);
            advanced_panel.Controls.Add(rootFolder_textBox);
            advanced_panel.Controls.Add(folderBrowser_button);
            advanced_panel.Dock = DockStyle.Top;
            advanced_panel.Location = new Point(0, 300);
            advanced_panel.Name = "advanced_panel";
            advanced_panel.Size = new Size(767, 163);
            advanced_panel.TabIndex = 21;
            advanced_panel.Visible = false;
            // 
            // controls_panel
            // 
            controls_panel.Controls.Add(connect_button);
            controls_panel.Controls.Add(disconect_button);
            controls_panel.Dock = DockStyle.Top;
            controls_panel.Location = new Point(0, 463);
            controls_panel.Name = "controls_panel";
            controls_panel.Size = new Size(767, 97);
            controls_panel.TabIndex = 22;
            // 
            // statusStrip
            // 
            statusStrip.Items.AddRange(new ToolStripItem[] { statusImageRed, statusImageGreen, statusLabel, statusMessage });
            statusStrip.Location = new Point(0, 489);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(784, 22);
            statusStrip.TabIndex = 23;
            statusStrip.Text = "statusStrip1";
            // 
            // statusImageRed
            // 
            statusImageRed.Image = (Image)resources.GetObject("statusImageRed.Image");
            statusImageRed.Name = "statusImageRed";
            statusImageRed.Size = new Size(16, 17);
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
            scrollPanel.Size = new Size(784, 489);
            scrollPanel.TabIndex = 24;
            // 
            // ConnectForm
            // 
            AutoScaleDimensions = new SizeF(8F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoScroll = true;
            ClientSize = new Size(784, 511);
            Controls.Add(scrollPanel);
            Controls.Add(statusStrip);
            Font = new Font("Microsoft YaHei", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(2, 3, 2, 3);
            MinimumSize = new Size(800, 550);
            Name = "ConnectForm";
            Text = "Onedata Drive";
            FormClosing += ConnectForm_Closing;
            FormClosed += ConnectForm_FormClosed;
            header_panel.ResumeLayout(false);
            header_panel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)logoPictureBox).EndInit();
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
        private PictureBox logoPictureBox;
        private Label headingLabel;
        private Label onezone_label;
        private TextBox onezone_textBox;
        private Label err_onezone_label;
        private Label err_rootFolder_label;
        private TextBox rootFolder_textBox;
        private Label rootFolder_label;
        private Label err_oneproviderToken_label;
        private TextBox oneproviderToken_textBox;
        private Label oneproviderToken_label;
        private Button folderBrowser_button;
        private FolderBrowserDialog rootFolder_folderBrowserDialog;
        private CheckBox deleteRoot_checkBox;
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
    }
}
