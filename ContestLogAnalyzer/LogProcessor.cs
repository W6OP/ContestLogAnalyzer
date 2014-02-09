using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace W6OP.ContestLogAnalyzer
{
    public delegate void ErrorRaised(string error);

    public class LogProcessor
    {
        public delegate void ProgressUpdate(Int32 progress);
        public event ProgressUpdate OnProgressUpdate;
        public event ErrorRaised OnErrorRaised;

        private string _LogFolder;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public LogProcessor()
        {

        }

        public LogProcessor(string logFolder)
        {
            this._LogFolder = logFolder;
        }


        /// <summary>
        /// Create a list of all of the log files in the working folder. Once the list is
        /// filled pass the list on to another thread.
        /// </summary>
        public Int32 BuildFileList(out IEnumerable<System.IO.FileInfo> logFileList)
        {
            // Take a snapshot of the file system. http://msdn.microsoft.com/en-us/library/bb546159.aspx
            DirectoryInfo dir = new DirectoryInfo(_LogFolder);

            // This method assumes that the application has discovery permissions for all folders under the specified path.
            IEnumerable<FileInfo> fileList = dir.GetFiles("*.log", System.IO.SearchOption.TopDirectoryOnly);

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
        /// See if there is a header and a footer
        /// load and process the header.
        /// </summary>
        /// <param name="fileInfo"></param>
        public string BuildContestLog(FileInfo fileInfo, List<ContestLog> contestLogs)
        {
            ContestLog contestLog = new ContestLog();
            string fullName = fileInfo.FullName;
            string badLog = null;
            string version = null;
            Int32 progress = 0;

            try
            {
                if (File.Exists(fullName))
                {
                    List<string> lineList = File.ReadAllLines(fullName).Select(i => i.ToString()).ToList();

                    version = lineList.Where(l => l.StartsWith("START-OF-LOG:")).FirstOrDefault().Substring(13).Trim();

                    //if (version == "2.0")
                    //{
                    //    contestLog.LogHeader = BuildHeaderV2(lineList, fullName);
                    //}
                    //else if (version == "3.0")
                    //{
                    contestLog.LogHeader = BuildHeaderV3(lineList, fullName);
                    //}
                    //else
                    //{
                    //    // handle unsupported version
                    //    badLog = fileInfo.Name;
                    //}


                    // Find DUPES in list
                    //http://stackoverflow.com/questions/454601/how-to-count-duplicates-in-list-with-linq

                    // this statement says to copy all QSO lines
                    lineList = lineList.Where(x => x.IndexOf("QSO:", 0) != -1).ToList();
                    contestLog.QSOCollection = CollectQSOs(lineList);

                    if (contestLog.QSOCollection == null)
                    {
                        contestLog.IsValidLog = false;
                        throw new Exception(fileInfo.Name); // don't want this added to collection
                        // maybe have _InvalidLogs collection
                    }
                    else
                    {
                        contestLog.IsValidLog = true;
                    }
                   
                    if (contestLog.LogHeader.OperatorCategory == CategoryOperator.CheckLog)
                    {
                        contestLog.IsCheckLog = true;
                    }

                    contestLogs.Add(contestLog);
                    progress = contestLogs.Count;

                    if (OnProgressUpdate != null)
                    {
                        OnProgressUpdate(progress);
                    }
                }
            }
            catch (Exception)
            {
                badLog = fileInfo.Name;
            }

            return badLog;
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
            List<QSO> qsoList = null;
            List<string> temp = new List<string>();

            try
            {
                // first clean up QSOs - sometimes there are extra spaces
                // later lets make a more elegant solution
                foreach (string line in lineList)
                {
                    temp.Add(Utility.RemoveRepeatedSpaces(line));
                }

                lineList = temp;

                IEnumerable<QSO> qso =
                     from line in lineList
                     let splitName = line.Split(' ')
                     select new QSO()
                     {
                         Frequency = splitName[1], 
                         Mode = splitName[2],
                         QsoDate = splitName[3],
                         QsoTime = splitName[4],   
                         OperatorCall = splitName[5],    
                         SentSerialNumber = ConvertSerialNumber(splitName[6]),    
                         OperatorName = splitName[7],   
                         ContactCall = splitName[8],    
                         ReceivedSerialNumber = ConvertSerialNumber(splitName[9]), 
                         ContactName = splitName[10],  
                         CallIsValid = CheckCallSignFormat(splitName[5])
                     };

                qsoList = qso.ToList();

            }
            catch (Exception ex)
            {
                if (OnErrorRaised != null)
                {
                    // send log name to look at later
                    OnErrorRaised(ex.Message);
                }
            }

            return qsoList;

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



    } // end class
}
