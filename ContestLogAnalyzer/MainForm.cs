using System;
using System.Collections;
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
        //private LogHeader _LogHeader;
        //private QSO _QSO;

        //private CWOpen _CWOpenScorer;

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
        private void ButtonLoadLogs_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(_LogFolder))
            {
                _ContestLogs = new List<ContestLog>();
                BuildFileList();

                ButtonStartAnalysis.Enabled = true;
            }
            else
            {
                MessageBox.Show("You must select a folder containing log files.", "Missing Folder Name", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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

        #region Load and PreProcess Logs

        /// <summary>
        /// See if there is a header and a footer
        /// Read the header
        /// 
        /// </summary>
        /// <param name="fileInfo"></param>
        private void BuildContestLog(FileInfo fileInfo)
        {
            ContestLog log = new ContestLog();
            string fullName = fileInfo.FullName;
            string version = null;

            try
            {
                if (File.Exists(fullName))
                {
                    //LogHeader logHeader = new LogHeader();
                    //QSO qso = new QSO();

                    List<string> lineList = File.ReadAllLines(fullName).Select(i => i.ToString()).ToList();

                    version = lineList.Where(l => l.StartsWith("START-OF-LOG:")).FirstOrDefault().Substring(13).Trim();

                    if (version == "2.0")
                    {
                        log.LogHeader = BuildHeaderV2(lineList, fullName);
                    }
                    else if (version == "3.0")
                    {
                        log.LogHeader = BuildHeaderV3(lineList, fullName);
                    }
                    else
                    {
                        // handle unsupported version
                    }


                    // Find DUPES in list
                    //http://stackoverflow.com/questions/454601/how-to-count-duplicates-in-list-with-linq

                    // this statement says to copy all QSO lines
                    lineList = lineList.Where(x => x.IndexOf("QSO:", 0) != -1).ToList();
                    log.QSOCollection = CollectQSOs(lineList);

                    //log.QSOCollection = qsoList;
                    log.IsValidLog = true;
                    if (log.LogHeader.OperatorCategory == CategoryOperator.CheckLog)
                    {
                        log.IsCheckLog = true;
                    }

                    _ContestLogs.Add(log);
                }
            }
            catch (Exception ex)
            {
                string a = ex.Message;
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
        private LogHeader BuildHeaderV3(List<string> lineList, string logFileName)
        {
            //try
            //{
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

            return logHeader.FirstOrDefault();
            //}
            //catch (Exception ex)
            //{
            //    string a = ex.Message;
            //}
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


        /// <summary>
        /// While this works I may want to leave out fields that are unsupported.
        /// Or just consolidate into the V3 method since it is identical at the moment.
        /// </summary>
        /// <param name="lineList"></param>
        /// <param name="logFileName"></param>
        private LogHeader BuildHeaderV2(List<string> lineList, string logFileName)
        {
            //try
            //{

            // LATER NEED TO EXAMINE ALL OF THE HEADERS AND FIX THE "UNKOWN" ENTRIES

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

            return logHeader.FirstOrDefault();
            //}
            //catch (Exception ex)
            //{
            //    string a = ex.Message;
            //}
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

            string a = "";
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
        private void ButtonStartAnalysis_Click(object sender, EventArgs e)
        {
            UpdateListView("", true);
            PreProcessContestLogs();

            // now score each log
            ScoreLogs();
        }

        private void ScoreLogs()
        {
            CWOpen cwOpen = new CWOpen();

            foreach (ContestLog contestLog in _ContestLogs)
            {
                cwOpen.CalculateScore(contestLog);
            }



        }

        /// <summary>
        /// This will probably go on another thread.
        /// Start processing individual logs.
        /// Find all of the logs that have the log owners call sign in them.
        /// Find all the logs that do not have a reference to this log.
        /// Now each log is self contained and can be operated on without opening other files.
        /// </summary>

        /// <summary>
        /// Open first log.
        /// look at first QSO.
        /// Do we already have a reference to it in our matching log collection, may have multiple
        /// if we have multiple, check band and sent?
        /// If it is in our matching collection, look in the log it belongs to for sent/recv. S/N, Band, etc - did we already do this?
        /// Does it show up in any other logs? Good check to see if it is a valid callsign
        /// Build a collection of totaly unique calls
        /// </summary>
        private void PreProcessContestLogs()
        {
            string call = null;
            Int32 count = 0; // QSOs processed?

            foreach (ContestLog contestLog in _ContestLogs)
            {
                call = contestLog.LogOwner;
                UpdateListView(call, false);

                if (!contestLog.IsCheckLog)
                {
                    count = CollateLogs(contestLog, count);
                }
            }

            // now I should every log with it's matching QSOs, QSOs to be checked and all the other logs with a reference
            string h = "";
        }

        /// <summary>
        /// This is the first pass and I am mostly interested in collating the logs.
        /// For each QSO in the log build a collection of all of the logs that have at least one exact match.
        /// Next, build a collection of all of the logs that have a call sign match but where the other information
        /// does not match. Later in the process we may get an exact match and then will want to remove that QSO from 
        /// the Review collection.
        /// Then get a collection of all logs where there was not a match at all. On the first pass there will be a lot 
        /// of duplicates because a log is added for each QSO that matches. Later I'll remove the duplicates.
        /// </summary>
        /// <param name="contestLog"></param>
        private Int32 CollateLogs(ContestLog contestLog, Int32 count)
        {
            List<ContestLog> matchingLogs = new List<ContestLog>();
            List<ContestLog> reviewLogs = new List<ContestLog>();
            List<ContestLog> otherLogs = new List<ContestLog>();
            string operatorCall = contestLog.LogOwner;
            string sentName = null;
            Int32 sentSerialNumber = 0;
            Int32 received = 0;
            Int32 band = 0;
            Int32 qsoCount = 0;
            Int32 otherCount = 0;
            Int32 reviewCount = 0;

            foreach (QSO qso in contestLog.QSOCollection)
            {
                // query all the other logs for a match
                // if there is a match, mark each QSO as valid.
                if (qso.Status == QSOStatus.InvalidQSO)
                {
                    sentSerialNumber = qso.SentSerialNumber;
                    received = qso.ReceivedSerialNumber;
                    band = qso.Band;
                    sentName = qso.OperatorName;

                    // get logs that have at least a partial match
                    // List<ContestLog> partialMatch = contestLog.Where(q => q.QSOCollection.Any(a => a.ContactCall == call)).ToList();

                    // get all the QSOs that match
                    //List<QSO> qsoList = contestLog.QSOCollection.Where(q => q.ContactCall == operatorCall && q.ReceivedSerialNumber == sent && q.Band == band && q.ContactName == sentName && q.Status == QSOStatus.InvalidQSO).ToList(); 
                    // get all the logs that have at least one exact match
                    //List<ContestLog> list = _ContestLogs.Where(q => q.QSOCollection.Any(a =>  a.Band == band)).ToList();

                    matchingLogs.AddRange(_ContestLogs.Where(q => q.QSOCollection.Any(a => a.ContactCall == operatorCall && a.ReceivedSerialNumber == sentSerialNumber && a.Band == band && a.ContactName == sentName && a.Status == QSOStatus.InvalidQSO)).ToList()); // && a.IsValidQSO == false
                    // some of these will be marked valid as we go along and need to be removed from this collection
                    //reviewLogs.AddRange(_ContestLogs.Where(q => q.QSOCollection.Any(a => a.ContactCall == operatorCall && (a.ReceivedSerialNumber != sentSerialNumber || a.Band == band || a.ContactName == sentName && a.Status == QSOStatus.InvalidQSO))).ToList());
                    // logs where there was no match at all - exclude this logs owner too
                    otherLogs.AddRange(_ContestLogs.Where(q => q.QSOCollection.All(a => a.ContactCall != operatorCall && a.OperatorCall != operatorCall)).ToList());

                    // need to determine if the matching log count went up, if it did, mark the QSO as valid
                    if (matchingLogs.Count > qsoCount)
                    {
                        qso.Status = QSOStatus.ValidQSO;
                        qsoCount++;

                    }

                    if (reviewLogs.Count > reviewCount)
                    {
                        if (qso.Status != QSOStatus.ValidQSO)
                        {
                            qso.Status = QSOStatus.ReviewQSO;
                            reviewCount++;
                        }
                    }

                    if (otherLogs.Count > otherCount)
                    {
                        if (qso.Status != QSOStatus.ReviewQSO)
                        {
                            qso.Status = QSOStatus.ValidQSO;
                            otherCount++;
                        }
                    }

                    count++;

                }
            }

            //reviewLogs.AddRange(otherLogs.Where(q => q.QSOCollection.Any(a => a.ContactCall == operatorCall && a.Status == QSOStatus.InvalidQSO)).ToList());

            // http://stackoverflow.com/questions/3319016/convert-list-to-dictionary-using-linq-and-not-worrying-about-duplicates
            // this gives me a dictionary with a unique log even if several QSOs
            contestLog.MatchLogs = matchingLogs
                .GroupBy(p => p.LogOwner, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            // THIS DOES THE SAME AS ABOVE BUT THE KEY IS THE LOG INSTEAD OF LogOwner
            //// http://social.msdn.microsoft.com/Forums/vstudio/en-US/c0f0141c-1f98-422e-89af-406638c4403f/how-to-write-linq-query-to-convert-to-dictionaryintlistint-in-c?forum=linqprojectgeneral
            //// this converts the list to a dictionary and lists how many logs were matching
            //var match = matchingLogs
            //    .Select((n, i) => new { Value = n, Index = i })
            //    .GroupBy(a => a.Value)
            //    .ToDictionary(
            //        g => g.Key,
            //        g => g.Select(a => a.Index).ToList()
            //     );

            // now cleanup - THIS NEEDS TO BE CHECKED TO SEE IF IT WORKS
            //if (reviewLogs.Count > 0)
            //{
                //foreach (ContestLog log in reviewLogs)
                //{
                //    Int32 asd = log.QSOCollection.Count;
                //    log.QSOCollection.RemoveAll(x => x.Status == QSOStatus.ValidQSO);
                   
                //}
            

            contestLog.ReviewLogs = reviewLogs
               .GroupBy(p => p.LogOwner, StringComparer.OrdinalIgnoreCase)
                   .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);


            // this also contains the LogOwner's log so I want to remove it in a bit
            contestLog.OtherLogs = otherLogs
               .GroupBy(p => p.LogOwner, StringComparer.OrdinalIgnoreCase)
                   .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            return count;
        }



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

        #region Update ListView

        private void UpdateListView(string message, bool clear)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<string, bool>(this.UpdateListView), message, clear);
                return;
            }

            if (clear)
            {
                listView1.Items.Clear();
            }
            else
            {
                ListViewItem item = new ListViewItem(message);
                listView1.Items.Insert(0, item);
            }
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