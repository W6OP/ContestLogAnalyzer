using System;
using System.Collections.Generic;
using System.Linq;
using W6OP.CallParser;

namespace W6OP.ContestLogAnalyzer
{
    public class LogAnalyzer
    {
        public delegate void ProgressUpdate(string value, string qsoCount, string validQsoCount, int progress);
        public event ProgressUpdate OnProgressUpdate;

        public ContestName ActiveContest;
        private ILookup<string, string> _BadCallList;
        public ILookup<string, string> BadCallList { set => _BadCallList = value; }

        public CallLookUp CallLookUp;

        /// <summary>
        /// Dictionary of all calls in all the logs as keys.
        /// The values are a list of all logs those calls are in.
        /// This almost quadruples lookup performance.
        /// </summary>
        public Dictionary<string, List<ContestLog>> CallDictionary;
        public Dictionary<string, List<Tuple<string, int>>> NameDictionary;
        public List<ContestLog> ContestLogList;
        readonly HPQAnalyzer hpqAnalyzer;
        readonly CWOpenAnalyzer cwOpenAnalyzer;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public LogAnalyzer()
        {
            hpqAnalyzer = new HPQAnalyzer(this);
            cwOpenAnalyzer = new CWOpenAnalyzer(this);
        }

        #region Common Code

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
            int validQsoCount;

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
                                cwOpenAnalyzer.MarkIncorrectSentName(qsoList, name);
                                break;
                            case ContestName.HQP:
                                hpqAnalyzer.MarkIncorrectSentEntity(qsoList);
                                hpqAnalyzer.MarkDuplicates(qsoList);
                                break;
                        }

                        foreach (QSO qso in qsoList)
                        {
                            if (qso.Status == QSOStatus.ValidQSO)
                            {
                                switch (ActiveContest)
                                {
                                    case ContestName.CW_OPEN:
                                        cwOpenAnalyzer.MarkIncorrectContactNames(qso);
                                        if (qso.Status == QSOStatus.ValidQSO)
                                        {
                                            cwOpenAnalyzer.FindCWOpenMatchingQsos(qso);
                                        }
                                        break;
                                    case ContestName.HQP:
                                        hpqAnalyzer.MarkIncorrectContactEntities(qso);
                                        if (qso.Status == QSOStatus.ValidQSO) {
                                            hpqAnalyzer.FindHQPMatchingQsos(qso);
                                        }
                                        break;
                                }
                            }
                        }

                        validQsoCount = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO).Count();

#if DEBUG
                        if (contestLog.LogOwner == "AH6KO")
                        {
                          var  invalidQsos = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.InvalidQSO).ToList();
                        }
