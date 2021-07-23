namespace W6OP.ContestLogAnalyzer
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
            this.BackgroundWorkerAnalyzeLogs = new System.ComponentModel.BackgroundWorker();
            this.ListViewAnalysis = new System.Windows.Forms.ListView();
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader13 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ListViewScore = new System.Windows.Forms.ListView();
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader8 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader11 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader12 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader9 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader10 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ProgressBarLoad = new System.Windows.Forms.ProgressBar();
            this.LabelProgress = new System.Windows.Forms.Label();
            this.TabControlMain = new System.Windows.Forms.TabControl();
            this.TabPageLogStatus = new System.Windows.Forms.TabPage();
            this.TabPageAnalysis = new System.Windows.Forms.TabPage();
            this.TabPageScoring = new System.Windows.Forms.TabPage();
            this.TabPageCompare = new System.Windows.Forms.TabPage();
            this.ListViewCompare = new System.Windows.Forms.ListView();
            this.columnHeader15 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader16 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader17 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader18 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader19 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader20 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader21 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader22 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader23 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.TabPageSearchLogs = new System.Windows.Forms.TabPage();
            this.ListViewLogSearch = new System.Windows.Forms.ListView();
            this.columnHeader14 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader24 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader25 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader26 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader27 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader28 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ComboBoxSelectSession = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.ButtonLoadBadcalls = new System.Windows.Forms.Button();
            this.OpenCSVFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.ButtonPreScoreReports = new System.Windows.Forms.Button();
            this.ButtonCompareLogs = new System.Windows.Forms.Button();
            this.BackgroundWorkerPreAnalysis = new System.ComponentModel.BackgroundWorker();
            this.TextBoxLog1 = new System.Windows.Forms.TextBox();
            this.TextBoxLog2 = new System.Windows.Forms.TextBox();
            this.SessionDateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.buttonLogSearch = new System.Windows.Forms.Button();
            this.textBoxLogSearch = new System.Windows.Forms.TextBox();
            this.OpenHawaiiCallFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.TabControlMain.SuspendLayout();
            this.TabPageLogStatus.SuspendLayout();
            this.TabPageAnalysis.SuspendLayout();
            this.TabPageScoring.SuspendLayout();
            this.TabPageCompare.SuspendLayout();
            this.TabPageSearchLogs.SuspendLayout();
            this.SuspendLayout();
            // 
            // LogFolderBrowserDialog
            // 
            this.LogFolderBrowserDialog.RootFolder = System.Environment.SpecialFolder.MyComputer;
            this.LogFolderBrowserDialog.SelectedPath = "C:\\";
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
            this.TextBoxLogFolder.Size = new System.Drawing.Size(460, 23);
            this.TextBoxLogFolder.TabIndex = 4;
            // 
            // ButtonSelectFolder
            // 
            this.ButtonSelectFolder.Location = new System.Drawing.Point(490, 85);
            this.ButtonSelectFolder.Name = "ButtonSelectFolder";
            this.ButtonSelectFolder.Size = new System.Drawing.Size(31, 23);
            this.ButtonSelectFolder.TabIndex = 5;
            this.ButtonSelectFolder.Text = ". . .";
            this.ButtonSelectFolder.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.ButtonSelectFolder.UseVisualStyleBackColor = true;
            this.ButtonSelectFolder.Click += new System.EventHandler(this.ButtonSelectFolder_Click);
            // 
            // ButtonLoadLogs
            // 
            this.ButtonLoadLogs.Enabled = false;
            this.ButtonLoadLogs.Location = new System.Drawing.Point(24, 124);
            this.ButtonLoadLogs.Name = "ButtonLoadLogs";
            this.ButtonLoadLogs.Size = new System.Drawing.Size(131, 23);
            this.ButtonLoadLogs.TabIndex = 6;
            this.ButtonLoadLogs.Text = "Load Contest Logs";
            this.ButtonLoadLogs.UseVisualStyleBackColor = true;
            this.ButtonLoadLogs.Click += new System.EventHandler(this.ButtonLoadLogs_Click);
            // 
            // ListViewLoad
            // 
            this.ListViewLoad.BackColor = System.Drawing.SystemColors.Info;
            this.ListViewLoad.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader7});
            this.ListViewLoad.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ListViewLoad.GridLines = true;
            this.ListViewLoad.HideSelection = false;
            this.ListViewLoad.Location = new System.Drawing.Point(3, 3);
            this.ListViewLoad.Name = "ListViewLoad";
            this.ListViewLoad.Size = new System.Drawing.Size(949, 520);
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
            this.columnHeader7.Width = 803;
            // 
            // ComboBoxSelectContest
            // 
            this.ComboBoxSelectContest.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ComboBoxSelectContest.FormattingEnabled = true;
            this.ComboBoxSelectContest.Location = new System.Drawing.Point(24, 36);
            this.ComboBoxSelectContest.Name = "ComboBoxSelectContest";
            this.ComboBoxSelectContest.Size = new System.Drawing.Size(131, 23);
            this.ComboBoxSelectContest.TabIndex = 1;
            this.ComboBoxSelectContest.SelectedIndexChanged += new System.EventHandler(this.ComboBoxSelectContest_SelectedIndexChanged);
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
            this.ButtonStartAnalysis.TabIndex = 7;
            this.ButtonStartAnalysis.Text = "Start Log Analysis";
            this.ButtonStartAnalysis.UseVisualStyleBackColor = true;
            this.ButtonStartAnalysis.Click += new System.EventHandler(this.ButtonStartAnalysis_Click);
            // 
            // ButtonScoreLogs
            // 
            this.ButtonScoreLogs.Enabled = false;
            this.ButtonScoreLogs.Location = new System.Drawing.Point(287, 124);
            this.ButtonScoreLogs.Name = "ButtonScoreLogs";
            this.ButtonScoreLogs.Size = new System.Drawing.Size(119, 23);
            this.ButtonScoreLogs.TabIndex = 8;
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
            // BackgroundWorkerAnalyzeLogs
            // 
            this.BackgroundWorkerAnalyzeLogs.WorkerSupportsCancellation = true;
            this.BackgroundWorkerAnalyzeLogs.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BackgroundWorkerAnalyzeLogs_DoWork);
            this.BackgroundWorkerAnalyzeLogs.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.BackgroundWorkerAnalyzeLogs_RunWorkerCompleted);
            // 
            // ListViewAnalysis
            // 
            this.ListViewAnalysis.BackColor = System.Drawing.SystemColors.Info;
            this.ListViewAnalysis.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader2,
            this.columnHeader6,
            this.columnHeader13});
            this.ListViewAnalysis.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ListViewAnalysis.GridLines = true;
            this.ListViewAnalysis.HideSelection = false;
            this.ListViewAnalysis.Location = new System.Drawing.Point(3, 3);
            this.ListViewAnalysis.Name = "ListViewAnalysis";
            this.ListViewAnalysis.Size = new System.Drawing.Size(949, 520);
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
            this.columnHeader6.Text = "Total QSOs";
            this.columnHeader6.Width = 76;
            // 
            // columnHeader13
            // 
            this.columnHeader13.Text = "Valid QSOs";
            this.columnHeader13.Width = 80;
            // 
            // ListViewScore
            // 
            this.ListViewScore.BackColor = System.Drawing.SystemColors.Info;
            this.ListViewScore.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader8,
            this.columnHeader11,
            this.columnHeader12,
            this.columnHeader9,
            this.columnHeader10});
            this.ListViewScore.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ListViewScore.FullRowSelect = true;
            this.ListViewScore.GridLines = true;
            this.ListViewScore.HideSelection = false;
            this.ListViewScore.Location = new System.Drawing.Point(0, 0);
            this.ListViewScore.MultiSelect = false;
            this.ListViewScore.Name = "ListViewScore";
            this.ListViewScore.Size = new System.Drawing.Size(955, 526);
            this.ListViewScore.TabIndex = 12;
            this.ListViewScore.UseCompatibleStateImageBehavior = false;
            this.ListViewScore.View = System.Windows.Forms.View.Details;
            this.ListViewScore.DoubleClick += new System.EventHandler(this.ListViewScore_DoubleClick);
            this.ListViewScore.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.ListViewScore_MouseDoubleClick);
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Call";
            this.columnHeader3.Width = 252;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Operator";
            this.columnHeader4.Width = 70;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "Station";
            this.columnHeader5.Width = 75;
            // 
            // columnHeader8
            // 
            this.columnHeader8.Text = "Total QSOs";
            this.columnHeader8.Width = 79;
            // 
            // columnHeader11
            // 
            this.columnHeader11.Text = "ValidQSOs";
            this.columnHeader11.Width = 71;
            // 
            // columnHeader12
            // 
            this.columnHeader12.Text = "Multipliers";
            this.columnHeader12.Width = 72;
            // 
            // columnHeader9
            // 
            this.columnHeader9.Text = "Claimed Score";
            this.columnHeader9.Width = 95;
            // 
            // columnHeader10
            // 
            this.columnHeader10.Text = "Actual Score";
            this.columnHeader10.Width = 88;
            // 
            // ProgressBarLoad
            // 
            this.ProgressBarLoad.Location = new System.Drawing.Point(424, 135);
            this.ProgressBarLoad.Name = "ProgressBarLoad";
            this.ProgressBarLoad.Size = new System.Drawing.Size(556, 5);
            this.ProgressBarLoad.Step = 1;
            this.ProgressBarLoad.TabIndex = 1399;
            this.ProgressBarLoad.Visible = false;
            // 
            // LabelProgress
            // 
            this.LabelProgress.AutoSize = true;
            this.LabelProgress.Location = new System.Drawing.Point(673, 117);
            this.LabelProgress.Name = "LabelProgress";
            this.LabelProgress.Size = new System.Drawing.Size(38, 15);
            this.LabelProgress.TabIndex = 14;
            this.LabelProgress.Text = "label2";
            // 
            // TabControlMain
            // 
            this.TabControlMain.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.TabControlMain.Controls.Add(this.TabPageLogStatus);
            this.TabControlMain.Controls.Add(this.TabPageAnalysis);
            this.TabControlMain.Controls.Add(this.TabPageScoring);
            this.TabControlMain.Controls.Add(this.TabPageCompare);
            this.TabControlMain.Controls.Add(this.TabPageSearchLogs);
            this.TabControlMain.Location = new System.Drawing.Point(24, 163);
            this.TabControlMain.Name = "TabControlMain";
            this.TabControlMain.SelectedIndex = 0;
            this.TabControlMain.Size = new System.Drawing.Size(963, 554);
            this.TabControlMain.TabIndex = 15;
            // 
            // TabPageLogStatus
            // 
            this.TabPageLogStatus.Controls.Add(this.ListViewLoad);
            this.TabPageLogStatus.Location = new System.Drawing.Point(4, 24);
            this.TabPageLogStatus.Name = "TabPageLogStatus";
            this.TabPageLogStatus.Padding = new System.Windows.Forms.Padding(3);
            this.TabPageLogStatus.Size = new System.Drawing.Size(955, 526);
            this.TabPageLogStatus.TabIndex = 0;
            this.TabPageLogStatus.Text = "Log Status";
            this.TabPageLogStatus.UseVisualStyleBackColor = true;
            // 
            // TabPageAnalysis
            // 
            this.TabPageAnalysis.Controls.Add(this.ListViewAnalysis);
            this.TabPageAnalysis.Location = new System.Drawing.Point(4, 24);
            this.TabPageAnalysis.Name = "TabPageAnalysis";
            this.TabPageAnalysis.Padding = new System.Windows.Forms.Padding(3);
            this.TabPageAnalysis.Size = new System.Drawing.Size(955, 526);
            this.TabPageAnalysis.TabIndex = 1;
            this.TabPageAnalysis.Text = "Analysis";
            this.TabPageAnalysis.UseVisualStyleBackColor = true;
            // 
            // TabPageScoring
            // 
            this.TabPageScoring.Controls.Add(this.ListViewScore);
            this.TabPageScoring.Location = new System.Drawing.Point(4, 24);
            this.TabPageScoring.Name = "TabPageScoring";
            this.TabPageScoring.Size = new System.Drawing.Size(955, 526);
            this.TabPageScoring.TabIndex = 2;
            this.TabPageScoring.Text = "Scoring";
            this.TabPageScoring.UseVisualStyleBackColor = true;
            // 
            // TabPageCompare
            // 
            this.TabPageCompare.Controls.Add(this.ListViewCompare);
            this.TabPageCompare.Location = new System.Drawing.Point(4, 24);
            this.TabPageCompare.Name = "TabPageCompare";
            this.TabPageCompare.Size = new System.Drawing.Size(955, 526);
            this.TabPageCompare.TabIndex = 3;
            this.TabPageCompare.Text = "Compare";
            this.TabPageCompare.UseVisualStyleBackColor = true;
            // 
            // ListViewCompare
            // 
            this.ListViewCompare.BackColor = System.Drawing.SystemColors.Info;
            this.ListViewCompare.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader15,
            this.columnHeader16,
            this.columnHeader17,
            this.columnHeader18,
            this.columnHeader19,
            this.columnHeader20,
            this.columnHeader21,
            this.columnHeader22,
            this.columnHeader23});
            this.ListViewCompare.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ListViewCompare.GridLines = true;
            this.ListViewCompare.HideSelection = false;
            this.ListViewCompare.Location = new System.Drawing.Point(0, 0);
            this.ListViewCompare.Name = "ListViewCompare";
            this.ListViewCompare.Size = new System.Drawing.Size(955, 526);
            this.ListViewCompare.TabIndex = 0;
            this.ListViewCompare.UseCompatibleStateImageBehavior = false;
            this.ListViewCompare.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader15
            // 
            this.columnHeader15.Text = "Band";
            // 
            // columnHeader16
            // 
            this.columnHeader16.Text = "Mode";
            // 
            // columnHeader17
            // 
            this.columnHeader17.Text = "Date";
            this.columnHeader17.Width = 100;
            // 
            // columnHeader18
            // 
            this.columnHeader18.Text = "Time";
            // 
            // columnHeader19
            // 
            this.columnHeader19.Text = "Exchange";
            this.columnHeader19.Width = 91;
            // 
            // columnHeader20
            // 
            this.columnHeader20.Text = "Sent";
            // 
            // columnHeader21
            // 
            this.columnHeader21.Text = "Contact";
            this.columnHeader21.Width = 89;
            // 
            // columnHeader22
            // 
            this.columnHeader22.Text = "Exchange";
            this.columnHeader22.Width = 97;
            // 
            // columnHeader23
            // 
            this.columnHeader23.Text = "Recv\'d";
            // 
            // TabPageSearchLogs
            // 
            this.TabPageSearchLogs.Controls.Add(this.ListViewLogSearch);
            this.TabPageSearchLogs.Location = new System.Drawing.Point(4, 24);
            this.TabPageSearchLogs.Name = "TabPageSearchLogs";
            this.TabPageSearchLogs.Size = new System.Drawing.Size(955, 526);
            this.TabPageSearchLogs.TabIndex = 6;
            this.TabPageSearchLogs.Text = "Log Search";
            this.TabPageSearchLogs.UseVisualStyleBackColor = true;
            // 
            // ListViewLogSearch
            // 
            this.ListViewLogSearch.BackColor = System.Drawing.SystemColors.Info;
            this.ListViewLogSearch.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader14,
            this.columnHeader24,
            this.columnHeader25,
            this.columnHeader26,
            this.columnHeader27,
            this.columnHeader28});
            this.ListViewLogSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ListViewLogSearch.GridLines = true;
            this.ListViewLogSearch.HideSelection = false;
            this.ListViewLogSearch.Location = new System.Drawing.Point(0, 0);
            this.ListViewLogSearch.Name = "ListViewLogSearch";
            this.ListViewLogSearch.Size = new System.Drawing.Size(955, 526);
            this.ListViewLogSearch.TabIndex = 0;
            this.ListViewLogSearch.UseCompatibleStateImageBehavior = false;
            this.ListViewLogSearch.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader14
            // 
            this.columnHeader14.Text = "Operator";
            this.columnHeader14.Width = 100;
            // 
            // columnHeader24
            // 
            this.columnHeader24.Text = "Contact";
            this.columnHeader24.Width = 100;
            // 
            // columnHeader25
            // 
            this.columnHeader25.Text = "Band";
            this.columnHeader25.Width = 100;
            // 
            // columnHeader26
            // 
            this.columnHeader26.Text = "Mode";
            this.columnHeader26.Width = 100;
            // 
            // columnHeader27
            // 
            this.columnHeader27.Text = "Time";
            this.columnHeader27.Width = 150;
            // 
            // columnHeader28
            // 
            this.columnHeader28.Text = "Raw QSO";
            this.columnHeader28.Width = 400;
            // 
            // ComboBoxSelectSession
            // 
            this.ComboBoxSelectSession.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ComboBoxSelectSession.Enabled = false;
            this.ComboBoxSelectSession.FormattingEnabled = true;
            this.ComboBoxSelectSession.Items.AddRange(new object[] {
            "Session 1",
            "Session 2",
            "Session 3"});
            this.ComboBoxSelectSession.Location = new System.Drawing.Point(162, 36);
            this.ComboBoxSelectSession.Name = "ComboBoxSelectSession";
            this.ComboBoxSelectSession.Size = new System.Drawing.Size(90, 23);
            this.ComboBoxSelectSession.TabIndex = 2;
            this.ComboBoxSelectSession.SelectedIndexChanged += new System.EventHandler(this.ComboBoxSelectSession_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(159, 18);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(80, 15);
            this.label2.TabIndex = 17;
            this.label2.Text = "Select Session";
            // 
            // ButtonLoadBadcalls
            // 
            this.ButtonLoadBadcalls.Location = new System.Drawing.Point(527, 35);
            this.ButtonLoadBadcalls.Name = "ButtonLoadBadcalls";
            this.ButtonLoadBadcalls.Size = new System.Drawing.Size(119, 23);
            this.ButtonLoadBadcalls.TabIndex = 18;
            this.ButtonLoadBadcalls.Text = "Load Bad Call List";
            this.ButtonLoadBadcalls.UseVisualStyleBackColor = true;
            this.ButtonLoadBadcalls.Click += new System.EventHandler(this.ButtonLoadBadcalls_Click);
            // 
            // OpenCSVFileDialog
            // 
            this.OpenCSVFileDialog.FileName = "BadGoodCalls.csv";
            this.OpenCSVFileDialog.Filter = "CSV File|*.csv";
            this.OpenCSVFileDialog.SupportMultiDottedExtensions = true;
            // 
            // ButtonPreScoreReports
            // 
            this.ButtonPreScoreReports.Enabled = false;
            this.ButtonPreScoreReports.Location = new System.Drawing.Point(361, 35);
            this.ButtonPreScoreReports.Name = "ButtonPreScoreReports";
            this.ButtonPreScoreReports.Size = new System.Drawing.Size(160, 23);
            this.ButtonPreScoreReports.TabIndex = 3;
            this.ButtonPreScoreReports.Text = "Create Pre Analysis Report";
            this.ButtonPreScoreReports.UseVisualStyleBackColor = true;
            this.ButtonPreScoreReports.Click += new System.EventHandler(this.ButtonPreScoreReports_Click);
            // 
            // ButtonCompareLogs
            // 
            this.ButtonCompareLogs.Location = new System.Drawing.Point(694, 35);
            this.ButtonCompareLogs.Name = "ButtonCompareLogs";
            this.ButtonCompareLogs.Size = new System.Drawing.Size(94, 23);
            this.ButtonCompareLogs.TabIndex = 11;
            this.ButtonCompareLogs.Text = "Compare Logs";
            this.ButtonCompareLogs.UseVisualStyleBackColor = true;
            this.ButtonCompareLogs.Click += new System.EventHandler(this.ButtonCompareLogs_Click);
            // 
            // BackgroundWorkerPreAnalysis
            // 
            this.BackgroundWorkerPreAnalysis.WorkerReportsProgress = true;
            this.BackgroundWorkerPreAnalysis.WorkerSupportsCancellation = true;
            this.BackgroundWorkerPreAnalysis.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BackgroundWorkerPreAnalysis_DoWork);
            this.BackgroundWorkerPreAnalysis.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.BackgroundWorkerPreAnalysis_ProgressChanged);
            this.BackgroundWorkerPreAnalysis.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.BackgroundWorkerPreAnalysis_RunWorkerCompleted);
            // 
            // TextBoxLog1
            // 
            this.TextBoxLog1.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.TextBoxLog1.Location = new System.Drawing.Point(794, 35);
            this.TextBoxLog1.MaxLength = 15;
            this.TextBoxLog1.Name = "TextBoxLog1";
            this.TextBoxLog1.Size = new System.Drawing.Size(90, 23);
            this.TextBoxLog1.TabIndex = 9;
            // 
            // TextBoxLog2
            // 
            this.TextBoxLog2.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.TextBoxLog2.Location = new System.Drawing.Point(890, 35);
            this.TextBoxLog2.MaxLength = 15;
            this.TextBoxLog2.Name = "TextBoxLog2";
            this.TextBoxLog2.Size = new System.Drawing.Size(90, 23);
            this.TextBoxLog2.TabIndex = 10;
            // 
            // SessionDateTimePicker
            // 
            this.SessionDateTimePicker.CustomFormat = "yyyy";
            this.SessionDateTimePicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.SessionDateTimePicker.Location = new System.Drawing.Point(258, 35);
            this.SessionDateTimePicker.Name = "SessionDateTimePicker";
            this.SessionDateTimePicker.ShowUpDown = true;
            this.SessionDateTimePicker.Size = new System.Drawing.Size(57, 23);
            this.SessionDateTimePicker.TabIndex = 1400;
            // 
            // buttonLogSearch
            // 
            this.buttonLogSearch.Enabled = false;
            this.buttonLogSearch.Location = new System.Drawing.Point(694, 68);
            this.buttonLogSearch.Name = "buttonLogSearch";
            this.buttonLogSearch.Size = new System.Drawing.Size(94, 23);
            this.buttonLogSearch.TabIndex = 1401;
            this.buttonLogSearch.Text = "Log Search";
            this.buttonLogSearch.UseVisualStyleBackColor = true;
            this.buttonLogSearch.Click += new System.EventHandler(this.ButtonLogSearch_Click);
            // 
            // textBoxLogSearch
            // 
            this.textBoxLogSearch.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.textBoxLogSearch.Location = new System.Drawing.Point(794, 68);
            this.textBoxLogSearch.MaxLength = 15;
            this.textBoxLogSearch.Name = "textBoxLogSearch";
            this.textBoxLogSearch.Size = new System.Drawing.Size(90, 23);
            this.textBoxLogSearch.TabIndex = 1402;
            // 
            // OpenHawaiiCallFileDialog
            // 
            this.OpenHawaiiCallFileDialog.FileName = "HQPCalls.csv";
            this.OpenHawaiiCallFileDialog.Filter = "CSV File|*.csv";
            this.OpenHawaiiCallFileDialog.Title = "Load Hawaii Call File";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Honeydew;
            this.ClientSize = new System.Drawing.Size(1008, 729);
            this.Controls.Add(this.textBoxLogSearch);
            this.Controls.Add(this.buttonLogSearch);
            this.Controls.Add(this.SessionDateTimePicker);
            this.Controls.Add(this.TextBoxLog2);
            this.Controls.Add(this.TextBoxLog1);
            this.Controls.Add(this.ButtonCompareLogs);
            this.Controls.Add(this.ButtonPreScoreReports);
            this.Controls.Add(this.ButtonLoadBadcalls);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.ComboBoxSelectSession);
            this.Controls.Add(this.TabControlMain);
            this.Controls.Add(this.LabelProgress);
            this.Controls.Add(this.ProgressBarLoad);
            this.Controls.Add(this.ButtonScoreLogs);
            this.Controls.Add(this.ButtonStartAnalysis);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.ComboBoxSelectContest);
            this.Controls.Add(this.ButtonLoadLogs);
            this.Controls.Add(this.ButtonSelectFolder);
            this.Controls.Add(this.TextBoxLogFolder);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MaximumSize = new System.Drawing.Size(1024, 1500);
            this.MinimumSize = new System.Drawing.Size(1024, 768);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "W6OP Contest Log Analyzer";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.TabControlMain.ResumeLayout(false);
            this.TabPageLogStatus.ResumeLayout(false);
            this.TabPageAnalysis.ResumeLayout(false);
            this.TabPageScoring.ResumeLayout(false);
            this.TabPageCompare.ResumeLayout(false);
            this.TabPageSearchLogs.ResumeLayout(false);
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
        private System.ComponentModel.BackgroundWorker BackgroundWorkerAnalyzeLogs;
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
        private System.Windows.Forms.TabControl TabControlMain;
        private System.Windows.Forms.TabPage TabPageLogStatus;
        private System.Windows.Forms.TabPage TabPageAnalysis;
        private System.Windows.Forms.TabPage TabPageScoring;
        private System.Windows.Forms.ColumnHeader columnHeader8;
        private System.Windows.Forms.ColumnHeader columnHeader9;
        private System.Windows.Forms.ColumnHeader columnHeader10;
        private System.Windows.Forms.ComboBox ComboBoxSelectSession;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ColumnHeader columnHeader11;
        private System.Windows.Forms.ColumnHeader columnHeader12;
        private System.Windows.Forms.ColumnHeader columnHeader13;
        private System.Windows.Forms.Button ButtonLoadBadcalls;
        private System.Windows.Forms.OpenFileDialog OpenCSVFileDialog;
        private System.Windows.Forms.Button ButtonPreScoreReports;
        private System.Windows.Forms.Button ButtonCompareLogs;
        private System.ComponentModel.BackgroundWorker BackgroundWorkerPreAnalysis;
        private System.Windows.Forms.TextBox TextBoxLog1;
        private System.Windows.Forms.TextBox TextBoxLog2;
        private System.Windows.Forms.TabPage TabPageCompare;
        private System.Windows.Forms.ListView ListViewCompare;
        private System.Windows.Forms.ColumnHeader columnHeader15;
        private System.Windows.Forms.ColumnHeader columnHeader16;
        private System.Windows.Forms.ColumnHeader columnHeader17;
        private System.Windows.Forms.ColumnHeader columnHeader18;
        private System.Windows.Forms.ColumnHeader columnHeader19;
        private System.Windows.Forms.ColumnHeader columnHeader20;
        private System.Windows.Forms.ColumnHeader columnHeader21;
        private System.Windows.Forms.ColumnHeader columnHeader22;
        private System.Windows.Forms.ColumnHeader columnHeader23;
        private System.Windows.Forms.DateTimePicker SessionDateTimePicker;
        private System.Windows.Forms.Button buttonLogSearch;
        private System.Windows.Forms.TextBox textBoxLogSearch;
        private System.Windows.Forms.TabPage TabPageSearchLogs;
        private System.Windows.Forms.ListView ListViewLogSearch;
        private System.Windows.Forms.ColumnHeader columnHeader14;
        private System.Windows.Forms.ColumnHeader columnHeader24;
        private System.Windows.Forms.ColumnHeader columnHeader25;
        private System.Windows.Forms.ColumnHeader columnHeader26;
        private System.Windows.Forms.ColumnHeader columnHeader27;
        private System.Windows.Forms.ColumnHeader columnHeader28;
        private System.Windows.Forms.OpenFileDialog OpenHawaiiCallFileDialog;
    }
}

