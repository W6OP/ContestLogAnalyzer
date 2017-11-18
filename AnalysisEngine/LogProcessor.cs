using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CallParser;
using W6OP.PrintEngine;
using NetworkLookup;

namespace W6OP.ContestLogAnalyzer
{
    //public delegate void ErrorRaised(string error);

    public class LogProcessor
    {
        public delegate void ProgressUpdate(Int32 progress);
        public event ProgressUpdate OnProgressUpdate;
        //public event ErrorRaised OnErrorRaised;

        public PrintManager _PrintManager = null;
        public QRZ _QRZ = null;

        public string _FailReason { get; set; }

        public string _FailingLine { get; set; }

        public string _LogSourceFolder { get; set; }

        public string _InspectionFolder { get; set; }

        public string _WorkingFolder { get; set; }

        public ContestName _ActiveContest { get; set; }

        private string _WorkingLine = null;

        private CallParser.CallsignParser _Parser;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public LogProcessor()
        {
            _FailingLine = "";
            _WorkingLine = "";
        }

        public void InitializeLogProcessor(ContestName contestName)
        {
            _ActiveContest = contestName;

            if (_ActiveContest == ContestName.HQP)
            {
                _Parser = new CallParser.CallsignParser();
                _Parser.PrefixFile = @"C:\Users\pbourget\Documents\Visual Studio Projects\Ham Radio\ContestLogAnalyzer\Support\CallParser\Prefix.lst"; //"prefix.lst";  // @"C:\Users\pbourget\Documents\Visual Studio 2012\Projects\Ham Radio\DXACollector\Support\CallParser\prefix.lst";

                _QRZ = new QRZ();
            }
        }
        /// <summary>
        /// Create a list of all of the log files in the working folder. Once the list is
        /// filled pass the list on to another thread.
        /// </summary>
        public Int32 BuildFileList(Session session, out IEnumerable<System.IO.FileInfo> logFileList)
        {
            string fileNameFormat = null;
            // Take a snapshot of the file system. http://msdn.microsoft.com/en-us/library/bb546159.aspx
            DirectoryInfo dir = new DirectoryInfo(_LogSourceFolder);

            switch (_ActiveContest)
            {
                case ContestName.CW_OPEN:
                    fileNameFormat = "*_" + ((uint)session).ToString() + ".log";
                    break;
                default:
                    fileNameFormat = "*.log";
                    break;
            }

            // This method assumes that the application has discovery permissions for all folders under the specified path.
            IEnumerable<FileInfo> fileList = dir.GetFiles(fileNameFormat, System.IO.SearchOption.TopDirectoryOnly);

            //Create the query
            logFileList =
                from file in fileList
                where file.Extension.ToLower() == ".log"
                orderby file.CreationTime ascending
                select file;

            return fileList.Cast<object>().Count();
        }

        #region Load and Pre Process Logs

