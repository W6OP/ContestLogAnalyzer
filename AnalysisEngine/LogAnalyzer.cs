using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Objects.SqlClient;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using W6OP.CallParser;

namespace W6OP.ContestLogAnalyzer
{
    public class LogAnalyzer
    {
        public delegate void ProgressUpdate(string value, string qsoCount, string validQsoCount, Int32 progress);
        public event ProgressUpdate OnProgressUpdate;

        private const string HQPHawaiiLiteral = "HAWAII";
        private const string HQPUSALiteral = "UNITED STATES OF AMERICA";
        private const string HQPAlaskaLiteral = "ALASKA";
        private const string HQPCanadaLiteral = "CANADA";

        readonly string[] States = { "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "DC", "FL", "GA", "HI", "ID", "IL", "IN", "IA", "KS", "KY", "LA", "ME", "MD", "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ", "NM", "NY", "NC", "ND", "OH", "OK", "OR", "PA", "RI", "SC", "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY", };
        readonly string[] Provinces = { "NL", "NS", "PE", "NB", "QC", "ON", "MB", "SK", "AB", "BC", "YT", "NT", "NU" };


        public ContestName ActiveContest;

        private ILookup<string, string> _BadCallList;

        public ILookup<string, string> BadCallList { set => _BadCallList = value; }

        public CallLookUp CallLookUp;

        /// <summary>
        /// Dictionary of all calls in all the logs as keys
        /// the values are a list of all logs those calls are in
        /// this almost quadruples lookup performance
        /// </summary>
        public Dictionary<string, List<ContestLog>> CallDictionary;
        public Dictionary<int, List<ContestLog>> BandDictionary;
        public Dictionary<string, List<ContestLog>> ModeDictionary;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public LogAnalyzer()
        {

        }

        /// <summary>
        /// Start processing individual logs.
        /// Find all of the logs that have the log owners call sign in them.
        /// Find all the logs that do not have a reference to this log.
        /// Now each log is self contained and can be operated on without opening other files.
        /// </summary>

        /// <summary>
        /// At this point we have already marked duplicate QSOs as invalid and
        /// any in the wrong or those with an incorrect call sign format invalid
        /// any QSOs where the op name or call sign doesn't match have been maked invalid
        /// 
        /// Open first log.
        /// look at first QSO.
        /// Do we already have a reference to it in our matching log collection, may have multiple
        /// if we have multiple, check band and sent?
        /// If it is in our matching collection, look in the log it belongs to for sent/recv. S/N, Band, etc - did we already do this?
        /// Does it show up in any other logs? Good check to see if it is a valid callsign
        /// Build a collection of totaly unique calls
        /// </summary>
        public void PreAnalyzeContestLogsOld(List<ContestLog> contestLogList, Dictionary<string, List<ContestLog>> callDictionary, Dictionary<int, List<ContestLog>> bandlDictionary, Dictionary<string, List<ContestLog>> modeDictionary)
        {
            CallDictionary = callDictionary;
            BandDictionary = bandlDictionary;
            ModeDictionary = modeDictionary;

            List<QSO> qsoList = new List<QSO>();
            string call = null;
            string name = null;
            int progress = 0;
            int validQsos;

            // Display the label
            OnProgressUpdate?.Invoke("1", "", "", 0);

            try
            {
                foreach (ContestLog contestLog in contestLogList)
                {
                    call = contestLog.LogOwner;

                    progress++;

                    if (!contestLog.IsCheckLog && contestLog.IsValidLog)
                    {
                        qsoList = contestLog.QSOCollection;

                        MarkIncorrectCallSigns(qsoList, call);

                        switch (ActiveContest)
                        {
                            case ContestName.CW_OPEN:
                                name = contestLog.LogHeader.NameSent;
                                MarkIncorrectSentName(qsoList, name);
                                break;
                            case ContestName.HQP:
                                // find the operator entity used on the majority of qsos
                                // this may be useful other places too
                                // maybe call all QSOs for an individual for their entity
                                var mostUsedOperatorEntity = qsoList
                                    .GroupBy(q => q.OperatorEntity)
                                    .OrderByDescending(gp => gp.Count())
                                    .Take(5)
                                    .Select(g => g.Key).ToList();

                                MarkIncorrectSentEntity(qsoList, mostUsedOperatorEntity[0]);
                                break;
                        }

                        /*
                         if (ActiveContest == ContestName.HQP)
                {
                    found = SearchForIncorrectEntity(qso, contestLogList);
                }
                else
                {
                    found = SearchForIncorrectName(qso, contestLogList);
                } 
                         */



                        MatchQSOs(qsoList, contestLogList, call);




                        MarkDuplicateQSOs(qsoList);

                        validQsos = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO).Count();

                        // ReportProgress with Callsign
                        OnProgressUpdate?.Invoke(call, contestLog.QSOCollection.Count.ToString(), validQsos.ToString(), progress);
                    }
                }

            }
            catch (Exception ex)
            {
                OnProgressUpdate?.Invoke("Error" + ex.Message, "", "", 0);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contestLogList"></param>
        /// <param name="callDictionary"></param>
        /// <param name="bandlDictionary"></param>
        /// <param name="modeDictionary"></param>
        public void PreAnalyzeContestLogs(List<ContestLog> contestLogList, Dictionary<string, List<ContestLog>> callDictionary, Dictionary<int, List<ContestLog>> bandlDictionary, Dictionary<string, List<ContestLog>> modeDictionary)
        {
            CallDictionary = callDictionary;
            BandDictionary = bandlDictionary;
            ModeDictionary = modeDictionary;

            List<QSO> qsoList = new List<QSO>();
            string call = null;
            string name = null;
            int progress = 0;
            int validQsos;

            // Display the label
            OnProgressUpdate?.Invoke("1", "", "", 0);

            try
            {
                foreach (ContestLog contestLog in contestLogList)
                {
                    call = contestLog.LogOwner;

                    progress++;

                    if (!contestLog.IsCheckLog && contestLog.IsValidLog)
                    {
                        qsoList = contestLog.QSOCollection;

                        MarkIncorrectCallSigns(qsoList, call);

                        switch (ActiveContest)
                        {
                            case ContestName.CW_OPEN:
                                name = contestLog.LogHeader.NameSent;
                                MarkIncorrectSentName(qsoList, name);
                                break;
                            case ContestName.HQP:
                                // find the operator entity used on the majority of qsos
                                // this may be useful other places too
                                // maybe call all QSOs for an individual for their entity
                                var mostUsedOperatorEntity = qsoList
                                    .GroupBy(q => q.OperatorEntity)
                                    .OrderByDescending(gp => gp.Count())
                                    .Take(5)
                                    .Select(g => g.Key).ToList();

                                MarkIncorrectSentEntity(qsoList, mostUsedOperatorEntity[0]);
                                break;
                        }

                        foreach (QSO qso in qsoList)
                        {
                            if (qso.Status == QSOStatus.ValidQSO)
                            {
                                if (ActiveContest == ContestName.HQP)
                                {
                                    SearchForIncorrectEntity(qso);
                                    // probably should only look at valid QSOS
                                    // may combine this withMatchQSOs()
                                    if (qso.Status != QSOStatus.InvalidQSO)
                                    {
                                        SearchHQPBustedCallSigns(qso);
                                    }
                                }
                                else
                                {
                                    SearchForIncorrectName(qso);
                                }
                            }
                        }

                        //MatchQSOs(qsoList, contestLogList, call);

                        MarkDuplicateQSOs(qsoList);

                        validQsos = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO).Count();
                     
                        // ReportProgress with Callsign
                        OnProgressUpdate?.Invoke(call, contestLog.QSOCollection.Count.ToString(), validQsos.ToString(), progress);
                    }
                }

            }
            catch (Exception ex)
            {
                OnProgressUpdate?.Invoke("Error" + ex.Message, "", "", 0);
            }
        }

        /// <summary>
        /// All we want to do here is look for matching QSOs
        /// I wonder if I still need this?
        /// </summary>
        /// <param name="contestLogList"></param>
        public void PreAnalyzeContestLogsReverse(List<ContestLog> contestLogList)
        {
            List<QSO> qsoList = new List<QSO>();
            string call = null;
            string name = null;
            int progress = 0;
            int validQsos;

            OnProgressUpdate?.Invoke("2", "", "", 0);
            contestLogList.Reverse();

            foreach (ContestLog contestLog in contestLogList)
            {
                call = contestLog.LogOwner;
                name = contestLog.LogHeader.NameSent;
               
                progress++;

                if (!contestLog.IsCheckLog && contestLog.IsValidLog)
                {
                    // only use valid QSOs in reverse
                    qsoList = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO).ToList();

                    MatchQSOs(qsoList, contestLogList, call);

                    validQsos = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO).Count();

                    Console.WriteLine("Pre - reverse: " + contestLog.LogOwner + " : " + validQsos.ToString());

                    OnProgressUpdate?.Invoke(call, contestLog.QSOCollection.Count.ToString(), validQsos.ToString(), progress);
                }
            }
        }

