using NetworkLookup;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using W6OP.CallParser;
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
        private const string LOG_ANALYSER_BASE_FOLDER_PATH = @"W6OP\LogAnalyser\Contest";
        private const string LOG_ANALYSER_WORKING_FOLDER_PATH = @"Working";
        private const string LOG_ANALYSER_INSPECT_FOLDER_PATH = @"Inspect";
        private const string LOG_ANALYSER_REPORT_FOLDER_PATH = @"Report";
        private const string LOG_ANALYSER_REVIEW_FOLDER_PATH = @"Review";
        private const string LOG_ANALYSER_SCORE_FOLDER_PATH = @"Score";

        // set the actual folders to use
        private string _BaseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), LOG_ANALYSER_BASE_FOLDER_PATH);
        private const string _BaseWorkingFolder = LOG_ANALYSER_WORKING_FOLDER_PATH;
        private const string _BaseInspectFolder = LOG_ANALYSER_INSPECT_FOLDER_PATH;
        private const string _BaseReportFolder = LOG_ANALYSER_REPORT_FOLDER_PATH;
        private const string _BaseReviewFolder = LOG_ANALYSER_REVIEW_FOLDER_PATH;
        private const string _BaseScoreFolder = LOG_ANALYSER_SCORE_FOLDER_PATH;

        private string _WorkingFolder = null;
        private string _InspectFolder = null;
        private string _ReportFolder = null;
        private string _ReviewFolder = null;
        private string _ScoreFolder = null;

        private List<ContestLog> _ContestLogs;
        private IEnumerable<FileInfo> _LogFileList;
        //private QRZ _QRZ;

        private LogProcessor _LogProcessor;
        private LogAnalyzer _LogAnalyser;
        private ScoreCWOpen _CWOpen;
        private ScoreHQP _HQP;
        private PrintManager _PrintManager;

        private string _LogSourceFolder = null;
        private Session _Session = Session.Session_0;

        private ContestName _ActiveContest;

        private bool _Initialized = false;

        private PrefixFileParser PrefixFileParser;
        private CallLookUp CallLookUp;

        #region Load and Initialize

        /// <summary>
        /// this.BeginInvoke(new Action<string, MessageType>(this.DisplayMessageForm), fullFileName, messageType);
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            //_QRZ = new QRZ();
        }

        /// <summary>
        /// Form load event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Load(object sender, EventArgs e)
        {
            // get the version number
            Assembly asm = Assembly.GetExecutingAssembly();
            AssemblyName an = asm.GetName();
            string version = an.Version.Major + "." + an.Version.Minor + "." + an.Version.Build + "." + an.Version.Revision;
            this.Text = "W6OP Contest Log Analyzer (" + version + ")";

            UpdateLabel("");
            SessionDateTimePicker.Value = DateTime.Now;

            if (_LogProcessor == null)
            {
                _LogProcessor = new LogProcessor();
                _LogProcessor.OnProgressUpdate += LogProcessor_OnProgressUpdate;
            }

            if (_LogAnalyser == null)
            {
                _LogAnalyser = new LogAnalyzer();
                _LogAnalyser.OnProgressUpdate += LogAnalyser_OnProgressUpdate;
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

            LoadResourceFiles();

            _Initialized = true;
        }

        private void LoadPrefixList()
        {
            PrefixFileParser = new PrefixFileParser();
            PrefixFileParser.ParsePrefixFile("");
            CallLookUp = new CallLookUp(PrefixFileParser);

            _LogAnalyser.CallLookUp = CallLookUp;
            _LogProcessor.CallLookUp = CallLookUp;
        }

        /// <summary>
        /// Adding support for Kansas and Ohio QSO parties. This will also be
        /// useful for the HQP
        /// 
        /// Load embedded resorces with valid county abbreviations for Kansas
        /// and Ohio. Create a collection of the abbreviations and names.
        /// 
        /// If a call comes in, look at the suffix and if it matches one of
        /// these we know for sure where it is from.
        /// 
        /// Some call prefixes are one and some two letters and a number and some are
        /// two letters ie. M = England
        /// 
        /// some - KC6/E,East Caroline have / in them
        /// 
        /// VRA-VRZ  Now China
        /// VR(alpha) is China VR(int) is other
        /// 
        /// KG4,USA OR Guantanamo Bay
        /// Single letter suffixes KG4x and three letter suffixes KG4xxx can be
        /// issued anywhere in the USA fourth call district, and used anywhere 
        /// in the United States.
        /// Two letter suffixes KG4xx are issued for Guantanamo Bay.
        /// 
        /// May need to use smething else for state prefixes - hashset can't have dupes
        /// </summary>
        private void LoadResourceFiles()
        {
            string result = null;
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames()
                .Single(str => str.EndsWith("EmbedCounties_Ohio.txt"));

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                result = reader.ReadToEnd();
                result = result.Replace("\r\n", "|");

                _LogProcessor.Ohio = (Lookup<string, string>)result.Split('|').Select(x => x.Split(',')).ToLookup(x => x[0], x => x[1]);
            }

            resourceName = assembly.GetManifestResourceNames()
               .Single(str => str.EndsWith("EmbedCounties_Kansas.txt"));

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                result = reader.ReadToEnd();
                // now split it into a collecction
                result = result.Replace("\r\n", "|");

                _LogProcessor.Kansas = (Lookup<string, string>)result.Split('|').Select(x => x.Split(',')).ToLookup(x => x[0], x => x[1]);
            }

            resourceName = assembly.GetManifestResourceNames()
              .Single(str => str.EndsWith("EmbedCountryPrefixes.txt"));

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                result = reader.ReadToEnd();
                result = result.Replace("\r\n", "|");

                _LogProcessor.CountryPrefixes = (Lookup<string, string>)result.Split('|').Select(x => x.Split(',')).ToLookup(x => x[0], x => x[1]);
            }
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBoxSelectSession_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_Initialized) return; // supress on form load

            // get the current session number - this is specific to CWOPEN
            Enum.TryParse(ComboBoxSelectSession.SelectedValue.ToString(), out _Session);
            ButtonLoadLogs.Enabled = true;

            SetupContestProperties();
        }

        /// <summary>
        /// Event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBoxSelectContest_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_Initialized) return; // supress on form load

            Enum.TryParse(ComboBoxSelectContest.SelectedValue.ToString(), out _ActiveContest);

            if (_ActiveContest == ContestName.HQP)
            {
                SetupContestProperties();
            }
            else
            {
                ComboBoxSelectSession.SelectedIndex = 1;
            }
        }

        /// <summary>
        /// Enable various controls.
        /// Create folder paths.
        /// </summary>
        private void SetupContestProperties()
        {
            string session = null;
            string contestName = null;

            EnableControl(ButtonStartAnalysis, false);
            EnableControl(ButtonScoreLogs, false);

            ResetViewsAndControls();

            contestName = _ActiveContest.ToString();

            _PrintManager = null;

            TabControlMain.SelectTab(TabPageLogStatus);
            _LogFileList = null;

            _BaseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), LOG_ANALYSER_BASE_FOLDER_PATH);
            _BaseFolder = _BaseFolder.Replace("Contest", contestName) + "_" + SessionDateTimePicker.Value.ToString("yyyy");

            switch (_ActiveContest)
            {
                case ContestName.CW_OPEN:
                    //if (initial)
                    //{
                    //    //ComboBoxSelectSession.SelectedIndex = 1;
                    //}

                    if (_CWOpen == null)
                    {
                        _CWOpen = new ScoreCWOpen();
                        _CWOpen.OnProgressUpdate += CWOpen_OnProgressUpdate;
                    }
                    session = EnumHelper.GetDescription(_Session);
                    _BaseFolder = Path.Combine(_BaseFolder, session);
                    TabControlMain.SelectTab(TabPageLogStatus);
                    ComboBoxSelectSession.Enabled = true;
                    break;
                case ContestName.HQP:
                    if (_HQP == null)
                    {
                        _HQP = new ScoreHQP();
                        _HQP.OnProgressUpdate += HQP_OnProgressUpdate;
                    }
                    ComboBoxSelectSession.SelectedIndex = 0;
                    ComboBoxSelectSession.Enabled = false;
                    Enum.TryParse(ComboBoxSelectSession.SelectedValue.ToString(), out _Session);
                    TabControlMain.SelectTab(TabPageLogStatus);
                    ButtonLoadLogs.Enabled = true;
                    Task.Run(() => LoadPrefixList());
                    break;
                default:
                    break;
            }

            _WorkingFolder = Path.Combine(_BaseFolder, _BaseWorkingFolder);
            _InspectFolder = Path.Combine(_BaseFolder, _BaseInspectFolder);
            _ReportFolder = Path.Combine(_BaseFolder, _BaseReportFolder);
            _ReviewFolder = Path.Combine(_BaseFolder, _BaseReviewFolder);
            _ScoreFolder = Path.Combine(_BaseFolder, _BaseScoreFolder);

            _PrintManager = new PrintManager(_ActiveContest);
            _LogProcessor._PrintManager = _PrintManager;
            _LogProcessor.ActiveContest = _ActiveContest;
            _LogAnalyser.ActiveContest = _ActiveContest;
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
            TabControlMain.SelectTab(TabPageLogStatus);
            _LogFileList = null;

            if (_ActiveContest == ContestName.Select)
            {
                MessageBox.Show("You must select a Contest to score.", "Select Contest", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                return;
            }

            LoadLogFiles();
        }

        /// <summary>
        /// Get a list and count of the files to be uploaded. Pass them off to another thread
        /// to preprocess the logs.
        /// </summary>
        private void LoadLogFiles()
        {
            try
            {
                ProgressBarLoad.Visible = true;
                UpdateLabel("Loading Contest Logs");
                ProgressBarLoad.Maximum = 0;
                UpdateListViewLoad("", "", true);
                UpdateListViewAnalysis("", "", "", true);
                UpdateListViewScore(new ContestLog(), true);
                ButtonStartAnalysis.Enabled = false;

                _ContestLogs = new List<ContestLog>();

                CreateFolders();

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

                int fileCount = _LogFileList.Cast<object>().Count();
                ResetProgressBar(true);
                ProgressBarLoad.Maximum = fileCount;

                UpdateListViewLoad(fileCount.ToString() + " logs total.", "", false);

                Cursor = Cursors.WaitCursor;
                BackgroundWorkerLoadLogs.RunWorkerAsync(fileCount);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Load or Pre-process Log Error", "Load and Pre-process Logs \r\n" + ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        /// <summary>
        /// Create all the folders necessary to run prograsm.
        /// </summary>
        private void CreateFolders()
        {
            // create folders if necessary
            // always clean the working folder
            if (!Directory.Exists(_WorkingFolder))
            {
                Directory.CreateDirectory(_WorkingFolder);
            }
            else
            {
                Directory.Delete(_WorkingFolder, true);
                Directory.CreateDirectory(_WorkingFolder);
            }

            if (!Directory.Exists(_InspectFolder))
            {
                Directory.CreateDirectory(_InspectFolder);
            }

            if (!Directory.Exists(_ReportFolder))
            {
                Directory.CreateDirectory(_ReportFolder);
            }

            if (!Directory.Exists(_ReviewFolder))
            {
                Directory.CreateDirectory(_ReviewFolder);
            }

            if (!Directory.Exists(_ScoreFolder))
            {
                Directory.CreateDirectory(_ScoreFolder);
            }
        }

        /// <summary>
        /// Copy all of the log files from the source folder to the working folder.
        /// </summary>
        private void CopyLogFilesToWorkingFolder()
        {
            string fileName;

            try
            {
                int fileCount = _LogProcessor.BuildFileList(_Session, out _LogFileList);
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
            string fileName;

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

        /// <summary>
        /// Update the progress bar.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="progress"></param>
        private void LogProcessor_OnProgressUpdate(Int32 progress)
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
                EnableControl(ButtonPreScoreReports, true);
                Cursor = Cursors.Default;
            }

            UpdateLabel("Load contest logs completed");

            EnableControl(ButtonStartAnalysis, true);

            ResetProgressBar(true);
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

            _LogAnalyser.ActiveContest = _ActiveContest;
            _LogAnalyser.PreAnalyzeContestLogs(_ContestLogs);

            UpdateListViewAnalysis("Pass 1 completed", "----------", "----------", false);
            ResetProgressBar(true);

            _LogAnalyser.PreAnalyzeContestLogsReverse(_ContestLogs);
        }

        /// <summary>
        /// Update the list view with the call sign last processed.
        /// </summary>
        /// <param name="value"></param>
        private void LogAnalyser_OnProgressUpdate(string value, string qsoCount, string validQsoCount, Int32 progress)
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
                UpdateListViewAnalysis(e.Error.Message, "Error", "Error", false);
            }
            else
            {
                ProgressBarLoad.Maximum = _ContestLogs.Count;
                ProgressBarLoad.Value = _ContestLogs.Count;

                UpdateListViewAnalysis("-----------------------", "", "", false);
                UpdateListViewAnalysis("Log analysis completed!", "", "", false);
                Cursor = Cursors.Default;
                EnableControl(ButtonScoreLogs, true);
                UpdateLabel("Log analysis completed");
            }

            ResetProgressBar(true);
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

            UpdateLabel("Scoring Contest Logs");

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
            switch (_ActiveContest)
            {
                case ContestName.CW_OPEN:
                    _CWOpen.ScoreContestLogs(_ContestLogs);
                    break;
                case ContestName.HQP:
                    _HQP.ScoreContestLogs(_ContestLogs);
                    break;
            }
        }

        private void CWOpen_OnProgressUpdate(ContestLog contestLog, Int32 progress)
        {
            UpdateProgress(progress);
            UpdateListViewScore(contestLog, false);
        }

        private void HQP_OnProgressUpdate(ContestLog contestLog, int progress)
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

            UpdateLabel("Log scoring completed");

            ResetProgressBar(true);

            Cursor = Cursors.Default;
        }

        #endregion

        /// <summary>
        /// http://stackoverflow.com/questions/721395/linq-question-querying-nested-collections
        /// http://stackoverflow.com/questions/15996168/linq-query-to-filter-items-by-criteria-from-multiple-lists


        /*
         * http://stackoverflow.com/questions/14893924/for-vs-linq-performance-vs-future
            int matchIndex = array.Select((r, i) => new { value = r, index = i })
                         .Where(t => t.value == matchString)
                         .Select(s => s.index).First();
         */

        #region Update Labels and ListViews

        /// <summary>
        /// Prevent cross thread calls.
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

        /// <summary>
        /// Clear everything when a new contest or session is selected.
        /// </summary>
        private void ResetViewsAndControls()
        {
            TextBoxLogFolder.Clear();
            label2.Text = "";
            _LogSourceFolder = string.Empty;

            ProgressBarLoad.Visible = false;

            ListViewLoad.Items.Clear();
            ListViewAnalysis.Items.Clear();
            ListViewScore.Items.Clear();
        }

        /// <summary>
        /// Prevent cross thread calls.
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
        /// Prevent cross thread calls.
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
        /// Prevent cross thread calls.
        /// </summary>
        /// <param name="logOwner"></param>
        /// <param name="clear"></param>
        private void UpdateListViewScore(ContestLog contestLog, bool clear)
        {
            Int32 validQsoCount = 0;

            if (InvokeRequired)
            {
                BeginInvoke(new Action<ContestLog, bool>(this.UpdateListViewScore), contestLog, clear);
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

        /// <summary>
        /// Prevent cross thread calls.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="clear"></param>
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
                ListViewScore.Items.Insert(0, item);
            }
        }

        /// <summary>
        /// Increment the progress bar. Prevent cross thread calls.
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
                ProgressBarLoad.Value += 2;
                ProgressBarLoad.Value -= 1;
            }
            else
            {
                ProgressBarLoad.PerformStep();
            }

        }

        /// <summary>
        /// General method for preventing cross thread calls.
        /// </summary>
        private void EnableControl(Control control, bool clear)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<Control, bool>(this.EnableControl), control, clear);
                //this.BeginInvoke(new Action<bool>(this.EnableControl), clear);
                return;
            }

            control.Enabled = clear;
        }

        /// <summary>
        /// Prevent cross thread calls.
        /// </summary>
        /// <param name="clear"></param>
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
                switch (_ActiveContest)
                {
                    case ContestName.CW_OPEN:
                        _PrintManager.PrintCWOpenPdfScoreSheet(_ContestLogs);
                        break;
                    case ContestName.HQP:
                        _PrintManager.PrintHQPPdfScoreSheet(_ContestLogs);
                        break;
                }
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


        /// <summary>
        /// Load a CSV file with a list of known bad calls.
        /// </summary>
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
            List<string> lineList = new List<string>();
            char[] SpaceDelimiter = new char[] { ' ' };
            char[] commaDelimiter = new char[] { ',' };
            ILookup<string, string> lines = null;

            if (OpenCSVFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (File.Exists(OpenCSVFileDialog.FileName))
                {
                    lineList = File.ReadAllLines(OpenCSVFileDialog.FileName).Select(i => i.ToString()).ToList();

                    if (lineList[0].IndexOf(",") == -1)
                    {
                        lines = File.ReadLines(OpenCSVFileDialog.FileName)
                      .Select(csvLine => csvLine.Split(SpaceDelimiter, StringSplitOptions.RemoveEmptyEntries))
                      .Distinct()
                      .ToLookup(s => s[0], s => s[1]);
                    }

                    if (lineList[0].IndexOf(",") != -1)
                    {
                        lines = File.ReadLines(OpenCSVFileDialog.FileName)
                      .Select(csvLine => csvLine.Split(commaDelimiter, StringSplitOptions.RemoveEmptyEntries))
                      .Distinct()
                      .ToLookup(s => s[0], s => s[1]);
                    }
                }
            }

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

        /// <summary>
        /// Create a report with all the calls and all the names and summarize
        /// how many times each call and each name is referenced in other logs.
        /// Should this be called form the print manager ???
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerPreAnalysis_DoWork(object sender, DoWorkEventArgs e)
        {
            // calls with <= 3 hits
            string session = _Session.ToString();

            //if (_Session == Session.Session_0)
            //{
            //    session = "";
            //}

            // list of all call/name pairs
            UpdateListViewLoad("List Unique Call Name Pairs", "", false);
            _PrintManager.PrintPreAnalysisReport(_ReportFolder, session, _ContestLogs);

            // list of all calls with number of hits and all names with number of hits
            UpdateListViewLoad("List Call Name Occurences", "", false);
        }

        private void BackgroundWorkerPreAnalysis_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        private void BackgroundWorkerPreAnalysis_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                UpdateListViewAnalysis(e.Error.Message, "", "", false);
            }
            else
            {
                UpdateListViewLoad("Reports completed.", "", false);
            }
        }

        #endregion

        #region Compare Logs

        /// <summary>
        /// Compare two logs.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonCompareLogs_Click(object sender, EventArgs e)
        {
            if (_ContestLogs == null || _ContestLogs.Count() == 0)
            {
                MessageBox.Show("You must load and analyze the logs before you can compare.", "Missing Log Files", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (String.IsNullOrEmpty(TextBoxLog1.Text) || String.IsNullOrEmpty(TextBoxLog2.Text))
            {
                MessageBox.Show("You must enter both call signs before you can compare.", "Missing Call Sign", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CompareLogs(TextBoxLog1.Text.Trim(), TextBoxLog2.Text.Trim());
        }

        /// <summary>
        /// Find the two logs to compare.
        /// </summary>
        /// <param name="call1"></param>
        /// <param name="call2"></param>
        private void CompareLogs(string call1, string call2)
        {
            ContestLog log1 = null;
            ContestLog log2 = null;
            int group = 0;
            string message = String.Format("The log for {0} could not be found.", call1);

            ListViewCompare.Items.Clear();
            ListViewCompare.Groups.Clear();

            ListViewCompare.Groups.Add(new ListViewGroup(call1, HorizontalAlignment.Left));
            ListViewCompare.Groups.Add(new ListViewGroup(call2, HorizontalAlignment.Left));

            TabControlMain.SelectTab(TabPageCompare);

            log1 = (ContestLog)_ContestLogs.FirstOrDefault(q => q.LogOwner == call1);
            log2 = (ContestLog)_ContestLogs.FirstOrDefault(q => q.LogOwner == call2);

            if (log1 != null)
            {
                List<QSO> list1 = log1.QSOCollection.Where(q => q.OperatorCall == call1 && q.ContactCall == call2).ToList();
                if (list1 != null && list1.Count > 0)
                {
                    group = 0;

                    foreach (QSO qso in list1)
                    {
                        UpdateListViewCompare(qso, group);
                    }
                }
            }
            else
            {
                MessageBox.Show(message, "Unable to find log", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            if (log2 != null)
            {
                List<QSO> list2 = log2.QSOCollection.Where(q => q.ContactCall == call1 && q.OperatorCall == call2).ToList();
                if (list2 != null && list2.Count > 0)
                {
                    group = 1;

                    foreach (QSO qso in list2)
                    {
                        UpdateListViewCompare(qso, group);
                    }
                }
            }
            else
            {
                message = String.Format("The log for {0} could not be found.", call2);
                MessageBox.Show(message, "Unable to find log", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="qso"></param>
        /// <param name="group"></param>
        private void UpdateListViewCompare(QSO qso, int group)
        {
            ListViewItem item = new ListViewItem(qso.Band.ToString())
            {
                Group = ListViewCompare.Groups[group]
            };

            switch (_ActiveContest)
            {
                case ContestName.CW_OPEN:
                    item.SubItems.Add(qso.Mode);
                    item.SubItems.Add(qso.QsoDate.ToString());
                    item.SubItems.Add(qso.QsoTime.ToString());
                    item.SubItems.Add(qso.SentSerialNumber.ToString());
                    item.SubItems.Add(qso.OperatorEntity);
                    item.SubItems.Add(qso.ContactCall);
                    item.SubItems.Add(qso.ReceivedSerialNumber.ToString());
                    item.SubItems.Add(qso.ContactName);
                    break;
                case ContestName.HQP:
                    item.SubItems.Add(qso.Mode);
                    item.SubItems.Add(qso.QsoDate.ToString());
                    item.SubItems.Add(qso.QsoTime.ToString());
                    item.SubItems.Add("");
                    item.SubItems.Add(qso.OriginalOperatorEntity);
                    item.SubItems.Add(qso.ContactCall);
                    item.SubItems.Add("");
                    item.SubItems.Add(qso.OriginalContactEntity);
                    break;
            }

            ListViewCompare.Items.Insert(0, item);
        }

        #endregion

    } // end class
}
