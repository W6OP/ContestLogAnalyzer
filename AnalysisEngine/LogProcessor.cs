using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using W6OP.CallParser;
using W6OP.PrintEngine;
using System.Collections;
using System.Threading;

namespace W6OP.ContestLogAnalyzer
{
    public delegate void ErrorRaised(string error);

    public class LogProcessor
    {
        readonly string[] States = { "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "DC", "FL", "GA", "HI", "ID", "IL", "IN", "IA", "KS", "KY", "LA", "ME", "MD", "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ", "NM", "NY", "NC", "ND", "OH", "OK", "OR", "PA", "RI", "SC", "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY", };
        readonly string[] Provinces = { "NL", "NS", "PE", "NB", "QC", "ON", "MB", "SK", "AB", "BC", "YT", "NT", "NU" };

        public delegate void ProgressUpdate(Int32 progress);
        public event ProgressUpdate OnProgressUpdate;

        private const string HQPHawaiiLiteral = "HAWAII";
        private const string HQPUSALiteral = "UNITED STATES OF AMERICA";
        //private const string HQPAlaskaLiteral = "ALASKA";
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

        // later replace this
        //private Hashtable PrefixTable;
        // with
        // private ILookup<string, string> _PrefixTable;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public LogProcessor()
        {
            FailingLine = "";
            WorkingLine = "";


        }

        /// <summary>
        /// Create a list of all of the log files in the working folder. Once the list is
        /// filled pass the list on to another thread.
        /// </summary>
        public int BuildFileList(Session session, out IEnumerable<FileInfo> logFileList)
        {
            string fileNameFormat = null;
            // Take a snapshot of the file system. http://msdn.microsoft.com/en-us/library/bb546159.aspx
            DirectoryInfo dir = new DirectoryInfo(LogSourceFolder);

            switch (ActiveContest)
            {
                case ContestName.CW_OPEN:
                    fileNameFormat = "*_" + ((uint)session).ToString() + ".log";
                    break;
                case ContestName.HQP:
                    //InitializeHQPLogProcessor();
                    fileNameFormat = "*.log";
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
        //int bcount = 0;
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
            string version = null;
            string reason = "Unable to build valid header. Check the Inspect folder for details.";
            int count = 0;

            FailReason = reason;

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

                    // List<QSO> validQsoList = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO).ToList();

                    // make sure minimum amout of information is correct
                    if (contestLog.LogHeader != null && AnalyzeHeader(contestLog, out reason) == true)
                    {
                        contestLog.LogHeader.HeaderIsValid = true;
                        FailReason = "Check the Inspect folder for details.";
                    }
                    else
                    {
                        FailReason = reason;
                        contestLog.IsValidLog = false;

                        throw new Exception(fileName); // don't want this added to collection
                    }

                    // Find DUPES in list
                    //http://stackoverflow.com/questions/454601/how-to-count-duplicates-in-list-with-linq

                    // this statement says to copy all QSO lines
                    // read all the QSOs in and mark any that are duplicates, bad call format, incorrect session
                    lineListX = lineList.Where(x => (x.IndexOf("XQSO:", 0) != -1) || (x.IndexOf("X-QSO:", 0) != -1)).ToList();
                    lineList = lineList.Where(x => (x.IndexOf("QSO:", 0) != -1) && (x.IndexOf("XQSO:", 0) == -1) && (x.IndexOf("X-QSO:", 0) == -1)).ToList();

                    switch (ActiveContest)
                    {
                        case ContestName.CW_OPEN:
                            contestLog.Session = (int)session;
                            break;
                        default:
                            break;
                    }

                    FailingLine = "";

                    //bcount += 1;
                    //Console.WriteLine(bcount.ToString());

                    contestLog.QSOCollection = CollectQSOs(lineList, session, false);
                    // add a reference to the parent log to each QSO
                    contestLog.QSOCollection.Select(c => { c.ParentLog = contestLog; return c; }).ToList();

                    // collect all the X-QSOs so we can mark them as IsXQSO - this log won't count them later but 
                    // others can get credit for them
                    xQSOCollection = CollectQSOs(lineListX, session, false);
                    xQSOCollection.Select(c => { c.ParentLog = contestLog; return c; }).ToList();
                    xQSOCollection.Select(c => { c.IsXQSO = true; return c; }).ToList();

                    // merge the two lists together so other logs can search for everything
                    contestLog.QSOCollection = contestLog.QSOCollection.Union(xQSOCollection).ToList();

                    //  it will never be null because line 186 will have an exception first
                    if (contestLog.QSOCollection.Count == 0)
                    {
                        // may want to expand on this for a future report
                        FailReason = "One or more QSOs may be in an invalid format."; // create enum
                        if (FailingLine.Length > 0)
                        {
                            FailReason += Environment.NewLine + FailingLine;
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
                            FailReason = "QSO collection is empty - Invalid session"; // create enum
                            contestLog.IsValidLog = false;
                            throw new Exception(fileInfo.Name); // don't want this added to collection
                        }
                    }

                    // find out if columns are missing - do something else, need to use reject reason
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
                        FailReason = "Name sent is 'NAME' - Invalid name."; // create enum
                        contestLog.IsValidLog = false;
                        throw new Exception(fileInfo.Name); // don't want this added to collection
                    }

                    // now add the DXCC information some contests need for multipliers
                    if (ActiveContest == ContestName.HQP)
                    {
                        SetHQPDXCCInformation(contestLog.QSOCollection, contestLog);
                    }

                    contestLogs.Add(contestLog);
                    OnProgressUpdate?.Invoke(contestLogs.Count);
                }
            }
            catch (Exception ex)
            {
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
            }