        /// <summary>
        /// http://stackoverflow.com/questions/16197290/checking-for-duplicates-in-a-list-of-objects-c-sharp
        /// Find duplicate QSOs in a log and mark the as dupes. Be sure
        /// to allow the first QSO to be marked as valid, though.
        /// </summary>
        /// <param name="qsoList"></param>
        private void MarkDuplicateQSOs(List<QSO> qsoList)
        {
            List<QSO> dupeList = new List<QSO>();

            var query = qsoList.GroupBy(x => new { x.ContactCall, x.Band, x.Mode })
             .Where(g => g.Count() > 1)
             .Select(y => y.Key)
             .ToList();

            // only check valid QSOs
            foreach (var qso in query)
            {
                switch (ActiveContest)
                {
                    case ContestName.CW_OPEN:
                        dupeList = qsoList.Where(item => item.ContactCall == qso.ContactCall && item.Band == qso.Band && item.Status == QSOStatus.ValidQSO).OrderBy(o => o.QSODateTime).ToList();
                        break;
                    case ContestName.HQP:
                        dupeList = qsoList.Where(item => item.ContactCall == qso.ContactCall && item.Band == qso.Band && qso.Mode == item.Mode && item.Status == QSOStatus.ValidQSO).OrderBy(o => o.QSODateTime).ToList();
                        break;
                }

                if (dupeList.Any())
                {
                    // set all as dupes
                    dupeList.Select(c => { c.QSOIsDupe = true && c.Status == QSOStatus.ValidQSO; return c; }).ToList();
                    // now reset the first one as not a dupe
                    dupeList.First().QSOIsDupe = false;
                    // let me know it has dupes for the rejected qso report
                    dupeList.First().QSOHasDupes = true;
                    // add all the dupes to the QSO
                    foreach (var item in dupeList.Skip(1))
                    {
                        dupeList.First().DuplicateQsoList.Add(item);
                        item.DupeListLocation = dupeList.First();
                    }
                    //dupeList[0].DuplicateQsoList = (List<QSO>)dupeList.Skip(1);
                }
            }
        }

        /// <summary>
        /// Mark all QSOs where the operator call sign doesn't match the log call sign as invalid.
        /// </summary>
        /// <param name="qsoList"></param>
        /// <param name="call"></param>
        private void MarkIncorrectCallSigns(List<QSO> qsoList, string call)
        {
            List<QSO> qsos = qsoList.Where(q => q.OperatorCall != call && q.Status == QSOStatus.ValidQSO).ToList();

            if (qsos.Any())
            {
                qsos.Select(c => { c.CallIsInValid = false; return c; }).ToList();
            }
        }