        /// <summary>
        /// See if there is a header and a footer. Load and process the header.
        /// Collect all of the QSOs for each log.
        /// </summary>
        /// <param name="fileInfo"></param>
        public string BuildContestLog(FileInfo fileInfo, List<ContestLog> contestLogs, Session session)
        {
            ContestLog contestLog = new ContestLog();
            List<string> lineList;
            List<string> lineListX;
            string fullName = fileInfo.FullName;
            string fileName = fileInfo.Name;
            string logFileName = null;
            string version = null;
            string reason = "Unable to build valid header.";
            Int32 progress = 0;
            Int32 count = 0;

            _FailReason = reason;

            try
            {
                if (File.Exists(fullName))
                {
                    lineList = File.ReadAllLines(fullName).Select(i => i.ToString()).ToList();

                    version = lineList.Where(l => l.StartsWith("START-OF-LOG:")).FirstOrDefault().Substring(13).Trim();

                    // build the header for the version of the log
                    if (version != null && version.Length > 2)
                    {
                        if (version.Substring(0, 1) == "2")
                        {
                            contestLog.LogHeader = BuildHeaderV2(lineList, fullName);
                        }

                        if (version.Substring(0, 1) == "3")
                        {
                            contestLog.LogHeader = BuildHeaderV3(lineList, fullName);
                        }
                    }
                    else
                    {
                        // Assume version 2 - this is just for the CWOPEN
                        contestLog.LogHeader = BuildHeaderV2(lineList, fullName);
                    }

                    List<QSO> validQsoList = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO).ToList();

                    // make sure minimum amout of information is correct
                    if (contestLog.LogHeader != null && AnalyzeHeader(contestLog, out reason) == true)
                    {
                        contestLog.LogHeader.HeaderIsValid = true;
                        _FailReason = "This log has a valid header.";
                    }
                    else
                    {
                        _FailReason = reason;
                        contestLog.IsValidLog = false;

                        throw new Exception(fileName); // don't want this added to collection
                    }

                    // Find DUPES in list
                    //http://stackoverflow.com/questions/454601/how-to-count-duplicates-in-list-with-linq

                    // this statement says to copy all QSO lines
                    // read all the QSOs in and mark any that are duplicates, bad call format, incorrect session
                    lineListX = lineList.Where(x => (x.IndexOf("XQSO:", 0) != -1) || (x.IndexOf("X-QSO:", 0) != -1)).ToList();
                    lineList = lineList.Where(x => (x.IndexOf("QSO:", 0) != -1) && (x.IndexOf("XQSO:", 0) == -1) && (x.IndexOf("X-QSO:", 0) == -1)).ToList();

                    switch (_ActiveContest)
                    {
                        case ContestName.CW_OPEN:
                            contestLog.Session = (int)session;
                            break;
                        default:
                            break;
                    }

                    _FailingLine = "";
                    contestLog.QSOCollection = CollectQSOs(lineList, session, false);
                    contestLog.QSOCollectionX = CollectQSOs(lineListX, session, false);

                    if (contestLog.QSOCollection == null || contestLog.QSOCollection.Count == 0)
                    {
                        // may want to expand on this for a future report
                        _FailReason = "QSO collection is null or empty. The QSOs may be in an invalid format."; // create enum
                        if (_FailingLine.Length > 0)
                        {
                            _FailReason += Environment.NewLine + _FailingLine;
                        }
                        contestLog.IsValidLog = false;
                        throw new Exception(fileInfo.Name); // don't want this added to collection
                    }
                    else
                    {
                        // this catches QSOs (or entire log) that do not belong to this session
                        count = contestLog.QSOCollection.Count;
                        contestLog.QSOCollection = contestLog.QSOCollection.Where(q => q.SessionIsValid == true).ToList();

                        if (count > 0 && contestLog.QSOCollection.Count == 0)
                        {
                            // may want to expand on this for a future report
                            _FailReason = "QSO collection is empty - Invalid session"; // create enum
                            contestLog.IsValidLog = false;
                            throw new Exception(fileInfo.Name); // don't want this added to collection
                        }
                    }

                    if (contestLog.LogHeader.OperatorCategory == CategoryOperator.CheckLog)
                    {
                        contestLog.IsCheckLog = true;
                    }

                    if (contestLog.LogHeader.NameSent == "NONE")
                    {
                        contestLog.LogHeader.NameSent = contestLog.QSOCollection[0].OperatorName;
                    }

                    // CHECK THESE - temp code so I can test print PDF
                    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    if (String.IsNullOrEmpty(contestLog.Operator))
                    {
                        contestLog.Operator = contestLog.QSOCollection[0].OperatorCall;
                    }

                    if (String.IsNullOrEmpty(contestLog.Station))
                    {
                        contestLog.Station = contestLog.QSOCollection[0].OperatorCall;
                    }

                    if (String.IsNullOrEmpty(contestLog.OperatorName))
                    {
                        contestLog.OperatorName = contestLog.QSOCollection[0].OperatorName;
                    }

                    if (contestLog.OperatorName.ToUpper() == "NAME")
                    {
                        // may want to expand on this for a future report
                        _FailReason = "Name sent is 'NAME' - Invalid name."; // create enum
                        contestLog.IsValidLog = false;
                        throw new Exception(fileInfo.Name); // don't want this added to collection
                    }

                    // now add the DXCC information some contests need for multipliers
                    // later break out to nwe method and add switch statement
                    if (_ActiveContest == ContestName.HQP)
                    {
                        SetDXCCInformation(contestLog.QSOCollection, contestLog);
                    }
                    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                    contestLogs.Add(contestLog);
                    progress = contestLogs.Count;

                    OnProgressUpdate?.Invoke(progress);
                }
            }
            catch (Exception ex)
            {
                string message = ex.Message;

                logFileName = fileName;

                if (ex.Message.IndexOf("Input string was not in a correct format.") != -1)
                {
                    message = ex.Message + "\r\nThere is probably an alpha character in the header where it should be numeric.";
                }
                if (ex.Message.IndexOf("Object reference not set to an instance of an object.") != -1)
                {
                    message = ex.Message + "\r\nA required field is missing from the header. Possibly the Contest Name.";
                }

                MoveFileToInpectFolder(fileName, message);
            }

