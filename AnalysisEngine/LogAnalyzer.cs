using System;
using System.Collections.Generic;
using System.Linq;
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
        public Dictionary<string, List<Tuple<string, int>>> NameDictionary;
        private List<ContestLog> ContestLogList;

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
        /// <param name="contestLogList"></param>
        /// <param name="callDictionary"></param>
        /// <param name="bandlDictionary"></param>
        public void PreAnalyzeContestLogs(List<ContestLog> contestLogList, Dictionary<string, List<ContestLog>> callDictionary, Dictionary<string, List<Tuple<string, int>>> nameDictionary)
        {
            CallDictionary = callDictionary;
            NameDictionary = nameDictionary;
            ContestLogList = contestLogList;

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

                        MarkIncorrectOperatorCallSigns(qsoList, call);

                        switch (ActiveContest)
                        {
                            case ContestName.CW_OPEN:
                                name = contestLog.LogHeader.NameSent;
                                MarkIncorrectSentName(qsoList, name);
                                break;
                            case ContestName.HQP:
                                // find the operator entity used on the majority of qsos
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
                                    FindHQPMatches(qso);

                                }
                                else
                                {
                                    SearchForIncorrectName(qso);
                                    try
                                    {
                                        FindCWOpenMatchingQsos(qso);
                                    }
                                    catch (Exception ex)
                                    {
                                        var a = ex.Message;
                                    }


                                }
                            }
                        }

                        // did busted call signs take care of this?
                        //MatchQSOs(qsoList, contestLogList, call);

                        // need to account for some already being marked
                        //MarkDuplicateQSOs(qsoList);

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
        //public void PreAnalyzeContestLogsReverse(List<ContestLog> contestLogList)
        //{
        //    List<QSO> qsoList = new List<QSO>();
        //    string call = null;
        //    string name = null;
        //    int progress = 0;
        //    int validQsos;

        //    OnProgressUpdate?.Invoke("2", "", "", 0);
        //    contestLogList.Reverse();

        //    foreach (ContestLog contestLog in contestLogList)
        //    {
        //        call = contestLog.LogOwner;
        //        name = contestLog.LogHeader.NameSent;

        //        progress++;

        //        if (!contestLog.IsCheckLog && contestLog.IsValidLog)
        //        {
        //            // only use valid QSOs in reverse
        //            qsoList = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO).ToList();

        //            MatchQSOs(qsoList, contestLogList, call);

        //            validQsos = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO).Count();

        //            Console.WriteLine("Pre - reverse: " + contestLog.LogOwner + " : " + validQsos.ToString());

        //            OnProgressUpdate?.Invoke(call, contestLog.QSOCollection.Count.ToString(), validQsos.ToString(), progress);
        //        }
        //    }
        //}

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
                    dupeList.Select(c => { c.IsDuplicateMatch = true && c.Status == QSOStatus.ValidQSO; return c; }).ToList();
                    // now reset the first one as not a dupe
                    dupeList.First().IsDuplicateMatch = false;
                    // let me know it has dupes for the rejected qso report
                    dupeList.First().QSOHasDupes = true;
                    // add all the dupes to the QSO
                    foreach (var item in dupeList.Skip(1))
                    {
                        //dupeList.First().DuplicateQsoList.Add(item);
                        //item.DupeListLocation = dupeList.First();
                    }

                }
            }
        }

        /// <summary>
        /// Mark all QSOs where the operator call sign doesn't match the log call sign as invalid.
        /// </summary>
        /// <param name="qsoList"></param>
        /// <param name="call"></param>
        private void MarkIncorrectOperatorCallSigns(List<QSO> qsoList, string call)
        {
            List<QSO> qsos = qsoList.Where(q => q.OperatorCall != call && q.Status == QSOStatus.ValidQSO).ToList();

            if (qsos.Any())
            {
                qsos.Select(c => { c.IncorrectOperatorCall = false; return c; }).ToList();
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
                mode = EnumHelper.GetDescription(qso.Mode);
                contactName = qso.ContactName;
                contactCall = qso.ContactCall;
                dxEntity = qso.OperatorEntity;   //qso.OperatorEntity; NEED TO MAKE EVERYTHING CONSISTANT
                receivedSerialNumber = qso.ReceivedSerialNumber;
                tempLog = null;
                matchLog = null;


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

                            if ((QSOMode)Enum.Parse(typeof(QSOMode), mode) == QSOMode.RY)
                            {
                                // don't check the time on digi contacts per Alan 09/25,2020
                                matchQSO = matchLog.QSOCollection.FirstOrDefault(q => q.Band == band && q.ContactEntity == dxEntity && q.OperatorCall == contactCall &&
                                                      q.ContactCall == operatorCall && EnumHelper.GetDescription(q.Mode) == mode);
                            }
                            else
                            {
                                matchQSO = matchLog.QSOCollection.FirstOrDefault(q => q.Band == band && q.ContactEntity == dxEntity && q.OperatorCall == contactCall &&
                                                     q.ContactCall == operatorCall && EnumHelper.GetDescription(q.Mode) == mode && Math.Abs(q.QSODateTime.Subtract(qsoDateTime).TotalMinutes) <= 5);
                            }

                            break;
                    }

                    if (matchQSO != null) // found it
                    {
                        // store the matching QSO
                        qso.HasBeenMatched = true;
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
                        if (!qso.HasBeenMatched)
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


        #region HQPpen Matching

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
        private void FindHQPMatches(QSO qso)
        {
            List<QSO> matches;
            List<ContestLog> contestLogs;
            List<KeyValuePair<string, List<QSO>>> qsos;

            if (qso.Status != QSOStatus.ValidQSO)
            {
                return;
            }

            // all logs with this operator call sign
            if (CallDictionary.ContainsKey(qso.OperatorCall))
            {
                contestLogs = CallDictionary[qso.OperatorCall];
            }
            else
            {
                // i.e. JF2IWL made 3 qsos and those three got in the CallDictionary
                // pointing at JF2IWL. However none of the 3 sent in a log so JF2IWL 
                // does not appear in the CallDictionary as there are no logs that
                // have a QSO for him so consider his valid

                return;
            }

            // if there is only one log this call is in then it is unique
            if (CallDictionary[qso.ContactCall].Count == 1)
            {
                qso.IsUniqueCall = true;
                return;
            }

            // there is no matching log for this call so we give him the qso
            // maybe should see if anyone else worked him?
            if (ContestLogList.Where(b => b.LogOwner == qso.ContactCall).Count() == 0)
            {
                //qsos = ContestLogList.SelectMany(z => z.QSODictionary).Where(x => x.Key == qso.ContactCall).ToList();

                //foreach (KeyValuePair<string, List<QSO>> item in qsos)
                //{
                //    foreach (QSO qItem in item.Value)
                //    {
                //        qso.AdditionalQSOs.Add(qItem);
                //    }
                //}

                qso.NoMatchingLog = true;

                return;
            }

            // no reason to process if it already has a match
            if (qso.HasBeenMatched)
            {
                return;
            }


            // List of List<QSO> from the list of contest logs that match this operator call sign
            qsos = contestLogs.SelectMany(z => z.QSODictionary).Where(x => x.Key == qso.OperatorCall).ToList();

            if (EnumHelper.GetDescription(qso.Mode) != "RY")
            {
                matches = RefineHQPMatch(qsos, qso, 2, 1);
            }
            else
            {
                matches = RefineHQPMatch(qsos, qso, 15, 1);
            }

            switch (matches.Count)
            {
                case 0:
                    // match not found so lets widen the search
                    matches = RefineHQPMatch(qsos, qso, 5, 1);
                    break;
                case 1:
                    // found one match so we mark both as matches and add matching QSO
                    qso.MatchingQSO = matches[0];
                    matches[0].MatchingQSO = qso;

                    qso.HasBeenMatched = true;
                    matches[0].HasBeenMatched = true;
                    return;
                case 2:
                    // two matches so these are probably dupes
                    qso.HasBeenMatched = true;
                    qso.MatchingQSO = matches[0];
                    qso.QSOHasDupes = true;

                    matches[0].HasBeenMatched = true;
                    matches[0].MatchingQSO = qso;

                    matches[1].HasBeenMatched = true;
                    matches[1].MatchingQSO = qso;
                    matches[1].FirstMatchingQSO = matches[0];
                    matches[1].IsDuplicateMatch = true;
                    return;
                default:
                    // more than two so we have to do more analysis
                    var a = 1;
                    Console.WriteLine("FindHQPMatches:");
                    break;
            }

            // this is hit only if previous switch hit case 0: so we up the time interval
            switch (matches.Count)
            {
                case 0:
                    // match not found so lets widen the search
                    matches = RefineHQPMatch(qsos, qso, 5, 2);
                    break;
                case 1:
                    // found one match so we mark both as matches and add matching QSO
                    qso.MatchingQSO = matches[0];
                    matches[0].MatchingQSO = qso;

                    qso.HasBeenMatched = true;
                    matches[0].HasBeenMatched = true;
                    return;
                case 2:
                    // two matches so these are probably dupes
                    qso.HasBeenMatched = true;
                    qso.MatchingQSO = matches[0];
                    qso.QSOHasDupes = true;

                    matches[0].HasBeenMatched = true;
                    matches[0].MatchingQSO = qso;

                    matches[1].HasBeenMatched = true;
                    matches[1].MatchingQSO = qso;
                    matches[1].FirstMatchingQSO = matches[0];
                    matches[1].IsDuplicateMatch = true;
                    return;
                default:
                    var b = 1;
                    Console.WriteLine("FindHQPMatches:");
                    break;
            }

            // ok, no hits yet so lets do some more checking and see if we find something with a different call
            switch (matches.Count)
            {
                case 0:
                    // match not found so lets widen the search
                    matches = RefineHQPMatch(qsos, qso, 5, 3);
                    break;
                default:
                    if (matches.Count > 0)
                    {
                        foreach (QSO near in matches)
                        {
                            qso.NearestMatches.Add(near);
                        }
                    }

                    qso.CallIsBusted = true;
                    return;
            }

            switch (matches.Count)
            {
                case 0:
                    // match not found because he is not in that log
                    // that log does not show up in the CallDictionary for that contact call
                    qso.CallIsBusted = true;
                    return;
                default:
                    if (matches.Count > 0)
                    {
                        foreach (QSO near in matches)
                        {
                            qso.NearestMatches.Add(near);
                        }
                    }

                    qso.CallIsBusted = true;
                    return;
            }

        }

        /// <summary>
        /// // this can have more entries than qsos because the list is flattened
        /// </summary>
        /// <param name="contestLogs"></param>
        /// <param name="qso"></param>
        /// <param name="timeInterval"></param>
        /// <returns></returns>
        private List<QSO> RefineHQPMatch(List<KeyValuePair<string, List<QSO>>> qsos, QSO qso, int timeInterval, int queryLevel)
        {
            List<QSO> matches = new List<QSO>();

            switch (queryLevel)
            {
                case 1:
                    matches = qsos.SelectMany(x => x.Value)
                    .Where(y => y.ContactCall == qso.OperatorCall && y.OperatorCall == qso.ContactCall && y.Band == qso.Band &&
                    y.Mode == qso.Mode && Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= timeInterval)
                    .ToList();
                    break;
                case 2:
                    qsos.SelectMany(x => x.Value)
                    .Where(y => y.OperatorCall == qso.ContactCall && y.Band == qso.Band && y.Mode == qso.Mode &&
                     Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= timeInterval)
                    .ToList();
                    break;
                case 3:
                    matches = qsos.SelectMany(x => x.Value)
                    .Where(y => y.Band == qso.Band && y.Mode == qso.Mode && Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= timeInterval)
                    .ToList();
                    break;
            }

            return matches;
        }

        #endregion

        #region CWOpen Matching

        /// <summary>
        /// WHEN DO I USE BAD CALL LIST???
        /// </summary>
        /// <param name="qso"></param>
        private void FindCWOpenMatchingQsos(QSO qso)
        {
            List<QSO> matches;
            IEnumerable<ContestLog> contestLogs;
            IEnumerable<KeyValuePair<string, List<QSO>>> qsos;
            double qsoPoints;
            double matchQsoPoints;

            if (qso.Status != QSOStatus.ValidQSO)
            {
                return;
            }

            // all logs with this operator call sign
            if (CallDictionary.ContainsKey(qso.OperatorCall))
            {
                contestLogs = CallDictionary[qso.OperatorCall];
            }
            else
            {
                // i.e. JF2IWL made 3 qsos and those three got in the CallDictionary
                // pointing at JF2IWL. However none of the 3 sent in a log so JF2IWL 
                // does not appear in the CallDictionary as there are no logs that
                // have a QSO for him so consider his all valid
                qso.IsUniqueCall = true;
                return;
            }


            // if there is only one log this call is in then it is unique
            if (CallDictionary[qso.ContactCall].Count == 1)
            {
                qso.IsUniqueCall = true;
                return;
            }

            // there is no matching log for this call so we give him the qso
            // maybe should see if anyone else worked him?
            if (ContestLogList.Where(b => b.LogOwner == qso.ContactCall).Count() == 0)
            {
                qso.NoMatchingLog = true;
                qso.IsUniqueCall = true;
                return;
            }

            // no reason to process if it already has a match
            if (qso.HasBeenMatched)
            {
                return;
            }

            // List of List<QSO> from the list of contest logs that match this operator call sign
            
            qsos = contestLogs.SelectMany(z => z.QSODictionary).Where(x => x.Key == qso.OperatorCall);

            // this gets all the QSOs in a flattened list
            IEnumerable<QSO> qsosXX = qsos.SelectMany(x => x.Value);

            matches = RefineCWOpenMatch(qsosXX, qso, 5, 1).ToList();


            switch (matches.Count)
            {
                case 0:
                    // match not found so lets search without serial number
                    matches = RefineCWOpenMatch(qsosXX, qso, 5, 2).ToList();
                    break;
                case 1:
                    // found one match so we mark both as matches and add matching QSO
                    qso.MatchingQSO = matches[0];
                    matches[0].MatchingQSO = qso;

                    qso.HasBeenMatched = true;
                    matches[0].HasBeenMatched = true;
                    return;
                case 2:
                    // two matches so these are probably dupes
                    qso.HasBeenMatched = true;
                    qso.MatchingQSO = matches[0];
                    qso.QSOHasDupes = true;

                    matches[0].HasBeenMatched = true;
                    matches[0].MatchingQSO = qso;

                    matches[1].HasBeenMatched = true;
                    matches[1].MatchingQSO = qso;
                    matches[1].FirstMatchingQSO = matches[0];
                    matches[1].IsDuplicateMatch = true;
                    return;
                default:
                    // more than two so we have to do more analysis
                    var a = 1;
                    Console.WriteLine("FindCWOpenMatches: 1");
                    break;
            }

            switch (matches.Count)
            {
                case 0:
                    // search without name
                    matches = RefineCWOpenMatch(qsosXX, qso, 5, 3).ToList();
                    break;
                case 1:
                    qso.MatchingQSO = matches[0];
                    matches[0].MatchingQSO = qso;

                    qso.HasBeenMatched = true;
                    matches[0].HasBeenMatched = true;

                    // determine which serial number(s) are incorrect
                    if (matches[0].ReceivedSerialNumber != qso.SentSerialNumber)
                    {
                        matches[0].IncorrectSerialNumber = true;
                        matches[0].IncorrectValue = $"{matches[0].ReceivedSerialNumber} --> {qso.SentSerialNumber}";
                    }

                    if (qso.ReceivedSerialNumber != matches[0].SentSerialNumber)
                    {
                        qso.IncorrectSerialNumber = true;
                        qso.IncorrectValue = $"{qso.ReceivedSerialNumber} --> {matches[0].SentSerialNumber}";
                    }
                    return;
                default:
                    bool matchFound = false;
                    // duplicate incorrect serial number QSOs
                    qso.MatchingQSO = matches[0];
                    qso.HasBeenMatched = true;

                    // This QSO is not in the other operators log or the call may be busted:
                    // QSO: 3533    CW  2016 - 09 - 03  0220    K3WA    111 BILL K3WJV   126 BILL
                    // QSO: 3533 CW 2016-09-03 0220 K3WA 111 BILL K3WJV 126 BILL
                    // QSO: 3533 CW 2016-09-03 0218 K3WJV 125 BILL K3WA 110 BILL
                    // QSO: 3533 CW 2016-09-03 0219 K3WJV 126 BILL K3WA 110 BILL
                    foreach (QSO matchQSO in matches)
                    {
                        matchQSO.MatchingQSO = qso;
                        matchQSO.HasBeenMatched = true;

                        // determine which serial number(s) are incorrect
                        if (matchQSO.ReceivedSerialNumber != qso.SentSerialNumber)
                        {
                            matchQSO.IncorrectSerialNumber = true;
                            matchQSO.IncorrectValue = $"{matchQSO.ReceivedSerialNumber} --> {qso.SentSerialNumber}";
                        }

                        // we don't know the order they are evaluated so a later bad QSO could invalidate a good QSO
                        if (qso.ReceivedSerialNumber != matchQSO.SentSerialNumber)
                        {
                            qso.IncorrectSerialNumber = true;
                            qso.IncorrectValue = $"{qso.ReceivedSerialNumber} --> {matchQSO.SentSerialNumber}";
                        }
                        else
                        {
                            matchFound = true;
                        }
                    }

                    if (matchFound == true)
                    {
                        qso.IncorrectSerialNumber = false;
                        qso.IncorrectValue = "";
                    }
                    return;
            }

            // name mismatch
            switch (matches.Count)
            {
                case 0:
                    // search without band
                    matches = RefineCWOpenMatch(qsosXX, qso, 5, 4).ToList();
                    break;
                case 1:
                    qso.MatchingQSO = matches[0];
                    matches[0].MatchingQSO = qso;

                    qso.HasBeenMatched = true;
                    matches[0].HasBeenMatched = true;

                    qso.IncorrectContactName = true;
                    qso.IncorrectValue = $"{qso.ContactName} --> {matches[0].OperatorName}";
                    return;
                default:
                    // duplicate QSOs

                    if (qso.HasBeenMatched == false)
                    {
                        qso.MatchingQSO = matches[0];
                        qso.HasBeenMatched = true;
                        Console.WriteLine("FindCWOpenMatches: 4.a");
                    }

                    foreach (QSO matchQSO in matches)
                    {
                        if (qso.HasBeenMatched == false)
                        {
                            Console.WriteLine("FindCWOpenMatches: 4.b");
                            matchQSO.MatchingQSO = qso;
                            matchQSO.HasBeenMatched = true;

                            matchQSO.IsDuplicateMatch = true;
                            //matchQSO.IncorrectValue = $"{qso.ContactName} --> {matches[0].OperatorName}";
                        }
                    }
                    Console.WriteLine("FindCWOpenMatches: 4");
                    return;
            }

            // band mismatch
            switch (matches.Count)
            {
                case 0:
                    // search without time
                    matches = RefineCWOpenMatch(qsosXX, qso, 5, 5).ToList();
                    break;
                case 1:
                    qso.MatchingQSO = matches[0];
                    matches[0].MatchingQSO = qso;

                    qso.HasBeenMatched = true;
                    matches[0].HasBeenMatched = true;

                    // whos at fault? need to get qsos around contact for each guy
                    qsoPoints = DetermineBandFault(qso);
                    matchQsoPoints = DetermineBandFault(matches[0]);

                    if (qsoPoints.Equals(matchQsoPoints))
                    {
                        // can't tell who's at fault so let them both have point
                        return;
                    }

                    if (qsoPoints > matchQsoPoints)
                    {
                        matches[0].IncorrectBand = true;
                        matches[0].IncorrectValue = $"{matches[0].Band} --> {qso.Band}";
                    }
                    else
                    {
                        qso.IncorrectBand = true;
                        qso.IncorrectValue = $"{qso} --> {matches[0].Band}";
                    }
                    return;
                default:
                    // duplicate incorrect QSOs
                    if (qso.HasBeenMatched == false)
                    {
                        qso.MatchingQSO = matches[0];
                        qso.HasBeenMatched = true;
                        Console.WriteLine("FindCWOpenMatches: 5.a");
                    }

                    foreach (QSO matchQSO in matches)
                    {
                        if (matchQSO.HasBeenMatched == false)
                        {
                            Console.WriteLine("FindCWOpenMatches: 5.b");
                            matchQSO.MatchingQSO = qso;
                            matchQSO.HasBeenMatched = true;

                            matchQSO.IsDuplicateMatch = true;
                            //matchQSO.IncorrectContactName = true;
                            //matchQSO.IncorrectValue = $"{qso} --> {matches[0].Band}";
                        }
                    }
                    Console.WriteLine("FindCWOpenMatches: 5");
                    return;
            }

            switch (matches.Count)
            {
                case 0:
                    qso.CallIsBusted = true;
                    return;
                case 1:
                    // for now don't penalize for time mismatch since everything else is correct
                    qso.MatchingQSO = matches[0];
                    matches[0].MatchingQSO = qso;

                    qso.HasBeenMatched = true;
                    matches[0].HasBeenMatched = true;

                    //qso.IncorrectQSODateTime = true;
                    //qso.IncorrectValue = $"{qso.QSODateTime} --> {matches[0].QSODateTime}";
                    return;
                default:
                    // duplicate incorrect QSOs
                    if (qso.HasBeenMatched == false)
                    {
                        qso.MatchingQSO = matches[0];
                        qso.HasBeenMatched = true;
                        Console.WriteLine("FindCWOpenMatches: 6.a");
                    }

                    foreach (QSO matchQSO in matches)
                    {
                        if (matchQSO.HasBeenMatched == false)
                        {
                            Console.WriteLine("FindCWOpenMatches: 6.b");
                            matchQSO.MatchingQSO = qso;
                            matchQSO.HasBeenMatched = true;

                            matchQSO.IsDuplicateMatch = true;
                            //matchQSO.IncorrectContactName = true;
                            //matchQSO.IncorrectValue = $"{qso} --> {matches[0].Band}";
                        }
                    }
                    Console.WriteLine("FindCWOpenMatches: 6");
                    break;
            }
        }

        /// <summary>
        /// // this can have more entries than qsos because the list is flattened
        /// </summary>
        /// <param name="contestLogs"></param>
        /// <param name="qso"></param>
        /// <param name="timeInterval"></param>
        /// <returns></returns>
        private IEnumerable<QSO> RefineCWOpenMatch(IEnumerable<QSO> qsos, QSO qso, int timeInterval, int queryLevel) // IEnumerable<KeyValuePair<string, List<QSO>>>
        {
            IEnumerable<QSO> matches = new List<QSO>();

            switch (queryLevel)
            {
                case 1: // exact match
                    matches = qsos
                   .Where(y => y.ContactCall == qso.OperatorCall && y.OperatorCall == qso.ContactCall && y.Band == qso.Band &&
                   y.OperatorName == qso.ContactName && y.SentSerialNumber == qso.ReceivedSerialNumber && y.ReceivedSerialNumber == qso.SentSerialNumber &&
                   Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= timeInterval);

                    //matches = qsos.SelectMany(x => x.Value)
                    //.Where(y => y.ContactCall == qso.OperatorCall && y.OperatorCall == qso.ContactCall && y.Band == qso.Band &&
                    //y.OperatorName == qso.ContactName && y.SentSerialNumber == qso.ReceivedSerialNumber && y.ReceivedSerialNumber == qso.SentSerialNumber &&
                    //Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= timeInterval);
                    ////.ToList();

                    break;
                case 2:
                    // catch incorrect serial number
                    matches = qsos
                    .Where(y => y.ContactCall == qso.OperatorCall && y.OperatorCall == qso.ContactCall && y.Band == qso.Band &&
                    y.OperatorName == qso.ContactName &&
                    Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= timeInterval);



                    //matches = qsos.SelectMany(x => x.Value)
                    //.Where(y => y.ContactCall == qso.OperatorCall && y.OperatorCall == qso.ContactCall && y.Band == qso.Band &&
                    //y.OperatorName == qso.ContactName &&
                    //Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= timeInterval);
                    ////.ToList();

                    break;
                case 3:
                    // incorrect name
                    matches = qsos
                    .Where(y => y.ContactCall == qso.OperatorCall && y.OperatorCall == qso.ContactCall && y.Band == qso.Band &&
                    y.SentSerialNumber == qso.ReceivedSerialNumber && y.ReceivedSerialNumber == qso.SentSerialNumber &&
                    Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= timeInterval);


                    //matches = qsos.SelectMany(x => x.Value)
                    //.Where(y => y.ContactCall == qso.OperatorCall && y.OperatorCall == qso.ContactCall && y.Band == qso.Band &&
                    //y.SentSerialNumber == qso.ReceivedSerialNumber && y.ReceivedSerialNumber == qso.SentSerialNumber &&
                    //Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= timeInterval);
                    //.ToList();

                    break;
                case 4:
                    // incorrect band
                    matches = qsos
                   .Where(y => y.ContactCall == qso.OperatorCall && y.OperatorCall == qso.ContactCall &&
                   y.SentSerialNumber == qso.ReceivedSerialNumber && y.ReceivedSerialNumber == qso.SentSerialNumber && y.OperatorName == qso.ContactName &&
                   Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= timeInterval);


                    //matches = qsos.SelectMany(x => x.Value)
                    //.Where(y => y.ContactCall == qso.OperatorCall && y.OperatorCall == qso.ContactCall &&
                    //y.SentSerialNumber == qso.ReceivedSerialNumber && y.ReceivedSerialNumber == qso.SentSerialNumber && y.OperatorName == qso.ContactName &&
                    //Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= timeInterval);
                    // //.ToList();
                    break;
                case 5:
                    // incorrect time
                    matches = qsos
                    .Where(y => y.ContactCall == qso.OperatorCall && y.OperatorCall == qso.ContactCall && y.Band == qso.Band &&
                     y.OperatorName == qso.ContactName && y.SentSerialNumber == qso.ReceivedSerialNumber && y.ReceivedSerialNumber == qso.SentSerialNumber);



                    //  matches = qsos.SelectMany(x => x.Value)
                    // .Where(y => y.ContactCall == qso.OperatorCall && y.OperatorCall == qso.ContactCall && y.Band == qso.Band &&
                    // y.OperatorName == qso.ContactName && y.SentSerialNumber == qso.ReceivedSerialNumber && y.ReceivedSerialNumber == qso.SentSerialNumber);
                    //// .ToList();
                    break;
                default:
                    Console.WriteLine("Failed search: " + qso.RawQSO);
                    break;

            }

            return matches;
        }

        #endregion

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
            List<QSO> matchingQSOSpecific = null;
            List<QSO> matchingQSOsGeneral = null;
            string name = qso.ContactName;

            // speeds up from 28 sec to 12 sec on first pass
            List<ContestLog> tempLog = CallDictionary[qso.ContactCall];

            // -------------------------------------
            IEnumerable<KeyValuePair<string, List<QSO>>> qsos;
            qsos = tempLog.SelectMany(z => z.QSODictionary).Where(x => x.Key == qso.ContactCall);
            // this gets all the QSOs in a flattened list
            matchingQSOSpecific = qsos.SelectMany(x => x.Value).Where(z => z.ContactName == qso.ContactName).ToList();
            matchingQSOsGeneral = qsos.SelectMany(x => x.Value).Where(z => z.ContactCall == qso.ContactCall).ToList();
            // ---------------------------------------------

            // get a list of all QSOs with the same contact callsign and same entity
            //matchingQSOSpecific = tempLog.SelectMany(z => z.QSOCollection).Where(q => q.ContactCall == qso.ContactCall && q.ContactName == qso.ContactName).ToList();

            // get a list of all QSOs with the same contact callsign but may have different entity
            //matchingQSOsGeneral = tempLog.SelectMany(z => z.QSOCollection).Where(q => q.ContactCall == qso.ContactCall).ToList();
            


            // if the above counts are different then someone is wrong
            matchCount = matchingQSOsGeneral.Count - matchingQSOSpecific.Count;

            switch (matchCount)
            {
                case 0:
                    // if no mismatches then assume it is correct
                    return;
                default:
                    // did contact submit a log
                    List<ContestLog> log = ContestLogList.Select(x => x).Where(y => y.LogOwner == qso.ContactCall).ToList();

                    if (log.Count > 0)
                    {
                        name = log.First().OperatorName;
                    }
                    else // no log submitted
                    {
                        try
                        {
                            // gives list of all non-distinct items
                            var nonDistinctNames =
                                from list in matchingQSOsGeneral
                                group list by list.ContactName into grouped
                                where grouped.Count() > 1
                                select grouped.ToList();

                            name = nonDistinctNames.First()[0].ContactName;

                        }
                        catch (Exception)
                        {
                            // all the names were distinct so can't determine correct name
                            name = qso.ContactName;
                        }
                    }

                    break;
            }

            // if entity is different
            if (name != qso.ContactName)
            {
                qso.IncorrectValue = qso.ContactName + " --> " + name;
                qso.IncorrectContactName = true;
            }
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
            //difference = majorityContactEntities.Count();

            if (majorityContactEntities.Count() > 1)
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
                    //var merged = listWithMostEntries.Except(listWithLeastEntries);

                    QSO firstMatch = listWithMostEntries.Except(listWithLeastEntries).FirstOrDefault();
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
                qso.IncorrectDXEntity = $"{qso.ContactEntity} --> {entity}";
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
                    if (qso.Mode != QSOMode.RY)
                    {
                        matchQSO = matchLog.QSOCollection.FirstOrDefault(q => q.Band == qso.Band && q.Mode == qso.Mode &&
                      q.ContactCall == qso.OperatorCall && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) > 5); //  && q.ContactEntity == qso.ContactEntity 
                    }
                    else
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
                //qso.ExcessTimeSpan = Math.Abs(ts.Minutes);

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
            double qsoPoints;
            double matchQsoPoints;
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
                    //isMatchQSO = DetermineBandFault(matchLog, qso, matchQSO);
                    //qsoPoints = DetermineBandFault(qso);
                    //matchQsoPoints = DetermineBandFault(matchQSO);

                }
                catch (Exception ex)
                {
                    string a = ex.Message;
                }

                qso.MatchingQSO = matchQSO;
                qso.HasBeenMatched = true;

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
                        qso.IncorrectValue = qso.ContactName;
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
                    matchQSO.HasBeenMatched = true;
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
        /// Determine the most likely qso to have recorded the incorrect band.
        /// Points given for rig control
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
            string frequency;
            bool isMatchQSO = true;

            // bonus point for having rig control frequency
            //frequency = qso.Frequency;


            //if (frequency != "3500" && frequency != "7000" && frequency != "14000" && frequency != "21000" && frequency != "28000")
            //{
            //    qsoPoints += 1;
            //}

            //frequency = matchQSO.Frequency;
            //if (frequency != "3500" && frequency != "7000" && frequency != "14000" && frequency != "21000" && frequency != "28000")
            //{
            //    matchQsoPoints += 1;
            //}

            // get the previous and next qso for the qso
            //previousQSO = GetPrevious(contestLog.QSOCollection, qso);



            // qsoPoints = ExamineBandUsage(qso, qsoPoints);
            //int counter = 0;
            //while (counter < 5)
            //{
            //    counter += 1;
            //    previousQSO = GetPrevious(contestLog.QSOCollection, qso);
            //    if (previousQSO != null)
            //    {
            //        if (previousQSO.Band == qso.Band)
            //        {
            //            qsoPoints += 1;
            //        }
            //        else
            //        {
            //            qsoPoints -= 1;
            //        }
            //    }
            //    else
            //    {
            //        qsoPoints += 1;
            //    }
            //}

            //while (counter < 5)
            //{
            //    counter += 1;
            //    nextQSO = GetNext(contestLog.QSOCollection, qso);
            //    if (nextQSO != null)
            //    {
            //        if (nextQSO.Band == qso.Band)
            //        {
            //            qsoPoints += 1;
            //        }
            //        else
            //        {
            //            qsoPoints -= 1;
            //        }
            //    }
            //    else
            //    {
            //        qsoPoints += 1;
            //    }
            //}


            //if (previousQSO != null)
            //{
            //    if (previousQSO.Band == qso.Band)
            //    {
            //        qsoPoints += 1;
            //    }
            //    else
            //    {
            //        qsoPoints -= 1;
            //    }
            //}
            //else
            //{
            //    qsoPoints += 1;
            //}

            //if (nextQSO != null)
            //{
            //    if (nextQSO.Band == qso.Band)
            //    {
            //        qsoPoints += 1;
            //    }
            //    else
            //    {
            //        qsoPoints -= 1;
            //    }
            //}
            //else
            //{
            //    qsoPoints += 1;
            //}

            // get the previous and next qso for the matchQso
            //previousQSO = GetPrevious(matchLog.QSOCollection, matchQSO);
            //nextQSO = GetNext(matchLog.QSOCollection, matchQSO);

            //matchQsoPoints = ExamineBandUsage(matchQSO, matchQsoPoints);

            //if (previousQSO != null)
            //{
            //    if (previousQSO.Band == matchQSO.Band)
            //    {
            //        matchQsoPoints += 1;
            //    }
            //    else
            //    {
            //        matchQsoPoints -= 1;
            //    }
            //}
            //else
            //{
            //    matchQsoPoints -= 1;
            //}

            //if (nextQSO != null)
            //{
            //    if (nextQSO.Band == matchQSO.Band)
            //    {
            //        matchQsoPoints += 1;
            //    }
            //    else
            //    {
            //        matchQsoPoints -= 1;
            //    }
            //}
            //else
            //{
            //    matchQsoPoints -= 1;
            //}


            if (qsoPoints < matchQsoPoints)
            {
                isMatchQSO = false;
            }

            return isMatchQSO;
        }

        private double DetermineBandFault(QSO qso)
        {
            ContestLog contestLog = qso.ParentLog;
            QSO previousQSO;
            QSO nextQSO;
            string frequency = qso.Frequency;
            double qsoPoints = 0;
            double counter = 0;

            // bonus point for having rig control frequency
            if (frequency != "1800" && frequency != "3500" && frequency != "7000" && frequency != "14000" && frequency != "21000" && frequency != "28000")
            {
                qsoPoints += 1;
            }

            while (counter < 10)
            {
                counter += 1;
                previousQSO = GetPrevious(contestLog.QSOCollection, qso);

                if (previousQSO != null)
                {
                    frequency = previousQSO.Frequency;
                    if (frequency != "1800" && frequency != "3500" && frequency != "7000" && frequency != "14000" && frequency != "21000" && frequency != "28000")
                    {
                        qsoPoints += 1;
                    }

                    if (previousQSO.Band == qso.Band)
                    {
                        qsoPoints += 1;
                    }
                    else
                    {
                        qsoPoints -= 1;
                    }

                    qso = previousQSO;
                }
                else
                {
                    // extra .1 point because he just started and more likely to be correct
                    qsoPoints += 1.1;
                }
            }

            counter = 0;

            while (counter < 10)
            {
                counter += 1;
                nextQSO = GetNext(contestLog.QSOCollection, qso);

                if (nextQSO != null)
                {
                    frequency = nextQSO.Frequency;
                    if (frequency != "1800" && frequency != "3500" && frequency != "7000" && frequency != "14000" && frequency != "21000" && frequency != "28000")
                    {
                        qsoPoints += 1;
                    }
                    if (nextQSO.Band == qso.Band)
                    {
                        qsoPoints += 1;
                    }
                    else
                    {
                        qsoPoints -= 1;
                    }
                    qso = nextQSO;
                }
                else
                {
                    // extra half point because he just ended and more likely to be correct
                    qsoPoints += 1.1;
                }
            }

            return qsoPoints;
        }

        /// <summary>
        /// Get the next item in a collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="current"></param>
        /// <returns></returns>
        private T GetNext<T>(IEnumerable<T> list, T current)
        {
            try
            {
                return list.SkipWhile(x => !x.Equals(current)).Skip(1).First();
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Get the previous item in a collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="current"></param>
        /// <returns></returns>
        private T GetPrevious<T>(IEnumerable<T> list, T current)
        {
            try
            {
                return list.TakeWhile(x => !x.Equals(current)).Last();
            }
            catch
            {
                return default;
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
                qsos.Select(c => { c.IncorrectOperatorName = false; return c; }).ToList();
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

            //  case string _ when pattern.Last().ToString().Contains("/"):
            //  if (Enum.IsDefined(typeof(HQPMults), qso.ContactEntity))
            // if there is only one then check it is valid
            if (qsoList.Count == 1)
            {
                switch (qsoList[0].OperatorEntity)
                {
                    case "DX":
                        return;
                    case string _ when States.Contains(qsoList[0].OperatorEntity):
                        return;
                    case string _ when Provinces.Contains(qsoList[0].OperatorEntity):
                        return;
                    case string _ when Enum.IsDefined(typeof(HQPMults), qsoList[0].OperatorEntity):
                        return;
                    default:
                        qsoList[0].SentEntityIsInValid = true;
                        break;
                }

            }

            if (qsos.Any())
            {
                _ = qsos.Select(c => { c.SentEntityIsInValid = true; return c; })
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
