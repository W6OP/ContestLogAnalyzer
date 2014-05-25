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
            this.ButtonLoadLogs = new System.Windows.Forms.Button();
            this.ListViewLoad = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ComboBoxSelectContest = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.BackgroundWorkerLoadLogs = new System.ComponentModel.BackgroundWorker();
            this.ButtonStartAnalysis = new System.Windows.Forms.Button();
            this.ButtonScoreLogs = new System.Windows.Forms.Button();
            this.BackgroundWorkerScoreLogs = new System.ComponentModel.BackgroundWorker();
            this.BackgroundWorkerAnalzeLogs = new System.ComponentModel.BackgroundWorker();
            this.ListViewAnalysis = new System.Windows.Forms.ListView();
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ListViewScore = new System.Windows.Forms.ListView();
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ProgressBarLoad = new System.Windows.Forms.ProgressBar();
            this.LabelProgress = new System.Windows.Forms.Label();
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
            this.label1.Location = new System.Drawing.Point(21, 68);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(97, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Select Log Folder";
            // 
            // TextBoxLogFolder
            // 
            this.TextBoxLogFolder.Location = new System.Drawing.Point(24, 86);
            this.TextBoxLogFolder.Name = "TextBoxLogFolder";
            this.TextBoxLogFolder.Size = new System.Drawing.Size(394, 23);
            this.TextBoxLogFolder.TabIndex = 1;
            this.TextBoxLogFolder.Text = "C:\\Users\\pbourget\\Documents\\2013_Session1\\Clean";
            // 
            // ButtonSelectFolder
            // 
            this.ButtonSelectFolder.Location = new System.Drawing.Point(424, 86);
            this.ButtonSelectFolder.Name = "ButtonSelectFolder";
            this.ButtonSelectFolder.Size = new System.Drawing.Size(31, 23);
            this.ButtonSelectFolder.TabIndex = 2;
            this.ButtonSelectFolder.Text = ". . .";
            this.ButtonSelectFolder.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.ButtonSelectFolder.UseVisualStyleBackColor = true;
            this.ButtonSelectFolder.Click += new System.EventHandler(this.ButtonSelectFolder_Click);
            // 
            // ButtonLoadLogs
            // 
            this.ButtonLoadLogs.Location = new System.Drawing.Point(24, 124);
            this.ButtonLoadLogs.Name = "ButtonLoadLogs";
            this.ButtonLoadLogs.Size = new System.Drawing.Size(119, 23);
            this.ButtonLoadLogs.TabIndex = 3;
            this.ButtonLoadLogs.Text = "Load Contest Logs";
            this.ButtonLoadLogs.UseVisualStyleBackColor = true;
            this.ButtonLoadLogs.Click += new System.EventHandler(this.ButtonLoadLogs_Click);
            // 
            // ListViewLoad
            // 
            this.ListViewLoad.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.ListViewLoad.BackColor = System.Drawing.SystemColors.Info;
            this.ListViewLoad.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader7});
            this.ListViewLoad.GridLines = true;
            this.ListViewLoad.Location = new System.Drawing.Point(24, 163);
            this.ListViewLoad.Name = "ListViewLoad";
            this.ListViewLoad.Size = new System.Drawing.Size(425, 554);
            this.ListViewLoad.TabIndex = 5;
            this.ListViewLoad.UseCompatibleStateImageBehavior = false;
            this.ListViewLoad.View = System.Windows.Forms.View.Details;
            this.ListViewLoad.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.ListViewLoad_MouseDoubleClick);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Processed";
            this.columnHeader1.Width = 140;
            // 
            // columnHeader7
            // 
            this.columnHeader7.Text = "Status";
            this.columnHeader7.Width = 380;
            // 
            // ComboBoxSelectContest
            // 
            this.ComboBoxSelectContest.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ComboBoxSelectContest.FormattingEnabled = true;
            this.ComboBoxSelectContest.Location = new System.Drawing.Point(24, 36);
            this.ComboBoxSelectContest.Name = "ComboBoxSelectContest";
            this.ComboBoxSelectContest.Size = new System.Drawing.Size(131, 23);
            this.ComboBoxSelectContest.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(21, 18);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(82, 15);
            this.label3.TabIndex = 8;
            this.label3.Text = "Select Contest";
            // 
            // BackgroundWorkerLoadLogs
            // 
            this.BackgroundWorkerLoadLogs.WorkerSupportsCancellation = true;
            this.BackgroundWorkerLoadLogs.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BackgroundWorkerLoadLogs_DoWork);
            this.BackgroundWorkerLoadLogs.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.BackgroundWorkerLoadLogs_RunWorkerCompleted);
            // 
            // ButtonStartAnalysis
            // 
            this.ButtonStartAnalysis.Enabled = false;
            this.ButtonStartAnalysis.Location = new System.Drawing.Point(162, 124);
            this.ButtonStartAnalysis.Name = "ButtonStartAnalysis";
            this.ButtonStartAnalysis.Size = new System.Drawing.Size(119, 23);
            this.ButtonStartAnalysis.TabIndex = 9;
            this.ButtonStartAnalysis.Text = "Start Log Analysis";
            this.ButtonStartAnalysis.UseVisualStyleBackColor = true;
            this.ButtonStartAnalysis.Click += new System.EventHandler(this.ButtonStartAnalysis_Click);
            // 
            // ButtonScoreLogs
            // 
            this.ButtonScoreLogs.Enabled = false;
            this.ButtonScoreLogs.Location = new System.Drawing.Point(299, 124);
            this.ButtonScoreLogs.Name = "ButtonScoreLogs";
            this.ButtonScoreLogs.Size = new System.Drawing.Size(119, 23);
            this.ButtonScoreLogs.TabIndex = 10;
            this.ButtonScoreLogs.Text = "Score Contest Logs";
            this.ButtonScoreLogs.UseVisualStyleBackColor = true;
            this.ButtonScoreLogs.Click += new System.EventHandler(this.ButtonScoreLogs_Click);
            // 
            // BackgroundWorkerScoreLogs
            // 
            this.BackgroundWorkerScoreLogs.WorkerSupportsCancellation = true;
            this.BackgroundWorkerScoreLogs.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BackgroundWorkerScoreLogs_DoWork);
            this.BackgroundWorkerScoreLogs.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.BackgroundWorkerScoreLogs_RunWorkerCompleted);
            // 
            // BackgroundWorkerAnalzeLogs
            // 
            this.BackgroundWorkerAnalzeLogs.WorkerSupportsCancellation = true;
            this.BackgroundWorkerAnalzeLogs.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BackgroundWorkerAnalyzeLogs_DoWork);
            this.BackgroundWorkerAnalzeLogs.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.BackgroundWorkerAnalyzeLogs_RunWorkerCompleted);
            // 
            // ListViewAnalysis
            // 
            this.ListViewAnalysis.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.ListViewAnalysis.BackColor = System.Drawing.SystemColors.Info;
            this.ListViewAnalysis.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader2,
            this.columnHeader6});
            this.ListViewAnalysis.GridLines = true;
            this.ListViewAnalysis.Location = new System.Drawing.Point(455, 163);
            this.ListViewAnalysis.Name = "ListViewAnalysis";
            this.ListViewAnalysis.Size = new System.Drawing.Size(257, 554);
            this.ListViewAnalysis.TabIndex = 11;
            this.ListViewAnalysis.UseCompatibleStateImageBehavior = false;
            this.ListViewAnalysis.View = System.Windows.Forms.View.Details;
            this.ListViewAnalysis.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.ListViewAnalysis_MouseDoubleClick);
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Processed";
            this.columnHeader2.Width = 140;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "QSO Count";
            this.columnHeader6.Width = 78;
            // 
            // ListViewScore
            // 
            this.ListViewScore.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.ListViewScore.BackColor = System.Drawing.SystemColors.Info;
            this.ListViewScore.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5});
            this.ListViewScore.GridLines = true;
            this.ListViewScore.Location = new System.Drawing.Point(718, 163);
            this.ListViewScore.MultiSelect = false;
            this.ListViewScore.Name = "ListViewScore";
            this.ListViewScore.Size = new System.Drawing.Size(278, 554);
            this.ListViewScore.TabIndex = 12;
            this.ListViewScore.UseCompatibleStateImageBehavior = false;
            this.ListViewScore.View = System.Windows.Forms.View.Details;
            this.ListViewScore.DoubleClick += new System.EventHandler(this.ListViewScore_DoubleClick);
            this.ListViewScore.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.ListViewScore_MouseDoubleClick);
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Processed";
            this.columnHeader3.Width = 140;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Claimed";
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "Actual";
            // 
            // ProgressBarLoad
            // 
            this.ProgressBarLoad.Location = new System.Drawing.Point(424, 124);
            this.ProgressBarLoad.Name = "ProgressBarLoad";
            this.ProgressBarLoad.Size = new System.Drawing.Size(428, 23);
            this.ProgressBarLoad.Step = 1;
            this.ProgressBarLoad.TabIndex = 13;
            // 
            // LabelProgress
            // 
            this.LabelProgress.AutoSize = true;
            this.LabelProgress.Location = new System.Drawing.Point(493, 105);
            this.LabelProgress.Name = "LabelProgress";
            this.LabelProgress.Size = new System.Drawing.Size(38, 15);
            this.LabelProgress.TabIndex = 14;
            this.LabelProgress.Text = "label2";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Honeydew;
            this.ClientSize = new System.Drawing.Size(1008, 729);
            this.Controls.Add(this.LabelProgress);
            this.Controls.Add(this.ProgressBarLoad);
            this.Controls.Add(this.ListViewScore);
            this.Controls.Add(this.ListViewAnalysis);
            this.Controls.Add(this.ButtonScoreLogs);
            this.Controls.Add(this.ButtonStartAnalysis);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.ComboBoxSelectContest);
            this.Controls.Add(this.ListViewLoad);
            this.Controls.Add(this.ButtonLoadLogs);
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
        private System.Windows.Forms.Button ButtonLoadLogs;
        private System.Windows.Forms.ListView ListViewLoad;
        private System.Windows.Forms.ComboBox ComboBoxSelectContest;
        private System.Windows.Forms.Label label3;
        private System.ComponentModel.BackgroundWorker BackgroundWorkerLoadLogs;
        private System.Windows.Forms.Button ButtonStartAnalysis;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.Button ButtonScoreLogs;
        private System.ComponentModel.BackgroundWorker BackgroundWorkerScoreLogs;
        private System.ComponentModel.BackgroundWorker BackgroundWorkerAnalzeLogs;
        private System.Windows.Forms.ListView ListViewAnalysis;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ListView ListViewScore;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.ProgressBar ProgressBarLoad;
        private System.Windows.Forms.Label LabelProgress;
        private System.Windows.Forms.ColumnHeader columnHeader7;
    }
}