            return logFileName;
        }

        /// <summary>
        /// Call parser handling
        /// </summary>
        /// <param name="qsoCollection"></param>
        private void SetDXCCInformation(List<QSO> qsoCollection, ContestLog contestLog)
        {
            CallParser.PrefixInfo prefixInfo = null;
            bool isValidHQPEntity = false;
            string[] info = new string[2] { "0", "0" };
            //HashSet<string> qsoSet = new HashSet<string>();

            contestLog.IsHQPEntity = false;
            contestLog.TotalPoints = 0;

            foreach (QSO qso in qsoCollection)
            {
                info = new string[2] { "0", "0" };

                qso.OperatorCountry = qso.OperatorName;
                qso.DXCountry = qso.ContactName;

                qso.HQPPoints = GetPoints(qso.Mode);

                qso.IsHQPEntity = false;

                isValidHQPEntity = Enum.IsDefined(typeof(HQPMults), qso.OperatorCountry);
                contestLog.IsHQPEntity = isValidHQPEntity;

                // -----------------------------------------------------------------------------------
                // DX station contacting Hawaiin station
                // if contact name (DXCountry) not in enum list of Hawaii entities (HIL, MAU, etc.)
                // QSO: 14242 PH 2017-08-26 1830 N5KXI 59 OK KH7XS 59  DX
                // this QSO is invalid - complete
                if (!isValidHQPEntity && qso.DXCountry == "DX")
                {
                    qso.EntityIsInValid = true;  
                }

                // -----------------------------------------------------------------------------------
                // Hawaiin station contacts a non Hawaiin station and puts "DX" as country instead of actual country
                // QSO:  7039 CW 2017-08-26 0524 AH7U 599 LHN ZL3PAH 599 DX
                if (isValidHQPEntity)
                {
                    qso.HQPEntity = qso.OperatorCountry;
                    qso.IsHQPEntity = isValidHQPEntity;

                    if (qso.DXCountry == "DX")
                    {
                        prefixInfo = GetPrefixInformation(qso.ContactCall);

                        if (prefixInfo != null)
                        {
                            if (prefixInfo.Territory != null)
                            {
                                qso.DXCountry = prefixInfo.Territory.ToString();

                                if (qso.DXCountry == "Canada" || qso.DXCountry == "United States of America")
                                {
                                    info = _QRZ.QRZLookup(qso.ContactCall, info);
                                    qso.DXCountry = info[0];
                                }
                            }
                        }
                    }

                    // query in scoring or count will be off if dupes eliminated
                    //contestLog.TotalPoints += 1;   
                }

                // -----------------------------------------------------------------------------------
                // Non Hawaii log uses "DX" for their own location (Op Name) instead of their real country
                // HI8A.log - QSO: 14000 CW 2017-08-27 1712 HI8A 599 DX KH6LC 599 HIL
                if (!isValidHQPEntity && qso.Status != QSOStatus.InvalidQSO)
                {
                    if (qso.OperatorCountry == "DX")
                    {
                        prefixInfo = GetPrefixInformation(qso.OperatorCall);

                        if (prefixInfo != null)
                        {
                            if (prefixInfo.Territory != null)
                            {
                                qso.OperatorCountry = prefixInfo.Territory.ToString();
                                if (qso.DXCountry == "Canada" || qso.DXCountry == "United States of America")
                                {
                                    info = _QRZ.QRZLookup(qso.ContactCall, info);
                                    qso.DXCountry = info[0];
                                }
                            }
                        }
                    }
                }
            }
        }

