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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConnectForm));
            statusValue_label = new Label();
            headerPanel = new Panel();
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
            rootPath_folderBrowserDialog = new FolderBrowserDialog();
            deleteRoot_checkBox = new CheckBox();
            connect_button = new Button();
            disconect_button = new Button();
            status_label = new Label();
            loadFromFile_button = new Button();
            config_openFileDialog = new OpenFileDialog();
            headerPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)logoPictureBox).BeginInit();
            SuspendLayout();
            // 
            // statusValue_label
            // 
            statusValue_label.AutoSize = true;
            statusValue_label.Location = new Point(97, 529);
            statusValue_label.Margin = new Padding(2, 0, 2, 0);
            statusValue_label.Name = "statusValue_label";
            statusValue_label.Size = new Size(57, 19);
            statusValue_label.TabIndex = 2;
            statusValue_label.Text = "nothing";
            // 
            // headerPanel
            // 
            headerPanel.BackColor = Color.FromArgb(64, 64, 64);
            headerPanel.Controls.Add(headingLabel);
            headerPanel.Controls.Add(logoPictureBox);
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Location = new Point(0, 0);
            headerPanel.Margin = new Padding(2, 3, 2, 3);
            headerPanel.Name = "headerPanel";
            headerPanel.Size = new Size(784, 128);
            headerPanel.TabIndex = 3;
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
            onezone_label.Location = new Point(31, 157);
            onezone_label.Margin = new Padding(2, 0, 2, 0);
            onezone_label.Name = "onezone_label";
            onezone_label.Size = new Size(64, 19);
            onezone_label.TabIndex = 4;
            onezone_label.Text = "Onezone";
            // 
            // onezone_textBox
            // 
            onezone_textBox.Location = new Point(175, 153);
            onezone_textBox.Margin = new Padding(2, 3, 2, 3);
            onezone_textBox.Name = "onezone_textBox";
            onezone_textBox.Size = new Size(474, 25);
            onezone_textBox.TabIndex = 5;
            // 
            // err_onezone_label
            // 
            err_onezone_label.AutoSize = true;
            err_onezone_label.ForeColor = Color.FromArgb(192, 0, 0);
            err_onezone_label.Location = new Point(175, 185);
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
            err_rootFolder_label.Location = new Point(175, 318);
            err_rootFolder_label.Margin = new Padding(2, 0, 2, 0);
            err_rootFolder_label.Name = "err_rootFolder_label";
            err_rootFolder_label.Size = new Size(48, 19);
            err_rootFolder_label.TabIndex = 9;
            err_rootFolder_label.Text = "Empty";
            err_rootFolder_label.Visible = false;
            // 
            // rootFolder_textBox
            // 
            rootFolder_textBox.Location = new Point(175, 289);
            rootFolder_textBox.Margin = new Padding(2, 3, 2, 3);
            rootFolder_textBox.Name = "rootFolder_textBox";
            rootFolder_textBox.ReadOnly = true;
            rootFolder_textBox.Size = new Size(474, 25);
            rootFolder_textBox.TabIndex = 8;
            // 
            // rootFolder_label
            // 
            rootFolder_label.AutoSize = true;
            rootFolder_label.Location = new Point(31, 290);
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
            err_oneproviderToken_label.Location = new Point(175, 252);
            err_oneproviderToken_label.Margin = new Padding(2, 0, 2, 0);
            err_oneproviderToken_label.Name = "err_oneproviderToken_label";
            err_oneproviderToken_label.Size = new Size(48, 19);
            err_oneproviderToken_label.TabIndex = 12;
            err_oneproviderToken_label.Text = "Empty";
            err_oneproviderToken_label.Visible = false;
            // 
            // oneproviderToken_textBox
            // 
            oneproviderToken_textBox.Location = new Point(175, 222);
            oneproviderToken_textBox.Margin = new Padding(2, 3, 2, 3);
            oneproviderToken_textBox.Name = "oneproviderToken_textBox";
            oneproviderToken_textBox.Size = new Size(474, 25);
            oneproviderToken_textBox.TabIndex = 11;
            // 
            // oneproviderToken_label
            // 
            oneproviderToken_label.AutoSize = true;
            oneproviderToken_label.Location = new Point(31, 223);
            oneproviderToken_label.Margin = new Padding(2, 0, 2, 0);
            oneproviderToken_label.Name = "oneproviderToken_label";
            oneproviderToken_label.Size = new Size(127, 19);
            oneproviderToken_label.TabIndex = 10;
            oneproviderToken_label.Text = "Oneprovider Token";
            // 
            // folderBrowser_button
            // 
            folderBrowser_button.Location = new Point(655, 290);
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
            deleteRoot_checkBox.Location = new Point(175, 357);
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
            connect_button.Location = new Point(502, 428);
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
            disconect_button.Location = new Point(365, 428);
            disconect_button.Margin = new Padding(2, 3, 2, 3);
            disconect_button.Name = "disconect_button";
            disconect_button.Size = new Size(118, 49);
            disconect_button.TabIndex = 17;
            disconect_button.Text = "Disconect";
            disconect_button.UseVisualStyleBackColor = false;
            disconect_button.Click += disconect_button_Click;
            // 
            // status_label
            // 
            status_label.AutoSize = true;
            status_label.Location = new Point(31, 529);
            status_label.Name = "status_label";
            status_label.Size = new Size(51, 19);
            status_label.TabIndex = 18;
            status_label.Text = "Status:";
            // 
            // loadFromFile_button
            // 
            loadFromFile_button.BackColor = Color.FromArgb(192, 255, 255);
            loadFromFile_button.Location = new Point(31, 428);
            loadFromFile_button.Margin = new Padding(2, 3, 2, 3);
            loadFromFile_button.Name = "loadFromFile_button";
            loadFromFile_button.Size = new Size(118, 49);
            loadFromFile_button.TabIndex = 19;
            loadFromFile_button.Text = "Load from file";
            loadFromFile_button.UseVisualStyleBackColor = false;
            loadFromFile_button.Click += loadFromFile_button_Click;
            // 
            // config_openFileDialog
            // 
            config_openFileDialog.FileName = "openFileDialog1";
            config_openFileDialog.Filter = "JSON|*.json";
            // 
            // ConnectForm
            // 
            AutoScaleDimensions = new SizeF(8F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoScroll = true;
            ClientSize = new Size(784, 557);
            Controls.Add(loadFromFile_button);
            Controls.Add(status_label);
            Controls.Add(disconect_button);
            Controls.Add(connect_button);
            Controls.Add(deleteRoot_checkBox);
            Controls.Add(folderBrowser_button);
            Controls.Add(err_oneproviderToken_label);
            Controls.Add(oneproviderToken_textBox);
            Controls.Add(oneproviderToken_label);
            Controls.Add(err_rootFolder_label);
            Controls.Add(rootFolder_textBox);
            Controls.Add(rootFolder_label);
            Controls.Add(err_onezone_label);
            Controls.Add(onezone_textBox);
            Controls.Add(onezone_label);
            Controls.Add(headerPanel);
            Controls.Add(statusValue_label);
            Font = new Font("Microsoft YaHei", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(2, 3, 2, 3);
            MinimumSize = new Size(800, 596);
            Name = "ConnectForm";
            Text = "Onedata Drive";
            FormClosed += ConnectForm_FormClosed;
            headerPanel.ResumeLayout(false);
            headerPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)logoPictureBox).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Label statusValue_label;
        private Panel headerPanel;
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
        private FolderBrowserDialog rootPath_folderBrowserDialog;
        private CheckBox deleteRoot_checkBox;
        private Button connect_button;
        private Button disconect_button;
        private Label status_label;
        private Button loadFromFile_button;
        private FolderBrowserDialog config_folderBrowserDialog;
        private OpenFileDialog config_openFileDialog;
    }
}