#endif

                        // ReportProgress with Callsign
                        OnProgressUpdate?.Invoke(call, contestLog.QSOCollection.Count.ToString(), validQsoCount.ToString(), progress);
                    }
                }

            }
            catch (Exception ex)
            {
                OnProgressUpdate?.Invoke("Error" + ex.Message, "", "", 0);
            }
        }

        /// <summary>
        /// See if the call is in the Good/Bad call list.
        /// These are calls known by experience to get busted.
        /// </summary>
        /// <param name="qso"></param>
        /// <returns></returns>
        internal bool CheckBadCallList(QSO qso)
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
                    qso.ReasonRejected = RejectReason.BustedCallSign;

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Mark all QSOs where the operator call sign doesn't match the log call sign as invalid.
        /// This is very rare.
        /// </summary>
        /// <param name="qsoList"></param>
        /// <param name="call"></param>
        private void MarkIncorrectOperatorCallSigns(List<QSO> qsoList, string call)
        {
            List<QSO> qsos = qsoList.Where(q => q.OperatorCall != call && q.Status == QSOStatus.ValidQSO).ToList();

            if (qsos.Any())
            {
                Console.WriteLine("Incorrect operator call found:");
                qsos.Select(c => { c.IsIncorrectOperatorCall = false; return c; }).ToList();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="qso"></param>
        /// <returns></returns>
        internal double DetermineBandFault(QSO qso)
        {
            ContestLog contestLog = qso.ParentLog;
            string frequency = qso.Frequency;
            double qsoPoints = 0;

            // bonus point for having rig control frequency
            if (UsesRigControl(frequency))
            {
                qsoPoints += 1;
                // if the log shows he was only on one band & rig control he wins
                if (contestLog.IsSingleBand)
                {
                    return 99.0;
                }
            }
            else
            {
                qsoPoints -= 1;
            }

            qsoPoints += cwOpenAnalyzer.InspectPreviousQSOsForFrequency(qso, contestLog, frequency);
            qsoPoints += cwOpenAnalyzer.InspectNextQSOsForFrequency(qso, contestLog, frequency);

            return qsoPoints;
        }

        /// <summary>
        /// Determine if the operator used rig control.
        /// </summary>
        /// <param name="frequency">string</param>
        /// <returns>bool</returns>
        internal bool UsesRigControl(string frequency)
        {
            string[] defaults = new string[] { "1800", "3500", "7000", "14000", "21000", "28000" };

            if (defaults.Contains(frequency))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// for each contact call see if the first letter matches, then the second etc.
        /// until letters don't match
        /// </summary>
        /// <param name="qso"></param>
        /// <param name="matches"></param>
        internal void DetermineBustedCallFault(QSO qso, List<QSO> matches)
        {
            string operatorCall = qso.OperatorCall;
            bool matched = false;

            foreach (QSO matchQSO in matches)
            {
                int score = LevenshteinDistance.Compute(operatorCall, matchQSO.ContactCall);
                if (score == 1)
                {
                    matched = true;

                    qso.MatchingQSO = matchQSO;
                    matchQSO.MatchingQSO = qso;

                    qso.HasBeenMatched = true;
                    matchQSO.HasBeenMatched = true;

                    matchQSO.CallIsBusted = true;
                    matchQSO.IncorrectValueMessage = $"{matchQSO.ContactCall} --> {operatorCall}";
                    break;
                }
            }

            if (matched == false)
            {
                qso.HasBeenMatched = true;
                qso.CallIsBusted = true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="qso"></param>
        internal void LastChanceMatch(QSO qso)
        {
            List<QSO> matches = new List<QSO>();
            IEnumerable<ContestLog> contestLogs;
            IEnumerable<KeyValuePair<string, List<QSO>>> qsos;
            IEnumerable<QSO> qsosFlattened = null;
            int queryLevel = 6;

            if (ContestLogList.Where(b => b.LogOwner == qso.ContactCall).Count() != 0)
            {
                contestLogs = ContestLogList.Where(b => b.LogOwner == qso.ContactCall);

                if (contestLogs.Count() > 0)
                {
                    qsos = contestLogs.SelectMany(z => z.QSODictionary).Where(x => x.Key != qso.ContactCall);
                    qsosFlattened = qsos.SelectMany(x => x.Value);
                    if (ActiveContest == ContestName.CW_OPEN)
                    {
                        matches = cwOpenAnalyzer.RefineCWOpenMatch(qsosFlattened, qso, queryLevel).ToList();
                    } else
                    {
                        matches = hpqAnalyzer.RefineHQPMatch(qsosFlattened, qso, queryLevel).ToList();
                    }
                   
                    switch (matches.Count)
                    {
                        case 0:
                            //var q = 1;
                            break;
                        default:
                            DetermineBustedCallFault(qso, matches);
                            break;
                    }
                }
            }
        }

        internal List<QSO> FullParameterSearch(IEnumerable<QSO> qsos, QSO qso)
        {
            IEnumerable<QSO> matches = null;
            List<QSO> matchList;
            int queryLevel = 1;
            //int count = 0;

            switch (ActiveContest)
            {
                case ContestName.CW_OPEN:
                    matches = cwOpenAnalyzer.RefineCWOpenMatch(qsos, qso, queryLevel);
                    break;
                case ContestName.HQP:
                    matches = hpqAnalyzer.RefineHQPMatch(qsos, qso, queryLevel);
                    break;
            }

            matchList = matches.ToList();

            switch (matchList.Count)
            {
                case 0:
                    switch (ActiveContest)
                    {
                        case ContestName.CW_OPEN:
                            // match not found so lets search without serial number
                            matchList = cwOpenAnalyzer.SearchWithoutSerialNumber(qsos, qso);
                            break;
                        default:
                            // match not found so lets widen the search
                            matchList = hpqAnalyzer.SearchWithoutEntity(qsos, qso, false);
                            break;
                    }
                           
                    return matchList;
                case 1:
                    // found one match so we mark both as matches and add matching QSO
                    qso.MatchingQSO = matchList[0];
                    matchList[0].MatchingQSO = qso;

                    qso.HasBeenMatched = true;
                    matchList[0].HasBeenMatched = true;
                    return matchList;
                default:
                    //Console.WriteLine("Dupe list - 1:" + qso.OperatorCall);
                    // more than one so these are probably dupes
                    if (qso.Status == QSOStatus.ValidQSO)
                    {
                        qso.HasBeenMatched = true;
                        qso.MatchingQSO = matchList[0];
                       // qso.QSOHasDupes = true;

                        matchList[0].HasBeenMatched = true;
                        matchList[0].MatchingQSO = qso;

                       // Console.WriteLine("Match - 1: " + matchList[0].ContactCall + " " + matchList[0].Band + " " + matchList[0].Mode + " " + matchList[0].QsoID);

                        //foreach (QSO dupe in matchList.Skip(1))
                        //{
                        //    count++;
                        //    Console.WriteLine("Dupe - 1: " + dupe.ContactCall + " " + dupe.Band + " " + dupe.Mode + " " + dupe.QsoID);
                        //    dupe.HasBeenMatched = true;
                        //    dupe.MatchingQSO = qso;
                        //    dupe.FirstMatchingQSO = matchList[0];
                        //    dupe.IsDuplicateMatch = true;
                        //}
                    }

                   // Console.WriteLine("Number of dupes-1: " + count.ToString());

                    return matchList;
            }
        }

        /// <summary>
        /// Remove the band component. Sometimes one of them changes band
        /// but the band is recorded wrong. Especially if not using rig control.
        /// </summary>
        /// <param name="qsos"></param>
        /// <param name="qso"></param>
        /// <returns></returns>
        internal List<QSO> SearchWithoutBand(IEnumerable<QSO> qsos, QSO qso)
        {
            List<QSO> matches;
            int queryLevel = 4;
            double qsoPoints;
            double matchQsoPoints;

            IEnumerable<QSO> enumerable;
            switch (ActiveContest)
            {
                case ContestName.CW_OPEN:
                    enumerable = cwOpenAnalyzer.RefineCWOpenMatch(qsos, qso, queryLevel);
                    break;
                default:
                    enumerable = hpqAnalyzer.RefineHQPMatch(qsos, qso, queryLevel);
                    break;
            }

            matches = enumerable.ToList();

            // band mismatch
            switch (matches.Count)
            {
                case 0:
                    switch (ActiveContest)
                    {
                        case ContestName.CW_OPEN:
                            // search without time now
                            matches = cwOpenAnalyzer.RefineCWOpenMatch(qsos, qso, 5).ToList();
                            break;
                        case ContestName.HQP:
                            matches = hpqAnalyzer.SearchWithoutModeHQP(qsos, qso);
                            break;
                    }

                    return matches;
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
                        return matches;
                    }

                    if (qsoPoints > matchQsoPoints)
                    {
                        matches[0].IsIncorrectBand = true;
                        matches[0].IncorrectValueMessage = $"{matches[0].Band} --> {qso.Band}";
                    }
                    else
                    {
                        qso.IsIncorrectBand = true;
                        qso.IncorrectValueMessage = $"{qso} --> {matches[0].Band}";
                    }
                    return matches;
                default:
                    // duplicate incorrect QSOs
                    foreach (QSO matchQSO in matches)
                    {
                        if (qso.HasBeenMatched == false)
                        {
                            qso.MatchingQSO = matchQSO;
                            qso.HasBeenMatched = true;
                        }

                        if (matchQSO.HasBeenMatched == false)
                        {
                            // whos at fault? need to get qsos around contact for each guy
                            qsoPoints = DetermineBandFault(qso);
                            matchQsoPoints = DetermineBandFault(matchQSO);

                            if (qsoPoints.Equals(matchQsoPoints))
                            {
                                // can't tell who's at fault so let them both have point
                                return matches;
                            }

                            if (qsoPoints > matchQsoPoints)
                            {
                                matchQSO.IsIncorrectBand = true;
                                matchQSO.IncorrectValueMessage = $"{matchQSO.Band} --> {qso.Band}";
                            }
                            else
                            {
                                qso.IsIncorrectBand = true;
                                qso.IncorrectValueMessage = $"{qso} --> {matchQSO.Band}";
                            }
                        }
                    }
                    return matches;
            }
        }

        #endregion

    } // end class
}