        // determine points by mode for the HQP
        private int GetPoints(string mode)
        {
            Int32 points = 0;

            try
            {
                CategoryMode catMode = (CategoryMode)Enum.Parse(typeof(CategoryMode), mode);

                switch (catMode)
                {
                    case CategoryMode.CW:
                        points = 3;
                        break;
                    case CategoryMode.RTTY:
                        points = 3;
                        break;
                    case CategoryMode.RY:
                        points = 3;
                        break;
                    case CategoryMode.PH:
                        points = 2;
                        break;
                    case CategoryMode.SSB:
                        points = 2;
                        break;
                    default:
                        points = 0;
                        break;
                }
            }
            catch (Exception)
            {
                throw new Exception("The mode " + mode + " is not valid for this contest.");
            }

            return points;
        }

        private PrefixInfo GetPrefixInformation(string call)
        {
            CallParser.PrefixInfo prefixInfo = null;

            _Parser.Callsign = call;

            if (_Parser.HitCount > 0)
            {
                prefixInfo = _Parser.Hits[0];
            }

            if (_Parser.HitCount > 1)
            {
                for (int i = 0; i < _Parser.HitCount; i++)
                {
                    prefixInfo = _Parser.Hits[0];
                }
            }

            return prefixInfo;
        }

        /// <summary>
        /// Move a file to the inspection folder.
        /// </summary>
        /// <param name="fileInfo"></param>
        private void MoveFileToInpectFolder(string fileName, string exception)
        {
            string logFileName = null;
            string inspectFileName = null;

            // move the file to inspection folder
            inspectFileName = Path.Combine(_InspectionFolder, fileName + ".txt");
            logFileName = Path.Combine(_InspectionFolder, fileName);

            if (File.Exists(logFileName))
            {
                File.Delete(logFileName);
            }

            if (File.Exists(inspectFileName))
            {
                File.Delete(inspectFileName);
            }

            if (File.Exists(Path.Combine(_WorkingFolder, fileName)))
            {
                File.Move(Path.Combine(_WorkingFolder, fileName), logFileName);
            }


            _PrintManager.PrintInspectionReport(fileName + ".txt", _FailReason + Environment.NewLine + " - " + exception);


            // create a text file with the reason for the rejection
            //using (StreamWriter sw = File.CreateText(inspectReasonFileName))
            //{
            //    sw.WriteLine(FailReason);
            //}
        }

        /// <summary>
        /// Check to see if a subset of fields in the header are valid.
        /// </summary>
        /// <param name="contestLog"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        private bool AnalyzeHeader(ContestLog contestLog, out string reason)
        {
            bool headerIsValid = true;
            reason = "Unable to build valid header.";

            if (contestLog.LogHeader.OperatorCallSign == "UNKNOWN")
            {
                reason = "Invalid operator callsign";
                return false;
            }

            if (contestLog.LogHeader.OperatorCategory == CategoryOperator.Uknown)
            {
                reason = "Invalid operator category";
                return false;
            }

            if (contestLog.LogHeader.Assisted == CategoryAssisted.Uknown)
            {
                reason = "Invalid assisted value";
                return false;
            }

            if (contestLog.LogHeader.PrimaryName == "UNKNOWN" || contestLog.LogHeader.PrimaryName == "[BLANK]")
            {
                reason = "Invalid primary name";
                return false;
            }
            if (contestLog.LogHeader.NameSent == "UNKNOWN" || contestLog.LogHeader.NameSent == "[BLANK]")
            {
                reason = "Invalid name sent";
                return false;
            }

            return headerIsValid;
        }

