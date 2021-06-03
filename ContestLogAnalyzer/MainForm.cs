using System;
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
        private string BaseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), LOG_ANALYSER_BASE_FOLDER_PATH);
        private const string BaseWorkingFolder = LOG_ANALYSER_WORKING_FOLDER_PATH;
        private const string BaseInspectFolder = LOG_ANALYSER_INSPECT_FOLDER_PATH;
        private const string BaseReportFolder = LOG_ANALYSER_REPORT_FOLDER_PATH;
        private const string BaseReviewFolder = LOG_ANALYSER_REVIEW_FOLDER_PATH;
        private const string BaseScoreFolder = LOG_ANALYSER_SCORE_FOLDER_PATH;

        private string WorkingFolder = null;
        private string InspectFolder = null;
        private string ReportFolder = null;
        private string ReviewFolder = null;
        private string ScoreFolder = null;

        private List<ContestLog> ContestLogs;
        private IEnumerable<FileInfo> ContestLogFileList;
        //private QRZ _QRZ;

        private LogProcessor LogProcessor;
        private LogAnalyzer LogAnalyser;
        private ScoreCWOpen CWOpen;
        private ScoreHQP HQP;
        private PrintManager PrintManager;

        private string LogSourceFolder = null;
        private Session Session = Session.Session_0;

        private ContestName ActiveContest;

        private bool Initialized = false;

        private PrefixFileParser PrefixFileParser;
        private CallLookUp CallLookUp;

        #region Load and Initialize

        /// <summary>
        /// this.BeginInvoke(new Action<string, MessageType>(this.DisplayMessageForm), fullFileName, messageType);
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
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

            if (LogProcessor == null)
            {
                LogProcessor = new LogProcessor();
                LogProcessor.OnProgressUpdate += LogProcessor_OnProgressUpdate;
            }

            if (LogAnalyser == null)
            {
                LogAnalyser = new LogAnalyzer();
                LogAnalyser.OnProgressUpdate += LogAnalyser_OnProgressUpdate;
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

            // should this only be for HQP?
            LoadResourceFiles();

            Initialized = true;
        }

        private void LoadPrefixList()
        {
            PrefixFileParser = new PrefixFileParser();
            PrefixFileParser.ParsePrefixFile("");
            CallLookUp = new CallLookUp(PrefixFileParser);

            LogAnalyser.CallLookUp = CallLookUp;
            LogProcessor.CallLookUp = CallLookUp;
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
        /// May need to use something else for state prefixes - hashset can't have dupes
        /// </summary>
        private void LoadResourceFiles()
        {
            var assembly = Assembly.GetExecutingAssembly();

            LoadOhioCounties(assembly);
            LoadKansasCounties(assembly);
            LoadCountryPrefixes(assembly);
            LoadHQPGrids(assembly);
            LoadULSData(assembly);
        }

        /// <summary>
        /// Load the ULS downloaded data.
        /// </summary>
        /// <param name="assembly">Assembly</param>
        private void LoadULSData(Assembly assembly)
        {
            Dictionary<string, string> ULSStateData = new Dictionary<string, string>();
            string line;
            string[] ulsData = new string[2];

            string resourceName = assembly.GetManifestResourceNames()
                          .Single(str => str.EndsWith("EmbedULSCallData.txt"));

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    ulsData = line.Split('|');
                    ULSStateData.Add(ulsData[0], ulsData[1]);
                }
            }
            LogProcessor.ULSStateData = ULSStateData;
        }

        /// <summary>
        /// Load the grid square to state file.
        /// </summary>
        /// <param name="assembly">Assembly</param>
        private void LoadHQPGrids(Assembly assembly)
        {
            Dictionary<string, List<string>> GridSquares = new Dictionary<string, List<string>>();
            string[] ulsData = new string[2];
            string[] grids = new string[3];
            string line;

            string resourceName = assembly.GetManifestResourceNames()
                          .Single(str => str.EndsWith("EmbedHQPGrids.csv"));

            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            using StreamReader reader = new StreamReader(stream);
            List<string> lineList = new List<string>();
            string key;

            while ((line = reader.ReadLine()) != null)
            {
                lineList = new List<string>();
                grids = line.Split(',');
                key = grids[0];

                grids = grids.Skip(1).ToArray();
                grids = grids.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                lineList = grids.ToList();

                GridSquares.Add(key, lineList);
            }

            LogProcessor.GridSquares = GridSquares;
        }

        /// <summary>
        /// Load the country prefixes.
        /// </summary>
        /// <param name="assembly">Assembly</param>
        private void LoadCountryPrefixes(Assembly assembly)
        {
            string resourceName = assembly.GetManifestResourceNames()
                          .Single(str => str.EndsWith("EmbedCountryPrefixes.txt"));

            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            using StreamReader reader = new StreamReader(stream);
            string result = reader.ReadToEnd();
            result = result.Replace("\r\n", "|");

            LogProcessor.CountryPrefixes = (Lookup<string, string>)result.Split('|').Select(x => x.Split(',')).ToLookup(x => x[0], x => x[1]);
        }

        /// <summary>
        /// Load the list of Kansas counties.
        /// </summary>
        /// <param name="assembly">Assembly</param>
        private void LoadKansasCounties(Assembly assembly)
        {
            string resourceName = assembly.GetManifestResourceNames()
                           .Single(str => str.EndsWith("EmbedCounties_Kansas.txt"));

            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            using StreamReader reader = new StreamReader(stream);
            string result = reader.ReadToEnd();
            // now split it into a collecction
            result = result.Replace("\r\n", "|");

            LogProcessor.Kansas = (Lookup<string, string>)result.Split('|').Select(x => x.Split(',')).ToLookup(x => x[0], x => x[1]);
        }

        /// <summary>
        /// Load the list of Ohio counties.
        /// </summary>
        /// <param name="assembly">Assembly</param>
        private void LoadOhioCounties(Assembly assembly)
        {
            string resourceName = assembly.GetManifestResourceNames()
                            .Single(str => str.EndsWith("EmbedCounties_Ohio.txt"));

            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            using StreamReader reader = new StreamReader(stream);
            string result = reader.ReadToEnd();
            result = result.Replace("\r\n", "|");

            LogProcessor.Ohio = (Lookup<string, string>)result.Split('|').Select(x => x.Split(',')).ToLookup(x => x[0], x => x[1]);
        }

        /// <summary>
        /// Use the .dat files initially to build the .txt files.
        /// The .dat files are too large to push to GitHub plus the smaller
        /// .txt files will load faster.
        /// Embed the .dat files and build the .txt files then remove the .dat
        /// and embed the .txt files.
        /// Only needed when a new ULS.dat is downloaded from the FCC.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerLoadULSResourceFiles_DoWork(object sender, DoWorkEventArgs e)
        {
            Dictionary<string, string> ULSStateData = new Dictionary<string, string>();
            var assembly = Assembly.GetExecutingAssembly();
            string line;

            // only for initial file build
            string resourceName = assembly.GetManifestResourceNames()
             .Single(str => str.EndsWith("EmbedULS_HD.dat"));

            string[] ulsData = new string[50];
            Dictionary<string, string> ulsHD = new Dictionary<string, string>(900000);

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    ulsData = line.Split('|');
                    if (ulsData[5] == "A")
                    {
                        if (!ulsHD.ContainsKey(ulsData[4]))
                        {
                            ulsHD.Add(ulsData[4], ulsData[1]);
                        }
                    }
                }
            }

            List<string[]> temp = new List<string[]>(900000);

            resourceName = assembly.GetManifestResourceNames()
              .Single(str => str.EndsWith("EmbedULSCallData.dat"));

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    ulsData = line.Split('|');
                    temp.Add(ulsData);
                    if (ulsData[6] != "")
                    {
                        if (!ULSStateData.ContainsKey(ulsData[4]))
                        {
                            // if the USI matches the USI in the ulsHD dictionary then add it
                            if (!ULSStateData.ContainsKey(ulsData[4]) && ulsHD.ContainsKey(ulsData[4]))
                            {
                                if (ulsHD[ulsData[4]] == ulsData[1])
                                {
                                    ULSStateData.Add(ulsData[4], ulsData[17]);
                                }
                            }
                        }
                    }
                }
            }

            BuildDataFiles(ULSStateData, "EmbedULSCallData.txt");
            Console.WriteLine("ULS Resource files loaded.");
        }

        private void BuildDataFiles(Dictionary<string, string> ulsHD, string fileName)
        {
            Console.WriteLine("Building data files.");
            string line;
            using StreamWriter outputFile = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName));
            ulsHD.ToList().ForEach(kv =>
            {
                line = kv.Key + "|" + kv.Value;
                outputFile.WriteLine(line);
            });
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
            if (LogFolderBrowserDialog.ShowDialog() != DialogResult.Cancel)
            {
                if (!string.IsNullOrEmpty(LogFolderBrowserDialog.SelectedPath))
                {
                    LogSourceFolder = LogFolderBrowserDialog.SelectedPath;
                    TextBoxLogFolder.Text = LogSourceFolder;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBoxSelectSession_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!Initialized) return; // supress on form load

            // get the current session number - this is specific to CWOPEN
            Enum.TryParse(ComboBoxSelectSession.SelectedValue.ToString(), out Session);
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
            if (!Initialized) return; // supress on form load

            Enum.TryParse(ComboBoxSelectContest.SelectedValue.ToString(), out ActiveContest);

            if (ActiveContest == ContestName.HQP)
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

            contestName = ActiveContest.ToString();

            PrintManager = null;

            TabControlMain.SelectTab(TabPageLogStatus);
            ContestLogFileList = null;

            BaseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), LOG_ANALYSER_BASE_FOLDER_PATH);
            BaseFolder = BaseFolder.Replace("Contest", contestName) + "_" + SessionDateTimePicker.Value.ToString("yyyy");

            switch (ActiveContest)
            {
                case ContestName.CW_OPEN:
                    if (CWOpen == null)
                    {
                        CWOpen = new ScoreCWOpen();
                        CWOpen.OnProgressUpdate += CWOpen_OnProgressUpdate;
                    }
                    session = EnumHelper.GetDescription(Session);
                    BaseFolder = Path.Combine(BaseFolder, session);
                    TabControlMain.SelectTab(TabPageLogStatus);
                    ComboBoxSelectSession.Enabled = true;
                    break;
                case ContestName.HQP:
                    if (HQP == null)
                    {
                        HQP = new ScoreHQP();
                        HQP.OnProgressUpdate += HQP_OnProgressUpdate;
                    }
                    ComboBoxSelectSession.SelectedIndex = 0;
                    ComboBoxSelectSession.Enabled = false;
                    Enum.TryParse(ComboBoxSelectSession.SelectedValue.ToString(), out Session);
                    TabControlMain.SelectTab(TabPageLogStatus);
                    ButtonLoadLogs.Enabled = true;
                    Task.Run(() => LoadPrefixList());
                    break;
                default:
                    break;
            }

            WorkingFolder = Path.Combine(BaseFolder, BaseWorkingFolder);
            InspectFolder = Path.Combine(BaseFolder, BaseInspectFolder);
            ReportFolder = Path.Combine(BaseFolder, BaseReportFolder);
            ReviewFolder = Path.Combine(BaseFolder, BaseReviewFolder);
            ScoreFolder = Path.Combine(BaseFolder, BaseScoreFolder);

            PrintManager = new PrintManager(ActiveContest);
            LogProcessor._PrintManager = PrintManager;
            LogProcessor.ActiveContest = ActiveContest;
            LogAnalyser.ActiveContest = ActiveContest;
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
            ContestLogFileList = null;

            if (ActiveContest == ContestName.Select)
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
            ContestLogs = new List<ContestLog>();

            try
            {
                ProgressBarLoad.Visible = true;
                UpdateLabel("Loading Contest Logs");
                ProgressBarLoad.Maximum = 0;
                UpdateListViewLoad("", "", true);
                UpdateListViewAnalysis("", "", "", true);
                UpdateListViewScore(new ContestLog(), true);
                ButtonStartAnalysis.Enabled = false;

                CreateFolders();

                if (string.IsNullOrEmpty(LogSourceFolder))
                {
                    ButtonSelectFolder.PerformClick();
                }

                // because PerformClick may be cancelled
                if (string.IsNullOrEmpty(LogSourceFolder))
                {
                    return;
                }

                // initialize other modules
                if (LogSourceFolder != WorkingFolder)
                {
                    LogProcessor.LogSourceFolder = LogSourceFolder;
                    // copy eveything to working folder so we don't modify originals
                    CopyLogFilesToWorkingFolder();
                }

                PrintManager.InspectionFolder = InspectFolder;
                PrintManager.WorkingFolder = WorkingFolder;
                PrintManager.ReportFolder = ReportFolder;
                PrintManager.ReviewFolder = ReviewFolder;
                PrintManager.ScoreFolder = ScoreFolder;

                LogProcessor.WorkingFolder = WorkingFolder;
                LogProcessor.InspectionFolder = InspectFolder;

                int fileCount = ContestLogFileList.Cast<object>().Count();
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
            if (!Directory.Exists(WorkingFolder))
            {
                Directory.CreateDirectory(WorkingFolder);
            }
            else
            {
                Directory.Delete(WorkingFolder, true);
                Directory.CreateDirectory(WorkingFolder);
            }

            if (!Directory.Exists(InspectFolder))
            {
                Directory.CreateDirectory(InspectFolder);
            }

            if (!Directory.Exists(ReportFolder))
            {
                Directory.CreateDirectory(ReportFolder);
            }

            if (!Directory.Exists(ReviewFolder))
            {
                Directory.CreateDirectory(ReviewFolder);
            }

            if (!Directory.Exists(ScoreFolder))
            {
                Directory.CreateDirectory(ScoreFolder);
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
                int fileCount = LogProcessor.BuildFileList(out ContestLogFileList);
                if (fileCount > 0)
                {
                    foreach (FileInfo fileInfo in ContestLogFileList)
                    {
                        fileName = fileInfo.Name;
                        if (fileName != null)
                        {
                            File.Copy(fileInfo.FullName, Path.Combine(WorkingFolder, fileName), true);
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

            // cleanup for next run
            LogProcessor.CallDictionary = new Dictionary<string, List<ContestLog>>();
            LogProcessor.NameDictionary = new Dictionary<string, List<Tuple<string, int>>>();

            ContestLogFileList = ContestLogFileList.OrderBy(x => x.FullName.ToUpper());

            foreach (FileInfo fileInfo in ContestLogFileList)
            {
                fileName = LogProcessor.BuildContestLog(fileInfo, ContestLogs, Session);
                if (fileName != null)
                {
                    UpdateListViewLoad(fileName, "Load failed." + " - " + LogProcessor.FailReason, false);
                }
            }
        }

        /// <summary>
        /// Update the progress bar.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="progress"></param>
        private void LogProcessor_OnProgressUpdate(int progress)
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
                foreach (ContestLog log in ContestLogs)
                {
                   LogProcessor.RefineHQPEntities(log);
                }

                List<string> qsosToInspect = LogProcessor.QSOStoInspect;
                if (qsosToInspect.Count > 0)
                {
                    PrintManager.PrintHHawaiiQSOStoInspect(qsosToInspect);
                }

                UpdateListViewLoad(ContestLogs.Count.ToString() + " logs loaded.", "", false);
                EnableControl(ButtonPreScoreReports, true);
                Cursor = Cursors.Default;
            }

            UpdateLabel("Load contest logs completed");

            EnableControl(ButtonStartAnalysis, true);
            EnableControl(buttonLogSearch, true);

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

            ContestLogs = ContestLogs.OrderBy(x => x.LogOwner).ToList();

            LogAnalyser.ActiveContest = ActiveContest;
            LogAnalyser.PreAnalyzeContestLogs(ContestLogs, LogProcessor.CallDictionary, LogProcessor.NameDictionary);

            UpdateListViewAnalysis("Analysis completed", "----------", "----------", false);
            ResetProgressBar(true);
        }

        /// <summary>
        /// Update the list view with the call sign last processed.
        /// </summary>
        /// <param name="value"></param>
        private void LogAnalyser_OnProgressUpdate(string value, string qsoCount, string validQsoCount, int progress)
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
                    UpdateLabel("Analysing Contest Logs");
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
                ProgressBarLoad.Maximum = ContestLogs.Count;
                ProgressBarLoad.Value = ContestLogs.Count;

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
            switch (ActiveContest)
            {
                case ContestName.CW_OPEN:
                    CWOpen.ScoreContestLogs(ContestLogs);
                    break;
                case ContestName.HQP:
                    HQP.ScoreContestLogs(ContestLogs);
                    break;
            }
        }

        private void CWOpen_OnProgressUpdate(ContestLog contestLog, int progress)
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
                ProgressBarLoad.Maximum = ContestLogs.Count;
                ProgressBarLoad.Value = ContestLogs.Count;

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
            LogSourceFolder = string.Empty;

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
                BeginInvoke(new Action<string, string, bool>(this.UpdateListViewLoad), message, status, clear);
                return;
            }

            if (clear)
            {
                ListViewLoad.Items.Clear();
            }
            else
            {
                ListViewItem item = new ListViewItem(message);
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
            int validQsoCount = 0;

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
                //item.SubItems.Add((contestLog.Multipliers * contestLog.TotalPoints).ToString());
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
                BeginInvoke(new Action<string, bool>(this.UpdateListViewScore), message, clear);
                return;
            }

            if (clear)
            {
                ListViewScore.Items.Clear();
            }
            else
            {
                ListViewItem item = new ListViewItem(message);
                _ = ListViewScore.Items.Insert(0, item);
            }
        }

        /// <summary>
        ///  Of course it would not necessarily have ALL QSOs the guy made, 
        ///  but probably most of them. From that I can see trends for band, mode, etc. 
        /// </summary>
        /// <param name="contestLog"></param>
        /// <param name="clear"></param>
        private void UpdateListViewLogSearch(QSO qso, bool clear)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<QSO, bool>(this.UpdateListViewLogSearch), qso, clear);
                return;
            }

            if (clear)
            {
                ListViewLogSearch.Items.Clear();
            }
            else
            {

                ListViewItem item = new ListViewItem(qso.OperatorCall);
                item.SubItems.Add(qso.ContactCall);
                item.SubItems.Add(qso.Band.ToString());
                item.SubItems.Add(EnumHelper.GetDescription(qso.Mode));
                item.SubItems.Add(qso.QSODateTime.ToString());
                item.SubItems.Add(qso.RawQSO);
                ListViewLogSearch.Items.Insert(0, item);
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
                BeginInvoke(new Action<int>(this.UpdateProgress), count);
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
                BeginInvoke(new Action<Control, bool>(this.EnableControl), control, clear);
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
                BeginInvoke(new Action<bool>(this.ResetProgressBar), clear);
                return;
            }

            ProgressBarLoad.Value = 0;
            ProgressBarLoad.Maximum = 0;
            ProgressBarLoad.Maximum = ContestLogs.Count;
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
                logList = ContestLogs.Where(a => a.LogOwner == callsign).ToList();

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

        /// <summary>
        /// 
        /// </summary>
        private void PrintQSORejectReport()
        {
            try
            {
                foreach (ContestLog contestLog in ContestLogs)
                {
                    PrintManager.PrintLogSummaryReport(contestLog, contestLog.LogOwner);
                }
            }
            catch (Exception ex)
            {
                // later just return a message to show in listview
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void PrintQSOReviewReport()
        {
            try
            {
                PrintManager.PrintReviewReport(ContestLogs);
            }
            catch (Exception ex)
            {
                // later just return a message to show in listview
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Print the final score report for the CWOpen and HQP contest.
        /// </summary>
        private void PrintFinalScoreReport()
        {
            PDFGenerator pdfGenerator = new PDFGenerator(ActiveContest)
            {
                ScoreFolder = ScoreFolder
            };

            try
            {
                switch (ActiveContest)
                {
                    case ContestName.CW_OPEN:
                        PrintManager.PrintCWOpenCsvFile(ContestLogs);
                        pdfGenerator.PrintCWOpenPdfScoreSheet(ContestLogs);
                        break;
                    case ContestName.HQP:
                        PrintManager.PrintHQPCsvFileEx(ContestLogs);
                        pdfGenerator.PrintHQPPdfScoreSheet(ContestLogs);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        #endregion

        #region Bad Call List

        private void ButtonLoadBadcalls_Click(object sender, EventArgs e)
        {
            TabControlMain.SelectTab(TabPageLogStatus);
            LoadBadCallList();
        }


        /// <summary>
        /// Load a CSV file with a list of known bad calls.
        /// </summary>
        private void LoadBadCallList()
        {
            ILookup<string, string> badCallList;

            try
            {
                badCallList = LoadFile();
                LogAnalyser.BadCallList = badCallList;
                UpdateListViewLoad("Loaded bad call file.", badCallList.Count.ToString() + " entries.", false);
            }
            catch (Exception ex)
            {
                UpdateListViewLoad("Unable to load bad call file.", ex.Message, false);
            }
        }

        /// <summary>
        /// Load the bad call list.
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
            string session = Session.ToString();

            // list of all call/name pairs
            UpdateListViewLoad("List Unique Call Name Pairs", "", false);
            PrintManager.PrintPreAnalysisReport(ReportFolder, session, ContestLogs);

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
            if (ContestLogs == null || ContestLogs.Count() == 0)
            {
                MessageBox.Show("You must load and analyze the logs before you can compare.", "Missing Log Files", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(TextBoxLog1.Text) || string.IsNullOrEmpty(TextBoxLog2.Text))
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
            string message = string.Format("The log for {0} could not be found.", call1);

            ListViewCompare.Items.Clear();
            ListViewCompare.Groups.Clear();

            ListViewCompare.Groups.Add(new ListViewGroup(call1, HorizontalAlignment.Left));
            ListViewCompare.Groups.Add(new ListViewGroup(call2, HorizontalAlignment.Left));

            TabControlMain.SelectTab(TabPageCompare);

            log1 = (ContestLog)ContestLogs.FirstOrDefault(q => q.LogOwner == call1);
            log2 = (ContestLog)ContestLogs.FirstOrDefault(q => q.LogOwner == call2);

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
                message = string.Format("The log for {0} could not be found.", call2);
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

            switch (ActiveContest)
            {
                case ContestName.CW_OPEN:
                    item.SubItems.Add(EnumHelper.GetDescription(qso.Mode));
                    item.SubItems.Add(qso.QsoDate.ToString());
                    item.SubItems.Add(qso.QsoTime.ToString());
                    item.SubItems.Add(qso.SentSerialNumber.ToString());
                    item.SubItems.Add(qso.OperatorEntity);
                    item.SubItems.Add(qso.ContactCall);
                    item.SubItems.Add(qso.ReceivedSerialNumber.ToString());
                    item.SubItems.Add(qso.ContactName);
                    break;
                case ContestName.HQP:
                    item.SubItems.Add(EnumHelper.GetDescription(qso.Mode));
                    item.SubItems.Add(qso.QsoDate.ToString());
                    item.SubItems.Add(qso.QsoTime.ToString());
                    item.SubItems.Add("");
                    item.SubItems.Add(qso.OperatorEntity);
                    item.SubItems.Add(qso.ContactCall);
                    item.SubItems.Add("");
                    item.SubItems.Add(qso.ContactEntity);
                    break;
            }

            ListViewCompare.Items.Insert(0, item);
        }

        #endregion

        #region Search Logs

        /// <summary>
        /// Search for all logs a specific call sign is in.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonLogSearch_Click(object sender, EventArgs e)
        {
            List<QSO> callList = new List<QSO>();
            string sourceCall = textBoxLogSearch.Text.Trim();

            TabControlMain.SelectTab(TabPageSearchLogs);
            ListViewLogSearch.Items.Clear();

            if (sourceCall != "")
            {
                foreach (ContestLog contestLog in ContestLogs)
                {
                    List<QSO> qsoList = contestLog.QSOCollection.Where(q => q.ContactCall == sourceCall).ToList();
                    if (qsoList.Count > 0)
                    {
                        foreach (QSO qso in qsoList)
                        {
                            UpdateListViewLogSearch(qso, false);
                        }
                    }
                }
            }
        }

        #endregion

    } // end class
}