        /// <summary>
        /// Need to search the other log that matches this QSO
        /// Check the serial number and the callsign, if either do not match then the call is busted
        /// 
        /// 1. Get the contact call - does a log exist? if not then not busted (for now) - could search through other logs to see if dx was worked by anyone else
        /// Could also search all other logs using date/time and serial number and name to be more thorough
        /// 
        /// 2. Log exists, search for op callsign, if not found search for serial number and compare band, mode, date/time
        ///
        /// First: See if the other call is in the 
        /// </summary>
        /// <param name="qsoList"></param>
        private void MatchQSOs(List<QSO> qsoList, List<ContestLog> contestLogList, string operatorCall)
        {
            ContestLog matchLog = null;
            RejectReason reason = RejectReason.None;
            List<ContestLog> tempLog = null; // new List<ContestLog>();
            QSO matchQSO = null;
            QSO matchQSO_X = null;

            DateTime qsoDateTime = DateTime.Now;
            int band = 0;
            string mode = null;
            string contactName = null;
            string contactCall = null;
            string dxEntity = null;
            int receivedSerialNumber = 0;

            foreach (QSO qso in qsoList)
            {
                qsoDateTime = qso.QSODateTime;
                band = qso.Band;
                mode = qso.Mode;
                contactName = qso.ContactName;
                contactCall = qso.ContactCall;
                dxEntity = qso.OperatorEntity;
                receivedSerialNumber = qso.ReceivedSerialNumber;
                tempLog = null;

              
                // if its invalid we don't care about matches
                if (qso.Status == QSOStatus.InvalidQSO) // && qso.ReasonRejected != RejectReason.None)
                {
                    //if (qso.ReasonRejected == RejectReason.InvalidCall)
                    //{
                        continue;
                    //}
                }

                if (ActiveContest == ContestName.HQP)
                {
                    if (!qso.IsHQPEntity && qso.ContactCountry != HQPHawaiiLiteral)
                    {
                        // this is a non Hawaiian station that has a non Hawaiian contact - maybe another QSO party
                        qso.Status = QSOStatus.InvalidQSO;
                        qso.ReasonRejected = RejectReason.NotCounted;
                        continue;
                    }
                }

                // if this QSO is and X-QSO it does not get counted for this operator
                if (qso.IsXQSO)
                {
                    qso.Status = QSOStatus.InvalidQSO;
                    qso.ReasonRejected = RejectReason.Marked_XQSO;
                    continue;
                }

               
                // get the other log that matches this QSO contact call
                matchLog = contestLogList.FirstOrDefault(q => q.LogOwner == qso.ContactCall);
               
                if (qso.ContactCall.IndexOf(@"/") != -1)             //                                |
                {                           //                                |
                    continue;   // Skip the remainder of this iteration. -----+
                }

                if (matchLog != null)
                {
                    switch (ActiveContest)
                    {
                        case ContestName.CW_OPEN:
                            // now see if a QSO matches this QSO
                            matchQSO = matchLog.QSOCollection.FirstOrDefault(q => q.Band == band && q.OperatorName == contactName && q.OperatorCall == contactCall &&
                                                  q.ContactCall == qso.OperatorCall && Math.Abs(q.SentSerialNumber - receivedSerialNumber) <= 1 && Math.Abs(q.QSODateTime.Subtract(qsoDateTime).Minutes) <= 5);
                            break;
                        case ContestName.HQP:
                            // now see if a QSO matches this QSO

                            if ((CategoryMode)Enum.Parse(typeof(CategoryMode), mode) == CategoryMode.RY)
                            {
                                // don't check the time on digi contacts per Alan 09/25,2020
                                matchQSO = matchLog.QSOCollection.FirstOrDefault(q => q.Band == band && q.ContactEntity == dxEntity && q.OperatorCall == contactCall &&
                                                      q.ContactCall == operatorCall && q.Mode == mode);
                            } else
                            {
                                matchQSO = matchLog.QSOCollection.FirstOrDefault(q => q.Band == band && q.ContactEntity == dxEntity && q.OperatorCall == contactCall &&
                                                     q.ContactCall == operatorCall && q.Mode == mode && Math.Abs(q.QSODateTime.Subtract(qsoDateTime).Minutes) <= 5);
                            }

                            break;
                    }

                    if (matchQSO != null) // found it
                    {
                        // store the matching QSO
                        qso.HasMatchingQso = true;
                        qso.MatchingQSO = matchQSO;
                    }
                    else if (matchQSO_X != null) // found it
                    {
                        // store the matching QSO
                        qso.MatchingQSO = matchQSO_X;
                    }
                    else
                    {
                        // narrow down search
                        reason = FindRejectReason(matchLog, qso);

                        // test for NONE because S/N can be off by one
                        if (reason != RejectReason.InvalidTime && reason != RejectReason.None)
                        {
                            qso.Status = QSOStatus.InvalidQSO;
                            //qso.GetRejectReasons().Clear();
                            //qso.GetRejectReasons().Add(reason, EnumHelper.GetDescription(reason));
                            qso.ReasonRejected = reason;
                        }
                    }
                }
                else
                {
                    // can't find a matching log
                    // find all the logs this operator is in so we can try to get a match without call signs
                    // don't want to exclude invalid QSOs as we are checking all logs and all QSOs
                    //tempLog = contestLogList.Where(q => q.QSOCollection.Any(p => p.ContactCall == qso.ContactCall)).ToList();

                    // speeds up from 44 sec to 28 sec on first pass
                    tempLog = CallDictionary[qso.ContactCall];

                    if (tempLog.Count <= 1) // 1 would mean this call sign only in this log
                    {
                        // the call is not in any other log
                        if (!qso.HasMatchingQso)
                        {
                            qso.Status = QSOStatus.ReviewQSO;
                            //qso.GetRejectReasons().Clear();
                            if (ActiveContest == ContestName.CW_OPEN)
                            {
                                // give them the point anyway
                                qso.IsMultiplier = true;
                                //qso.GetRejectReasons().Add(RejectReason.NoQSO, EnumHelper.GetDescription(RejectReason.NoQSO));
                                qso.ReasonRejected = RejectReason.NoQSO;
                            }

                            if (ActiveContest == ContestName.HQP)
                            {
                                if (qso.ContactCountry != HQPUSALiteral && qso.ContactCountry != HQPCanadaLiteral && qso.ContactCountry != HQPHawaiiLiteral && qso.ContactCountry != HQPAlaskaLiteral)
                                {
                                    //qso.GetRejectReasons().Clear();
                                    qso.ReasonRejected = RejectReason.None;
                                    qso.Status = QSOStatus.ValidQSO;
                                }
                                else
                                {
                                    // THIS NEEDS FIXING - should always be the same
                                    if (qso.ContactEntity != qso.ContactEntity)
                                    {
                                        qso.Status = QSOStatus.InvalidQSO;
                                        qso.EntityIsInValid = true;
                                        qso.IncorrectDXEntity = qso.ContactEntity + " --> " + qso.ContactEntity;
                                    }
                                    else
                                    {
                                        qso.ReasonRejected = RejectReason.NoQSO;
                                        //qso.GetRejectReasons().Add(RejectReason.NoQSO, EnumHelper.GetDescription(RejectReason.NoQSO));
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (ActiveContest == ContestName.HQP)
                            {
                                // if only in one log give it to them per Alan 09/25/2020
                                qso.Status = QSOStatus.ValidQSO;
                            }
                            else 
                            { 
                                qso.Status = QSOStatus.InvalidQSO;
                                //qso.GetRejectReasons().Clear();
                                //qso.GetRejectReasons().Add(RejectReason.NoQSO, EnumHelper.GetDescription(RejectReason.BustedCallSign));
                                qso.ReasonRejected = RejectReason.BustedCallSign;
                            }
                        }
                    }
                    else
                    { // ok call is in 2 or more logs, is it busted
                        switch (CheckBadCallList(qso))
                        {
                            case false:
                                //  not used right now
                                //if (SearchForBustedCall(qso, contestLogList) == false) // did not find it so is name incorrect
                                //{
                                //    qso.Status = QSOStatus.ValidQSO;
                                //}

                                break;
                            default:
                                qso.Status = QSOStatus.InvalidQSO;
                                break;
                        }
                    }
                }
            }
        }

        private void SearchCWOpenBustedCallSigns(QSO qso)
        {

        }

        /// <summary>
        /// Need to look through every log and find the match for this QSO.
        /// If there are no matches either the call is busted or it is a unique
        /// call. If the QSO has a time match but the calls don't match we need
        /// to do some work to see if the call is busted.
        /// 
        /// Get a list of all logs from the CallDictionary that have this call sign in it
        /// Now we can query the QSODictionary for each QSO that may match
        /// 
        /// We can also flag some duplicates here
        /// </summary>
        /// <param name="contestLog"></param>
        /// <param name="qso"></param>
        private void SearchHQPBustedCallSigns(QSO qso)
        {
            List<QSO> matches;
            List<KeyValuePair<string, List<QSO>>> qsos;

            // all logs with this operator call sign
            List<ContestLog> contestLogs = CallDictionary[qso.OperatorCall];

            // List of List<QSO> from the list of contest logs that match this operator call sign
            qsos = contestLogs.SelectMany(z => z.QSODictionary).Where(x => x.Key == qso.OperatorCall).ToList();

            // this can have more entries than qsos because the list is flattened
            matches = qsos.SelectMany(x => x.Value)
                .Where(y => y.ContactCall == qso.OperatorCall && y.OperatorCall == qso.ContactCall && y.Band == qso.Band && y.Mode == qso.Mode && Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= 1)
                .ToList();

            // found a match so we mark both as matches and add matching QSO
            if (matches.Count == 1)
            {
                qso.MatchingQSO = matches[0];
                matches[0].MatchingQSO = qso;
                return;
            }

            // match not found so lets widen the search
            if (matches.Count == 0)
            {
               matches = qsos.SelectMany(x => x.Value)
                    .Where(y => y.ContactCall == qso.OperatorCall && y.OperatorCall == qso.ContactCall && y.Band == qso.Band && y.Mode == qso.Mode &&  Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= 5)
                    .ToList();
            }

            // found a single match so we mark both as matches and add matching QSO
            if (matches.Count == 1)
            {
                qso.MatchingQSO = matches[0];
                matches[0].MatchingQSO = qso;
                return;
            }

            // multiple matches found so need to narrow the search
            // these are probably dupes
            if (matches.Count > 1)
            {
                qso.MatchingQSO = matches[0];
                matches[0].MatchingQSO = qso;
                matches[1].QSOIsDupe = true;

                // have QSO class handle this
                qso.DuplicateQsoList.Add(matches[1]);

            }

            foreach (QSO o in matches)
            {
                Console.WriteLine(qso.ContactCall + ":" + o.OperatorCall + ":" + o.QSODateTime);
            }
        }

        /// <summary>
        /// Search every log for a match to this QSO without the call sign
        /// </summary>
        /// <param name="qso"></param>
        private bool SearchForBustedCall(QSO qso, List<ContestLog> contestLogList)
        {
            bool found = false;
            //List<QSO> matchingQSOs = new List<QSO>();

            // speeds up from 22 sec to 18 sec on first pass on CWOpen
            //List<ContestLog> tempLog = BandDictionary[qso.Band];

            // THIS NEVER FINDS ANYTHING - WHAT AM I TRYING TO DO ???
            // look for a log without a call sign parameter
            //switch (ActiveContest)
            //{
            //    case ContestName.CW_OPEN:
            //        // was contestLogList - also no need to do band compare
            //        // this never finds anything
            //        matchingQSOs = tempLog.SelectMany(z => z.QSOCollection).Where(q => q.OperatorCall == qso.ContactCall &&
            //                          Math.Abs(q.SentSerialNumber - qso.ReceivedSerialNumber) <= 1 && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).Minutes) <= 2).ToList();
                    
            //        //matchingQSOs = tempLog.SelectMany(z => z.QSOCollection).Where(q => q.Band == qso.Band && q.OperatorCall == qso.ContactCall &&
            //        //                  Math.Abs(q.SentSerialNumber - qso.ReceivedSerialNumber) <= 1 && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).Minutes) <= 2).ToList();
            //        break;
            //    case ContestName.HQP:
            //        // this never finds anything
            //        // was contestLogList
            //        matchingQSOs = tempLog.SelectMany(z => z.QSOCollection).Where(q => q.OperatorCall == qso.ContactCall &&
            //                    q.Mode == qso.Mode && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).Minutes) <= 2).ToList();
            //        //matchingQSOs = tempLog.SelectMany(z => z.QSOCollection).Where(q => q.Band == qso.Band && q.OperatorCall == qso.ContactCall &&
            //        //            q.Mode == qso.Mode && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).Minutes) <= 2).ToList();

            //        //if (matchingQSOs.Count > 0)
            //        //{
            //        //    Console.WriteLine("Count:" + matchingQSOs.Count);
            //        //}
            //        break;
            //}

            /*
                QSO: 14039 CW 2020-08-22 2226 AH6KO 599 HIL NS6T 599 AL
                QSO: 14039 CW 2020-08-22 2226 NH6T 599 AL AH6KO 599 HIL
             */
            //if (matchingQSOs != null && matchingQSOs.Count > 0)
            //{
            //    if (matchingQSOs.Count == 1)
            //    {    // found it so call is busted
            //        found = true;
            //        qso.CallIsBusted = true;
            //         qso.MatchingQSO = matchingQSOs[0];
            //    }
            //}
            //else// did not find it
            //{
                //if (ActiveContest == ContestName.HQP)
                //{
                //    SearchForIncorrectEntity(qso, contestLogList);
                //}
                //else
                //{
                //    found = SearchForIncorrectName(qso, contestLogList);
                //}
            //}

            return found;
        }

        /// <summary>
        /// See if the call is in the Good/Bad call list
        /// </summary>
        /// <param name="qso"></param>
        /// <returns></returns>
        private bool CheckBadCallList(QSO qso)
        {
            if (_BadCallList != null)
            {
                var uniqueResults = _BadCallList
                    .Where(item => item.Key == qso.ContactCall) // filter the collection
                    .SelectMany(item => item)                   // get the Values from KeyValuePairs
                    .Distinct()                                   // remove duplicates
                    .ToList();


                if (uniqueResults.Count > 0)
                {
                    qso.Status = QSOStatus.InvalidQSO;
                    qso.CallIsBusted = true;
                    qso.BustedCallGuess = uniqueResults[0];
                    //qso.GetRejectReasons().Clear();
                    //qso.GetRejectReasons().Add(RejectReason.NoQSO, EnumHelper.GetDescription(RejectReason.BustedCallSign));
                    qso.ReasonRejected = RejectReason.BustedCallSign;

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// CWOpen
        /// Now see if the name is incorrect and that is why we can't find the QSO
        /// This is only when the call sign did not submit a log
        /// </summary>
        /// <param name="qso"></param>
        /// <param name="contestLogList"></param>
        /// <returns></returns>
        private void SearchForIncorrectName(QSO qso)
        {
            int matchCount = 0;
            List<QSO> matchingQSOs = null;
            List<string> matchNames = new List<string>();

            // speeds up from 34 sec to 22 sec on first pass
            List<ContestLog> tempLog = CallDictionary[qso.ContactCall];
            
            // now look for a match without the operator name
            matchingQSOs = tempLog.SelectMany(z => z.QSOCollection).Where(q => q.ContactCall == qso.ContactCall).ToList();

            if (matchingQSOs.Count >= 1)
            {
                // loop through and see if first few names match
                for (int i = 0; i < matchingQSOs.Count; i++)
                {
                    if (qso.ContactName != matchingQSOs[i].ContactName)
                    {
                        matchNames.Add(matchingQSOs[i].ContactName);
                    }
                }

                switch (matchNames.Count)
                {
                    case 0:
                        // unique call
                        return;
                    case 1:
                        // only one so we give it to him
                        return;
                    default:
                        var mostUsed = (from i in matchNames
                                    group i by i into grp
                                    orderby grp.Count() descending
                                    select grp.Key).First();
                        
                        if (qso.ContactName != mostUsed)
                        {
                            qso.IncorrectName = qso.ContactName + " --> " + mostUsed;
                            qso.ContactNameIsInValid = true;
                        }

                        break;
                }
            }
        }


        /*
         Busted call requires a matching QSO
         Unique call give to it to him

         */

        /// <summary>
        /// If the operator is a Hawaii entity:
        /// The entity should be two characters either a US state, Canadian
        /// province or "DX".
        /// If the operator is not Hawaii the entity should be 3 characters
        /// and match the Hawaii entity list.
        /// </summary>
        /// <param name="qso"></param>
        private void MarkIncorrectEntities(QSO qso)
        {

        }

        /// <summary>
        /// HQP only
        /// Is this a Hawaiin station then, state, province, DX
        /// Non Hawaiin ststion then
        /// if US or Canadian - Hawwaiin entity
        /// DX then Hawaiin entity
        /// </summary>
        /// <param name="qso"></param>
        /// <param name="contestLogList"></param>
        /// <returns></returns>
        private void SearchForIncorrectEntity(QSO qso)
        {
            int matchCount = 0;
            List<QSO> matchingQSOSpecific = null;
            List<QSO> matchingQSOsGeneral = null;
            string entity = null;
            string[] info = new string[2] { "0", "0" };
            int count = 0;


            // speeds up from 28 sec to 12 sec on first pass
           List<ContestLog> tempLog = CallDictionary[qso.ContactCall];

            // get a list of all QSOs with the same contact callsign and same entity
            matchingQSOSpecific = tempLog.SelectMany(z => z.QSOCollection).Where(q => q.ContactCall == qso.ContactCall && q.ContactEntity == qso.ContactEntity).ToList();
            
            // get a list of all QSOs with the same contact callsign but may have different entity
            matchingQSOsGeneral = tempLog.SelectMany(z => z.QSOCollection).Where(q => q.ContactCall == qso.ContactCall).ToList();

            // if the above counts are different then someone is wrong
            matchCount = matchingQSOsGeneral.Count - matchingQSOSpecific.Count;

            // if no mismatches then assume it is correct
            if (matchCount == 0)
            {
                return;
            }

            // need to have more than two QSOs to VOTE on most prevalent contactEntity

            // https://stackoverflow.com/questions/17323804/compare-two-lists-via-one-property-using-linq
            // if matchingQSOSpecific and matchingQSOsGeneral are not equal this will give a list with the majority of values (inner join)
            // actually it gives a list of the majority of values that match - so could return only one item
            IEnumerable<QSO> majorityContactEntities = from firstQSO in matchingQSOSpecific
                                                       join secondQSO in matchingQSOsGeneral
                                                       on firstQSO.ContactEntity equals secondQSO.ContactEntity
                                                       into matches
                                                       where matches.Any()
                                                       select firstQSO;

            // need to account for one Hawaii entry in the future
            count = majorityContactEntities.Count();

            if (count > 1)
            {
                QSO firstMatch = majorityContactEntities.FirstOrDefault();
                entity = firstMatch.ContactEntity;
            }
            else // count = 1 so use the items in the larger list
            {
                // these must be Lists, not IEnumerable so .Except works - only look at QSOs with valid entities
                List<QSO> listWithMostEntries = (new List<List<QSO>> { matchingQSOSpecific, matchingQSOsGeneral })
                   .OrderByDescending(x => x.Count())
                   .Take(1).FirstOrDefault().Where(q => q.EntityIsInValid == false).ToList();

                List<QSO> listWithLeastEntries = (new List<List<QSO>> { matchingQSOSpecific, matchingQSOsGeneral })
                   .OrderBy(x => x.Count())
                   .Take(1).FirstOrDefault().Where(q => q.EntityIsInValid == false).ToList();

                // we have enough to vote
                if (listWithMostEntries.Count() > 2)
                {
                    // we want to take the list with the most entries and remove the entries in the least entries list
                    var difference = listWithMostEntries.Except(listWithLeastEntries);

                    QSO firstMatch = difference.FirstOrDefault();
                    entity = firstMatch.ContactEntity;
                }
                else
                { // two entries, either could be correct
                    IEnumerable<CallSignInfo> hitCollection = CallLookUp.LookUpCall(qso.ContactCall);
                    List<CallSignInfo> hitList = hitCollection.ToList();
                    if (hitList.Count != 0)
                    {
                        entity = hitList[0].Country;

                        switch (entity)
                        {
                            case "United States of America":
                                if (qso.ContactEntity.Length > 2)
                                {
                                    entity = hitList[0].Province; // this gives State, need state code
                                }
                                else if (Provinces.Contains(qso.ContactEntity))
                                {
                                    entity = hitList[0].Province; // this gives State, need state code
                                }
                                else
                                {
                                    if (States.Contains(qso.ContactEntity))
                                    {
                                        return;
                                    }
                                }
                                break;
                            case "Canada":
                                if (qso.ContactEntity.Length > 2)
                                {
                                    entity = hitList[0].Admin1;
                                }
                                else
                                {
                                    if (Provinces.Contains(qso.ContactEntity))
                                    {
                                        return;
                                    }
                                }
                                break;
                            case "Hawaii":
                                return;
                            default:
                                if (qso.IsHQPEntity)
                                {
                                    entity = "DX";
                                } 
                                break;
                        }
                    }
                }
            }

            // if entity is different
            if (!entity.Contains(qso.ContactEntity))
            {
                qso.IncorrectDXEntity = $"{qso.OriginalContactEntity} --> {entity}";
                qso.EntityIsInValid = true;
            }
        }

        /// <summary>
        /// Determine the reason to reject the QSO
        /// 
        /// This needs to be refactored
        /// </summary>
        /// <param name="matchLog"></param>
        /// <param name="qso"></param>
        /// <returns></returns>
        private RejectReason FindRejectReason(ContestLog matchLog, QSO qso)
        {
            RejectReason reason = RejectReason.None;

            if (reason == RejectReason.None)
            {
                // query for incorrect serial number
                if (ActiveContest == ContestName.CW_OPEN)
                {
                    reason = CheckForSerialNumberMismatch(matchLog, qso);
                }
            }

            if (reason == RejectReason.None)
            {
                // query for incorrect band
                reason = CheckForBandMismatch(matchLog, qso);
            }

            if (reason == RejectReason.None)
            {
                // query for incorrect mode
                if (ActiveContest == ContestName.HQP)
                {
                    reason = CheckForModeMismatch(matchLog, qso);
                }
            }

            if (reason == RejectReason.None)
            {
                // query for incorrect name or entity (HQP)
                reason = CheckForNameOrEntityMismatch(matchLog, qso);
            }

            if (reason == RejectReason.None)
            {
                // query for incorrect call
                reason = CheckForCallMismatch(matchLog, qso);
            }

            if (reason == RejectReason.None)
            {
                // check times
                reason = CheckForTimeMismatch(matchLog, qso);
            }

            return reason;
        }


       
        // test without checking for time
        // maybe check to be sure it in contest time

        /*
         I would simply accept ANY digital QSO where the software thinks there was a busted call. That may be the simplest solution. 
        Allowing FTx in the contest brings new challenges and hassles. However, at least for HQP I think it's a good thing overall.
         */


        /// <summary>
        /// See if the qso was rejected because the time stamps don't match.
        /// </summary>
        /// <param name="matchLog"></param>
        /// <param name="qso"></param>
        /// <returns></returns>
        private RejectReason CheckForTimeMismatch(ContestLog matchLog, QSO qso)
        {
            QSO matchQSO = null;
            RejectReason reason = RejectReason.None;
            TimeSpan ts;

            switch (ActiveContest)
            {
                case ContestName.CW_OPEN:
                    matchQSO = matchLog.QSOCollection.FirstOrDefault(q => q.Band == qso.Band && q.ContactName == qso.OperatorName && Math.Abs(q.SentSerialNumber - qso.ReceivedSerialNumber) <= 1 &&
                       q.ContactCall == qso.OperatorCall && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) > 5);
                    break;
                case ContestName.HQP:
                    // don't check the time on digi contacts per Alan 09/25,2020
                    if ((CategoryMode)Enum.Parse(typeof(CategoryMode), qso.Mode) != CategoryMode.RY)
                    {
                        matchQSO = matchLog.QSOCollection.FirstOrDefault(q => q.Band == qso.Band && q.Mode == qso.Mode &&
                      q.ContactCall == qso.OperatorCall && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) > 5); //  && q.ContactEntity == qso.ContactEntity 
                    } else
                    {
                        return reason;
                    }
                    break;
            }

            if (matchQSO != null)
            {
                // store the matching QSO
                qso.MatchingQSO = matchQSO;
                ts = qso.QSODateTime.Subtract(matchQSO.QSODateTime);
                qso.ExcessTimeSpan = Math.Abs(ts.Minutes);

                reason = RejectReason.InvalidTime;
            }

            return reason;
        }

        /// <summary>
        /// See if the qso was rejected because the serial numbers don't match.
        /// This is for the CWOpen only
        /// </summary>
        /// <param name="matchLog"></param>
        /// <param name="qso"></param>
        /// <returns></returns>
        private RejectReason CheckForSerialNumberMismatch(ContestLog matchLog, QSO qso)
        {
            QSO matchQSO = null;
            RejectReason reason = RejectReason.None;

            matchQSO = matchLog.QSOCollection.Where(q => q.Band == qso.Band && q.ContactName == qso.OperatorName && q.ContactCall == qso.OperatorCall &&
                        Math.Abs(q.SentSerialNumber - qso.ReceivedSerialNumber) > 1).FirstOrDefault(); //

            if (matchQSO != null)
            {
                qso.MatchingQSO = matchQSO;
                reason = RejectReason.SerialNumber;
            }
            return reason;
        }

        /// <summary>
        /// See if the qso was rejected because the band doesn't match.
        /// </summary>
        /// <param name="matchLog"></param>
        /// <param name="qso"></param>
        /// <returns></returns>
        private RejectReason CheckForBandMismatch(ContestLog matchLog, QSO qso)
        {
            QSO matchQSO = null;
            RejectReason reason = RejectReason.None;
            bool isMatchQSO = false;

            switch (ActiveContest)
            {
                case ContestName.CW_OPEN:
                    matchQSO = matchLog.QSOCollection.FirstOrDefault(q => q.Band != qso.Band && q.ContactName == qso.OperatorName && Math.Abs(q.SentSerialNumber - qso.ReceivedSerialNumber) <= 1 &&
                         q.ContactCall == qso.OperatorCall);
                    break;
                case ContestName.HQP:
                    matchQSO = matchLog.QSOCollection.FirstOrDefault(q => q.Band != qso.Band && q.ContactName == qso.OperatorName && q.Mode == qso.Mode &&
                        q.ContactCall == qso.OperatorCall && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) < 5);
                    break;
            }

            if (matchQSO != null)
            {
                try
                {
                    // determine who is at fault
                    isMatchQSO = DetermineBandFault(matchLog, qso, matchQSO);
                }
                catch (Exception ex)
                {
                    string a = ex.Message;
                }

                qso.MatchingQSO = matchQSO;
                qso.HasMatchingQso = true;

                if (isMatchQSO)
                {
                    // matchQSO.GetRejectReasons().Clear(); // should not be a collection ?? or lets actually look for multiple reasons
                    //matchQSO.GetRejectReasons().Add(RejectReason.Band, EnumHelper.GetDescription(RejectReason.Band));
                    matchQSO.ReasonRejected = RejectReason.Band;
                }
                else
                {
                    reason = RejectReason.Band;
                }
            }

            return reason;
        }

        /// <summary>
        /// See if the qso was rejected because the mode doesn't match.
        /// </summary>
        /// <param name="matchLog"></param>
        /// <param name="qso"></param>
        /// <returns></returns>
        private RejectReason CheckForModeMismatch(ContestLog matchLog, QSO qso)
        {
            QSO matchQSO = null;
            RejectReason reason = RejectReason.None;
            bool isMatchQSO = false;

            //switch (ActiveContest)
            //{
            //    case ContestName.HQP:
                    matchQSO = matchLog.QSOCollection.FirstOrDefault(q => q.Band == qso.Band && q.ContactName == qso.OperatorName && q.Mode != qso.Mode &&
                        q.ContactCall == qso.OperatorCall && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) < 5);
                   // break;
            //}

            if (matchQSO != null)
            {
                // determine who is at fault
                isMatchQSO = DetermineModeFault(matchLog, qso, matchQSO);

                qso.MatchingQSO = matchQSO;

                if (isMatchQSO)
                {
                    //matchQSO.GetRejectReasons().Clear(); // should not be a collection ?? or lets actually look for multiple reasons
                    //matchQSO.GetRejectReasons().Add(RejectReason.Mode, EnumHelper.GetDescription(RejectReason.Mode));
                    matchQSO.ReasonRejected = RejectReason.Mode;
                }
                else
                {
                    reason = RejectReason.Mode;
                }
            }

            return reason;
        }

        /// <summary>
        /// See if the qso was rejected because the name or entity depending on contest don't match.
        /// </summary>
        /// <param name="matchLog"></param>
        /// <param name="qso"></param>
        /// <returns></returns>
        private RejectReason CheckForNameOrEntityMismatch(ContestLog matchLog, QSO qso)
        {
            QSO matchQSO = null;
            RejectReason reason = RejectReason.None;

            switch (ActiveContest)
            {
                case ContestName.CW_OPEN:
                    matchQSO = matchLog.QSOCollection.FirstOrDefault(q => q.Band == qso.Band && Math.Abs(q.SentSerialNumber - qso.ReceivedSerialNumber) <= 1 && q.ContactCall == qso.OperatorCall &&
                       q.ContactName == qso.OperatorName && q.OperatorName != qso.ContactName); //
                    break;
                case ContestName.HQP:
                    matchQSO = matchLog.QSOCollection.FirstOrDefault(q => q.Band == qso.Band && q.Mode == qso.Mode && q.ContactCall == qso.OperatorCall &&
                       q.ContactEntity == qso.OperatorEntity && q.OperatorEntity != qso.ContactEntity && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) < 5);
                    break;
            }

            if (matchQSO != null)
            {
                qso.MatchingQSO = matchQSO;

                switch (ActiveContest)
                {
                    case ContestName.CW_OPEN:
                        qso.IncorrectName = qso.ContactName;
                        reason = RejectReason.OperatorName;
                        break;
                    case ContestName.HQP:
                        qso.IncorrectDXEntity = qso.ContactEntity;
                        reason = RejectReason.EntityName;
                        break;
                }
            }

            return reason;
        }

        /// <summary>
        /// See if the qso was rejected because the callsign doesn't match.
        /// </summary>
        /// <param name="matchLog"></param>
        /// <param name="qso"></param>
        /// <returns></returns>
        private RejectReason CheckForCallMismatch(ContestLog matchLog, QSO qso)
        {
            QSO matchQSO = null;
            RejectReason reason = RejectReason.None;

            /* figure out how to catch this
                QSO: 14039 CW 2020-08-22 2226 AH6KO 599 HIL NS6T 599 AL
                QSO: 14039 CW 2020-08-22 2226 NH6T 599 AL AH6KO 599 HIL
             */

            switch (ActiveContest)
            {
                case ContestName.CW_OPEN:
                    matchQSO = matchLog.QSOCollection.FirstOrDefault(q => q.Band == qso.Band && q.ContactName == qso.OperatorName && q.ContactCall != qso.OperatorCall &&
                       Math.Abs(q.SentSerialNumber - qso.ReceivedSerialNumber) <= 1); // 
                    break;
                case ContestName.HQP:
                    matchQSO = matchLog.QSOCollection.FirstOrDefault(q => q.Band == qso.Band && q.ContactName == qso.OperatorName && q.ContactCall != qso.OperatorCall &&
                        q.Mode == qso.Mode && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) < 1); // SHOULD I REDUCE THIS ??
                    break;
            }

            if (matchQSO != null) // this QSO is not in the other log
            {
                /*
                QSO: 14039 CW 2020-08-22 2226 AH6KO 599 HIL NS6T 599 AL
                QSO: 14039 CW 2020-08-22 2226 NH6T 599 AL AH6KO 599 HIL
             */
                //determine which guy is at fault
                if (qso.ContactCall != matchQSO.OperatorCall)
                {
                    qso.MatchingQSO = matchQSO;
                    reason = RejectReason.BustedCallSign;
                }
                else
                {
                    matchQSO.HasMatchingQso = true;
                    matchQSO.MatchingQSO = qso;
                    //matchQSO.GetRejectReasons().Clear(); // should not be a collection ?? or lets actually look for multiple reasons
                    //matchQSO.GetRejectReasons().Add(RejectReason.NoQSOMatch, EnumHelper.GetDescription(RejectReason.NoQSOMatch));
                    matchQSO.ReasonRejected = RejectReason.NoQSOMatch;
                }
            }

            return reason;
        }

