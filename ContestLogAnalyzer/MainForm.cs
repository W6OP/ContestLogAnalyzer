﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using W6OP.PrintEngine;

namespace W6OP.ContestLogAnalyzer
{
    /// <summary>
    /// Select folder.
    /// Enumerate folders with extension of .log
    /// Load first file. 
    /// Look for header and footer.
    /// Validate header.
    /// Load QSOs. Actually I will have to compare all logs so probably just validate headers and footers on all logs first.
    /// Validate QSOs.
    /// Calculate score.
    /// </summary>
    public partial class MainForm : Form
    {
        // Constants to define the folders required by the application
        private const string LOG_ANALYSER_WORKING_FOLDER_PATH = @"W6OP\LogAnalyser\Working";
        private const string LOG_ANALYSER_INSPECT_FOLDER_PATH = @"W6OP\LogAnalyser\Inspect";
        private const string LOG_ANALYSER_REPORT_FOLDER_PATH = @"W6OP\LogAnalyser\Report";
        private const string LOG_ANALYSER_REVIEW_FOLDER_PATH = @"W6OP\LogAnalyser\Review";
        private const string LOG_ANALYSER_SCORE_FOLDER_PATH = @"W6OP\LogAnalyser\Score";

        // set the actual folders to use
        private string _BaseWorkingFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), LOG_ANALYSER_WORKING_FOLDER_PATH);
        private string _BaseInspectFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), LOG_ANALYSER_INSPECT_FOLDER_PATH);
        private string _BaseReportFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), LOG_ANALYSER_REPORT_FOLDER_PATH);
        private string _BaseReviewFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), LOG_ANALYSER_REVIEW_FOLDER_PATH);
        private string _BaseScoreFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), LOG_ANALYSER_SCORE_FOLDER_PATH);

        private string _WorkingFolder = null;
        private string _InspectFolder = null;
        private string _ReportFolder = null;
        private string _ReviewFolder = null;
        private string _ScoreFolder = null;

        private List<ContestLog> _ContestLogs;
        private IEnumerable<System.IO.FileInfo> _LogFileList;
        //private ILookup<string, string> _BadCallList;

        private LogProcessor _LogProcessor;
        private LogAnalyzer _LogAnalyser;
        private ScoreCWOpen _CWOpen;
        private PrintManager _PrintManager;

        private string _LogSourceFolder = null;
        private Session _Session = Session.Session_0;

        #region Load and Initialize

        /// <summary>
        /// this.BeginInvoke(new Action<string, MessageType>(this.DisplayMessageForm), fullFileName, messageType);
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Load(object sender, EventArgs e)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            AssemblyName an = asm.GetName();
            string version = an.Version.Major + "." + an.Version.Minor + "." + an.Version.Build + "." + an.Version.Revision;

            UpdateLabel("");

            this.Text = "W6OP Contest Log Analyzer (" + version + ")";

            if (_LogProcessor == null)
            {
                _LogProcessor = new LogProcessor();
                _LogProcessor.OnProgressUpdate += _LogProcessor_OnProgressUpdate;
            }

            if (_LogAnalyser == null)
            {
                _LogAnalyser = new LogAnalyzer();
                _LogAnalyser.OnProgressUpdate += _LogAnalyser_OnProgressUpdate;
            }

            if (_CWOpen == null)
            {
                TabControlMain.SelectTab(TabPageScoring);
                _CWOpen = new ScoreCWOpen();
                _CWOpen.OnProgressUpdate += _CWOpen_OnProgressUpdate;
            }

            if (_PrintManager == null)
            {
                _PrintManager = new PrintManager();
                _LogProcessor._PrintManager = _PrintManager;
            }

            if (_PrintManager == null)
            {
                _PrintManager = new PrintManager();
                _LogProcessor._PrintManager = _PrintManager;
            }

            ComboBoxSelectContest.DataSource = Enum.GetValues(typeof(ContestName))
                .Cast<ContestName>()
                .Select(p => new { Key = (int)p, Value = p.ToString() })
                .ToList();

            ComboBoxSelectContest.DisplayMember = "Value";
            ComboBoxSelectContest.ValueMember = "Key";

            ComboBoxSelectSession.DataSource = Enum.GetValues(typeof(Session))
            .Cast<Enum>()
            .Select(value => new
            {
                (Attribute.GetCustomAttribute(value.GetType().GetField(value.ToString()), typeof(DescriptionAttribute)) as DescriptionAttribute).Description,
                value
            })
            .OrderBy(item => item.value)
            .ToList();
            ComboBoxSelectSession.DisplayMember = "Description";
            ComboBoxSelectSession.ValueMember = "value";

            TabControlMain.SelectTab(TabPageLogStatus);
        }

        #endregion

        #region Select Log folder

        /// <summary>
        /// Handle the ButtonSelectFolder click event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonSelectFolder_Click(object sender, EventArgs e)
        {
            SelectLogFileSourceFolder();
        }

        /// <summary>
        /// Launch the folder browser dialog to select the folder the logs reside in.
        /// </summary>
        private void SelectLogFileSourceFolder()
        {
            LogFolderBrowserDialog.ShowDialog();

            if (!string.IsNullOrEmpty(LogFolderBrowserDialog.SelectedPath))
            {
                _LogSourceFolder = LogFolderBrowserDialog.SelectedPath;
                TextBoxLogFolder.Text = _LogSourceFolder;
            }
        }

        private void ComboBoxSelectSession_SelectedIndexChanged(object sender, EventArgs e)
        {
            // get the current session number - this is specific to CWOPEN and will need to be moved later
            Enum.TryParse(ComboBoxSelectSession.SelectedValue.ToString(), out _Session);
        }

        #endregion

        #region Load Log Files



        /// <summary>
        /// Handle the click event for the ButtonLoadLogs button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonLoadLogs_Click(object sender, EventArgs e)
        {
            //Session currentSession = Session.Session_0;
            string session = null;

            TabControlMain.SelectTab(TabPageLogStatus);
            _LogFileList = null;

            // check the current session number - this is specific to CWOPEN and will need to be moved later
            if (_Session == Session.Session_0)
            {
                MessageBox.Show("You must select the session to be scored", "Invalid Session", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }

            session = EnumHelper.GetDescription(_Session);
            _WorkingFolder = Path.Combine(_BaseWorkingFolder, session);
            _InspectFolder = Path.Combine(_BaseInspectFolder, session);
            _ReportFolder = Path.Combine(_BaseReportFolder, session);
            _ReviewFolder = Path.Combine(_BaseReviewFolder, session);
            _ScoreFolder = Path.Combine(_BaseScoreFolder, session);

            LoadLogFiles(session);
        }

        /// <summary>
        /// Get a list and count of the files to be uploaded. Pass them off to another thread
        /// to preprocess the logs.
        /// </summary>
        private void LoadLogFiles(string session)
        {
            Int32 fileCount = 0;

            try
            {
                ProgressBarLoad.Maximum = 0;
                UpdateListViewLoad("", "", true);
                UpdateListViewAnalysis("", "", "", true);
                UpdateListViewScore(new ContestLog(), true);
                ButtonStartAnalysis.Enabled = false;

                _ContestLogs = new List<ContestLog>();

                // create folders if necessary
                if (!Directory.Exists(Path.Combine(_WorkingFolder, session)))
                {
                    Directory.CreateDirectory(_WorkingFolder);
                }

                if (!Directory.Exists(Path.Combine(_InspectFolder, session)))
                {
                    Directory.CreateDirectory(_InspectFolder);
                }

                if (!Directory.Exists(Path.Combine(_ReportFolder, session)))
                {
                    Directory.CreateDirectory(_ReportFolder);
                }

                if (!Directory.Exists(Path.Combine(_ReviewFolder, session)))
                {
                    Directory.CreateDirectory(_ReviewFolder);
                }

                if (!Directory.Exists(Path.Combine(_ScoreFolder, session)))
                {
                    Directory.CreateDirectory(_ScoreFolder);
                }

                if (String.IsNullOrEmpty(_LogSourceFolder))
                {
                    ButtonSelectFolder.PerformClick();
                }

                // initialize other modules
                if (_LogSourceFolder != _WorkingFolder)
                {
                    _LogProcessor.LogSourceFolder = _LogSourceFolder;
                    // copy eveything to working folder so we don't modify originals
                    CopyLogFilesToWorkingFolder();
                }

                _PrintManager.InspectionFolder = _InspectFolder;
                _PrintManager.WorkingFolder = _WorkingFolder;
                _PrintManager.ReportFolder = _ReportFolder;
                _PrintManager.ReviewFolder = _ReviewFolder;
                _PrintManager.ScoreFolder = _ScoreFolder;

                _LogProcessor.WorkingFolder = _WorkingFolder;
                _LogProcessor.InspectionFolder = _InspectFolder;


                if (_LogFileList == null)
                {
                    fileCount = _LogProcessor.BuildFileList(_Session, out _LogFileList);
                }

                if (_LogFileList.Cast<object>().Count() == 0)
                {
                    fileCount = _LogProcessor.BuildFileList(_Session, out _LogFileList);
                }
                else
                {
                    fileCount = _LogFileList.Cast<object>().Count();
                }

                ResetProgressBar(true);
                ProgressBarLoad.Maximum = fileCount;

                UpdateListViewLoad(fileCount.ToString() + " logs total.", "", false);

                Cursor = Cursors.WaitCursor;
                BackgroundWorkerLoadLogs.RunWorkerAsync(fileCount);
            }
            catch (Exception)
            {
                MessageBox.Show("Load or Pre-process Log Error", "Load and Pre-process Logs", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        /// <summary>
        /// Copy all of the log files from the source folder to the working folder.
        /// </summary>
        private void CopyLogFilesToWorkingFolder()
        {
            string fileName = null;
            int fileCount = 0;

            try
            {
                fileCount = _LogProcessor.BuildFileList(_Session, out _LogFileList);

                if (fileCount > 0)
                {
                    foreach (FileInfo fileInfo in _LogFileList)
                    {
                        fileName = fileInfo.Name;
                        if (fileName != null)
                        {
                            File.Copy(fileInfo.FullName, Path.Combine(_WorkingFolder, fileName), true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "File Copy Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Background Worker Load Logs

        /// <summary>
        /// Start building the log objects.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerLoadLogs_DoWork(object sender, DoWorkEventArgs e)
        {
            string fileName = null;

            try
            {
                UpdateListViewAnalysis("", "", "", true);

                foreach (FileInfo fileInfo in _LogFileList)
                {
                    fileName = _LogProcessor.BuildContestLog(fileInfo, _ContestLogs, _Session);
                    if (fileName != null)
                    {
                        UpdateListViewLoad(fileName, "Load failed." + " - " + _LogProcessor.FailReason, false);
                    }
                }
            }
            catch (Exception)
            {
                //ComboBoxSelectContest.Enabled = true;
                //ComboBoxSelectSession.Enabled = true;
            }
        }

        /// <summary>
        /// Update the progress bar.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="progress"></param>
        private void _LogProcessor_OnProgressUpdate(Int32 progress)
        {
            UpdateProgress(progress);
        }

        /// <summary>
        /// When the background worker has completed, update the status and progress.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerLoadLogs_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                UpdateListViewLoad(e.Error.Message, "", false);
            }
            else
            {
                UpdateListViewLoad(_ContestLogs.Count.ToString() + " logs loaded.", "", false);
                ButtonPreScoreReports.Enabled = true;
                Cursor = Cursors.Default;
            }

            ProgressBarLoad.Maximum = _ContestLogs.Count;
            ProgressBarLoad.Value = _ContestLogs.Count;

            EnableControl(true);

            ComboBoxSelectContest.Enabled = true;
            ComboBoxSelectSession.Enabled = true;
        }

        #endregion

        #region Start Log Analysis

        /// <summary>
        /// Open first log
        /// get first QSO
        /// get Contact call sign
        /// see if it is in any other logs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonStartAnalysis_Click(object sender, EventArgs e)
        {
            TabControlMain.SelectTab(TabPageAnalysis);
            AnalyzeLogs();
        }

        /// <summary>
        /// Clear the ListView and Reset the progress bar.
        /// </summary>
        private void AnalyzeLogs()
        {
            UpdateListViewAnalysis("", "", "", true);
            ResetProgressBar(true);

            BackgroundWorkerAnalyzeLogs.RunWorkerAsync();
        }

        #endregion

        #region BackGround Worker Analyze Logs

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerAnalyzeLogs_DoWork(object sender, DoWorkEventArgs e)
        {
            UpdateLabel("");

            _LogAnalyser.PreProcessContestLogs(_ContestLogs);

            UpdateListViewAnalysis("", "", "", true);
            ResetProgressBar(true);

            _LogAnalyser.PreProcessContestLogsReverse(_ContestLogs);
        }

        /// <summary>
        /// Update the list view with the call sign last processed.
        /// </summary>
        /// <param name="value"></param>
        private void _LogAnalyser_OnProgressUpdate(string value, string qsoCount, string validQsoCount, Int32 progress)
        {

            if (value.Length > 1)
            {
                UpdateListViewAnalysis(value, qsoCount, validQsoCount, false);
                UpdateProgress(progress);
            }
            else
            {
                if (value == "1")
                {
                    UpdateLabel("Analysing Pass 1");
                }

                if (value == "2")
                {
                    UpdateLabel("Analysing Pass 2");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerAnalyzeLogs_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                UpdateListViewAnalysis(e.Error.Message, "", "", false);
            }
            else
            {
                ProgressBarLoad.Maximum = _ContestLogs.Count;
                ProgressBarLoad.Value = _ContestLogs.Count;

                UpdateListViewAnalysis("-----------------------", "", "", false);
                UpdateListViewAnalysis("Log analysis completed!", "", "", false);
                Cursor = Cursors.Default;
                ButtonScoreLogs.Enabled = true; // might be cross thread
                UpdateLabel("");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        private void UpdateLabel(string message)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<string>(this.UpdateLabel), message);
                return;
            }

            LabelProgress.Enabled = true;
            LabelProgress.Visible = true;
            LabelProgress.Text = message;

        }

        #endregion

        #region Score Logs

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonScoreLogs_Click(object sender, EventArgs e)
        {
            TabControlMain.SelectTab(TabPageScoring);
            UpdateListViewScore(new ContestLog(), true);

            Cursor = Cursors.WaitCursor;
            ResetProgressBar(true);
            // now score each log
            BackgroundWorkerScoreLogs.RunWorkerAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerScoreLogs_DoWork(object sender, DoWorkEventArgs e)
        {
            _CWOpen.ScoreContestLogs(_ContestLogs);
        }

        private void _CWOpen_OnProgressUpdate(ContestLog contestLog, Int32 progress)
        {
            UpdateProgress(progress);
            UpdateListViewScore(contestLog, false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerScoreLogs_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                UpdateListViewScore(e.Error.Message, false);
            }
            else
            {
                ProgressBarLoad.Maximum = _ContestLogs.Count;
                ProgressBarLoad.Value = _ContestLogs.Count;

                UpdateListViewAnalysis("-----------------------", "", "", false);
                UpdateListViewScore("Log scoring complete.", false);

                PrintQSORejectReport();
                UpdateListViewAnalysis("-----------------------", "", "", false);
                UpdateListViewScore("Rejected QSO report has been generated.", false);

                PrintQSOReviewReport();
                UpdateListViewAnalysis("-----------------------", "", "", false);
                UpdateListViewScore("Review QSO report has been generated.", false);

                PrintFinalScoreReport();
                UpdateListViewAnalysis("-----------------------", "", "", false);
                UpdateListViewScore("Final score report has been generated.", false);
            }

            Cursor = Cursors.Default;

        }

        #endregion

        #region Commented Out Code

        /// <summary>
        /// Look at the QSOs and see if they are valid - how do you know?
        /// need all the QSOs from two lists
        /// the qsos that match between the two logs
        /// </summary>
        /// <param name="log"></param>
        //private void AnalyzeQSOs(ContestLog log)
        //{
        //    List<QSO> qsoList;
        //    List<QSO> matchList = null;
        //    List<QSO> totalList = null;
        //    List<QSO> exceptionList = null;
        //    Hashtable callTable = new Hashtable();
        //    string logOwnerCall = log.LogHeader.OperatorCallSign;
        //    string operatorCall = null;
        //    string sentName = null;
        //    Int32 sent = 0;
        //    Int32 received = 0;
        //    Int32 band = 0;
        //    Int32 matchCount = 0;
        //    QSOStatus valid = QSOStatus.ValidQSO;


        //    foreach (ContestLog contestLog in log.MatchLogs)
        //    {
        //        //operatorCall = qso.OperatorCall;
        //        //sent = qso.SentSerialNumber;
        //        //received = qso.ReceivedSerialNumber;
        //        //band = qso.Band;
        //        //sentName = qso.OperatorName;

        //        // list of QSOs from the other guy where 
        //        qsoList = contestLog.QSOCollection.Where(a => a.ContactCall == logOwnerCall).ToList();


        //        //matchList = contestLog.QSOCollection.Where(a => a.ContactCall == operatorCall && a.SentSerialNumber == received && a.Band == band && a.ContactName == sentName).ToList<QSO>();

        //        return;
        //        //totalList = contestLog.QSOCollection.Where(a => a.ContactCall == logOwnerCall && a.SentSerialNumber == received && a.Band == band && a.ContactName == sentName).ToList<QSO>();
        //        // now query this guys log for matches
        //        foreach (QSO qso in qsoList)
        //        {
        //            operatorCall = qso.OperatorCall;
        //            sent = qso.SentSerialNumber;
        //            received = qso.ReceivedSerialNumber;
        //            band = qso.Band;
        //            sentName = qso.OperatorName; // check on this
        //            valid = QSOStatus.ValidQSO;

        //            //totalList = log.QSOCollection.Where(a => a.ContactCall == operatorCall).ToList<QSO>();
        //            // could there be more than one returned?
        //            matchList = log.QSOCollection.Where(a => a.ContactCall == operatorCall && a.SentSerialNumber == received && a.Band == band && a.ContactName == sentName).ToList<QSO>();
        //            foreach (QSO q in matchList)
        //            {
        //                q.Status = QSOStatus.ValidQSO;
        //                valid = QSOStatus.InvalidQSO;
        //                if (q.Status == QSOStatus.InvalidQSO)
        //                {
        //                    // later expand this with more information
        //                    q.RejectReason = "Dupe";
        //                }
        //            }

        //            //// see if there are any QSOs that need correction or looking at
        //            //if (totalList.Count != matchList.Count)
        //            //{
        //            //    exceptionList = totalList.Except(qsoList).ToList<QSO>();
        //            //}
        //        }
        //    }
        //}



        /// <summary>
        /// http://stackoverflow.com/questions/721395/linq-question-querying-nested-collections
        /// http://stackoverflow.com/questions/15996168/linq-query-to-filter-items-by-criteria-from-multiple-lists


        /*
         * http://stackoverflow.com/questions/14893924/for-vs-linq-performance-vs-future
            int matchIndex = array.Select((r, i) => new { value = r, index = i })
                         .Where(t => t.value == matchString)
                         .Select(s => s.index).First();
         */

        #endregion

        #region Update ListViews

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="clear"></param>
        private void UpdateListViewLoad(string message, string status, bool clear)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<string, string, bool>(this.UpdateListViewLoad), message, status, clear);
                return;
            }

            if (clear)
            {
                ListViewLoad.Items.Clear();
            }
            else
            {
                ListViewItem item = new ListViewItem(message);
                //item.Tag = contestLog;
                item.SubItems.Add(status);
                ListViewLoad.Items.Insert(0, item);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="clear"></param>
        private void UpdateListViewAnalysis(string message, string count, string validCount, bool clear)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<string, string, string, bool>(this.UpdateListViewAnalysis), message, count, validCount, clear);
                return;
            }

            if (clear)
            {
                ListViewAnalysis.Items.Clear();
            }
            else
            {
                ListViewItem item = new ListViewItem(message);
                item.SubItems.Add(count);
                item.SubItems.Add(validCount);
                ListViewAnalysis.Items.Insert(0, item);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logOwner"></param>
        /// <param name="clear"></param>
        private void UpdateListViewScore(ContestLog contestLog, bool clear)
        {
            Int32 validQsoCount = 0;

            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<ContestLog, bool>(this.UpdateListViewScore), contestLog, clear);
                return;
            }

            if (clear)
            {
                ListViewScore.Items.Clear();
            }
            else
            {
                validQsoCount = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO).ToList().Count();

                ListViewItem item = new ListViewItem(contestLog.LogOwner + "     -     " + contestLog.LogHeader.PrimaryName);
                item.SubItems.Add(contestLog.Operator);
                item.SubItems.Add(contestLog.Station);
                item.SubItems.Add(contestLog.QSOCollection.Count.ToString());
                item.SubItems.Add(validQsoCount.ToString());
                item.SubItems.Add(contestLog.Multipliers.ToString());
                item.SubItems.Add(contestLog.ClaimedScore.ToString());
                item.SubItems.Add(contestLog.ActualScore.ToString());
                ListViewScore.Items.Insert(0, item);
            }
        }

        private void UpdateListViewScore(string message, bool clear)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<string, bool>(this.UpdateListViewScore), message, clear);
                return;
            }

            if (clear)
            {
                ListViewScore.Items.Clear();
            }
            else
            {
                ListViewItem item = new ListViewItem(message);
                //item.SubItems.Add(contestLog.Operator);
                //item.SubItems.Add(contestLog.;
                //item.SubItems.Add(claimed);
                //item.SubItems.Add(actual);
                ListViewScore.Items.Insert(0, item);
            }
        }

        /// <summary>
        /// Increment the progress bar.
        /// </summary>
        /// <param name="count"></param>
        private void UpdateProgress(int count)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<Int32>(this.UpdateProgress), count);
                return;
            }

            // hack to get smooth progress bar
            if (ProgressBarLoad.Maximum >= ProgressBarLoad.Value + 2)
            {
                ProgressBarLoad.Value = ProgressBarLoad.Value + 2;
                ProgressBarLoad.Value = ProgressBarLoad.Value - 1;
            }
            else
            {
                ProgressBarLoad.PerformStep();
            }

            
            //LabelProgress.Text = count.ToString();
        }

        /// <summary>
        /// General method for preventing cross thread calls.
        /// </summary>
        private void EnableControl(bool clear)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<bool>(this.EnableControl), clear);
                return;
            }

            ButtonStartAnalysis.Enabled = clear;
        }

        private void ResetProgressBar(bool clear)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<bool>(this.ResetProgressBar), clear);
                return;
            }

            ProgressBarLoad.Value = 0;
            ProgressBarLoad.Maximum = 0;
            ProgressBarLoad.Maximum = _ContestLogs.Count;
            Application.DoEvents();
        }

        #endregion

        #region ListView Click Handling

        private void ListViewLoad_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListView listView = (ListView)sender;

            ListView.SelectedListViewItemCollection items = listView.SelectedItems;
            ListViewItem item = items[0];

            if (item.Tag != null)
            {
                ContestLog contestLog = (ContestLog)item.Tag;
                LogViewForm form = new LogViewForm(contestLog);
                form.Show();
            }
        }

        private void ListViewScore_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // pop up form with all of the statistics on it
            // only allow one form to show at a time

        }

        private void ListViewAnalysis_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // popup form with all of the log entries
            // mark ones to review in blue
            // allow changes to be made and saved
            // allow multiple forms so thay can be compared
        }



        #endregion

        #region Show QSO Form
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListViewScore_DoubleClick(object sender, EventArgs e)
        {
            List<ContestLog> logList = new List<ContestLog>();
            string callsign = null;

            if (ListViewScore.SelectedItems.Count > 0)
            {
                callsign = ListViewScore.SelectedItems[0].Text;
                logList = _ContestLogs.Where(a => a.LogOwner == callsign).ToList();

                QSOForm form = new QSOForm(logList);
                form.Show();
            }
        }

        #endregion

        #region Print and Score Buttons

        /// <summary>
        /// Print a report for each log showing the QSOs that were not counted and why.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonPrint_Click(object sender, EventArgs e)
        {
            PrintQSORejectReport();
        }

        private void PrintQSORejectReport()
        {
            try
            {
                foreach (ContestLog contestLog in _ContestLogs)
                {
                    _PrintManager.PrintLogSummaryReport(contestLog, contestLog.LogOwner);
                }
            }
            catch (Exception ex)
            {
                // later just return a message to show in listview
                MessageBox.Show(ex.Message);
            }
        }

        private void PrintQSOReviewReport()
        {
            try
            {
                //foreach (ContestLog contestLog in _ContestLogs)
                //{
                _PrintManager.PrintReviewReport(_ContestLogs);
                //}
            }
            catch (Exception ex)
            {
                // later just return a message to show in listview
                MessageBox.Show(ex.Message);
            }
        }

        private void PrintFinalScoreReport()
        {
            try
            {
                //_PrintManager.PrintScoreSheet(_ContestLogs, true);
                _PrintManager.PrintPdfScoreSheet(_ContestLogs);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            //MessageBox.Show("Score report completed");
        }

        /// <summary>
        /// Create a PDF file with all the scores.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonPrintScores_Click(object sender, EventArgs e)
        {
            PrintFinalScoreReport();
        }

        private void ButtonLoadBadcalls_Click(object sender, EventArgs e)
        {
            TabControlMain.SelectTab(TabPageLogStatus);
            LoadCSVFile();
        }

       

        private void LoadCSVFile()
        {
            ILookup<string, string> badCallList;

            try
            {
                badCallList = LoadFile();
                _LogAnalyser.BadCallList = badCallList;
                UpdateListViewLoad("Loaded bad call file.", badCallList.Count.ToString() + " entries.", false);
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message, "Load CSV File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateListViewLoad("Unable to load bad call file.", ex.Message, false);
            }
            finally
            {
              
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private ILookup<string, string> LoadFile()
        {
            // List<string> lineList = new List<string>();
            char[] SpaceDelimiter = new char[] { ' ' };
            ILookup<string, string> lines = null;

            if (OpenCSVFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (File.Exists(OpenCSVFileDialog.FileName))
                {
                    lines = File.ReadLines(OpenCSVFileDialog.FileName)
                      .Select(csvLine => csvLine.Split(SpaceDelimiter, StringSplitOptions.RemoveEmptyEntries))
                      .Distinct()
                      .ToLookup(s => s[0], s => s[1]);
                }
            }

            // return empty list
            return lines;
        }


        #endregion

        #region Pre Analysis Reports

        /// <summary>
        /// Handle button click.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonPreScoreReports_Click(object sender, EventArgs e)
        {
            CreateCallNameFile();
            
        }

        /// <summary>
        /// Start the background worker to create the pre-analysis reports.
        /// </summary>
        private void CreateCallNameFile()
        {
            UpdateListViewAnalysis("", "", "", true);

            ResetProgressBar(true);

            BackgroundWorkerPreAnalysis.RunWorkerAsync();
        }

        //List<QSO> _DistinctQSOs;


        /// <summary>
        /// Create a reportWith all the calls and all the names and summarize
        /// how many times each call and each name is referenced in other logs.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerPreAnalysis_DoWork(object sender, DoWorkEventArgs e)
        {
            List<Tuple<string, string>> distinctQSOs = _LogAnalyser.CollectAllDistinctQSOs(_ContestLogs);
            _PrintManager.CreateUniqueFile(distinctQSOs, _BaseReportFolder, _Session.ToString());

        }

        private void BackgroundWorkerPreAnalysis_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        private void BackgroundWorkerPreAnalysis_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }

        #endregion

        //if (InvokeRequired)
        //   {
        //       this.BeginInvoke(new Action<string, DXAFileType>(this.MoveFileToBackupDirectory), uploadFile, fileType);
        //       return;
        //   }

    } // end class
}

/*
  tempLog = _ContestLogs.Where(q => q.QSOCollection.Any(a => a.ContactCall == operatorCall && a.ReceivedSerialNumber == sent && a.Band == band && a.ContactName == sentName && a.Status == QSOStatus.InvalidQSO)).ToList();
                    if (tempLog != null && tempLog.Count > 0)
                    {
                        contestLog.MatchLogs.AddRange(tempLog);
                    }

                    // some of these will be marked valid as we go along and need to be removed from this collection
                    tempLog = _ContestLogs.Where(q => q.QSOCollection.Any(a => a.ContactCall == operatorCall && (a.ReceivedSerialNumber != sent || a.Band == band || a.ContactName == sentName && a.Status == QSOStatus.InvalidQSO))).ToList();
                    if (tempLog != null && tempLog.Count > 0)
                    {
                        contestLog.ReviewLogs.AddRange(tempLog);
                    }

                    tempLog = _ContestLogs.Where(q => q.QSOCollection.All(a => a.ContactCall != operatorCall)).ToList();
                    if (tempLog != null && tempLog.Count > 0)
                    {
                        contestLog.OtherLogs.AddRange(tempLog);
                    }

 */
