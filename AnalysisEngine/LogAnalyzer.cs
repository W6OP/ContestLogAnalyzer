using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Objects.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W6OP.ContestLogAnalyzer
{
    public class LogAnalyzer
    {
        public ContestName ActiveContest;

        private ILookup<string, string> _BadCallList;
        public ILookup<string, string> BadCallList
        {
            set
            {
                _BadCallList = value;
            }
        }

        public delegate void ProgressUpdate(string value, string qsoCount, string validQsoCount, Int32 progress);
        public event ProgressUpdate OnProgressUpdate;

        //private SortedDictionary<string, string> _CallTable;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public LogAnalyzer()
        {

        }

        /// <summary>
        /// Get a list of all distinct call/name pairs by grouped by call sign. This is used
        /// for the bad call list.
        /// </summary>
        /// <param name="contestLogList"></param>
        public List<Tuple<string, string>> CollectAllCallNamePairs(List<ContestLog> contestLogList)
        {
            // list of all distinct call/name pairs
            List<Tuple<string, string>> distinctCallNamePairs = contestLogList.SelectMany(z => z.QSOCollection)
              .Select(r => new Tuple<string, string>(r.ContactCall, r.ContactName))
              .GroupBy(p => new Tuple<string, string>(p.Item1, p.Item2))
              .Select(g => g.First())
              .OrderBy(q => q.Item1)
              .ToList();

            return distinctCallNamePairs;
        }

        /// <summary>
        /// Take the list of distinct call signs and a list of all call/name pairs. For every
        /// call sign see how many times it was used. Also, get the call and name combination
        /// and see how many times each name was used.
        /// public List<Tuple<string, Int32, string, Int32>> CollectCallNameHitData(List<Tuple<string, string>> distinctCallNamePairs, List<ContestLog> contestLogList, out List<Tuple<string, string>> suspectCallList)
        /// </summary>
        /// <param name="distinctCallNamePairs"></param>
        /// <param name="contestLogList"></param>
        /// <returns></returns>
        public List<Tuple<string, Int32, string, Int32>> CollectCallNameHitData(List<Tuple<string, string>> distinctCallNamePairs, List<ContestLog> contestLogList)
        {
            string currentCall = "";
            string previousCall = "";
            Int32 count = 0;

            //suspectCallList = new List<Tuple<string, string>>();

            List<Tuple<string, Int32, string, Int32>> callNameCountList = new List<Tuple<string, int, string, int>>();

            List<Tuple<string, string>> allCallNamePairs = contestLogList.SelectMany(z => z.QSOCollection)
                .Select(r => new Tuple<string, string>(r.ContactCall, r.ContactName))
                .ToList();

            for (int i = 0; i < distinctCallNamePairs.Count; i++)
            {
                IEnumerable<Tuple<string, string>> callCount = allCallNamePairs.Where(t => t.Item1 == distinctCallNamePairs[i].Item1);
                IEnumerable<Tuple<string, string>> nameCount = allCallNamePairs.Where(t => t.Item1 == distinctCallNamePairs[i].Item1 && t.Item2 == distinctCallNamePairs[i].Item2);

                if (previousCall != distinctCallNamePairs[i].Item1)
                {
                    previousCall = distinctCallNamePairs[i].Item1;
                    currentCall = distinctCallNamePairs[i].Item1;
                    count = callCount.Count();

                    //if (count <= 3)
                    //{
                    //    suspectCallList.Add(distinctCallNamePairs[i]); // this is an OUT parameter
                    //}
                }
                else
                {
                    currentCall = "";
                    count = 0;
                }

                Tuple<string, Int32, string, Int32> tuple = new Tuple<string, Int32, string, Int32>(currentCall, count, distinctCallNamePairs[i].Item2, nameCount.Count());

                callNameCountList.Add(tuple);
            }

            return callNameCountList;
        }

        /// <summary>
        /// Get every QSO that matches the call/name in the suspect call list.
        /// NOT IMPLEMENTED YET - need to determine if I need this
        /// </summary>
        /// <param name="suspectCallList"></param>
        /// <returns></returns>
        public List<QSO> CollectSuspectQSOs(List<Tuple<string, string>> suspectCallList, List<ContestLog> contestLogList)
        {
            List<QSO> suspectQSOs = new List<QSO>();


            //suspectQSOs = (QSO)contestLog.QSOCollection.FirstOrDefault(q => q.Band == qso.Band && q.OperatorName == qso.ContactName && q.OperatorCall == qso.ContactCall &&
            //                             q.ContactCall == qso.OperatorCall && Math.Abs(q.SentSerialNumber - qso.ReceivedSerialNumber) <= 1 && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).Minutes) <= 5);


            List<Tuple<string, QSO>> allCallNamePairs = contestLogList.SelectMany(z => z.QSOCollection)
                .Select(r => new Tuple<string, QSO>(r.ContactCall, r))
                .ToList();


            foreach (Tuple<string, string> tuple in suspectCallList)
            {
                // WHERE I LEFT OFF
                // allCallNamePairs - query this

                //QSO qso = (QSO)contestLogList.FirstOrDefault(q => q.QSOCollection != null).QSOCollection.FirstOrDefault((q => q.ContactCall == tuple.Item1 && q.ContactName == tuple.Item2));


            }

            return suspectQSOs;
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
        public void PreProcessContestLogs(List<ContestLog> contestLogList)
        {
            List<QSO> qsoList = new List<QSO>();
            string call = null;
            string name = null;
            Int32 progress = 0;
            Int32 validQsos;

            OnProgressUpdate?.Invoke("1", "", "", 0);

            foreach (ContestLog contestLog in contestLogList)
            {
                call = contestLog.LogOwner;
                name = contestLog.LogHeader.NameSent.ToUpper();

                progress++;

                if (!contestLog.IsCheckLog && contestLog.IsValidLog)
                {
                    qsoList = contestLog.QSOCollection;

                    MarkIncorrectCallSigns(qsoList, call);

                    switch (ActiveContest)
                    {
                        case ContestName.CW_OPEN:
                            MarkIncorrectSentName(qsoList, name);
                            break;
                        case ContestName.HQP:
                            MarkIncorrectSentEntity(qsoList, name);
                            break;
                    }

                    MatchQSOs(qsoList, contestLogList, call, name);

                    MarkDuplicateQSOs(qsoList);

                    validQsos = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO).Count();

                    // ReportProgress with Callsign
                    OnProgressUpdate?.Invoke(call, contestLog.QSOCollection.Count.ToString(), validQsos.ToString(), progress);
                }
            }
        }

        /// <summary>
        /// All we want to do here is look for matching QSOs
        /// </summary>
        /// <param name="contestLogList"></param>
        public void PreProcessContestLogsReverse(List<ContestLog> contestLogList)
        {
            List<QSO> qsoList = new List<QSO>();
            string call = null;
            string name = null;
            Int32 progress = 0;
            Int32 validQsos;

            OnProgressUpdate?.Invoke("2", "", "", 0);
            contestLogList.Reverse();

            foreach (ContestLog contestLog in contestLogList)
            {
                call = contestLog.LogOwner;
                name = contestLog.LogHeader.NameSent.ToUpper();

                progress++;

                if (!contestLog.IsCheckLog && contestLog.IsValidLog)
                {
                    // only use valid QSOs in reverse
                    qsoList = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO).ToList();

                    MatchQSOs(qsoList, contestLogList, call, name);

                    validQsos = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO).Count();

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
            List<QSO> qsos = qsoList.Where(q => q.OperatorCall.ToUpper() != call && q.Status == QSOStatus.ValidQSO).ToList();

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
        private void MatchQSOs(List<QSO> qsoList, List<ContestLog> contestLogList, string operatorCall, string sentName)
        {
            ContestLog matchLog = null;
            RejectReason reason = RejectReason.None;
            List<ContestLog> tempLog = new List<ContestLog>();
            QSO matchQSO = null;
            QSO matchQSO_X = null;

            foreach (QSO qso in qsoList)
            {
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
                            // leave the time check here for future use
                            matchQSO = (QSO)matchLog.QSOCollection.FirstOrDefault(q => q.Band == qso.Band && q.OperatorName == qso.ContactName && q.OperatorCall == qso.ContactCall &&
                                                  q.ContactCall == qso.OperatorCall && Math.Abs(q.SentSerialNumber - qso.ReceivedSerialNumber) <= 1 && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).Minutes) <= 5);

                            matchQSO_X = (QSO)matchLog.QSOCollectionX.FirstOrDefault(q => q.Band == qso.Band && q.OperatorName == qso.ContactName && q.OperatorCall == qso.ContactCall &&
                                                  q.ContactCall == qso.OperatorCall && Math.Abs(q.SentSerialNumber - qso.ReceivedSerialNumber) <= 1 && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).Minutes) <= 5);
                            break;
                        case ContestName.HQP:
                            // now see if a QSO matches this QSO
                            matchQSO = (QSO)matchLog.QSOCollection.FirstOrDefault(q => q.Band == qso.Band && q.OperatorName == qso.ContactName && q.OperatorCall == qso.ContactCall &&
                                                  q.ContactCall == qso.OperatorCall && q.Mode == qso.Mode && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).Minutes) <= 5);

                            matchQSO_X = (QSO)matchLog.QSOCollectionX.FirstOrDefault(q => q.Band == qso.Band && q.OperatorName == qso.ContactName && q.OperatorCall == qso.ContactCall &&
                                                  q.ContactCall == qso.OperatorCall && q.Mode == qso.Mode && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).Minutes) <= 5);
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
                            qso.RejectReasons.Clear();
                            qso.RejectReasons.Add(reason, EnumHelper.GetDescription(reason));
                        }
                    }
                }
                else
                {
                    // can't find a matching log
                    // find all the logs this operator is in so we can try to get a match without call signs
                    // don't want to exclude invalid QSOs as we are checking all logs and all QSOs
                    tempLog = contestLogList.Where(q => q.QSOCollection.Any(p => p.ContactCall == qso.ContactCall)).ToList();

                    if (tempLog.Count <= 1) // 1 would mean this call sign only in this log
                    {
                        // the call is not in any other log
                        if (!qso.HasMatchingQso)
                        {
                            qso.Status = QSOStatus.ReviewQSO;
                            qso.RejectReasons.Clear();
                            if (ActiveContest == ContestName.CW_OPEN)
                            {
                                // give them the point anyway
                                qso.IsMultiplier = true;
                                qso.RejectReasons.Add(RejectReason.NoQSO, EnumHelper.GetDescription(RejectReason.NoQSO));
                            }

                            if (ActiveContest == ContestName.HQP)
                            {
                                if (qso.ContactName != qso.DXCountry)
                                {
                                    qso.Status = QSOStatus.InvalidQSO;
                                    qso.EntityIsInValid = true;
                                    qso.IncorrectName = qso.ContactName + " --> " + qso.DXCountry;
                                    // not needed - qso.EntityIsInValid = true; does same thing
                                    //qso.RejectReasons.Add(RejectReason.InvalidEntity, EnumHelper.GetDescription(RejectReason.InvalidEntity));
                                }
                                else {
                                    // they get the point
                                    qso.RejectReasons.Add(RejectReason.NoQSO, EnumHelper.GetDescription(RejectReason.NoQSO));
                                }
                            }
                            
                        }
                        else
                        {
                            qso.Status = QSOStatus.InvalidQSO;
                            qso.RejectReasons.Clear();
                            qso.RejectReasons.Add(RejectReason.NoQSO, EnumHelper.GetDescription(RejectReason.BustedCallSign));
                        }
                    }
                    else
                    { // ok call is in 2 or more logs, is it busted
                        if (CheckBadCallList(qso) == false)
                        {
                            if (SearchForBustedCall(qso, contestLogList) == false) // did not find it so is name incorrect
                            {
                                qso.Status = QSOStatus.ValidQSO;
                            }
                        }
                        else
                        {
                            qso.Status = QSOStatus.InvalidQSO;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Search every log for a match to this QSO without the call sign
        /// </summary>
        /// <param name="qso"></param>
        private bool SearchForBustedCall(QSO qso, List<ContestLog> contestLogList)
        {
            bool found = false;
            List<QSO> matchingQSOs = null;

            // look for a log without a call sign parameter
            switch (ActiveContest)
            {
                case ContestName.CW_OPEN:
                    matchingQSOs = contestLogList.SelectMany(z => z.QSOCollection).Where(q => q.Band == qso.Band && q.OperatorCall == qso.ContactCall &&
                                      Math.Abs(q.SentSerialNumber - qso.ReceivedSerialNumber) <= 1 && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).Minutes) <= 5).ToList();
                    break;
                case ContestName.HQP:
                    // look for a log without a call sign parameter
                    matchingQSOs = contestLogList.SelectMany(z => z.QSOCollection).Where(q => q.Band == qso.Band && q.OperatorCall == qso.ContactCall &&
                                q.Mode == qso.Mode && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).Minutes) <= 5).ToList();
                    break;
            }

            if (matchingQSOs != null && matchingQSOs.Count > 0)
            {
                if (matchingQSOs.Count == 1)
                {    // found it so call is busted
                    found = true;
                    qso.CallIsBusted = true;
                    qso.MatchingQSO = matchingQSOs[0];
                }
            }
            else// did not find it
            {
                found = SearchForIncorrectName(qso, contestLogList);
            }

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
                    qso.RejectReasons.Clear();
                    qso.RejectReasons.Add(RejectReason.NoQSO, EnumHelper.GetDescription(RejectReason.BustedCallSign));

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Now see if the name is incorrect and that is why we can't find the QSO
        /// This is only when the call sign did not submit a log
        /// </summary>
        /// <param name="qso"></param>
        /// <param name="contestLogList"></param>
        /// <returns></returns>
        private bool SearchForIncorrectName(QSO qso, List<ContestLog> contestLogList)
        {
            bool found = false;
            Int32 matchCount = 0;
            List<QSO> matchingQSOs = null;
            string matchName = null;

            // now look for a match without the operator name - only search those not already eliminated
            //matchingQSOs = contestLogList.SelectMany(z => z.QSOCollection).Where(q => q.ContactCall == qso.ContactCall && q.Status == QSOStatus.ValidQSO).ToList();
            matchingQSOs = contestLogList.SelectMany(z => z.QSOCollection).Where(q => q.ContactCall == qso.ContactCall).ToList();

            if (matchingQSOs.Count >= 1)
            {
                // loop through and see if first few names match
                for (int i = 0; i < matchingQSOs.Count; i++)
                {
                    if (qso.ContactName != matchingQSOs[i].ContactName)
                    {
                        matchCount++;
                        matchName = matchingQSOs[i].ContactName;
                    }
                }

                // this is on for HQP
                if (ActiveContest == ContestName.HQP && qso.RealDXCountry != null && (qso.RealDXCountry == "Canada" || qso.RealDXCountry == "United States of America"))
                {
                    if (qso.DXCountry.Length > 2)
                    {
                        found = true;
                        qso.IncorrectName = qso.ContactName + " --> " + matchName;
                        qso.EntityIsInValid = true;
                    } else
                    {
                        if (qso.ContactName != matchName)
                        {
                            found = true;
                            qso.IncorrectName = qso.ContactName + " --> " + matchName;

                            qso.EntityIsInValid = true;
                        }
                    }
                    
                }
                else if ((Convert.ToDouble(matchCount) / Convert.ToDouble(matchingQSOs.Count)) * 100 > 50)
                {
                    //Console.WriteLine((Convert.ToDouble(matchCount) / Convert.ToDouble(matchingQSOs.Count)) * 100);
                    found = true;
                    qso.IncorrectName = qso.ContactName + " --> " + matchName;

                    switch (ActiveContest)
                    {
                        case ContestName.CW_OPEN:
                            qso.OpNameIsInValid = true;
                            break;
                        case ContestName.HQP:
                            qso.EntityIsInValid = true;
                            break;
                    }
                }
            }

            return found;
        }

        /// <summary>
        /// Determine the reason to reject the QSO
        /// </summary>
        /// <param name="matchLog"></param>
        /// <param name="qso"></param>
        /// <returns></returns>
        private RejectReason FindRejectReason(ContestLog matchLog, QSO qso)
        {
            QSO matchQSO = null;
            RejectReason reason = RejectReason.None;
            TimeSpan ts;
            bool isMatchQSO = false;

            switch (ActiveContest)
            {
                case ContestName.CW_OPEN:
                    matchQSO = (QSO)matchLog.QSOCollection.FirstOrDefault(q => q.Band == qso.Band && q.ContactName == qso.OperatorName && Math.Abs(q.SentSerialNumber - qso.ReceivedSerialNumber) <= 1 &&
                       q.ContactCall == qso.OperatorCall && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) > 5);
                    break;
                case ContestName.HQP:
                    matchQSO = (QSO)matchLog.QSOCollection.FirstOrDefault(q => q.Band == qso.Band && q.ContactName == qso.OperatorName && q.Mode == qso.Mode &&
                       q.ContactCall == qso.OperatorCall && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) > 5);
                    break;
            }

            if (matchQSO != null)
            {
                // store the matching QSO
                qso.MatchingQSO = matchQSO;
                ts = qso.QSODateTime.Subtract(matchQSO.QSODateTime);
                qso.ExcessTimeSpan = Math.Abs(ts.Minutes);

                return RejectReason.InvalidTime;
            }

            // query for incorrect serial number
            if (ActiveContest == ContestName.CW_OPEN)
            {
                matchQSO = (QSO)matchLog.QSOCollection.Where(q => q.Band == qso.Band && q.ContactName == qso.OperatorName && q.ContactCall == qso.OperatorCall &&
                            Math.Abs(q.SentSerialNumber - qso.ReceivedSerialNumber) > 1).FirstOrDefault(); //

                if (matchQSO != null)
                {
                    qso.MatchingQSO = matchQSO;
                    return RejectReason.SerialNumber;
                }
            }

            // query for incorrect band
            switch (ActiveContest)
            {
                case ContestName.CW_OPEN:
                    matchQSO = (QSO)matchLog.QSOCollection.FirstOrDefault(q => q.Band != qso.Band && q.ContactName == qso.OperatorName && Math.Abs(q.SentSerialNumber - qso.ReceivedSerialNumber) <= 1 &&
                         q.ContactCall == qso.OperatorCall);
                    break;
                case ContestName.HQP:
                    matchQSO = (QSO)matchLog.QSOCollection.FirstOrDefault(q => q.Band != qso.Band && q.ContactName == qso.OperatorName && q.Mode == qso.Mode &&
                        q.ContactCall == qso.OperatorCall && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) < 5);
                    break;
            }

            if (matchQSO != null)
            {
                // determine who is at fault
                isMatchQSO = DetermineBandFault(matchLog, qso, matchQSO);

                qso.MatchingQSO = matchQSO;
                qso.HasMatchingQso = true;

                if (isMatchQSO)
                {
                    matchQSO.RejectReasons.Clear(); // should not be a collection ?? or lets actually look for multiple reasons
                    matchQSO.RejectReasons.Add(RejectReason.Band, EnumHelper.GetDescription(RejectReason.Band));
                }
                else
                {
                    return RejectReason.Band;
                }

                return reason;
            }

            // query for incorrect mode
            switch (ActiveContest)
            {
                case ContestName.HQP:
                    matchQSO = (QSO)matchLog.QSOCollection.FirstOrDefault(q => q.Band == qso.Band && q.ContactName == qso.OperatorName && q.Mode != qso.Mode &&
                        q.ContactCall == qso.OperatorCall && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) < 5);
                    break;
            }

            if (matchQSO != null)
            {
                // determine who is at fault
                isMatchQSO = DetermineModeFault(matchLog, qso, matchQSO);

                qso.MatchingQSO = matchQSO;

                if (isMatchQSO)
                {
                    matchQSO.RejectReasons.Clear(); // should not be a collection ?? or lets actually look for multiple reasons
                    matchQSO.RejectReasons.Add(RejectReason.Mode, EnumHelper.GetDescription(RejectReason.Mode));
                }
                else
                {
                    return RejectReason.Mode;
                }

                return reason;
            }

            // query for incorrect name or entity (HQP)
            switch (ActiveContest)
            {
                case ContestName.CW_OPEN:
                    matchQSO = matchLog.QSOCollection.FirstOrDefault(q => q.Band == qso.Band && Math.Abs(q.SentSerialNumber - qso.ReceivedSerialNumber) <= 1 && q.ContactCall == qso.OperatorCall &&
                       q.ContactName == qso.OperatorName && q.OperatorName != qso.ContactName); //
                    break;
                case ContestName.HQP:
                    matchQSO = matchLog.QSOCollection.FirstOrDefault(q => q.Band == qso.Band && q.Mode == qso.Mode && q.ContactCall == qso.OperatorCall &&
                       q.ContactName == qso.OperatorName && q.OperatorName != qso.ContactName && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) < 5);
                    break;
            }

            if (matchQSO != null)
            {
                qso.MatchingQSO = matchQSO;
                qso.IncorrectName = qso.ContactName;

                switch (ActiveContest)
                {
                    case ContestName.CW_OPEN:
                        return RejectReason.OperatorName;
                    case ContestName.HQP:
                        return RejectReason.EntityName;
                }
            }

            // query for incorrect call
            switch (ActiveContest)
            {
                case ContestName.CW_OPEN:
                    matchQSO = (QSO)matchLog.QSOCollection.FirstOrDefault(q => q.Band == qso.Band && q.ContactName == qso.OperatorName && q.ContactCall != qso.OperatorCall &&
                       Math.Abs(q.SentSerialNumber - qso.ReceivedSerialNumber) <= 1); // 
                    break;
                case ContestName.HQP:
                    matchQSO = (QSO)matchLog.QSOCollection.FirstOrDefault(q => q.Band == qso.Band && q.ContactName == qso.OperatorName && q.ContactCall != qso.OperatorCall &&
                        q.Mode == qso.Mode && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) < 5); // 
                    break;
            }

            if (matchQSO != null) // this QSO is not in the other log
            {
                // determine which guy is at fault
                if (qso.ContactCall != matchQSO.OperatorCall)
                {
                    qso.MatchingQSO = matchQSO;
                    reason = RejectReason.BustedCallSign;
                }
                else
                {
                    matchQSO.HasMatchingQso = true;
                    matchQSO.MatchingQSO = qso;
                    matchQSO.RejectReasons.Clear(); // should not be a collection ?? or lets actually look for multiple reasons
                    matchQSO.RejectReasons.Add(RejectReason.NoQSOMatch, EnumHelper.GetDescription(RejectReason.NoQSOMatch));
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
            List<QSO> qsos = qsoList.Where(q => q.OperatorName.ToUpper() != name && q.Status == QSOStatus.ValidQSO).ToList();

            if (qsos.Any())
            {
                qsos.Select(c => { c.OpNameIsInValid = false; return c; }).ToList();
            }
        }

        private void MarkIncorrectSentEntity(List<QSO> qsoList, string name)
        {
            List<QSO> qsos = qsoList.Where(q => q.OperatorName.ToUpper() != name && q.Status == QSOStatus.ValidQSO).ToList();

            if (qsos.Any())
            {
                qsos.Select(c => { c.EntityIsInValid = false; return c; }).ToList();
            }
        }

        #region Soundex

        //if (Soundex(qso.IncorrectName) == Soundex(qso.MatchingQSO.OperatorName))
        //{
        //    var a = 1;
        //    //string zz = Soundex(qso.IncorrectName);
        //    //string xx = Soundex(qso.ContactName);
        //}

        private string Soundex(string data)
        {
            StringBuilder result = new StringBuilder();
            string previousCode = "";
            string currentCode = "";
            string currentLetter = "";

            if (data != null && data.Length > 0)
            {
                previousCode = "";
                currentCode = "";
                currentLetter = "";

                result.Append(data.Substring(0, 1));

                for (int i = 1; i < data.Length; i++)
                {
                    currentLetter = data.Substring(1, 1).ToLower();
                    currentCode = "";

                    if ("bfpv".IndexOf(currentLetter) > -1)
                    {
                        currentCode = "1";
                    }
                    else if ("cgjkqsxz".IndexOf(currentLetter) > -1)
                    {
                        currentCode = "2";
                    }
                    else if ("dt".IndexOf(currentLetter) > -1)
                    {
                        currentCode = "3";
                    }
                    else if (currentLetter == "1")
                    {
                        currentCode = "4";
                    }
                    else if ("mn".IndexOf(currentLetter) > -1)
                    {
                        currentCode = "5";
                    }
                    else if (currentLetter == "r")
                    {
                        currentCode = "6";
                    }

                    if (currentCode != previousCode)
                    {
                        result.Append(currentCode);
                    }

                    if (result.Length == 4)
                    {
                        break;
                    }

                    if (currentCode != previousCode)
                    {
                        previousCode = currentCode;
                    }
                }
            }

            if (result.Length < 4)
            {
                result.Append(new String('0', 4 - result.Length));
            }

            return result.ToString().ToUpper();
        }

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