        /// <summary>
        /// LINQ SAMPLES
        /// http://code.msdn.microsoft.com/101-LINQ-Samples-3fb9811b
        /// 
        /// THERE ARE SOME ITEMS MISSING
        /// DATE, TIME
        /// TIME OFF
        /// 
        /// NOTE: CATEGORY defaults to SINGLE-OP instead of UNKNOWN because Alan (for CWOPEN) doesn't care if it is specified wrong. CWOPEN is always SINGLE_OP
        /// </summary>
        /// <param name="lineList"></param>
        /// <param name="match"></param>
        private LogHeader BuildHeaderV2(List<string> lineList, string logFileName)
        {
            IEnumerable<LogHeader> logHeader = null;

            try
            {

                logHeader =
                    from line in lineList
                    select new LogHeader()
                    {
                        // NEED StringComparer.CurrentCultureIgnoreCase ???
                        LogFileName = logFileName,
                        Version = lineList.Where(l => l.StartsWith("START-OF-LOG:")).FirstOrDefault().Substring(13).Trim(),
                        Location = lineList.Where(l => l.StartsWith("LOCATION:")).DefaultIfEmpty("LOCATION: UNKNOWN").First().Substring(9).Trim(),
                        OperatorCallSign = CheckForNull(lineList.Where(l => l.StartsWith("CALLSIGN:")).DefaultIfEmpty("CALLSIGN: UNKNOWN").First(), 9, "UNKNOWN").ToUpper(),
                        OperatorCategory = Utility.GetValueFromDescription<CategoryOperator>(lineList.Where(l => l.StartsWith("CATEGORY:")).DefaultIfEmpty("CATEGORY: SINGLE-OP").First().Substring(9).Trim().ToUpper()),
                        // this is for when the CATEGORY-ASSISTED: is missing or has no value
                        Assisted = Utility.GetValueFromDescription<CategoryAssisted>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-ASSISTED:")).DefaultIfEmpty("CATEGORY-ASSISTED: ASSISTED").First(), 18, "ASSISTED")),   //.Substring(18).Trim().ToUpper()),
                        Band = Utility.GetValueFromDescription<CategoryBand>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-BAND:")).DefaultIfEmpty("CATEGORY-BAND: ALL").First(), 14, "ALL")),
                        Power = Utility.GetValueFromDescription<CategoryPower>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-POWER:")).DefaultIfEmpty("CATEGORY-POWER: HIGH").First(), 15, "HIGH")),
                        Mode = Utility.GetValueFromDescription<CategoryMode>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-MODE:")).DefaultIfEmpty("CATEGORY-MODE: MIXED").First(), 14, "MIXED")),
                        Station = Utility.GetValueFromDescription<CategoryStation>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-STATION:")).DefaultIfEmpty("CATEGORY-STATION: UNKNOWN").First(), 17, "UNKNOWN")),
                        Transmitter = Utility.GetValueFromDescription<CategoryTransmitter>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-TRANSMITTER:")).DefaultIfEmpty("CATEGORY-TRANSMITTER: UNKNOWN").First(), 21, "UNKNOWN")),
                        ClaimedScore = Convert.ToInt32(CheckForNumeric(CheckForNull(lineList.Where(l => l.StartsWith("CLAIMED-SCORE:")).DefaultIfEmpty("CLAIMED-SCORE: 0").First(), 14, "0"))),
                        Club = CheckForNull(lineList.Where(l => l.StartsWith("CLUB:")).DefaultIfEmpty("CLUB: NONE").First(), 5, "NONE"),
                        Contest = Utility.GetValueFromDescription<ContestName>(lineList.Where(l => l.StartsWith("CONTEST:")).FirstOrDefault().Substring(9).Trim().ToUpper()),
                        CreatedBy = CheckForNull(lineList.Where(l => l.StartsWith("CREATED-BY:")).DefaultIfEmpty("CREATED-BY: NONE").First(), 11, "NONE"),
                        PrimaryName = CheckForNull(lineList.Where(l => l.StartsWith("NAME:")).DefaultIfEmpty("NAME: NONE").First(), 5, "NONE"),
                        NameSent = CheckForNull(lineList.Where(l => l.StartsWith("Name Sent:")).DefaultIfEmpty("Name Sent: NONE").First(), 10, "NONE"),
                        // need to work on address
                        Operators = lineList.Where(l => l.StartsWith("OPERATORS:")).ToList(),
                        SoapBox = CheckForNull(lineList.Where(l => l.StartsWith("SOAPBOX:")).DefaultIfEmpty("SOAPBOX:").First(), 8, ""),
                    };

                //return logHeader.FirstOrDefault();
            }
            catch (Exception ex)
            {
                string a = ex.Message;
                throw;
            }

            return logHeader.FirstOrDefault();
        }

