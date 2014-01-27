using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using W6OP.ContestLogAnalyzer;

namespace ContestLogAnalyzer
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
        private List<ContestLog> _ContestLogs;
        private LogHeader _LogHeader;
        private QSO _QSO;

        private string _LogFolder = null;
        //private List<string> _LogFileList;

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
            //string guidString = Guid.NewGuid().ToString();

            _LogFolder = TextBoxLogFolder.Text;
        }


        #region Select Log folder

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonSelectFolder_Click(object sender, EventArgs e)
        {
            SelectWorkingFolder();
        }

        /// <summary>
        /// 
        /// </summary>
        private void SelectWorkingFolder()
        {
            LogFolderBrowserDialog.ShowDialog();
            if (!String.IsNullOrEmpty(LogFolderBrowserDialog.SelectedPath))
            {
                _LogFolder = LogFolderBrowserDialog.SelectedPath;
                TextBoxLogFolder.Text = _LogFolder;
            }
        }

        #endregion

        #region Start Log Analysis

        /// <summary>
        /// Handle the click event for the ButtonAnalyzeLog button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonAnalyzeLog_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(_LogFolder))
            {
                _ContestLogs = new List<ContestLog>();
                BuildFileList();
            }
            else
            {
                MessageBox.Show("You must selct a folder containg log files.", "Missing Folder Name", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        IEnumerable<System.IO.FileInfo> _LogFileList;

        /// <summary>
        /// Create a list of all of the log files in the working folder. Once the list is
        /// filled pass the list on to another thread.
        /// </summary>
        private void BuildFileList()
        {
            //string fileFullName = null;
            Int32 fileCount;

            // Take a snapshot of the file system. http://msdn.microsoft.com/en-us/library/bb546159.aspx
            DirectoryInfo dir = new DirectoryInfo(_LogFolder);

            // This method assumes that the application has discovery permissions for all folders under the specified path.
            IEnumerable<FileInfo> fileList = dir.GetFiles("*.log", System.IO.SearchOption.TopDirectoryOnly);

            //Create the query
            _LogFileList =
                from file in fileList
                where file.Extension.ToLower() == ".log"
                orderby file.CreationTime ascending
                select file;

            fileCount = fileList.Cast<object>().Count();

            Cursor = Cursors.WaitCursor;
            BackgroundWorkerAnalyze.RunWorkerAsync(fileCount);
        }



        #endregion

        #region Analyze Header and Footer

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonValidateHeader_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// See if there is a header and a footer - CHECK HOW TO READ FILE WITH LINQ FROM PREFIX LIST ON HAIDA
        /// Read the header
        /// 
        /// </summary>
        /// <param name="fileInfo"></param>
        private void BuildContestLog(FileInfo fileInfo)
        {
            ContestLog log = new ContestLog();
            string fullName = fileInfo.FullName;
            string version = null;

            if (File.Exists(fullName))
            {
                _LogHeader = new LogHeader();
                _QSO = new QSO();

                List<string> lineList = File.ReadAllLines(fullName).Select(i => i.ToString()).ToList();

                version = lineList.Where(l => l.StartsWith("START-OF-LOG:")).FirstOrDefault().Substring(13).Trim();

                if (version == "2.0")
                {
                    //BuildHeaderV2(lineList);
                    return;
                }
                else if (version == "3.0")
                {
                    BuildHeaderV3(lineList, fullName);
                }
                else
                {
                    // handle unsupported version
                }


                log.LogHeader = _LogHeader;

                // Find DUPES in list
                //http://stackoverflow.com/questions/454601/how-to-count-duplicates-in-list-with-linq

                // this statement says to copy all QSO lines
                lineList = lineList.Where(x => x.IndexOf("QSO:", 0) != -1).ToList();
                List<QSO> qsoList = CollectQSOs(lineList);

                log.QSOCollection = qsoList;
                log.LogIsValid = true;
                if (log.LogHeader.OperatorCategory == CategoryOperator.CheckLog)
                {
                    log.IsCheckLog = true;
                }
                _ContestLogs.Add(log);

            }
        }

        /// <summary>
        /// LINQ SAMPLES
        /// http://code.msdn.microsoft.com/101-LINQ-Samples-3fb9811b
        /// 
        /// THERE ARE SOME ITEMS MISSING
        /// DATE, TIME
        /// TIME OFF
        /// </summary>
        /// <param name="lineList"></param>
        /// <param name="match"></param>
        private void BuildHeaderV3(List<string> lineList, string logFileName)
        {
            try
            {
                // MAY HAVE TO LOOK AT VERSION OF LOG AND FORK


                // Merge the data sources using a named type. 
                // var could be used instead of an explicit type.
                IEnumerable<LogHeader> logHeader =
                    from line in lineList
                    select new LogHeader()
                    {
                        LogFileName = logFileName,
                        Version = lineList.Where(l => l.StartsWith("START-OF-LOG:")).FirstOrDefault().Substring(13).Trim(),
                        Location = lineList.Where(l => l.StartsWith("LOCATION:")).DefaultIfEmpty("LOCATION: Unknown").First().Substring(9).Trim(),
                        OperatorCallSign = CheckForNull(lineList.Where(l => l.StartsWith("CALLSIGN:")).DefaultIfEmpty("CALLSIGN: UNKNOWN").First(), 9, "UNKNOWN"),
                        OperatorCategory = Utility.GetValueFromDescription<CategoryOperator>(lineList.Where(l => l.StartsWith("CATEGORY-OPERATOR:")).DefaultIfEmpty("CATEGORY-OPERATOR: UNKNOWN").First().Substring(18).Trim().ToUpper()),
                        // this is for when the CATEGORY-ASSISTED: is missing or has no value
                        Assisted = Utility.GetValueFromDescription<CategoryAssisted>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-ASSISTED:")).DefaultIfEmpty("CATEGORY-ASSISTED: UNKNOWN").First(), 18, "UNKNOWN")),   //.Substring(18).Trim().ToUpper()),
                        Band = Utility.GetValueFromDescription<CategoryBand>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-BAND:")).DefaultIfEmpty("CATEGORY-BAND: ALL").First(), 14, "ALL")),
                        Power = Utility.GetValueFromDescription<CategoryPower>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-POWER:")).DefaultIfEmpty("CATEGORY-POWER: UNKNOWN").First(), 15, "UNKNOWN")),
                        Mode = Utility.GetValueFromDescription<CategoryMode>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-MODE:")).DefaultIfEmpty("CATEGORY-MODE: MIXED").First(), 14, "MIXED")),
                        Station = Utility.GetValueFromDescription<CategoryStation>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-STATION:")).DefaultIfEmpty("CATEGORY-STATION: UNKNOWN").First(), 17, "UNKNOWN")),
                        Transmitter = Utility.GetValueFromDescription<CategoryTransmitter>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-TRANSMITTER:")).DefaultIfEmpty("CATEGORY-TRANSMITTER: UNKNOWN").First(), 21, "UNKNOWN")),
                        ClaimedScore = Convert.ToInt32(CheckForNull(lineList.Where(l => l.StartsWith("CLAIMED-SCORE:")).DefaultIfEmpty("CLAIMED-SCORE: 0").First(), 14, "0")),
                        Club = CheckForNull(lineList.Where(l => l.StartsWith("CLUB:")).DefaultIfEmpty("CLUB: NONE").First(), 5, "NONE"),
                        Contest = Utility.GetValueFromDescription<ContestName>(lineList.Where(l => l.StartsWith("CONTEST:")).FirstOrDefault().Substring(9).Trim().ToUpper()),
                        CreatedBy = CheckForNull(lineList.Where(l => l.StartsWith("CREATED-BY:")).DefaultIfEmpty("CREATED-BY: NONE").First(), 11, "NONE"),
                        PrimaryName = CheckForNull(lineList.Where(l => l.StartsWith("NAME:")).DefaultIfEmpty("NAME: NONE").First(), 5, "NONE"),
                        NameSent = CheckForNull(lineList.Where(l => l.StartsWith("Name Sent")).DefaultIfEmpty("Name Sent NONE").First(), 10, "NONE"),
                        // need to work on address
                        Operators = lineList.Where(l => l.StartsWith("OPERATORS:")).ToList(),
                        //SoapBox = lineList.Where(l => l.StartsWith("SOAPBOX:")).FirstOrDefault().Substring(7).Trim()
                    };

                _LogHeader = logHeader.FirstOrDefault();

                string b = "";
            }
            catch (Exception ex)
            {
                string a = ex.Message;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="len"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private string CheckForNull(string s, Int32 len, string defaultValue)
        {
            if (s.Trim().Length <= len)
            {
                return defaultValue;
            }

            return s.Substring(len).Trim().ToUpper();
        }

        private void BuildHeaderV2(List<string> lineList)
        {
            try
            {

                // LATER NEED TO EXAMINE ALL OF THE HEADERS AND FIX THE "UNKOWN" ENTRIES

                // Merge the data sources using a named type. 
                // var could be used instead of an explicit type.
                IEnumerable<LogHeader> logHeader =
                    from line in lineList
                    select new LogHeader()
                    {
                        Version = lineList.Where(l => l.StartsWith("START-OF-LOG:")).FirstOrDefault().Substring(13).Trim(),
                        Location = lineList.Where(l => l.StartsWith("LOCATION:")).DefaultIfEmpty("LOCATION: Unknown").First().Substring(9).Trim(),
                        OperatorCallSign = lineList.Where(l => l.StartsWith("CALLSIGN:")).FirstOrDefault().Substring(9).Trim().ToUpper(),
                        OperatorCategory = Utility.GetValueFromDescription<CategoryOperator>(lineList.Where(l => l.StartsWith("CATEGORY-OPERATOR:")).FirstOrDefault().Substring(18).Trim().ToUpper()),
                        Assisted = Utility.GetValueFromDescription<CategoryAssisted>(lineList.Where(l => l.StartsWith("CATEGORY-ASSISTED:")).DefaultIfEmpty("CATEGORY-ASSISTED: UNKNOWN").First().Substring(18).Trim().ToUpper()),
                        //Band = Utility.GetValueFromDescription<CategoryBand>(lineList.Where(l => l.StartsWith("CATEGORY-BAND:")).FirstOrDefault().Substring(14).Trim().ToUpper()),
                        //Power = Utility.GetValueFromDescription<CategoryPower>(lineList.Where(l => l.StartsWith("CATEGORY-POWER:")).FirstOrDefault().Substring(15).Trim().ToUpper()),
                        //Mode = Utility.GetValueFromDescription<CategoryMode>(lineList.Where(l => l.StartsWith("CATEGORY-MODE:")).FirstOrDefault().Substring(14).Trim().ToUpper()),
                        //Station = Utility.GetValueFromDescription<CategoryStation>(lineList.Where(l => l.StartsWith("CATEGORY-STATION:")).FirstOrDefault().Substring(17).Trim().ToUpper()),
                        //Transmitter = Utility.GetValueFromDescription<CategoryTransmitter>(lineList.Where(l => l.StartsWith("CATEGORY-TRANSMITTER:")).FirstOrDefault().Substring(21).Trim().ToUpper()),
                        //ClaimedScore = Convert.ToInt32(lineList.Where(l => l.StartsWith("CLAIMED-SCORE:")).FirstOrDefault().Substring(14).Trim()),
                        //Club = lineList.Where(l => l.StartsWith("CLUB:")).FirstOrDefault().Substring(5).Trim(),
                        //Contest = Utility.GetValueFromDescription<ContestName>(lineList.Where(l => l.StartsWith("CONTEST:")).FirstOrDefault().Substring(9).Trim().ToUpper()),
                        //CreatedBy = lineList.Where(l => l.StartsWith("CREATED-BY:")).FirstOrDefault().Substring(11).Trim(),
                        //PrimaryName = lineList.Where(l => l.StartsWith("NAME:")).FirstOrDefault().Substring(5).Trim(),
                        //NameSent = lineList.Where(l => l.StartsWith("Name Sent")).FirstOrDefault().Substring(10).Trim(),
                        // need to work on address
                        //Operators = lineList.Where(l => l.StartsWith("OPERATORS:")).ToList(),
                        //SoapBox = lineList.Where(l => l.StartsWith("SOAPBOX:")).FirstOrDefault().Substring(7).Trim()
                    };

                _LogHeader = logHeader.FirstOrDefault();
            }
            catch (Exception ex)
            {
                string a = ex.Message;
            }
        }


        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/bb513866.aspx
        /// </summary>
        /// <param name="lineList"></param>
        /// <returns></returns>
        private List<QSO> CollectQSOs(List<string> lineList)
        {
            List<QSO> qsoList = null; ;

            try
            {
                IEnumerable<QSO> qso =
                     from line in lineList
                     let splitName = line.Split(' ')
                     select new QSO()
                     {
                         Frequency = splitName[1],  //lineList.GroupBy(x => x.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries)).ToString(),
                         Mode = splitName[2],//lineList.GroupBy(x => x.Split(' ')[2]).ToString(),
                         QsoDate = splitName[3],//lineList.GroupBy(x => x.Split(' ')[3]).ToString(),
                         QsoTime = splitName[4],    //lineList.GroupBy(x => x.Split(' ')[4]).ToString(),
                         OperatorCall = splitName[5],    //lineList.GroupBy(x => x.Split(' ')[5]).ToString(),
                         SentSerialNumber = ConvertSerialNumber(splitName[6]),    //lineList.GroupBy(x => x.Split(' ')[6]).ToString(),
                         OperatorName = splitName[7],    //lineList.GroupBy(x => x.Split(' ')[7]).ToString(),
                         ContactCall = splitName[8],    //lineList.GroupBy(x => x.Split(' ')[8]).ToString(),
                         ReceivedSerialNumber = ConvertSerialNumber(splitName[9]),  //lineList.GroupBy(x => x.Split(' ')[9]).ToString(),
                         ContactName = splitName[10],   //lineList.GroupBy(x => x.Split(' ')[10]).ToString()
                         CallIsValid = CheckCallSignFormat(splitName[5])
                     };

                qsoList = qso.ToList();

            }
            catch (Exception ex)
            {
                string a = ex.Message;
            }

            return qsoList;


            //QSO: 14027 CW 2013-08-31 0005 W0BR 1 BOB W4BQF 10 TOM
        }

        /// <summary>
        /// Convert a string to an Int32. Also extract a number from a string as a serial
        /// number may be O39 instead of 039.
        /// </summary>
        /// <param name="serialNumber"></param>
        /// <returns></returns>
        private Int32 ConvertSerialNumber(string serialNumber)
        {
            Int32 number = 0; // indicates invalid serial number

            try
            {
                serialNumber = Regex.Match(serialNumber, @"\d+").Value; ;
                number = Convert.ToInt32(serialNumber);
            }
            catch (Exception ex)
            {
                string a = ex.Message;
            }

            return number;
        }

        /// <summary>
        /// Quick check to see if a call sign is formatted correctly.
        /// </summary>
        /// <param name="call"></param>
        /// <returns></returns>
        private bool CheckCallSignFormat(string call)
        {
            string regex = @"^([A-Z]{1,2}|[0-9][A-Z])([0-9])([A-Z]{1,3})$";
            bool match = false;

            if (Regex.IsMatch(call.ToUpper(), regex, RegexOptions.IgnoreCase))
            {
                match = true;
            }

            return match;
        }

        #endregion

        #region Background Worker

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerAnalyze_DoWork(object sender, DoWorkEventArgs e)
        {
            foreach (FileInfo fileInfo in _LogFileList)
            {
                BuildContestLog(fileInfo);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerAnalyze_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerAnalyze_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string a = "";
            Cursor = Cursors.Default;
        }

        #endregion

        /// <summary>
        /// Open first log
        /// get first QSO
        /// get Contact call sign
        /// see if it is in any other logs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            SearchForMatchingLogs();
        }

        /// <summary>
        /// This will probably go on another thread.
        /// Start processing individual logs.
        /// First we will find all of the logs that have this operators call sign and add a pointer
        /// to them in this log.
        /// </summary>
        private void SearchForMatchingLogs()
        {
            string call = null;

            foreach (ContestLog log in _ContestLogs)
            {
                call = log.LogHeader.OperatorCallSign;
                UpdateListView(call);

                log.MatchingLogs = _ContestLogs.Where(q => q.QSOCollection.Any(a => a.ContactCall == call)).ToList();

            }

            // now I have logs ready to process
            ProcessContestLogs();
        }

        /// <summary>
        /// Open first log.
        /// look at first QSO.
        /// Do we already have a reference to it in our matching log collection, may have multiple
        /// if we have multiple, check band and sent?
        /// If it is in our matching collection, look in the log it belongs to for sent/recv. S/N, Band, etc - did we already do this?
        /// Does it show up in any other logs? Good check to see if it is a valid callsign
        /// Build a collection of totaly unique calls
        /// </summary>
        private void ProcessContestLogs()
        {
            List<QSO> qsoList = null;
            List<QSO> qsos = null;
            string call = null;

            foreach (ContestLog log in _ContestLogs)
            {
                if (!log.LogIsCheckLog)
                {
                    call = log.LogHeader.OperatorCallSign;
                    AnalyzeQSOs(log);
                    //qsos = log.MatchingLogs
                    qsoList = log.QSOCollection.Where(a => a.ContactCall == call).ToList();
                }
            }
        }

        /// <summary>
        /// Look at the QSOs and see if they are valid
        /// need all the QSOs from two lists
        /// the qsos that match between the two logs
        /// </summary>
        /// <param name="log"></param>
        private void AnalyzeQSOs(ContestLog log)
        {
            List<QSO> qsoList;
            List<QSO> matchList = null;
            string logOwnerCall = log.LogHeader.OperatorCallSign;
            string operatorCall = null;
            string sentName = null;
            Int32 sent = 0;
            Int32 received = 0;
            Int32 band = 0;
            bool valid = true;


            foreach (ContestLog contestLog in log.MatchingLogs)
            {
                // list of Qs from the other guy
                qsoList = contestLog.QSOCollection.Where(a => a.ContactCall == logOwnerCall).ToList();
                // now query this guys log for matches
                foreach (QSO qso in qsoList)
                {
                    operatorCall = qso.OperatorCall;
                    sent = qso.SentSerialNumber;
                    received = qso.ReceivedSerialNumber;
                    band = qso.Band;
                    sentName = qso.OperatorName; // check on this
                    valid = true;

                    // could there be more than one returned?
                    matchList = log.QSOCollection.Where(a => a.ContactCall == operatorCall && a.SentSerialNumber == received && a.Band == band && a.ContactName == sentName).ToList<QSO>();
                    foreach (QSO q in matchList)
                    {
                        q.QSOIsValid = valid;
                        valid = false;
                        if (!q.QSOIsValid)
                        {
                            // later expand this with more information
                            q.RejectReason = "Dupe";
                        }
                    }
                    string z = "";
                }
            }

        }



        /// <summary>
        /// http://stackoverflow.com/questions/721395/linq-question-querying-nested-collections
        /// http://stackoverflow.com/questions/15996168/linq-query-to-filter-items-by-criteria-from-multiple-lists
        /// </summary>
        /// <param name="call"></param>
        /// <param name="qso"></param>
        private void SearchForLogs(string call)
        {
            List<QSO> qsoList = null;

            try
            {
                //List<QSO> logList = _ContestLogs.SelectMany(q => q.QSOCollection).Where(a => a.ContactCall == call).ToList();
                // get list of logs this call sign is in
                List<ContestLog> logList = _ContestLogs.Where(q => q.QSOCollection.Any(a => a.ContactCall == call)).ToList();


                foreach (ContestLog log in logList)
                {
                    //UpdateListView(log.LogHeader.OperatorCallSign);

                    // this works
                    qsoList = log.QSOCollection.Where(a => a.ContactCall == call).ToList();
                }

                //List<QSO> qsoList = logList.Where(q => q.QSOCollection)
                //var asd = question.Where(q => q.Answers.Any(a => a.Name == "SomeName"))

                string c = "";
            }
            catch (Exception ex)
            {
                string b = ex.Message;
            }
        }

        #region Update ListView

        private void UpdateListView(string message)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<string>(this.UpdateListView), message);
                return;
            }

            ListViewItem item = new ListViewItem(message);
            listView1.Items.Insert(0, item);
        }


        #endregion

        //if (InvokeRequired)
        //   {
        //       this.BeginInvoke(new Action<string, DXAFileType>(this.MoveFileToBackupDirectory), uploadFile, fileType);
        //       return;
        //   }

    } // end class
}