        /// <summary>
        /// Determine who is at fault when a band or mode mismatch occurs.
        /// The first QSO gets preference over the match QSO.
        /// http://www.herlitz.nu/2011/12/01/getting-the-previous-and-next-record-from-list-using-linq/
        /// </summary>
        /// <param name="qso"></param>
        /// <param name="matchQSO"></param>
        /// <param name="modeType"></param>
        private bool DetermineModeFault(ContestLog matchLog, QSO qso, QSO matchQSO)
        {
            ContestLog contestLog = qso.ParentLog;
            QSO previousQSO;
            QSO nextQSO;
            int qsoPoints = 1;
            int matchQsoPoints = 1;
            string frequency;
            bool isMatchQSO = true;

            // bonus point for having rig control frequency
            frequency = qso.Frequency;
            if (frequency != "3500" && frequency != "7000" && frequency != "14000" && frequency != "21000" && frequency != "28000")
            {
                qsoPoints += 1;
            }

            frequency = matchQSO.Frequency;
            if (frequency != "3500" && frequency != "7000" && frequency != "14000" && frequency != "21000" && frequency != "28000")
            {
                matchQsoPoints += 1;
            }

            // get the previous and next qso for the qso
            previousQSO = GetPrevious<QSO>(contestLog.QSOCollection, qso);
            nextQSO = GetNext<QSO>(contestLog.QSOCollection, qso);

            if (previousQSO != null)
            {
                if (previousQSO.Mode == qso.Mode)
                {
                    qsoPoints += 1;
                }
                else
                {
                    qsoPoints -= 1;
                }
            }
            else
            {
                qsoPoints += 1;
            }



            if (nextQSO != null)
            {
                if (nextQSO.Mode == qso.Mode)
                {
                    qsoPoints += 1;
                }
                else
                {
                    qsoPoints -= 1;
                }
            }
            else
            {
                //qsoPoints += 1;
            }

            // get the previous and next qso for the matchQso
            previousQSO = GetPrevious<QSO>(matchLog.QSOCollection, matchQSO);
            nextQSO = GetNext<QSO>(matchLog.QSOCollection, matchQSO);

            if (previousQSO != null)
            {
                if (previousQSO.Mode == matchQSO.Mode)
                {
                    matchQsoPoints += 1;
                }
                else
                {
                    matchQsoPoints -= 1;
                }
            }
            else
            {
                matchQsoPoints += 1;
            }

            if (nextQSO != null)
            {
                if (nextQSO.Mode == matchQSO.Mode)
                {
                    matchQsoPoints += 1;
                }
                else
                {
                    matchQsoPoints -= 1;
                }
            }
            else
            {
                //matchQsoPoints += 1;
            }


            if (qsoPoints < matchQsoPoints)
            {
                isMatchQSO = false;
            }

            return isMatchQSO;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="matchLog"></param>
        /// <param name="qso"></param>
        /// <param name="matchQSO"></param>
        /// <returns></returns>
        private bool DetermineBandFault(ContestLog matchLog, QSO qso, QSO matchQSO)
        {
            ContestLog contestLog = qso.ParentLog;
            QSO previousQSO;
            QSO nextQSO;
            int qsoPoints = 1;
            int matchQsoPoints = 1;
            string frequency = "";
            bool isMatchQSO = true;

            // bonus point for having rig control frequency
            frequency = qso.Frequency;
            if (frequency != "3500" && frequency != "7000" && frequency != "14000" && frequency != "21000" && frequency != "28000")
            {
                qsoPoints += 1;
            }

            frequency = matchQSO.Frequency;
            if (frequency != "3500" && frequency != "7000" && frequency != "14000" && frequency != "21000" && frequency != "28000")
            {
                matchQsoPoints += 1;
            }

            // get the previous and next qso for the qso
            previousQSO = GetPrevious<QSO>(contestLog.QSOCollection, qso);
            nextQSO = GetNext<QSO>(contestLog.QSOCollection, qso);

            if (previousQSO != null)
            {
                if (previousQSO.Band == qso.Band)
                {
                    qsoPoints += 1;
                }
                else
                {
                    qsoPoints -= 1;
                }
            }
            else
            {
                qsoPoints += 1;
            }

            if (nextQSO != null)
            {
                if (nextQSO.Band == qso.Band)
                {
                    qsoPoints += 1;
                }
                else
                {
                    qsoPoints -= 1;
                }
            }
            else
            {
                qsoPoints += 1;
            }

            // get the previous and next qso for the matchQso
            previousQSO = GetPrevious<QSO>(matchLog.QSOCollection, matchQSO);
            nextQSO = GetNext<QSO>(matchLog.QSOCollection, matchQSO);

            if (previousQSO != null)
            {
                if (previousQSO.Band == matchQSO.Band)
                {
                    matchQsoPoints += 1;
                }
                else
                {
                    matchQsoPoints -= 1;
                }
            }
            else
            {
                matchQsoPoints -= 1;
            }

            if (nextQSO != null)
            {
                if (nextQSO.Band == matchQSO.Band)
                {
                    matchQsoPoints += 1;
                }
                else
                {
                    matchQsoPoints -= 1;
                }
            }
            else
            {
                matchQsoPoints -= 1;
            }


            if (qsoPoints < matchQsoPoints)
            {
                isMatchQSO = false;
            }

            return isMatchQSO;
        }

        private T GetNext<T>(IEnumerable<T> list, T current)
        {
            try
            {
                return list.SkipWhile(x => !x.Equals(current)).Skip(1).First();
            }
            catch
            {
                return default(T);
            }
        }

        private T GetPrevious<T>(IEnumerable<T> list, T current)
        {
            try
            {
                return list.TakeWhile(x => !x.Equals(current)).Last();
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// Mark all QSOs that don't have the correct name sent as invalid.
        /// </summary>
        /// <param name="qsoList"></param>
        /// <param name="name"></param>
        private void MarkIncorrectSentName(List<QSO> qsoList, string name)
        {
            List<QSO> qsos = qsoList.Where(q => q.OperatorName != name && q.Status == QSOStatus.ValidQSO).ToList();

            if (qsos.Any())
            {
                qsos.Select(c => { c.OpNameIsInValid = false; return c; }).ToList();
            }
        }

        /// <summary>
        /// Mark all QSOs that don't have the correct operator on one or more QSOs entity set as invalid.
        /// This rarely finds that the operator made a mistake
        /// Finds where the entity is not consistent among all the QSOs made with that operator.
        /// </summary>
        /// <param name="qsoList"></param>
        /// <param name="entity"></param>
        private void MarkIncorrectSentEntity(List<QSO> qsoList, string entity)
        {
            List<QSO> qsos = qsoList.Where(q => q.OperatorEntity != entity && q.Status == QSOStatus.ValidQSO).ToList();

            if (qsos.Any())
            {
                _ = qsos.Select(c => { c.EntityIsInValid = false; return c; })
                        .ToList();
            }
        }

        #region Soundex

        //if (Soundex(qso.IncorrectName) == Soundex(qso.MatchingQSO.OperatorName))
        //{
        //    var a = 1;
        //    //string zz = Soundex(qso.IncorrectName);
        //    //string xx = Soundex(qso.ContactName);
        //}

        //private string Soundex(string data)
        //{
        //    StringBuilder result = new StringBuilder();
        //    string previousCode = "";
        //    string currentCode = "";
        //    string currentLetter = "";

        //    if (data != null && data.Length > 0)
        //    {
        //        previousCode = "";
        //        currentCode = "";
        //        currentLetter = "";

        //        result.Append(data.Substring(0, 1));

        //        for (int i = 1; i < data.Length; i++)
        //        {
        //            currentLetter = data.Substring(1, 1).ToLower();
        //            currentCode = "";

        //            if ("bfpv".IndexOf(currentLetter) > -1)
        //            {
        //                currentCode = "1";
        //            }
        //            else if ("cgjkqsxz".IndexOf(currentLetter) > -1)
        //            {
        //                currentCode = "2";
        //            }
        //            else if ("dt".IndexOf(currentLetter) > -1)
        //            {
        //                currentCode = "3";
        //            }
        //            else if (currentLetter == "1")
        //            {
        //                currentCode = "4";
        //            }
        //            else if ("mn".IndexOf(currentLetter) > -1)
        //            {
        //                currentCode = "5";
        //            }
        //            else if (currentLetter == "r")
        //            {
        //                currentCode = "6";
        //            }

        //            if (currentCode != previousCode)
        //            {
        //                result.Append(currentCode);
        //            }

        //            if (result.Length == 4)
        //            {
        //                break;
        //            }

        //            if (currentCode != previousCode)
        //            {
        //                previousCode = currentCode;
        //            }
        //        }
        //    }

        //    if (result.Length < 4)
        //    {
        //        result.Append(new String('0', 4 - result.Length));
        //    }

        //    return result.ToString().ToUpper();
        //}

        #endregion


        //    // http://stackoverflow.com/questions/3319016/convert-list-to-dictionary-using-linq-and-not-worrying-about-duplicates
        //    // this gives me a dictionary with a unique log even if several QSOs
        //    contestLog.MatchLogs = matchingLogs
        //        .GroupBy(p => p.LogOwner, StringComparer.OrdinalIgnoreCase)
        //            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        //    // THIS DOES THE SAME AS ABOVE BUT THE KEY IS THE LOG INSTEAD OF LogOwner
        //    //// http://social.msdn.microsoft.com/Forums/vstudio/en-US/c0f0141c-1f98-422e-89af-406638c4403f/how-to-write-linq-query-to-convert-to-dictionaryintlistint-in-c?forum=linqprojectgeneral
        //    //// this converts the list to a dictionary and lists how many logs were matching
        //    //var match = matchingLogs
        //    //    .Select((n, i) => new { Value = n, Index = i })
        //    //    .GroupBy(a => a.Value)
        //    //    .ToDictionary(
        //    //        g => g.Key,
        //    //        g => g.Select(a => a.Index).ToList()
        //    //     );


    } // end class
}
