using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using W6OP.CallParser;
using W6OP.PrintEngine;

namespace W6OP.ContestLogAnalyzer
{
    public delegate void ErrorRaised(string error);

    public class LogProcessor
    {
        readonly string[] States = { "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "DC", "FL", "GA", "HI", "ID", "IL", "IN", "IA", "KS", "KY", "LA", "ME", "MD", "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ", "NM", "NY", "NC", "ND", "OH", "OK", "OR", "PA", "RI", "SC", "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY", };
        readonly string[] Provinces = { "NL", "NS", "PE", "NB", "QC", "ON", "MB", "SK", "AB", "BC", "YT", "NT", "NU" };

        public delegate void ProgressUpdate(int progress);
        public event ProgressUpdate OnProgressUpdate;

        private const string HQPHawaiiLiteral = "HAWAII";
        private const string HQPUSALiteral = "UNITED STATES OF AMERICA";
        private const string HQPCanadaLiteral = "CANADA";

        public PrintManager _PrintManager = null;
        public Lookup<string, string> CountryPrefixes { get; set; }
        public Lookup<string, string> Kansas { get; set; }
        public Lookup<string, string> Ohio { get; set; }

        public string FailReason { get; set; }
        public string FailingLine { get; set; }
        public string LogSourceFolder { get; set; }
        public string InspectionFolder { get; set; }
        public string WorkingFolder { get; set; }
        public ContestName ActiveContest { get; set; }
        private string WorkingLine = null;
        public CallLookUp CallLookUp;

        /// <summary>
        /// Dictionary of all calls, bands, modes in all the logs as keys
        /// the values are a list of all logs those calls are in
        /// </summary>
        public Dictionary<string, List<ContestLog>> CallDictionary;
        public Dictionary<string, List<Tuple<string, int>>> NameDictionary;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public LogProcessor()
        {
            CallDictionary = new Dictionary<string, List<ContestLog>>();
            NameDictionary = new Dictionary<string, List<Tuple<string, int>>>();

            FailingLine = "";
            WorkingLine = "";
        }

        /// <summary>
        /// Create a list of all of the log files in the working folder. Once the list is
        /// filled pass the list on to another thread.
        /// </summary>
        public int BuildFileList(out IEnumerable<FileInfo> logFileList)
        {
            string fileNameFormat = "*.log";

            DirectoryInfo dir = new DirectoryInfo(LogSourceFolder);

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
            List<QSO> xQSOCollection; // X-QSOs
            string fullName = fileInfo.FullName;
            string fileName = fileInfo.Name;
            string logFileName = null;
            string reason = "Unable to build valid header. Check the Inspect folder for details.";

            FailReason = reason;
            FailingLine = "";

            try
            {
                if (File.Exists(fileInfo.FullName))
                {
                    switch (ActiveContest)
                    {
                        case ContestName.CW_OPEN:
                            contestLog.Session = (int)session;
                            break;
                        default:
                            break;
                    }

                    lineList = File.ReadAllLines(fullName).Select(i => i.ToString()).ToList();

                    try
                    {
                        contestLog = ProcessHeader(contestLog, lineList, fileInfo);
                    }
                    catch (Exception)
                    {
                        throw new Exception(fullName);
                    }

                    // Find DUPES in list
                    //http://stackoverflow.com/questions/454601/how-to-count-duplicates-in-list-with-linq

                    // this statement says to copy all QSO lines
                    lineListX = lineList.Where(x => (x.IndexOf("XQSO:", 0) != -1) || (x.IndexOf("X-QSO:", 0) != -1)).ToList();
                    lineList = lineList.Where(x => (x.IndexOf("QSO:", 0) != -1) && (x.IndexOf("XQSO:", 0) == -1) && (x.IndexOf("X-QSO:", 0) == -1)).ToList();

                    // collect regular QSOs
                    contestLog.QSOCollection = CollectQSOs(lineList, session);
                   
                    //// add a reference to the parent log to each QSO
                    //if (contestLog.QSOCollection != null)
                    //{
                    //    contestLog.QSOCollection.Select(c => { c.ParentLog = contestLog; return c; }).ToList();
                    //}

                    // collect all the X-QSOs so we can mark them as IsXQSO - this log won't count them but others can get credit for them
                    xQSOCollection = CollectQSOs(lineListX, session);
                    xQSOCollection.Select(c => { c.ParentLog = contestLog; return c; }).ToList();
                    xQSOCollection.Select(c => { c.IsXQSO = true; return c; }).ToList();

                    // merge the two lists together so other logs can search for everything
                    if (contestLog.QSOCollection != null)
                    {
                        contestLog.QSOCollection = contestLog.QSOCollection.Union(xQSOCollection).ToList();
                        // add a reference to the parent log to each QSO
                        contestLog.QSOCollection.Select(c => { c.ParentLog = contestLog; return c; }).ToList();

                        // if all QSOs are on one band mark log as IsSingleBand = true - used in DetermineBandFault()
                        contestLog.IsSingleBand = contestLog.QSOCollection.All(j => j.Band == contestLog.QSOCollection[0].Band);
                        // if all QSOs are on one mode mark log as IsSingleMode = true - used in DetermineModeFault()
                        contestLog.IsSingleMode = contestLog.QSOCollection.All(j => j.Mode == contestLog.QSOCollection[0].Mode);
                    }

                    // complete information for printing pdf file - must be before CheckHeader()
                    AddPrintInformation(contestLog);
                    CheckHeader(fileInfo, contestLog);

                    // check for valid QSOs
                    CheckQSOCollection(fileName, contestLog);

                    // now add the DXCC information some contests need for multipliers
                    if (ActiveContest == ContestName.HQP)
                    {
                        bool isHQPEntity = Enum.IsDefined(typeof(HQPMults), contestLog.QSOCollection[0].OperatorEntity);
                        contestLog.IsHQPEntity = isHQPEntity;
                        SetHQPDXCCInformation(contestLog.QSOCollection, isHQPEntity);
                    }

                    // -----------------Performance upgrade---------------------------------------------------------------

                    BuildDictionaries(contestLog);

                    // --------------------------------------------------------------------------------

                    contestLogs.Add(contestLog);

                    OnProgressUpdate?.Invoke(contestLogs.Count);
                }
            }
            catch (Exception ex)
            {
                logFileName = HandleExceptions(contestLog, fileName, ex);
            }

            return logFileName;
        }

        /// <summary>
        /// By building both dictionaries I save significant time in the
        /// LogAnalyzer() linq queries. I only have to query a subset
        /// of all the contest logs.
        /// 
        /// The CallDictionary contains every log that contains a specific call sign
        /// The QSO Dictionary contains all QSOs in a log keyed by call sign
        /// </summary>
        /// <param name="contestLog"></param>
        private void BuildDictionaries(ContestLog contestLog)
        {
            List<ContestLog> contestLogs;
            List<QSO> qsos;
            List<Tuple<string, int>> names = new List<Tuple<string, int>>();
            string firstOperatorName;

            foreach (QSO qso in contestLog.QSOCollection)
            {
                // QSODictionary
                if (contestLog.QSODictionary.ContainsKey(qso.ContactCall))
                {
                    qsos = contestLog.QSODictionary[qso.ContactCall];
                    qsos.Add(qso);
                }
                else
                {
                    qsos = new List<QSO>
                    {
                        qso
                    };
                    contestLog.QSODictionary.Add(qso.ContactCall, qsos);
                }

                // Call
                if (CallDictionary.ContainsKey(qso.ContactCall))
                {
                    contestLogs = CallDictionary[qso.ContactCall];
                    if (!contestLogs.Contains(contestLog))
                    {
                        contestLogs.Add(contestLog);
                    }
                }
                else
                {
                    contestLogs = new List<ContestLog>
                    {
                        contestLog
                    };
                    CallDictionary.Add(qso.ContactCall, contestLogs);
                }


                // names - first all who submitted logs
                if (NameDictionary.ContainsKey(qso.OperatorCall))
                {
                    firstOperatorName = qso.OperatorName;

                    names = NameDictionary[qso.OperatorCall];
                    var item = names.Where(x => x.Item1 == qso.OperatorName).ToList();

                    if (item.Count == 1)
                    {
                        int count = item[0].Item2;
                        count += 1;
                        names.Remove(item[0]);

                        var name = new Tuple<string, int>(qso.OperatorName, count);
                        names.Add(name);
                    }
                    else
                    {
                        var name = new Tuple<string, int>(qso.OperatorName, 1);
                        names.Add(name);
                    }
                }
                else
                {
                    var name = new Tuple<string, int>(qso.OperatorName, 1);
                    names = new List<Tuple<string, int>>
                            {
                                name
                            };
                    NameDictionary[qso.OperatorCall] = names;
                }

                // names - may not have submitted logs
                if (NameDictionary.ContainsKey(qso.ContactCall))
                {
                    names = NameDictionary[qso.ContactCall];
                    var item = names.Where(x => x.Item1 == qso.ContactName).ToList();

                    if (item.Count == 1)
                    {
                        int count = item[0].Item2;
                        count += 1;
                        names.Remove(item[0]);

                        var name = new Tuple<string, int>(qso.ContactName, count);
                        names.Add(name);
                    }
                    else
                    {
                        var name = new Tuple<string, int>(qso.ContactName, 1);
                        names.Add(name);
                    }
                }
                else
                {
                    var name = new Tuple<string, int>(qso.ContactName, 1);
                    names = new List<Tuple<string, int>>
                            {
                                name
                            };
                    NameDictionary[qso.ContactCall] = names;
                }
            }
        }

        /// <summary>
        /// Format exceptions.
        /// </summary>
        /// <param name="contestLog"></param>
        /// <param name="fileName"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        private string HandleExceptions(ContestLog contestLog, string fileName, Exception ex)
        {
            string logFileName;
            string message = ex.Message;

            logFileName = fileName;

            if (ex.Message.IndexOf("Input string was not in a correct format.") != -1)
            {
                message += "\r\nThere is probably an alpha character in the header where it should be numeric.";
            }
            else if (ex.Message.IndexOf("Object reference not set to an instance of an object.") != -1)
            {
                message = "\r\nA required field is missing from the header. Possibly the Contest Name.";
            }
            else
            {
                if (contestLog.QSOCollection == null)
                {
                    FailReason = FailReason + " - QSO collection is null." + Environment.NewLine + FailingLine;
                }
                else
                {
                    FailReason = FailReason + " - Unable to process log." + Environment.NewLine + FailingLine;
                }
            }

            if (message.IndexOf("Value cannot be null") != -1)
            {
                message = "Unable to process log.";
            }
            MoveFileToInpectFolder(fileName, message);
            return logFileName;
        }

        /// <summary>
        /// Verify QSOs are not missing the contact name column.
        /// Verify there is an operator name.
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="contestLog"></param>
        /// <param name="missingQSOS"></param>
        private void CheckHeader(FileInfo fileInfo, ContestLog contestLog)
        {
            List<QSO> missingQSOS = contestLog.QSOCollection.Where(q => q.ContactName == "MISSING_COLUMN").ToList();

            if (missingQSOS.Count > 0)
            {
                FailReason = "One or more columns are missing.";
                contestLog.IsValidLog = false;
                throw new Exception(fileInfo.Name); // don't want this added to collection
            }

            if (contestLog.LogHeader.OperatorCategory == CategoryOperator.CheckLog)
            {
                contestLog.IsCheckLog = true;
            }

            if (contestLog.LogHeader.NameSent == "NONE")
            {
                contestLog.LogHeader.NameSent = contestLog.QSOCollection[0].OperatorName;
            }

            if (contestLog.OperatorName.ToUpper() == "NAME")
            {
                // may want to expand on this for a future report
                FailReason = "Name sent is 'NAME' - Invalid name."; 
                contestLog.IsValidLog = false;
                throw new Exception(fileInfo.Name); // don't want this added to collection
            }
        }

        /// <summary>
        /// Do a light check on the QSO collection. make sure there is at least one QSO.
        /// make sure the QSOs are for this session if its the CWOpen
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="contestLog"></param>
        private void CheckQSOCollection(string fileName, ContestLog contestLog)
        {
            List<QSO> invalidQsos;

            //  it will never be null because we will have an exception first
            switch (contestLog.QSOCollection.Count)
            {
                case 0:
                    // may want to expand on this for a future report
                    FailReason = "One or more QSOs may be in an invalid format."; // create enum
                    if (FailingLine.Length > 0)
                    {
                        FailReason += Environment.NewLine + FailingLine;
                    }
                    contestLog.IsValidLog = false;
                    throw new Exception(fileName); // don't want this added to collection
                default:
                    {
                        switch (ActiveContest)
                        {
                            case ContestName.CW_OPEN:
                                // this catches QSOs (or entire log) that do not belong to this session
                                contestLog.QSOCollection = contestLog.QSOCollection.Where(q => q.SessionIsValid == true).ToList();

                                if (contestLog.QSOCollection.Count == 0)
                                {
                                    // may want to expand on this for a future report
                                    FailReason = "QSO collection is empty" + Environment.NewLine + "Invalid session" + Environment.NewLine;
                                    contestLog.IsValidLog = false;
                                    throw new Exception(fileName); // don't want this added to collection
                                }

                                invalidQsos = contestLog.QSOCollection.Where(q => q.Status != QSOStatus.ValidQSO).ToList();

                                if (invalidQsos.Count != 0)
                                {
                                    FailReason = "There are " + invalidQsos.Count.ToString() + " invalid QSOs" + Environment.NewLine;
                                    foreach (QSO qso in invalidQsos)
                                    {
                                        FailReason += qso.Status.ToString() + " : " + qso.RawQSO + Environment.NewLine;
                                    }
                                    contestLog.IsValidLog = false;
                                    throw new Exception(fileName); // don't want this added to collection
                                }
                                break;
                            case ContestName.HQP:
                                invalidQsos = contestLog.QSOCollection.Where(q => q.Status != QSOStatus.ValidQSO).ToList();

                                if (invalidQsos.Count != 0)
                                {
                                    FailReason = "There are " + invalidQsos.Count.ToString() + " invalid QSOs" + Environment.NewLine;
                                    foreach (QSO qso in invalidQsos)
                                    {
                                        FailReason += qso.Status.ToString() + " : " + qso.RawQSO + Environment.NewLine;
                                    }
                                    contestLog.IsValidLog = false;
                                    throw new Exception(fileName); // don't want this added to collection
                                }
                                break;
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Add missing information needed for printing PDF file.
        /// </summary>
        /// <param name="contestLog"></param>
        private void AddPrintInformation(ContestLog contestLog)
        {
            if (string.IsNullOrEmpty(contestLog.Operator))
            {
                contestLog.Operator = contestLog.QSOCollection[0].OperatorCall;
            }

            if (string.IsNullOrEmpty(contestLog.Station))
            {
                contestLog.Station = contestLog.QSOCollection[0].OperatorCall;
            }

            if (string.IsNullOrEmpty(contestLog.OperatorName))
            {
                contestLog.OperatorName = contestLog.QSOCollection[0].OperatorName;
            }
        }

        /// <summary>
        /// Process and build the log header.
        /// </summary>
        /// <param name="contestLog"></param>
        /// <param name="lineList"></param>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        private ContestLog ProcessHeader(ContestLog contestLog, List<string> lineList, FileInfo fileInfo)
        {
            string fullName = fileInfo.FullName;
            string fileName = fileInfo.Name;
            string version = null;
            string reason = "Unable to build valid header. Check the Inspect folder for details." + Environment.NewLine;

            FailReason = reason;

            version = lineList.Where(l => l.StartsWith("START-OF-LOG:")).FirstOrDefault().Substring(13).Trim();

            // build the header for the version of the log
            if (version != null && version.Length > 2)
            {
                if (version.Substring(0, 1) == "2")
                {
                    // this is probably obsolete
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

            // make sure minimum amout of information is correct
            if (contestLog.LogHeader != null && AnalyzeHeader(contestLog, out reason) == true)
            {
                contestLog.LogHeader.HeaderIsValid = true;
                FailReason = "Check the Inspect folder for details." + Environment.NewLine;
            }
            else
            {
                FailReason = reason;
                contestLog.IsValidLog = false;

                throw new Exception(fullName); // don't want this added to collection
            }

            return contestLog;
        }

        /// <summary>
        /// HQP Only
        /// Now add the DXCC country information the HQP needs for multipliers
        /// Non Hawaii stations contacting Hawaii stations from US or Canada send state or province
        /// Sometimes they are in another QSO party and send 3 or 4 letter county - need to change to state
        /// Other country stations contacting Hawaii stations send "DX" - need to get real country for multipliers
        /// Hawaii stations work anyone – non-Hawaii stations work only Hawaii
        /// </summary>
        /// <param name="qsoCollection"></param>
        /// <param name="contestLog"></param>
        private void SetHQPDXCCInformation(List<QSO> qsoCollection, bool isHQPEntity)
        {
            // set the entity of the log owner
            if (isHQPEntity)
            {
                ProcessHawaiiOperators(qsoCollection);
            }
            else
            {
                ProcessNonHawaiiOperators(qsoCollection);
            }
        }

        /// <summary>
        /// HQP Only
        /// DX station contacting Hawaii station
        /// if DXEntity is not in enum list of Hawaii entities (HIL, MAU, etc.)
        /// this QSO is invalid
        /// </summary>
        /// <param name="qsoCollection"></param>
        private void ProcessNonHawaiiOperators(List<QSO> qsoCollection)
        {
            foreach (QSO qso in qsoCollection)
            {
                if (!CheckForValidCallsign(qso.ContactCall))
                {
                    qso.Status = QSOStatus.InvalidQSO;
                    qso.ReasonRejected = RejectReason.InvalidCall;
                    continue;
                }

                if (qso.ContactEntity.Length != 3)
                {
                    if (Enum.IsDefined(typeof(ALTHQPMults), qso.ContactEntity))
                    {
                        // they just used alternate HPQ Mult entity - correct it for them
                        var entity = (ALTHQPMults)Enum.Parse(typeof(ALTHQPMults), qso.ContactEntity);
                        qso.ContactEntity = EnumHelper.GetDescription(entity);
                    }
                    else
                    {
                        qso.InvalidEntity = true;
                        qso.IncorrectDXEntity = $"{qso.ContactEntity} --> should be a Hawai’i district )";
                    }

                    continue;
                }

                // at this point we have the country info
                SetContactCountry(qso);

                if (qso.ContactCountry == HQPHawaiiLiteral)
                {
                    SetNonHQPEntityInfo(qso);
                }
            }

            // happens if operator entity is invalid
            if (qsoCollection[0].ParentLog.IsValidLog == false)
            {
                throw new Exception(qsoCollection[0].OperatorCall + ".log");
            }
        }

        /// <summary>
        /// HQP Only
        /// </summary>
        /// <param name="qsoCollection"></param>
        private void ProcessHawaiiOperators(List<QSO> qsoCollection)
        {
            foreach (QSO qso in qsoCollection)
            {
                if (!CheckForValidCallsign(qso.ContactCall))
                {
                    qso.Status = QSOStatus.InvalidQSO;
                    qso.ReasonRejected = RejectReason.InvalidCall;
                    continue;
                }

                // at this point we have the country info
                SetContactCountry(qso);

                qso.OperatorCountry = HQPHawaiiLiteral;
                qso.HQPEntity = qso.OperatorEntity;
                qso.IsHQPEntity = true;

                if (qso.ContactCountry == HQPCanadaLiteral || qso.ContactCountry == HQPUSALiteral)
                {
                    SetHQPEntityInfo(qso);
                }
            }
        }

        /// <summary>
        /// HQP only
        /// This is an HQP entity so their contacts can be a: 
        /// US State (two chars)
        /// Canadian Province (two chars)
        /// another HQP Entity (three chars)
        /// another country ("DX")
        /// </summary>
        /// <param name="qso"></param>
        private void SetContactCountry(QSO qso)
        {
            string contactEntity = qso.ContactEntity;
            string contactFullCall = qso.ContactCall;

            if (!string.IsNullOrEmpty(qso.ContactPrefix)) {
                contactFullCall = qso.ContactPrefix + "/" + qso.ContactCall;
            }

            if (!string.IsNullOrEmpty(qso.ContactSuffix))
            {
                contactFullCall = qso.ContactCall + "/" + qso.ContactSuffix;
            }


            switch (contactEntity.Length)
            {
                case 2:
                    SetDXorStateContactEntity(qso, contactEntity, contactFullCall);
                    break;
                case 3:
                    SetHawaiiContactEntity(qso, contactEntity);
                    break;
                case 4:
                    SetContactEntityFromGrid(qso, contactEntity);
                    break;
                default:
                    qso.InvalidEntity = true;
                    if (qso.ContactEntity == "MISSING_COLUMN")
                    {
                        qso.IncorrectDXEntity = "The contact entity column is missing";
                    }
                    break;
            }
        }

        private void SetContactEntityFromGrid(QSO qso, string contactEntity)
        {
            if (ValidateGrid(contactEntity))
            {
                // if it is a grid, first see if it is in the grid collection and a grid that does not span states
                // if it spans states use the ULS data
               // qso.ContactEntity = UL
            } else
            {
                qso.InvalidEntity = true;
               // TODO: THIS NEEDS BETTER HANDLING
                qso.IncorrectDXEntity = "The contact entity column is missing";
            }
        }

        /// <summary>
        /// Test for a valid grid - CM98
        /// </summary>
        /// <param name="contactEntity"></param>
        /// <returns></returns>
        private bool ValidateGrid(string contactEntity)
        {
            Regex regex = new Regex("[A-Z{2}0-9{2}]/g", RegexOptions.IgnoreCase);

            if (regex.Match(contactEntity).Success)
            {
                return true;
            }

            return false;
        }

        private static void SetHawaiiContactEntity(QSO qso, string contactEntity)
        {
            if (Enum.IsDefined(typeof(HQPMults), contactEntity))
            {
                qso.ContactCountry = HQPHawaiiLiteral;
            }
            else
            {
                if (Enum.IsDefined(typeof(ALTHQPMults), qso.ContactEntity))
                {
                    // they just used alternate HPQ Mult entity - correct it for them
                    var entity = (ALTHQPMults)Enum.Parse(typeof(ALTHQPMults), qso.ContactEntity);
                    qso.ContactEntity = EnumHelper.GetDescription(entity);
                }
                else
                {
                    qso.InvalidEntity = true;
                    qso.IncorrectDXEntity = $"{qso.ContactEntity} is not valid for this contest";
                }
            }
        }

        private void SetDXorStateContactEntity(QSO qso, string contactEntity, string contactFullCall)
        {
            IEnumerable<CallSignInfo> hitCollection;
            List<CallSignInfo> hitList;

            if (qso.ContactEntity == "DX")
            {
                // need to look it up
                hitCollection = CallLookUp.LookUpCall(contactFullCall);
                hitList = hitCollection.ToList();
                if (hitList.Count != 0)
                {
                    qso.ContactCountry = hitList[0].Country;
                }
                else
                {
                    qso.InvalidEntity = true;
                    qso.IncorrectDXEntity = $"{qso.ContactEntity} --> DX or State or Province";
                }
            }
            else
            {
                if (States.Contains(contactEntity))
                {
                    qso.ContactCountry = HQPUSALiteral;
                }
                else if (Provinces.Contains(contactEntity))
                {
                    qso.ContactCountry = HQPCanadaLiteral;
                }
                else
                {
                    qso.InvalidEntity = true;

                    hitCollection = CallLookUp.LookUpCall(qso.ContactCall);
                    hitList = hitCollection.ToList();
                    if (hitList.Count != 0)
                    {
                        qso.ContactCountry = hitList[0].Country.ToUpper();
                        if (qso.ContactCountry == HQPCanadaLiteral || qso.ContactCountry == HQPUSALiteral)
                        {
                            qso.IncorrectDXEntity = $"{qso.ContactEntity} --> {hitList[0].Province}";
                        }
                        else
                        {
                            qso.IncorrectDXEntity = $"{qso.ContactEntity} --> DX ({qso.ContactCountry})";
                        }
                    }
                    else
                    {
                        qso.IncorrectDXEntity = $"{qso.ContactEntity} --> {qso.ContactCountry}";
                    }
                }
            }
        }


        /// <summary>
        /// Ensure the call is not all alpha or all numeric
        /// </summary>
        /// <param name="contactCall"></param>
        /// <returns></returns>
        private bool CheckForValidCallsign(string contactCall)
        {
            bool isValid = true;

            if (contactCall.All(b => char.IsLetter(b)) == true || contactCall.All(b => char.IsNumber(b)) == true)
            {
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// Non Hawaii log uses "DX" for their own location (Op Entity) instead of their real country
        /// HI8A.log - QSO: 14000 CW 2017-08-27 1712 HI8A 599 DX KH6LC 599 HIL
        /// Set the correct entity for HQP participants.
        /// </summary>
        /// <param name="qso"></param>
        /// <param name="prefixInfo"></param>
        private void SetNonHQPEntityInfo(QSO qso)
        {
            if (qso.Status != QSOStatus.InvalidQSO)
            {
                switch (qso.OperatorEntity)
                {
                    case "DX":
                        IEnumerable<CallSignInfo> hitCollection = CallLookUp.LookUpCall(qso.OperatorCall);
                        List<CallSignInfo> hitList = hitCollection.ToList();
                        if (hitList.Count != 0)
                        {
                            qso.OperatorCountry = hitList[0].Country;

                            if (qso.ContactEntity == HQPCanadaLiteral || qso.ContactEntity == HQPUSALiteral)
                            {
                                qso.ContactCountry = hitList[0].Province;
                            }
                        }
                        else
                        {
                            qso.Status = QSOStatus.InvalidQSO;
                            qso.ReasonRejected = RejectReason.EntityName;
                        }
                        break;
                    case string _ when States.Contains(qso.OperatorEntity):
                        qso.OperatorCountry = HQPUSALiteral;
                        break;
                    case string _ when Provinces.Contains(qso.OperatorEntity):
                        qso.OperatorCountry = HQPCanadaLiteral;
                        return;
                    case string _ when Enum.IsDefined(typeof(HQPMults), qso.OperatorEntity):
                        qso.OperatorCountry = HQPHawaiiLiteral;
                        return;
                    default:
                        if (CheckCountyFiles(qso.OperatorEntity) == null)
                        {
                            qso.InvalidSentEntity = true;
                            FailReason += "The operator entity is invalid: " + Environment.NewLine + qso.RawQSO + Environment.NewLine;
                            qso.ParentLog.IsValidLog = false;
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// This never seems to get hit! -> if (qso.ContactEntity == "DX")
        /// Set the correct entity for HQP participants (contacts).
        /// The correct territory was found from the CallParser component
        /// </summary>
        /// <param name="qso"></param>
        /// <param name="prefixInfo"></param>
        private void SetHQPEntityInfo(QSO qso) // , string territory
        {
            if (qso.ContactEntity == "DX")
            {
                // this is very rarely hit -  This is only for when the US or Canada entry is input as DX by the operator
                IEnumerable<CallSignInfo> hitCollection = CallLookUp.LookUpCall(qso.ContactCall);
                List<CallSignInfo> hitList = hitCollection.ToList();
                if (hitList.Count != 0)
                {
                    qso.ContactEntity = hitList[0].Province;
                }
            }   // counties are 3 or more - a ; in it means a list of states
            else if (qso.ContactEntity.Length > 2)
            {
                // check if its in one of the county files
                qso.ContactEntity = CheckCountyFiles(qso.ContactEntity);
            }
        }

        /// <summary>
        /// Check the county files to find the state before checking
        /// QRZ.com Sometimes they put the county instead of the state.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private string CheckCountyFiles(string entity)
        {
            // check the Ohio file
            if (Ohio.Contains(entity))
            {
                return "OH";
            }

            // check the Kansas file
            if (Kansas.Contains(entity))
            {
                return "KS";
            }

            return null;
        }

        /// <summary>
        /// Move a file to the inspection folder.
        /// </summary>
        /// <param name="fileInfo"></param>
        private void MoveFileToInpectFolder(string fileName, string exception)
        {
            string logFileName;
            string inspectFileName;

            // move the file to inspection folder
            inspectFileName = Path.Combine(InspectionFolder, fileName + ".txt");
            logFileName = Path.Combine(InspectionFolder, fileName);

            if (File.Exists(logFileName))
            {
                File.Delete(logFileName);
            }

            if (File.Exists(inspectFileName))
            {
                File.Delete(inspectFileName);
            }

            if (File.Exists(Path.Combine(WorkingFolder, fileName)))
            {
                File.Move(Path.Combine(WorkingFolder, fileName), logFileName);
            }

            _PrintManager.PrintInspectionReport(fileName + ".txt", FailReason + Environment.NewLine + " - " + exception);
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
            string prefix = string.Empty;
            string suffix = string.Empty;

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
                        OperatorCallSign = ParseCallSign(CheckForNull(lineList.Where(l => l.StartsWith("CALLSIGN:")).DefaultIfEmpty("CALLSIGN: UNKNOWN").First(), 9, "UNKNOWN"), out prefix, out suffix).ToUpper(),
                        OperatorPrefix = prefix,
                        OperatorSuffix = suffix,
                        OperatorCategory = Utility.GetValueFromDescription<CategoryOperator>(lineList.Where(l => l.StartsWith("CATEGORY:")).DefaultIfEmpty("CATEGORY: SINGLE-OP").First().Substring(9).Trim().ToUpper()),
                        // this is for when the CATEGORY-ASSISTED: is missing or has no value
                        Assisted = Utility.GetValueFromDescription<CategoryAssisted>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-ASSISTED:")).DefaultIfEmpty("CATEGORY-ASSISTED: ASSISTED").First(), 18, "ASSISTED")),   //.Substring(18).Trim().ToUpper()),
                        Band = Utility.GetValueFromDescription<QSOBand>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-BAND:")).DefaultIfEmpty("CATEGORY-BAND: ALL").First(), 14, "ALL")),
                        Power = Utility.GetValueFromDescription<CategoryPower>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-POWER:")).DefaultIfEmpty("CATEGORY-POWER: HIGH").First(), 15, "HIGH")),
                        Mode = Utility.GetValueFromDescription<QSOMode>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-MODE:")).DefaultIfEmpty("CATEGORY-MODE: MIXED").First(), 14, "MIXED")),
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
        private int CheckForNumeric(string inputString)
        {
            // first get rid of commas ie. 5,234
            inputString = inputString.Replace(",", "");

            bool result = int.TryParse(inputString, out int i);

            if (!result)
            {
                i = 999999;
            }

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
            string prefix = string.Empty;
            string suffix = string.Empty;

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
                    QTH = lineList.Where(l => l.StartsWith("ADDRESS-STATE-PROVINCE:")).DefaultIfEmpty("ADDRESS-STATE-PROVINCE: ").First().Substring(23).Trim(),
                    Country = lineList.Where(l => l.StartsWith("ADDRESS-COUNTRY:")).DefaultIfEmpty("ADDRESS-COUNTRY: ").First().Substring(16).Trim(),
                    OperatorCallSign = ParseCallSign(CheckForNull(lineList.Where(l => l.StartsWith("CALLSIGN:")).DefaultIfEmpty("CALLSIGN: UNKNOWN").First(), 9, "UNKNOWN"), out prefix, out suffix).ToUpper(),
                    OperatorPrefix = prefix,
                    OperatorSuffix = suffix,
                    OperatorCategory = Utility.GetValueFromDescription<CategoryOperator>(lineList.Where(l => l.StartsWith("CATEGORY-OPERATOR:")).DefaultIfEmpty("CATEGORY-OPERATOR: SINGLE-OP").First().Substring(18).Trim().ToUpper()),
                    // this is for when the CATEGORY-ASSISTED: is missing or has no value
                    Assisted = Utility.GetValueFromDescription<CategoryAssisted>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-ASSISTED:")).DefaultIfEmpty("CATEGORY-ASSISTED: ASSISTED").First(), 18, "ASSISTED")),
                    Band = Utility.GetValueFromDescription<QSOBand>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-BAND:")).DefaultIfEmpty("CATEGORY-BAND: ALL").First(), 14, "ALL")),
                    Power = Utility.GetValueFromDescription<CategoryPower>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-POWER:")).DefaultIfEmpty("CATEGORY-POWER: HIGH").First(), 15, "HIGH")),
                    Mode = Utility.GetValueFromDescription<QSOMode>(CheckForNull(lineList.Where(l => l.StartsWith("CATEGORY-MODE:")).DefaultIfEmpty("CATEGORY-MODE: MIXED").First(), 14, "MIXED")),
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
        /// Handle a null for a header value that is missing.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="length"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private string CheckForNull(string source, int length, string defaultValue)
        {
            if (source.Trim().Length <= length)
            {
                return defaultValue;
            }

            return source.Substring(length).Trim().ToUpper();
        }

        /// <summary>
        /// Clean up the individual lines and create a QSO collection. Ensure each QSO belongs to the 
        /// specified session. Do some validation on each QSO, call sign format, session, duplicates.
        /// http://msdn.microsoft.com/en-us/library/bb513866.aspx
        /// </summary>
        /// <param name="lineList"></param>
        /// <returns></returns>
        private List<QSO> CollectQSOs(List<string> lineList, Session session)
        {
            List<QSO> qsoList = null;
            List<string> tempList = new List<string>();
            string tempLine = null;
            string prefix = string.Empty;
            string suffix = string.Empty;

            WorkingLine = "";

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

                IEnumerable<QSO> qsos =
                     from line in lineList
                     let split = CheckQSOLength(line.Split(' '))
                     select new QSO()
                     {
                         RawQSO = line,
                         Status = CheckCompleteQSO(split, line),
                         Frequency = CheckFrequency(split[1], line),
                         Mode = NormalizeMode(split[2]),
                         QsoDate = split[3],
                         QsoTime = CheckTime(split[4], line),
                         OperatorCall = ParseCallSign(split[5], out prefix, out suffix).ToUpper(),
                         OperatorPrefix = prefix,
                         OperatorSuffix = suffix,
                         SentSerialNumber = ConvertSerialNumber(split[6], line),
                         SentReport = split[6],
                         OperatorName = CheckActiveContest(split[7], "OperatorName").ToUpper(),
                         OperatorEntity = CheckActiveContest(split[7], "OperatorEntity").ToUpper(),
                         ContactCall = ParseCallSign(split[8], out prefix, out suffix).ToUpper(),
                         ContactPrefix = prefix,
                         ContactSuffix = suffix,
                         ReceivedSerialNumber = ConvertSerialNumber(split[9], line),
                         ReceivedReport = split[9],
                         ContactName = CheckActiveContest(split[10], "ContactName").ToUpper(),
                         ContactEntity = CheckActiveContest(split[10], "ContactEntity").ToUpper(),
                         SessionIsValid = CheckForValidSession(session, split[4])
                     };
                qsoList = qsos.ToList();
            }
            catch (Exception ex)
            {
                if (ex is FormatException && ActiveContest == ContestName.CW_OPEN)
                {
                    if (ex.Message == "Serial Number Format Incorrect.")
                    {
                        FailingLine += Environment.NewLine;
                    }
                    else
                    {
                        FailingLine += Environment.NewLine + WorkingLine + " --- " + Environment.NewLine + ex.Message;
                        FailingLine += Environment.NewLine;
                        Console.WriteLine("CollectQSOS()");
                    }
                }
                else
                {
                    if (FailingLine.IndexOf(WorkingLine) == -1)
                    {
                        FailingLine += Environment.NewLine + WorkingLine + " --- " + ex.Message;
                    }
                }
                throw;
            }

            return qsoList;
        }

        /// <summary>
        /// Populate the correct field for the Active Contest
        /// Eliminates confusion later
        /// </summary>
        /// <param name="message"></param>
        /// <param name="literal"></param>
        /// <returns></returns>
        private string CheckActiveContest(string message, string literal)
        {
            switch (ActiveContest)
            {
                case ContestName.CW_OPEN:
                    if (literal == "ContactName" || literal == "OperatorName")
                    {
                        return message;
                    }
                    break;
                case ContestName.HQP:
                    if (literal == "ContactEntity" || literal == "OperatorEntity")
                    {
                        return message;
                    }
                    break;
            }

            return "";
        }

        /// <summary>
        /// Make sure all columns are present. This is 11 because it includes QSO:
        /// </summary>
        /// <param name="split"></param>
        /// <returns></returns>
        private string[] CheckQSOLength(string[] split)
        {
            int missing;

            if (split.Length < 11)
            {
                missing = 11 - split.Length;
                Array.Resize(ref split, 11);

                for (int i = 11 - missing; i < 11; i++)
                {
                    split[i] = "MISSING_COLUMN";
                }

            }

            return split;
        }

        /// <summary>
        /// Normalize all the modes to make searches easier.
        /// Only used in the HQP.
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        private QSOMode NormalizeMode(string mode)
        {
            QSOMode qsoMode = (QSOMode)Enum.Parse(typeof(QSOMode), mode);

            switch (qsoMode)
            {
                case QSOMode.CW:
                    return QSOMode.CW;
                case QSOMode.RTTY:
                    return QSOMode.RY;
                case QSOMode.RY:
                    return QSOMode.RY;
                case QSOMode.FT8:
                    return QSOMode.RY;
                case QSOMode.DG:
                    return QSOMode.RY;
                case QSOMode.DIGI:
                    return QSOMode.RY;
                case QSOMode.PH:
                    return QSOMode.PH;
                case QSOMode.SSB:
                    return QSOMode.PH;
                case QSOMode.USB:
                    return QSOMode.PH;
                default:
                    return qsoMode;
            }
        }



        /// <summary>
        /// Remove the prefix or suffix from call signs so they can be compared.
        /// Sometimes it is KH6/N6JI and sometimes N6JI/KH6.
        /// 
        /// Save the suffix and or prefix so I can use it later to determine
        /// the real dx entity.
        /// </summary>
        /// <param name="callSign"></param>
        /// <returns></returns>
        private string ParseCallSign(string callSign, out string prefix, out string suffix)
        {
            prefix = string.Empty;
            suffix = string.Empty;

            if (callSign.IndexOf("/") != -1)
            {
                string call1 = callSign.Substring(0, callSign.IndexOf("/"));
                string call2 = callSign.Substring(callSign.IndexOf("/"));
                string result = null;
                int temp1 = call1.Length;
                int temp2 = call2.Length - 1;
                bool containsInt;

                // this really isn't necessary but makes it easier later
                if (call2 == "/QRP" || call2 == "/P" || call2 == "/M" || call2 == "/MM" || call2 == "/MOBILE" || call2 == "/AE" || call2 == "/AG" || call2 == "/A" || call2 == "/R")
                //if (call2.All(c => char.IsLetter(c) || c == '/') == true) // this may not be good for kansas and other QSO party prefixes
                {
                    prefix = "";
                    suffix = "";
                    return call1;
                }

                // temp until Alan oks it - for W6OP/65
                if (call2.All(c => char.IsDigit(c) || c == '/') == true)
                {
                    if (call2.Length > 1)
                    {
                        prefix = "";
                        suffix = "";
                        return call1;
                    }
                }

                if (temp1 > temp2)
                {
                    result = new string(call1.Where(x => Char.IsDigit(x)).ToArray());

                    if (!string.IsNullOrEmpty(result))
                    {
                        callSign = call1;
                        suffix = call2.Replace("/", "");

                        // is the suffix a single digit or something like W4 - KHDD/W6 or VK1BL/W6
                        // if so then make it the prefix
                        if (suffix.Length > 1)
                        {
                            result = new string(suffix.Where(x => Char.IsDigit(x)).ToArray());
                            if (!string.IsNullOrEmpty(result))
                            {
                                prefix = suffix;
                                suffix = string.Empty;
                            }
                        }
                    }
                    else
                    {
                        // KCCXX - bad call so need to flag or return null or error
                        callSign = call1;
                        // prefix = call1.Replace("/", "");
                    }
                }
                else if (temp1 == temp2)
                {
                    containsInt = call1.Any(char.IsDigit);
                    //result = new string(call1.Where(x => Char.IsDigit(x)).ToArray());
                    if (containsInt)
                    {
                        callSign = call1;
                        suffix = call2.Replace("/", "");
                    }
                    else
                    {
                        callSign = call2.Substring(call2.IndexOf("/") + 1);
                        prefix = call1.Replace("/", "");
                    }
                }
                else
                {
                    result = new string(call2.Where(x => char.IsDigit(x)).ToArray());
                    if (!string.IsNullOrEmpty(result))
                    {
                        callSign = call2.Substring(call2.IndexOf("/") + 1);
                        prefix = call1.Replace("/", "");
                    }
                    else
                    {
                        callSign = call1;
                        suffix = call2.Replace("/", "");
                    }
                }
            }

            return callSign;
        }

        /// <summary>
        /// Check and see if every field is in the line.
        /// </summary>
        /// <param name="split"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        private QSOStatus CheckCompleteQSO(string[] split, string line)
        {
            QSOStatus status = QSOStatus.ValidQSO;

            WorkingLine = line;

            if (split[10] == "MISSING_COLUMN")
            {
                status = QSOStatus.IncompleteQSO;
                FailingLine += Environment.NewLine + "One or more columns are missing.";
                FailingLine += Environment.NewLine + line;
            }

            return status;
        }

        /// <summary>
        /// Check if the frequency field is formatted correctly.
        /// The 999999 means I was unabble to parse the field
        /// to a valid frequency.
        /// </summary>
        /// <param name="frequency"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        private string CheckFrequency(string frequency, string line)
        {
            WorkingLine = line;

            if (CheckForNumeric(frequency) == 999999)
            {
                FailingLine += Environment.NewLine + "Frequency is not correctly formatted.";
                FailingLine += Environment.NewLine + line;
            }

            return frequency;
        }

        /// <summary>
        /// Check if the time field is formatted correctly.
        /// The 999999 means I was unabble to parse the field
        /// to a valid time.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        private string CheckTime(string time, string line)
        {
            WorkingLine = line;

            if (CheckForNumeric(time) == 999999)
            {
                FailingLine += Environment.NewLine + "The time is not correctly formatted. Possibly alpha instead of numeric";
                FailingLine += Environment.NewLine + line;
            }

            return time;
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
        private bool CheckForValidSession(Session session, string qsoTime)
        {
            bool isValidSession = false;
            int qsoSessionTime = Convert.ToInt16(qsoTime);

            // later may want to check time stamp for that contest
            if (ActiveContest != ContestName.CW_OPEN)
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
        /// Convert a string to an int. Also extract a number from a string as a serial
        /// number may be O39 instead of 039.
        /// </summary>
        /// <param name="serialNumber"></param>
        /// <returns></returns>
        private int ConvertSerialNumber(string serialNumber, string line)
        {
            int number; // catches invalid serial number

            // catch when SN is swapped with Name - set serial number so it will never match
            if (Regex.Match(serialNumber, @"\d+").Success)
            {
                serialNumber = Regex.Match(serialNumber, @"\d+").Value;
                number = Convert.ToInt32(serialNumber);
            }
            else
            {
                FailingLine += Environment.NewLine + "The Serial Number is not correctly formatted.";
                FailingLine += Environment.NewLine + line;
                throw new FormatException("Serial Number Format Incorrect.");
            }

            return number;
        }

        #endregion
    } // end class
}