            return logFileName;
        }

        /// <summary>
        /// HQP Only
        /// now add the DXCC country information the HQP needs for multipliers
        /// </summary>
        /// <param name="qsoCollection"></param>
        /// <param name="contestLog"></param>
        private void SetHQPDXCCInformation(List<QSO> qsoCollection, ContestLog contestLog)
        {
            string[] info;
            string operatorCall;
            string contactCall;

            contestLog.IsHQPEntity = false;
            contestLog.TotalPoints = 0;


            // FIGURE OUT WHAT IS HAPPENING HERE AND MAKE SOME COMMENTS.
            bool isValidHQPEntity = Enum.IsDefined(typeof(HQPMults), contestLog.QSOCollection[0].OperatorEntity);
            contestLog.IsHQPEntity = isValidHQPEntity;

            foreach (QSO qso in qsoCollection)
            {
                info = new string[2] { "0", "0" };

                operatorCall = qso.OperatorCall;
                contactCall = qso.ContactCall;

                if (!CheckForValidCallsign(contactCall))
                {
                    qso.Status = QSOStatus.InvalidQSO;
                    qso.GetRejectReasons().Clear();
                    qso.GetRejectReasons().Add(RejectReason.InvalidCall, EnumHelper.GetDescription(RejectReason.InvalidCall));
                    continue;
                }

                // -----------------------------------------------------------------------------------
                // DX station contacting Hawaiin station
                // if DXEntity is not in enum list of Hawaii entities (HIL, MAU, etc.)
                // QSO: 14242 PH 2017-08-26 1830 N5KXI 59 OK KH7XS 59  DX
                // this QSO is invalid - complete
                if (!contestLog.IsHQPEntity && qso.ContactEntity == "DX")
                {
                    qso.EntityIsInValid = true;
                    qso.Status = QSOStatus.InvalidQSO;
                    qso.GetRejectReasons().Clear();
                    qso.GetRejectReasons().Add(RejectReason.NotCounted, EnumHelper.GetDescription(RejectReason.NotCounted));
                    continue;
                }

                // at this point we have the country info
                SetContactEntity(qso);

                if (isValidHQPEntity)
                {
                    qso.HQPEntity = qso.OperatorEntity;
                    qso.IsHQPEntity = isValidHQPEntity;
                    if (qso.ContactCountry == HQPCanadaLiteral || qso.ContactCountry == HQPUSALiteral)
                    {
                        SetHQPEntityInfo(qso); //, territory
                    }
                }
                else if (qso.ContactCountry == HQPHawaiiLiteral)
                {
                    SetNonHQPEntityInfo(qso);
                }
                else
                {
                    // this is a non Hawaiian station that has a non Hawaiian contact - maybe another QSO party
                    qso.Status = QSOStatus.InvalidQSO;
                    qso.GetRejectReasons().Clear();
                    qso.GetRejectReasons().Add(RejectReason.NotCounted, EnumHelper.GetDescription(RejectReason.NotCounted));
                }
            }
        }