        /// <summary>
        /// Some logs have 5,234 for a score and some have "Not Required"
        /// when it should be an integer
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        private object CheckForNumeric(string inputString)
        {
            // first get rid of commas ie. 5,234
            inputString = inputString.Replace(",", "");

            int i = 0;
            bool result = int.TryParse(inputString, out i); 
            return i;
        }

        /// <summary>
        /// LINQ SAMPLES
        /// http://code.msdn.microsoft.com/101-LINQ-Samples-3fb9811b
        /// 
        /// THERE ARE SOME ITEMS MISSING
        /// DATE, TIME
        /// TIME OFF
        /// 
        /// NOTE: CATEGORY defaults to SINGLE-OP instead of UNKNOWN because Alan (for CWOPEN) doesn't care if it is specified wrong. CWOPEN is always SINGLE_OP
        /// </summary>
        /// <param name="lineList"></param>
        /// <param name="match"></param>
        private LogHeader BuildHeaderV3(List<string> lineList, string logFileName)
        {
            IEnumerable<LogHeader> logHeader = null;

            try
            {
                logHeader =
                from line in lineList
                select new LogHeader()
                {
                    // NEED StringComparer.CurrentCultureIgnoreCase ???
                    LogFileName = logFileName,
                    Version = lineList.Where(l => l.StartsWith("START-OF-LOG:")).FirstOrDefault().Substring(13).Trim(),
                    Location = lineList.Where(l => l.StartsWith("LOCATION:")).DefaultIfEmpty("LOCATION: UNKNOWN").First().Substring(9).Trim(),
                    OperatorCallSign = CheckForNull(lineList.Where(l => l.StartsWith("CALLSIGN:")).DefaultIfEmpty("CALLSIGN: UNKNOWN").First(), 9, "UNKNOWN").ToUpper(),
                    OperatorCategory = Utility.GetValueFromDescription<CategoryOperator>(lineList.Where(l => l.StartsWith("CATEGORY-OPERATOR:")).DefaultIfEmpty("CATEGORY-OPERATOR: SINGLE-OP").First().Substring(18).Trim().ToUpper()),
                    // this is for when the CATEGORY-ASSISTED: is missing or has no value
                    Assisted = Utility.GetValueFromDescription<CategoryAssisted>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-ASSISTED:")).DefaultIfEmpty("CATEGORY-ASSISTED: ASSISTED").First(), 18, "ASSISTED")),
                    Band = Utility.GetValueFromDescription<CategoryBand>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-BAND:")).DefaultIfEmpty("CATEGORY-BAND: ALL").First(), 14, "ALL")),
                    Power = Utility.GetValueFromDescription<CategoryPower>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-POWER:")).DefaultIfEmpty("CATEGORY-POWER: HIGH").First(), 15, "HIGH")),
                    Mode = Utility.GetValueFromDescription<CategoryMode>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-MODE:")).DefaultIfEmpty("CATEGORY-MODE: MIXED").First(), 14, "MIXED")),
                    Station = Utility.GetValueFromDescription<CategoryStation>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-STATION:")).DefaultIfEmpty("CATEGORY-STATION: UNKNOWN").First(), 17, "UNKNOWN")),
                    Transmitter = Utility.GetValueFromDescription<CategoryTransmitter>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-TRANSMITTER:")).DefaultIfEmpty("CATEGORY-TRANSMITTER: UNKNOWN").First(), 21, "UNKNOWN")),
                    ClaimedScore = Convert.ToInt32(CheckForNumeric(CheckForNull(lineList.Where(l => l.StartsWith("CLAIMED-SCORE:")).DefaultIfEmpty("CLAIMED-SCORE: 0").First().Replace(",", ""), 14, "0"))), // some guys do score as 52,000
                    Club = CheckForNull(lineList.Where(l => l.StartsWith("CLUB:")).DefaultIfEmpty("CLUB: NONE").First(), 5, "NONE"),
                    Contest = Utility.GetValueFromDescription<ContestName>(lineList.Where(l => l.StartsWith("CONTEST:")).FirstOrDefault().Substring(9).Trim().ToUpper()),
                    CreatedBy = CheckForNull(lineList.Where(l => l.StartsWith("CREATED-BY:")).DefaultIfEmpty("CREATED-BY: NONE").First(), 11, "NONE"),
                    PrimaryName = CheckForNull(lineList.Where(l => l.StartsWith("NAME:")).DefaultIfEmpty("NAME: NONE").First(), 5, "NONE"),
                    // NameSent will always be NONE
                    NameSent = CheckForNull(lineList.Where(l => l.StartsWith("Name Sent:")).DefaultIfEmpty("Name Sent: NONE").First(), 10, "NONE"),
                    // need to work on address
                    Operators = lineList.Where(l => l.StartsWith("OPERATORS:")).ToList(),
                    SoapBox = CheckForNull(lineList.Where(l => l.StartsWith("SOAPBOX:")).DefaultIfEmpty("SOAPBOX:").First(), 8, ""),
                };

            }
            catch (Exception ex)
            {
                string a = ex.Message;
                throw;
            }

            return logHeader.FirstOrDefault();
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
        /// Clean up the individual lines and create a QSO collection. Ensure each QSO belongs to the 
        /// specified session. Do some validation on each QSO, call sign format, session, duplicates.
        /// http://msdn.microsoft.com/en-us/library/bb513866.aspx
        /// </summary>
        /// <param name="lineList"></param>
        /// <returns></returns>
        private List<QSO> CollectQSOs(List<string> lineList, Session session, bool reverse)
        {
            List<QSO> qsoList = null;
            List<string> tempList = new List<string>();
            string tempLine = null;

            _WorkingLine = "";

            try
            {
                // first clean up QSOs - sometimes there are extra spaces
                // later lets make a more elegant solution
                foreach (string line in lineList)
                {
                    tempLine = line.Replace("\t", "");
                    tempList.Add(Utility.RemoveRepeatedSpaces(tempLine));
                }

                lineList = tempList;

                if (!reverse)
                {
                    //try // DEBUGGING ONLY
                    //{
                    IEnumerable<QSO> qso =
                         from line in lineList
                         let split = line.Split(' ')
                         select new QSO()
                         {
                             // maybe .toUpper() will be needed
                             Frequency = CheckCompleteQSO(split, line),
                             Mode = split[2],
                             QsoDate = split[3],
                             QsoTime = split[4],
                             OperatorCall = split[5],
                             SentSerialNumber = ConvertSerialNumber(split[6]),
                             OperatorName = split[7],
                             ContactCall = split[8],
                             ReceivedSerialNumber = ConvertSerialNumber(split[9]),
                             ContactName = split[10],
                             CallIsInValid = CheckCallSignFormat(split[5]),
                             SessionIsValid = CheckForvalidSession(session, split[4])
                         };
                    qsoList = qso.ToList();
                    //}
                    //catch(Exception eex)
                    //{
                    //    string a = eex.Message;OnErrorRaised
                    //} 
                }
                else
                {
                    IEnumerable<QSO> qso =
                         from line in lineList
                         let split = line.Split(' ')
                         select new QSO()
                         {
                             // maybe .toUpper() will be needed
                             Frequency = CheckCompleteQSO(split, line),
                             Mode = split[2],
                             QsoDate = split[3],
                             QsoTime = split[4],
                             OperatorCall = split[5],

                             OperatorName = split[6],
                             SentSerialNumber = ConvertSerialNumber(split[7]),

                             ContactCall = split[8],

                             ContactName = split[9],
                             ReceivedSerialNumber = ConvertSerialNumber(split[10]),

                             CallIsInValid = CheckCallSignFormat(split[5]),
                             SessionIsValid = CheckForvalidSession(session, split[4])
                         };
                    qsoList = qso.ToList();
                }


            }
            catch (Exception ex)
            {
                // if there is a format exception it means the CWT template may have been used
                // this swaps the name and serial number columns
                if (ex is FormatException && reverse == false && _ActiveContest == ContestName.CW_OPEN)
                {
                    qsoList = CollectQSOs(lineList, session, true);
                }
                else
                {
                    if (_FailingLine.IndexOf(_WorkingLine) == -1)
                    {
                        _FailingLine += Environment.NewLine + _WorkingLine;
                    }
                }
            }

            return qsoList;
        }

        /// <summary>
        /// Check and see if every field is in the line
        /// </summary>
        /// <param name="split"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        private string CheckCompleteQSO(string[] split, string line)
        {
            _WorkingLine = line;

            if (split.Length != 11)
            {
                _FailingLine += Environment.NewLine + line;
            }

            return split[1];
        }

        /// <summary>
        /// The Date and Time must match for the session specified.
        /// Session 1 is 0000 - 0359
        /// Session 2 is 1200 - 1559
        /// Session 3 is 2000 - 2359
        /// </summary>
        /// <param name="session"></param>
        /// <param name="qsoDate"></param>
        /// <param name="qsoTime"></param>
        /// <returns></returns>
        private bool CheckForvalidSession(Session session, string qsoTime)
        {
            bool isValidSession = false;
            int qsoSessionTime = Convert.ToInt16(qsoTime);

            // later may want to check time stamp for that contest
            if (_ActiveContest != ContestName.CW_OPEN)
            {
                return true;
            }

            switch (session)
            {
                case Session.Session_1:
                    if (qsoSessionTime >= 0 && qsoSessionTime <= 359)
                    {
                        isValidSession = true;
                    }
                    break;
                case Session.Session_2:
                    if (qsoSessionTime >= 1200 && qsoSessionTime <= 1559)
                    {
                        isValidSession = true;
                    }
                    break;
                case Session.Session_3:
                    if (qsoSessionTime >= 2000 && qsoSessionTime <= 2359)
                    {
                        isValidSession = true;
                    }
                    break;
                default:
                    isValidSession = false;
                    break;
            }

            return isValidSession;
        }

        // http://stackoverflow.com/questions/16197290/checking-for-duplicates-in-a-list-of-objects-c-sharp
        // http://stackoverflow.com/questions/18547354/c-sharp-linq-find-duplicates-in-list

        /// <summary>
        /// Convert a string to an Int32. Also extract a number from a string as a serial
        /// number may be O39 instead of 039.
        /// </summary>
        /// <param name="serialNumber"></param>
        /// <returns></returns>
        private Int32 ConvertSerialNumber(string serialNumber)
        {
            Int32 number = 0; // catches invalid serial number

            // catch when SN is swapped with Name
            serialNumber = Regex.Match(serialNumber, @"\d+").Value;
            number = Convert.ToInt32(serialNumber);

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
            bool invalid = true;

            // should this pass "DR50RRDXA" it does not currently
            if (Regex.IsMatch(call.ToUpper(), regex, RegexOptions.IgnoreCase))
            {
                invalid = false;
            }

            return invalid;
        }

        #endregion



    } // end class
}
