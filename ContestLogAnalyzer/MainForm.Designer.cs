namespace ContestLogAnalyzer
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.LogFolderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.label1 = new System.Windows.Forms.Label();
            this.TextBoxLogFolder = new System.Windows.Forms.TextBox();
            this.ButtonSelectFolder = new System.Windows.Forms.Button();
            this.ButtonAnalyzeLog = new System.Windows.Forms.Button();
            this.ButtonValidateHeader = new System.Windows.Forms.Button();
            this.listView1 = new System.Windows.Forms.ListView();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.BackgroundWorkerAnalyze = new System.ComponentModel.BackgroundWorker();
            this.button1 = new System.Windows.Forms.Button();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.SuspendLayout();
            // 
            // LogFolderBrowserDialog
            // 
            this.LogFolderBrowserDialog.RootFolder = System.Environment.SpecialFolder.MyDocuments;
            this.LogFolderBrowserDialog.ShowNewFolderButton = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 76);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(97, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Select Log Folder";
            // 
            // TextBoxLogFolder
            // 
            this.TextBoxLogFolder.Location = new System.Drawing.Point(12, 94);
            this.TextBoxLogFolder.Name = "TextBoxLogFolder";
            this.TextBoxLogFolder.Size = new System.Drawing.Size(394, 23);
            this.TextBoxLogFolder.TabIndex = 1;
            this.TextBoxLogFolder.Text = "C:\\Users\\pbourget\\Documents\\2013_Session1\\Clean";
            // 
            // ButtonSelectFolder
            // 
            this.ButtonSelectFolder.Location = new System.Drawing.Point(412, 93);
            this.ButtonSelectFolder.Name = "ButtonSelectFolder";
            this.ButtonSelectFolder.Size = new System.Drawing.Size(31, 23);
            this.ButtonSelectFolder.TabIndex = 2;
            this.ButtonSelectFolder.Text = ". . .";
            this.ButtonSelectFolder.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.ButtonSelectFolder.UseVisualStyleBackColor = true;
            this.ButtonSelectFolder.Click += new System.EventHandler(this.ButtonSelectFolder_Click);
            // 
            // ButtonAnalyzeLog
            // 
            this.ButtonAnalyzeLog.Location = new System.Drawing.Point(369, 305);
            this.ButtonAnalyzeLog.Name = "ButtonAnalyzeLog";
            this.ButtonAnalyzeLog.Size = new System.Drawing.Size(119, 23);
            this.ButtonAnalyzeLog.TabIndex = 3;
            this.ButtonAnalyzeLog.Text = "Start Log Analysis";
            this.ButtonAnalyzeLog.UseVisualStyleBackColor = true;
            this.ButtonAnalyzeLog.Click += new System.EventHandler(this.ButtonAnalyzeLog_Click);
            // 
            // ButtonValidateHeader
            // 
            this.ButtonValidateHeader.Location = new System.Drawing.Point(24, 305);
            this.ButtonValidateHeader.Name = "ButtonValidateHeader";
            this.ButtonValidateHeader.Size = new System.Drawing.Size(119, 23);
            this.ButtonValidateHeader.TabIndex = 4;
            this.ButtonValidateHeader.Text = "Validate Headers";
            this.ButtonValidateHeader.UseVisualStyleBackColor = true;
            this.ButtonValidateHeader.Click += new System.EventHandler(this.ButtonValidateHeader_Click);
            // 
            // listView1
            // 
            this.listView1.BackColor = System.Drawing.SystemColors.Info;
            this.listView1.CheckBoxes = true;
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.listView1.Location = new System.Drawing.Point(516, 47);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(415, 282);
            this.listView1.TabIndex = 5;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(513, 28);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(270, 15);
            this.label2.TabIndex = 6;
            this.label2.Text = "List log and status and score as we process them ?";
            // 
            // comboBox1
            // 
            this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(12, 47);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(255, 23);
            this.comboBox1.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 28);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(82, 15);
            this.label3.TabIndex = 8;
            this.label3.Text = "Select Contest";
            // 
            // BackgroundWorkerAnalyze
            // 
            this.BackgroundWorkerAnalyze.WorkerReportsProgress = true;
            this.BackgroundWorkerAnalyze.WorkerSupportsCancellation = true;
            this.BackgroundWorkerAnalyze.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BackgroundWorkerAnalyze_DoWork);
            this.BackgroundWorkerAnalyze.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.BackgroundWorkerAnalyze_ProgressChanged);
            this.BackgroundWorkerAnalyze.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.BackgroundWorkerAnalyze_RunWorkerCompleted);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(24, 276);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(119, 23);
            this.button1.TabIndex = 9;
            this.button1.Text = "Check First Log";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Processed";
            this.columnHeader1.Width = 200;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Honeydew;
            this.ClientSize = new System.Drawing.Size(943, 348);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.ButtonValidateHeader);
            this.Controls.Add(this.ButtonAnalyzeLog);
            this.Controls.Add(this.ButtonSelectFolder);
            this.Controls.Add(this.TextBoxLogFolder);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "W6OP Contest Log Analyzer";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.FolderBrowserDialog LogFolderBrowserDialog;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox TextBoxLogFolder;
        private System.Windows.Forms.Button ButtonSelectFolder;
        private System.Windows.Forms.Button ButtonAnalyzeLog;
        private System.Windows.Forms.Button ButtonValidateHeader;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Label label3;
        private System.ComponentModel.BackgroundWorker BackgroundWorkerAnalyze;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ColumnHeader columnHeader1;
    }
}

