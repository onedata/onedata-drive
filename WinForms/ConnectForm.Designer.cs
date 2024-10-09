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
            label1 = new Label();
            headerPanel = new Panel();
            headingLabel = new Label();
            logoPictureBox = new PictureBox();
            zoneHost_label = new Label();
            zoneHost_textBox = new TextBox();
            err_zoneHost_label = new Label();
            err_rootPath_label = new Label();
            rootPath_textBox = new TextBox();
            rootPath_label = new Label();
            err_providerToken_label = new Label();
            providerToken_textBox = new TextBox();
            providerToken_label = new Label();
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
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(97, 529);
            label1.Margin = new Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new Size(57, 19);
            label1.TabIndex = 2;
            label1.Text = "nothing";
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
            // zoneHost_label
            // 
            zoneHost_label.AutoSize = true;
            zoneHost_label.Location = new Point(31, 157);
            zoneHost_label.Margin = new Padding(2, 0, 2, 0);
            zoneHost_label.Name = "zoneHost_label";
            zoneHost_label.Size = new Size(73, 19);
            zoneHost_label.TabIndex = 4;
            zoneHost_label.Text = "Zone Host";
            // 
            // zoneHost_textBox
            // 
            zoneHost_textBox.Location = new Point(146, 153);
            zoneHost_textBox.Margin = new Padding(2, 3, 2, 3);
            zoneHost_textBox.Name = "zoneHost_textBox";
            zoneHost_textBox.Size = new Size(474, 25);
            zoneHost_textBox.TabIndex = 5;
            // 
            // err_zoneHost_label
            // 
            err_zoneHost_label.AutoSize = true;
            err_zoneHost_label.ForeColor = Color.FromArgb(192, 0, 0);
            err_zoneHost_label.Location = new Point(146, 185);
            err_zoneHost_label.Margin = new Padding(2, 0, 2, 0);
            err_zoneHost_label.Name = "err_zoneHost_label";
            err_zoneHost_label.Size = new Size(45, 19);
            err_zoneHost_label.TabIndex = 6;
            err_zoneHost_label.Text = "label3";
            err_zoneHost_label.Visible = false;
            // 
            // err_rootPath_label
            // 
            err_rootPath_label.AutoSize = true;
            err_rootPath_label.ForeColor = Color.FromArgb(192, 0, 0);
            err_rootPath_label.Location = new Point(146, 318);
            err_rootPath_label.Margin = new Padding(2, 0, 2, 0);
            err_rootPath_label.Name = "err_rootPath_label";
            err_rootPath_label.Size = new Size(45, 19);
            err_rootPath_label.TabIndex = 9;
            err_rootPath_label.Text = "label4";
            err_rootPath_label.Visible = false;
            // 
            // rootPath_textBox
            // 
            rootPath_textBox.Location = new Point(146, 289);
            rootPath_textBox.Margin = new Padding(2, 3, 2, 3);
            rootPath_textBox.Name = "rootPath_textBox";
            rootPath_textBox.Size = new Size(474, 25);
            rootPath_textBox.TabIndex = 8;
            // 
            // rootPath_label
            // 
            rootPath_label.AutoSize = true;
            rootPath_label.Location = new Point(31, 290);
            rootPath_label.Margin = new Padding(2, 0, 2, 0);
            rootPath_label.Name = "rootPath_label";
            rootPath_label.Size = new Size(70, 19);
            rootPath_label.TabIndex = 7;
            rootPath_label.Text = "Root Path";
            // 
            // err_providerToken_label
            // 
            err_providerToken_label.AutoSize = true;
            err_providerToken_label.ForeColor = Color.FromArgb(192, 0, 0);
            err_providerToken_label.Location = new Point(146, 252);
            err_providerToken_label.Margin = new Padding(2, 0, 2, 0);
            err_providerToken_label.Name = "err_providerToken_label";
            err_providerToken_label.Size = new Size(45, 19);
            err_providerToken_label.TabIndex = 12;
            err_providerToken_label.Text = "label6";
            err_providerToken_label.Visible = false;
            // 
            // providerToken_textBox
            // 
            providerToken_textBox.Location = new Point(146, 222);
            providerToken_textBox.Margin = new Padding(2, 3, 2, 3);
            providerToken_textBox.Name = "providerToken_textBox";
            providerToken_textBox.Size = new Size(474, 25);
            providerToken_textBox.TabIndex = 11;
            // 
            // providerToken_label
            // 
            providerToken_label.AutoSize = true;
            providerToken_label.Location = new Point(31, 223);
            providerToken_label.Margin = new Padding(2, 0, 2, 0);
            providerToken_label.Name = "providerToken_label";
            providerToken_label.Size = new Size(101, 19);
            providerToken_label.TabIndex = 10;
            providerToken_label.Text = "Provider Token";
            // 
            // folderBrowser_button
            // 
            folderBrowser_button.Location = new Point(626, 290);
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
            deleteRoot_checkBox.Location = new Point(146, 357);
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
            ClientSize = new Size(784, 557);
            Controls.Add(loadFromFile_button);
            Controls.Add(status_label);
            Controls.Add(disconect_button);
            Controls.Add(connect_button);
            Controls.Add(deleteRoot_checkBox);
            Controls.Add(folderBrowser_button);
            Controls.Add(err_providerToken_label);
            Controls.Add(providerToken_textBox);
            Controls.Add(providerToken_label);
            Controls.Add(err_rootPath_label);
            Controls.Add(rootPath_textBox);
            Controls.Add(rootPath_label);
            Controls.Add(err_zoneHost_label);
            Controls.Add(zoneHost_textBox);
            Controls.Add(zoneHost_label);
            Controls.Add(headerPanel);
            Controls.Add(label1);
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
        private Label label1;
        private Panel headerPanel;
        private PictureBox logoPictureBox;
        private Label headingLabel;
        private Label zoneHost_label;
        private TextBox zoneHost_textBox;
        private Label err_zoneHost_label;
        private Label err_rootPath_label;
        private TextBox rootPath_textBox;
        private Label rootPath_label;
        private Label err_providerToken_label;
        private TextBox providerToken_textBox;
        private Label providerToken_label;
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