        /// <summary>
        /// This is an HQP entity so their contacts can be a: 
        /// US State (two chars)
        /// Canadian Province (two chars)
        /// another HQP Entity (three chars)
        /// another country ("DX")
        /// </summary>
        /// <param name="qso"></param>
        private void SetContactEntity(QSO qso)
        {
            IEnumerable<CallSignInfo> hitCollection; // = CallLookUp.LookUpCall(operatorCall);
            List<CallSignInfo> hitList; // = hitCollection.ToList();
            string contactEntity = qso.ContactEntity;

            switch (contactEntity.Length)
            {
                case 2:
                    if (contactEntity == "DX")
                    {
                        // need to look it up
                        hitCollection = CallLookUp.LookUpCall(qso.ContactCall);
                        hitList = hitCollection.ToList();
                        if (hitList.Count != 0)
                        {
                            qso.ContactCountry = hitList[0].Country;
                        }
                        else
                        {
                            qso.Status = QSOStatus.InvalidQSO;
                            qso.GetRejectReasons().Clear();
                            qso.GetRejectReasons().Add(RejectReason.InvalidEntity, EnumHelper.GetDescription(RejectReason.InvalidEntity));
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
                            qso.Status = QSOStatus.InvalidQSO;
                            qso.GetRejectReasons().Clear();
                            qso.GetRejectReasons().Add(RejectReason.InvalidEntity, EnumHelper.GetDescription(RejectReason.InvalidEntity));
                        }
                    }
                    break;
                case 3:
                    if (Enum.IsDefined(typeof(HQPMults), contactEntity))
                    {
                        qso.ContactCountry = HQPHawaiiLiteral;
                    }
                    else if (CheckCountyFiles(qso.ContactEntity) != null)
                    {
                        qso.ContactEntity = CheckCountyFiles(qso.ContactEntity);
                        if (States.Contains(qso.ContactEntity))
                        {
                            qso.ContactCountry = HQPUSALiteral;
                        } else
                        {
                            qso.Status = QSOStatus.InvalidQSO;
                            qso.GetRejectReasons().Clear();
                            qso.GetRejectReasons().Add(RejectReason.EntityName, EnumHelper.GetDescription(RejectReason.EntityName));
                        }
                    }
                    else
                    {
                        qso.Status = QSOStatus.InvalidQSO;
                        qso.GetRejectReasons().Clear();
                        qso.GetRejectReasons().Add(RejectReason.EntityName, EnumHelper.GetDescription(RejectReason.EntityName));
                    }
                    break;
                case 4:
                   if (CheckCountyFiles(qso.ContactEntity) != null)
                    {
                        qso.ContactEntity = CheckCountyFiles(qso.ContactEntity);
                        if (States.Contains(qso.ContactEntity))
                        {
                            qso.ContactCountry = HQPUSALiteral;
                        }
                        else
                        {
                            qso.Status = QSOStatus.InvalidQSO;
                            qso.GetRejectReasons().Clear();
                            qso.GetRejectReasons().Add(RejectReason.EntityName, EnumHelper.GetDescription(RejectReason.EntityName));
                        }
                    }
                    break;
                default:
                    qso.Status = QSOStatus.InvalidQSO;
                    qso.GetRejectReasons().Clear();
                    qso.GetRejectReasons().Add(RejectReason.EntityName, EnumHelper.GetDescription(RejectReason.EntityName));
                    break;
            }
        }

        //// This is probably where I should verify the state if USA
        //// if ContactEntity is more than 2 chars I need to fix so later I get correct 
        //// entity in LogAnalyser SearchForIncorrect entity?
        //// should really enhance PrefixTable later to hold tuples so I can do this earlier
        //// I do this here because if it is a non hawaii to non hawaii contact it is invalid and this 
        //// never gets fixed.
        ///Must be a state, canadian province or HQPentity or country
        //if (qso.ContactEntity.Length > 2) // counties are 3 or more
        //{
        //    // check if its in one of the county files
        //    qso.ContactEntity = CheckCountyFiles(qso.ContactEntity);
        //}
        private string SetContactInformation(QSO qso, string contactCall)
        {
            IEnumerable<CallSignInfo> hitCollection;
            List<CallSignInfo> hitList;
            string entity = null;

            if (qso.ContactPrefix != string.Empty)
            {
                hitCollection = CallLookUp.LookUpCall(qso.ContactPrefix + "/" + contactCall);
                hitList = hitCollection.ToList();
                if (hitList.Count != 0)
                {
                    if (hitList.Count == 1)
                    {
                        return hitList[0].Country;
                    }
                    entity = RefineEntity(hitList);
                }
            }
            else if (qso.ContactSuffix != string.Empty)
            {
                hitCollection = CallLookUp.LookUpCall(contactCall + "/" + qso.ContactSuffix);
                hitList = hitCollection.ToList();
                if (hitList.Count != 0)
                {
                    if (hitList.Count == 1)
                    {
                        return hitList[0].Country;
                    }
                    entity = RefineEntity(hitList);
                }
            }
            else
            {
                hitCollection = CallLookUp.LookUpCall(contactCall);
                hitList = hitCollection.ToList();
                if (hitList.Count != 0)
                {
                    if (hitList.Count == 1)
                    {
                        return hitList[0].Country;
                    }
                    entity = RefineEntity(hitList);
                }
            }
           

            // NOTE: check for AC7N and see if I have to do anything special for him
            if (entity == null)
            {
                qso.Status = QSOStatus.InvalidQSO;
                qso.GetRejectReasons().Clear();
                qso.GetRejectReasons().Add(RejectReason.InvalidCall, EnumHelper.GetDescription(RejectReason.InvalidCall));
                return entity;
            }


            return entity.ToUpper();
        }

        private string RefineEntity(List<CallSignInfo> hitList)
        {



            return "";
            
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
                if (qso.OperatorEntity == "DX")
                {
                    // prefixInfo = GetPrefixInformation(qso.OperatorCall);
                    IEnumerable<CallSignInfo> hitCollection = CallLookUp.LookUpCall(qso.OperatorCall);
                    List<CallSignInfo> hitList = hitCollection.ToList();
                    if (hitList.Count != 0)
                    {
                        qso.OperatorEntity = hitList[0].Country;

                        if (qso.ContactEntity == HQPCanadaLiteral || qso.ContactEntity == HQPUSALiteral)
                        {
                            qso.ContactCountry = hitList[0].Province;
                        }
                    }
                    else
                    {
                        qso.Status = QSOStatus.InvalidQSO;
                        qso.GetRejectReasons().Clear();
                        qso.GetRejectReasons().Add(RejectReason.EntityName, EnumHelper.GetDescription(RejectReason.EntityName));
                    }
                }
            }
        }

        /// <summary>
        /// Set the correct entity for HQP participants (contacts).
        ///
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
            }   // counties are 3 or more - a ; in it means a list od states
            else if (qso.ContactEntity.Length > 2 && qso.ContactEntity.IndexOf(";") != -1)
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

        //// determine points by mode for the HQP
        //private int GetPoints(string mode)
        //{
        //    int points;

        //    try
        //    {
        //        CategoryMode catMode = (CategoryMode)Enum.Parse(typeof(CategoryMode), mode);

        //        switch (catMode)
        //        {
        //            case CategoryMode.CW:
        //                points = 3;
        //                break;
        //            case CategoryMode.RTTY:
        //                points = 3;
        //                break;
        //            case CategoryMode.RY:
        //                points = 3;
        //                break;
        //            case CategoryMode.PH:
        //                points = 2;
        //                break;
        //            case CategoryMode.SSB:
        //                points = 2;
        //                break;
        //            default:
        //                points = 0;
        //                break;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        throw new Exception("The mode " + mode + " is not valid for this contest.");
        //    }

        //    return points;
        //}

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
                    OperatorCallSign = ParseCallSign(CheckForNull(lineList.Where(l => l.StartsWith("CALLSIGN:")).DefaultIfEmpty("CALLSIGN: UNKNOWN").First(), 9, "UNKNOWN"), out prefix, out suffix).ToUpper(),
                    OperatorPrefix = prefix,
                    OperatorSuffix = suffix,
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

                if (!reverse)
                {
                    IEnumerable<QSO> qso =
                         from line in lineList
                         let split = CheckQSOLength(line.Split(' '))
                         select new QSO()
                         {
                             Status = CheckCompleteQSO(split, line),
                             Frequency = CheckFrequency(split[1], line),
                             Mode = NormalizeMode(split[2]),
                             QsoDate = split[3],
                             QsoTime = CheckTime(split[4], line),
                             OperatorCall = ParseCallSign(split[5], out prefix, out suffix).ToUpper(),
                             OperatortPrefix = prefix,
                             OperatorSuffix = suffix,
                             SentSerialNumber = ConvertSerialNumber(split[6], line),
                             OperatorName = CheckActiveContest(split[7], "OperatorName"),
                             OperatorEntity = CheckActiveContest(split[7], "OperatorEntity"),
                             OriginalOperatorEntity = CheckActiveContest(split[7], "OperatorEntity"),
                             ContactCall = ParseCallSign(split[8], out prefix, out suffix).ToUpper(),
                             ContactPrefix = prefix,
                             ContactSuffix = suffix,
                             ReceivedSerialNumber = ConvertSerialNumber(split[9], line),
                             ContactName = CheckActiveContest(split[10], "ContactName"),
                             ContactEntity = CheckActiveContest(split[10], "ContactEntity"),
                             OriginalContactEntity = CheckActiveContest(split[10], "ContactEntity"),
                             CallIsInValid = false,  //CheckCallSignFormat(ParseCallSign(split[5]).ToUpper()), Do I need this?? ValidateCallSign(split[8].ToUpper())
                             SessionIsValid = CheckForvalidSession(session, split[4])
                         };
                    qsoList = qso.ToList();
                }
                else
                {
                    IEnumerable<QSO> qso =
                         from line in lineList
                         let split = CheckQSOLength(line.Split(' '))

                         select new QSO()
                         {
                             Status = CheckCompleteQSO(split, line),
                             Frequency = CheckFrequency(split[1], line),
                             Mode = NormalizeMode(split[2]),
                             QsoDate = split[3],
                             QsoTime = CheckTime(split[4], line),
                             OperatorCall = ParseCallSign(split[5], out prefix, out suffix).ToUpper(),
                             OperatortPrefix = prefix,
                             OperatorSuffix = suffix,
                             OperatorName = CheckActiveContest(split[6], "OperatorName"),
                             OperatorEntity = CheckActiveContest(split[6], "OperatorEntity"),
                             OriginalOperatorEntity = CheckActiveContest(split[6], "OperatorEntity"),
                             SentSerialNumber = ConvertSerialNumber(split[7], line),
                             ContactCall = ParseCallSign(split[8], out prefix, out suffix).ToUpper(),
                             ContactPrefix = prefix,
                             ContactSuffix = suffix,
                             ContactName = CheckActiveContest(split[9], "ContactName"),
                             ContactEntity = CheckActiveContest(split[9], "ContactEntity"),
                             OriginalContactEntity = CheckActiveContest(split[9], "ContactEntity"),
                             ReceivedSerialNumber = ConvertSerialNumber(split[10], line),
                             CallIsInValid = false,  //CheckCallSignFormat(ParseCallSign(split[5]).ToUpper()), Do I need this??
                             SessionIsValid = CheckForvalidSession(session, split[4])
                         };
                    qsoList = qso.ToList();
                }
            }
            catch (Exception ex)
            {
                if (ex is FormatException && reverse == false && ActiveContest == ContestName.CW_OPEN)
                {
                    if (ex.Message == "Serial Number Format Incorrect.")
                    {
                        FailingLine += Environment.NewLine;
                    }
                    else
                    {
                        qsoList = CollectQSOs(lineList, session, true);
                    }
                }
                else
                {
                    if (FailingLine.IndexOf(WorkingLine) == -1)
                    {
                        FailingLine += Environment.NewLine + WorkingLine + " --- " + ex.Message;
                    }
                }
            }

            return qsoList;
        }

        /// <summary>
        /// Populate the correct field for the Active Contest
        /// Eliminates cofusion later
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
        /// Make sure all columns are present
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
                    split[i] = "missing_column";
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
        private string NormalizeMode(string mode)
        {
            if (ActiveContest == ContestName.HQP)
            {
                CategoryMode catMode = (CategoryMode)Enum.Parse(typeof(CategoryMode), mode);

                switch (catMode)
                {
                    case CategoryMode.CW:
                        mode = "CW";
                        break;
                    case CategoryMode.RTTY:
                        mode = "RY";
                        break;
                    case CategoryMode.RY:
                        mode = "RY";
                        break;
                    case CategoryMode.FT8:
                        mode = "RY";
                        break;
                    case CategoryMode.DG:
                        mode = "RY";
                        break;
                    case CategoryMode.DIGI:
                        mode = "RY";
                        break;
                    case CategoryMode.PH:
                        mode = "PH";
                        break;
                    case CategoryMode.SSB:
                        mode = "PH";
                        break;
                    case CategoryMode.USB:
                        mode = "PH";
                        break;
                    default:
                        mode = "Unknown";
                        break;
                }
            }

            return mode;
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
                    result = new String(call1.Where(x => Char.IsDigit(x)).ToArray());

                    if (!string.IsNullOrEmpty(result))
                    {
                        callSign = call1;
                        suffix = call2.Replace("/", "");

                        // is the suffix a single digit or something like W4 - KHDD/W6 or VK1BL/W6
                        // if so then make it the prefix
                        if (suffix.Length > 1)
                        {
                            result = new String(suffix.Where(x => Char.IsDigit(x)).ToArray());
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
                    //result = new String(call1.Where(x => Char.IsDigit(x)).ToArray());
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
                    result = new String(call2.Where(x => Char.IsDigit(x)).ToArray());
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
        /// Check and see if every field is in the line
        /// </summary>
        /// <param name="split"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        private QSOStatus CheckCompleteQSO(string[] split, string line)
        {
            QSOStatus status = QSOStatus.ValidQSO;

            WorkingLine = line;

            if (split[10] == "missing_column")
            {
                status = QSOStatus.InvalidQSO;
                FailingLine += Environment.NewLine + "One or more columns are missing.";
                FailingLine += Environment.NewLine + line;
            }

            return status;
        }

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
        private bool CheckForvalidSession(Session session, string qsoTime)
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
        /// Convert a string to an Int32. Also extract a number from a string as a serial
        /// number may be O39 instead of 039.
        /// </summary>
        /// <param name="serialNumber"></param>
        /// <returns></returns>
        private Int32 ConvertSerialNumber(string serialNumber, string line)
        {
            Int32 number; // catches invalid serial number

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

        /// <summary>
        /// Check for empty call.
        /// Check for no alpha characters.
        /// A call must be made up of only alpha, numeric and can have one or more "/".
        /// Must start with letter or number.
        /// </summary>
        /// <param name="callSign"></param>
        /// <returns></returns>
        //private bool ValidateCallSign(string callSign)
        //{
        //    // check for empty or null string
        //    if (string.IsNullOrEmpty(callSign)) { return false; }

        //    // check if first character is "/"
        //    if (callSign.IndexOf("/", 0, 1) == 0) { return false; }

        //    // check if second character is "/"
        //    if (callSign.IndexOf("/", 1, 1) == 1) { return false; }

        //    // check for a "-" ie: VE7CC-7, OH6BG-1, WZ7I-3 
        //    if (callSign.IndexOf("-") != -1) { return false; }

        //    // can't be all numbers
        //    if (IsNumeric(callSign)) { return false; }

        //    // look for at least one number character
        //    if (!callSign.Where(x => Char.IsDigit(x)).Any()) { return false; }

        //    return true;
        //}

        public bool IsNumeric(string value)
        {
            return value.All(char.IsNumber);
        }

        /// <summary>
        /// Quick check to see if a call sign is formatted correctly.
        /// </summary>
        /// <param name="call"></param>
        /// <returns></returns>
        //private bool CheckCallSignFormat(string call)
        //{
        //    string regex = @"^([A-Z]{1,2}|[0-9][A-Z])([0-9])([A-Z]{1,3})$";
        //    bool invalid = true;

        //    // should this pass "DR50RRDXA" it does not currently
        //    if (Regex.IsMatch(call.ToUpper(), regex, RegexOptions.IgnoreCase))
        //    {
        //        invalid = false;
        //    }

        //    if (call == "S57DX")
        //    {
        //        invalid = false;
        //    }

        //    return invalid;
        //}

        #endregion
    } // end class
}
