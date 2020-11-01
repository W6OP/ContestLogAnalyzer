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
                                MarkIncorrectSentEntity(qsoList);
                                break;
                        }

                        foreach (QSO qso in qsoList)
                        {
                            if (qso.Status == QSOStatus.ValidQSO)
                            {
                                if (ActiveContest == ContestName.HQP)
                                {
                                    MarkIncorrectContactEntities(qso);
                                    FindHQPMatchingQsos(qso);
                                }
                                else
                                {
                                    MarkIncorrectContactNames(qso);
                                    FindCWOpenMatchingQsos(qso);
                                }
                            }
                        }

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
        /// See if the call is in the Good/Bad call list.
        /// These are calls known by experience to get busted.
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
                qsos.Select(c => { c.IncorrectOperatorCall = false; return c; }).ToList();
            }
        }

        #endregion

        #region HQP Only Code

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
        private void FindHQPMatchingQsos(QSO qso)
        {
            IEnumerable<QSO> enumerable;
            List<QSO> matches;
            IEnumerable<ContestLog> contestLogs;
            IEnumerable<KeyValuePair<string, List<QSO>>> qsos;

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
                switch (CheckBadCallList(qso))
                {
                    case false:
                        qso.IsUniqueCall = true;
                        qso.HasBeenMatched = true;
                        break;
                    default:
                        qso.CallIsBusted = true;
                        qso.HasBeenMatched = true;
                        break;
                }
                //Console.WriteLine("FindHQPMatchingQsos - 1 : " + qso.OperatorCall);
                return;
            }

            // if there is only one log this call is in then it is unique
            // look for partial match
            // get 
            if (CallDictionary[qso.ContactCall].Count == 1)
            {
                switch (CheckBadCallList(qso))
                {
                    case false:
                        qso.IsUniqueCall = true;
                        qso.HasBeenMatched = true;
                        break;
                    default:
                        qso.HasBeenMatched = true;
                        qso.CallIsBusted = true;
                        break;
                }
                //Console.WriteLine("FindHQPMatchingQsos - 2 : " + qso.ContactCall);
                return;
            }

            // there is no matching log for this call so we give him the qso
            // maybe should see if anyone else worked him?
            if (ContestLogList.Where(b => b.LogOwner == qso.ContactCall).Count() == 0)
            {
                qso.NoMatchingLog = true;
                qso.HasBeenMatched = true;
                //Console.WriteLine("FindHQPMatchingQsos - 3 : " + qso.ContactCall);
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
            // do these parameters up front and I don't have to do them for every subsequent query
            IEnumerable<QSO> qsosFlattened = qsos.SelectMany(x => x.Value).Where(y => y.ContactCall == qso.OperatorCall && y.OperatorCall == qso.ContactCall);

            // Start the search process
            enumerable = HQPFullParameterSearch(qsosFlattened, qso);

            // if the qso has been matched at any previous level we don't need to continue
            if (qso.HasBeenMatched)
            {
                return;
            }

            matches = enumerable.ToList();

            // ok, no hits yet so lets do some more checking and see if we find something with a different call
            switch (matches.Count) // && qso.ContactCall == "AH6SZ"
            {
                case 0:
                    LastChanceMatch(qso);
                    return;
                case 1:
                    // for now don't penalize for time mismatch since everything else is correct
                    qso.MatchingQSO = matches[0];
                    matches[0].MatchingQSO = qso;

                    qso.HasBeenMatched = true;
                    matches[0].HasBeenMatched = true;
                    return;
                default:
                    // duplicate incorrect QSOs - could this ever happen this deeply into the search?
                    Console.WriteLine("FindHQPMatchingQsos: 7 " + matches.Count.ToString());
                    break;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="qso"></param>
        private void LastChanceMatch(QSO qso)
        {
            List<QSO> matches = new List<QSO>();
            IEnumerable<ContestLog> contestLogs;
            IEnumerable<KeyValuePair<string, List<QSO>>> qsos;
            IEnumerable<QSO> qsosFlattened = null;
            int timeInterval = 5;
            int queryLevel = 6;
           
            if (ContestLogList.Where(b => b.LogOwner == qso.ContactCall).Count() != 0)
            {
                contestLogs = ContestLogList.Where(b => b.LogOwner == qso.ContactCall);

                if (contestLogs.Count() > 0)
                {
                    qsos = contestLogs.SelectMany(z => z.QSODictionary).Where(x => x.Key != qso.ContactCall);
                    qsosFlattened = qsos.SelectMany(x => x.Value);
                    matches = RefineHQPMatch(qsosFlattened, qso, timeInterval, queryLevel).ToList(); //SearchForBustedCall(qsosFlattened, qso).ToList();

                    if (matches.Count == 1)
                    {
                        if (matches[0].HasBeenMatched)
                        {
                            DetermineBustedCallFault(qso, matches);
                            return;
                        }
                        // see if the contact call is unique, if so it's the busted call
                        if (!CallDictionary.ContainsKey(matches[0].ContactCall))
                        {
                            DetermineBustedCallFault(qso, matches);
                        }
                        else
                        {
                            DetermineBustedCallFault(qso, matches);
                        }
                    }
                    else
                    {
                        if (matches.Count > 1)
                        {
                            // this sometimes marks the wrong person - if busted call and busted entity
                            // for each contact call see if the first letter matches, then the second etc
                            // until letters don't match
                            DetermineBustedCallFault(qso, matches);
                        } else {
                            var q = 1;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// for each contact call see if the first letter matches, then the second etc.
        /// until letters don't match
        /// </summary>
        /// <param name="qso"></param>
        /// <param name="matches"></param>
        private void DetermineBustedCallFault(QSO qso, List<QSO> matches)
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
                    matchQSO.IncorrectValue = $"{matchQSO.ContactCall} --> {operatorCall}";
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
        /// Search using all parameters. A hit here is exact, though there
        /// could be dupes.
        /// </summary>
        /// <param name="qsos"></param>
        /// <param name="qso"></param>
        /// <returns></returns>
        private List<QSO> HQPFullParameterSearch(IEnumerable<QSO> qsos, QSO qso)
        {
            IEnumerable<QSO> enumerable;
            List<QSO> matches;
            int timeInterval = 10; // because everything else must match
            int queryLevel = 1;

            // full search
            if (EnumHelper.GetDescription(qso.Mode) != "RY")
            {
                enumerable = RefineHQPMatch(qsos, qso, timeInterval, queryLevel);
            }
            else
            {
                timeInterval = 15;
                enumerable = RefineHQPMatch(qsos, qso, timeInterval, queryLevel);
            }

            matches = enumerable.ToList();

            switch (matches.Count)
            {
                case 0:
                    // match not found so lets widen the search
                    matches = SearchWithoutContactEntity(qsos, qso);
                    return matches;
                case 1:
                    // found one match so we mark both as matches and add matching QSO
                    qso.MatchingQSO = matches[0];
                    matches[0].MatchingQSO = qso;

                    qso.HasBeenMatched = true;
                    matches[0].HasBeenMatched = true;
                    return matches;
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
                    return matches;
                default:
                    // more than two so we have to do more analysis
                    Console.WriteLine("FindHQPMatches: 1");
                    return matches;
            }
        }

        /// <summary>
        /// Check to see if the Contact Entity is incorrect.
        /// </summary>
        /// <param name="qsos"></param>
        /// <param name="qso"></param>
        /// <returns></returns>
        private List<QSO> SearchWithoutContactEntity(IEnumerable<QSO> qsos, QSO qso)
        {
            IEnumerable<QSO> enumerable;
            List<QSO> matches;
            int timeInterval = 10;
            int queryLevel = 2;

            enumerable = RefineHQPMatch(qsos, qso, timeInterval, queryLevel);

            matches = enumerable.ToList();

            // this is hit only if previous switch hit case 0: so we up the time interval
            switch (matches.Count)
            {
                case 0:
                    matches = SearchWithoutOperatorEntity(qsos, qso);
                    return matches;
                case 1:
                    qso.HasBeenMatched = true;
                    qso.MatchingQSO = matches[0];

                    if (matches[0].Status == QSOStatus.ValidQSO)
                    {
                        matches[0].MatchingQSO = qso;
                        matches[0].HasBeenMatched = true;
                        matches[0].IncorrectDXEntity = $"{matches[0].ContactEntity} --> {qso.OperatorEntity}";
                        matches[0].InvalidEntity = true;
                    }
                    return matches;
                case 2:
                    qso.HasBeenMatched = true;
                    qso.MatchingQSO = matches[0];

                    if (matches[0].Status == QSOStatus.ValidQSO)
                    {
                        matches[0].MatchingQSO = qso;
                        matches[0].HasBeenMatched = true;
                        matches[0].IncorrectDXEntity = $"{matches[0].ContactEntity} --> {qso.OperatorEntity}";
                        matches[0].InvalidEntity = true;
                    }

                    if (matches[1].Status == QSOStatus.ValidQSO)
                    {
                        matches[1].MatchingQSO = qso;
                        matches[1].HasBeenMatched = true;
                        matches[1].IncorrectDXEntity = $"{matches[1].ContactEntity} --> {qso.OperatorEntity}";
                        matches[1].InvalidEntity = true;
                    }
                    return matches;
                default:
                    Console.WriteLine("FindHQPMatches: 2");
                    return matches;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="qsos"></param>
        /// <param name="qso"></param>
        /// <returns></returns>
        private List<QSO> SearchWithoutOperatorEntity(IEnumerable<QSO> qsos, QSO qso)
        {
            IEnumerable<QSO> enumerable;
            List<QSO> matches;
            int timeInterval = 10;
            int queryLevel = 3;

            enumerable = RefineHQPMatch(qsos, qso, timeInterval, queryLevel);

            // don't ToList() unless something returned
            matches = enumerable.ToList();

            // this is hit only if previous switch hit case 0: so we up the time interval
            switch (matches.Count)
            {
                case 0:
                    matches = SearchWithoutBandHQP(qsos, qso);
                    return matches;
                case 1:
                    qso.HasBeenMatched = true;
                    qso.MatchingQSO = matches[0];

                    if (matches[0].Status == QSOStatus.ValidQSO)
                    {
                        matches[0].MatchingQSO = qso;
                        matches[0].HasBeenMatched = true;
                        matches[0].IncorrectDXEntity = $"{matches[0].OperatorEntity} --> {qso.ContactEntity}";
                        matches[0].InvalidEntity = true;
                    }
                    return matches;
                case 2:
                    // two matches so these are probably dupes
                    qso.HasBeenMatched = true;
                    qso.MatchingQSO = matches[0];

                    if (matches[0].Status == QSOStatus.ValidQSO)
                    {
                        matches[0].HasBeenMatched = true;
                        matches[0].MatchingQSO = qso;
                        matches[0].IncorrectDXEntity = $"{matches[0].OperatorEntity} --> {qso.ContactEntity}";
                        matches[0].InvalidEntity = true;
                    }

                    if (matches[1].Status == QSOStatus.ValidQSO)
                    {
                        matches[1].HasBeenMatched = true;
                        matches[1].MatchingQSO = qso;
                        matches[1].IncorrectDXEntity = $"{matches[1].OperatorEntity} --> {qso.ContactEntity}";
                        matches[1].InvalidEntity = true;
                    }
                    return matches;
                default:
                    Console.WriteLine("FindHQPMatches: 3");
                    return matches;
            }
        }

        private List<QSO> SearchWithoutBandHQP(IEnumerable<QSO> qsos, QSO qso)
        {
            IEnumerable<QSO> enumerable;
            List<QSO> matches;
            int timeInterval = 5;
            int queryLevel = 4;
            double qsoPoints;
            double matchQsoPoints;

            enumerable = RefineHQPMatch(qsos, qso, timeInterval, queryLevel);

            matches = enumerable.ToList();

            // band mismatch
            switch (matches.Count)
            {
                case 0:
                    matches = SearchWithoutModeHQP(qsos, qso);
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
                        matches[0].IncorrectBand = true;
                        matches[0].IncorrectValue = $"{matches[0].Band} --> {qso.Band}";
                    }
                    else
                    {
                        qso.IncorrectBand = true;
                        qso.IncorrectValue = $"{qso.Band} --> {matches[0].Band}";
                    }
                    return matches;
                default:
                    // duplicate incorrect band QSOs
                    foreach (QSO matchQSO in matches)
                    {
                        if (matchQSO.HasBeenMatched == false)
                        {
                            // should this be a collection?
                            qso.MatchingQSO = matchQSO;
                            qso.HasBeenMatched = true;

                            matchQSO.MatchingQSO = qso;
                            matchQSO.HasBeenMatched = true;

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
                                matchQSO.IncorrectBand = true;
                                matchQSO.IncorrectValue = $"{matchQSO.Band} --> {qso.Band}";

                            }
                            else
                            {
                                qso.IncorrectBand = true;
                                qso.IncorrectValue = $"{qso.Band} --> {matchQSO.Band}";
                            }
                        }
                    }
                    return matches;
            }
        }

        private List<QSO> SearchWithoutModeHQP(IEnumerable<QSO> qsos, QSO qso)
        {
            IEnumerable<QSO> enumerable;
            List<QSO> matches;
            int timeInterval = 5;
            int queryLevel = 5;
            double qsoPoints;
            double matchQsoPoints;

            enumerable = RefineHQPMatch(qsos, qso, timeInterval, queryLevel);

            // don't ToList() unless something returned
            matches = enumerable.ToList();

            // this is hit only if previous switch hit case 0: so we up the time interval
            switch (matches.Count)
            {
                case 0:
                    return matches;
                case 1:
                    qso.MatchingQSO = matches[0];
                    matches[0].MatchingQSO = qso;

                    qso.HasBeenMatched = true;
                    matches[0].HasBeenMatched = true;

                    // whos at fault? need to get qsos around contact for each guy
                    qsoPoints = DetermineModeFault(qso);
                    matchQsoPoints = DetermineModeFault(matches[0]);

                    if (qsoPoints.Equals(matchQsoPoints))
                    {
                        // can't tell who's at fault so let them both have point
                        return matches;
                    }

                    if (qsoPoints > matchQsoPoints)
                    {
                        matches[0].IncorrectMode = true;
                        matches[0].IncorrectValue = $"{matches[0].Mode} --> {qso.Mode}";
                    }
                    else
                    {
                        qso.IncorrectMode = true;
                        qso.IncorrectValue = $"{qso.Mode} --> {matches[0].Mode}";
                    }
                    return matches;
                default:
                    // duplicate incorrect mode QSOs
                    foreach (QSO matchQSO in matches)
                    {
                        if (matchQSO.HasBeenMatched == false)
                        {
                            // should this be a collection?
                            qso.MatchingQSO = matchQSO;
                            qso.HasBeenMatched = true;

                            matchQSO.MatchingQSO = qso;
                            matchQSO.HasBeenMatched = true;

                            // whos at fault? need to get qsos around contact for each guy
                            qsoPoints = DetermineModeFault(qso);
                            matchQsoPoints = DetermineModeFault(matchQSO);

                            if (qsoPoints.Equals(matchQsoPoints))
                            {
                                // can't tell who's at fault so let them both have point
                                return matches;
                            }

                            if (qsoPoints > matchQsoPoints)
                            {
                                matchQSO.IncorrectMode = true;
                                matchQSO.IncorrectValue = $"{matchQSO.Mode} --> {qso.Mode}";

                            }
                            else
                            {
                                qso.IncorrectMode = true;
                                qso.IncorrectValue = $"{qso.Mode} --> {matchQSO.Mode}";
                            }
                        }
                    }
                    return matches;
            }
        }

        /// <summary>
        /// // this can have more entries than qsos because the list is flattened
        /// </summary>
        /// <param name="contestLogs"></param>
        /// <param name="qso"></param>
        /// <param name="timeInterval"></param>
        /// <returns></returns>
        private IEnumerable<QSO> RefineHQPMatch(IEnumerable<QSO> qsos, QSO qso, int timeInterval, int queryLevel)
        {
            IEnumerable<QSO> matches;

            switch (queryLevel)
            {
                case 1:
                    // full search - QSO: 14038 CW 2020-08-22 0430 KH6EU 599 KON KH6TU 599 MAU
                    //               QSO: 14038 CW 2020-08-22 0430 KH6TU 599 MAU KH6EU 599 KON 
                    matches = qsos
                    .Where(y => y.Band == qso.Band
                                && y.Mode == qso.Mode
                                && y.ContactEntity == qso.OperatorEntity
                                && y.OperatorEntity == qso.ContactEntity
                                && Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= timeInterval);
                    return matches;
                case 2:
                    // without y.ContactEntity == qso.OperatorEntity - 
                    // QSO: 14034 CW 2020-08-22 1732 KH6TU 599 MAU W7JMM 599 OR  
                    // QSO: 21000 CW 2020-08-22 1732 W7JMM 599 OR WH6TU 599 MAU
                    matches = qsos
                    .Where(y => y.Band == qso.Band
                                && y.Mode == qso.Mode
                                && y.OperatorEntity == qso.ContactEntity
                                && Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= timeInterval);
                    return matches;
                case 3:
                    // without y.OperatorCall == qso.ContactCall -
                    //  QSO: 14034 CW 2020-08-22 1732 KH6TU 599 MAU W7JMM 599 OR  
                    //  QSO: 21000 CW 2020-08-22 1732 W7JMM 599 OR WH6TU 599 MAU
                    matches = qsos
                    .Where(y => y.Band == qso.Band
                                && y.Mode == qso.Mode
                                && y.ContactEntity == qso.OperatorEntity
                                && Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= timeInterval);
                    return matches;
                case 4:
                    // without y.Band == qso.Band -
                    //  QSO: 14034 CW 2020-08-22 1732 KH6TU 599 MAU W7JMM 599 OR  
                    //  QSO: 21000 CW 2020-08-22 1732 W7JMM 599 OR WH6TU 599 MAU
                    matches = qsos
                    .Where(y => y.Mode == qso.Mode
                                && y.ContactEntity == qso.OperatorEntity
                                && y.OperatorEntity == qso.ContactEntity
                                && Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= timeInterval);
                    return matches;
                case 5:
                    // general search
                    matches = qsos
                    .Where(y => y.Band == qso.Band
                                && y.ContactEntity == qso.OperatorEntity
                                && y.OperatorEntity == qso.ContactEntity
                                && Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= timeInterval);
                    return matches;
                case 6:
                    // this is a full search only with mismatching contact and operator calls
                    matches = qsos
                   .Where(y => y.Band == qso.Band
                               && y.Mode == qso.Mode
                               && y.ContactEntity == qso.OperatorEntity
                               && y.OperatorEntity == qso.ContactEntity
                               && Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= timeInterval);

                    switch(matches.ToList().Count)
                    {
                        case 0:
                            matches = RefineHQPMatch(qsos, qso, timeInterval, 7);
                            break;
                        case 1:
                            return matches;
                        default:
                            matches = RefineHQPMatch(qsos, qso, timeInterval, 8);
                            break;
                    }
                   
                    return matches;
                case 7:
                    // this is a full search only with mismatching contact and operator calls
                    matches = qsos
                   .Where(y => y.Band == qso.Band
                               && y.Mode == qso.Mode
                               && (y.ContactEntity == qso.OperatorEntity
                               || y.OperatorEntity == qso.ContactEntity)
                               && Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= timeInterval);
                    return matches;
                case 8:
                    // this is a full search only with mismatching entities
                    matches = qsos
                   .Where(y => y.Band == qso.Band
                               && y.Mode == qso.Mode
                               && Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= timeInterval);
                    return matches;
                default:
                    Console.WriteLine("Failed search: " + qso.RawQSO);
                    return new List<QSO>();
            }
        }

        /// <summary>
        /// HQP only
        /// Is this a Hawai'in station then, state, province or DX are valid.
        /// Non Hawaiin station then
        /// if US or Canadian - Hawwai'in entity
        /// DX then Hawai'in entity
        /// </summary>
        /// <param name="qso"></param>
        /// <param name="contestLogList"></param>
        /// <returns></returns>
        private void MarkIncorrectContactEntities(QSO qso)
        {
            int matchCount = 0;
            List<QSO> matchingQSOSpecific = null;
            List<QSO> matchingQSOsGeneral = null;
            string entity = null;

            // using a CallDictionary speeds up from 28 sec to 12 sec
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
                   .Take(1).FirstOrDefault().Where(q => q.InvalidEntity == false).ToList();

                List<QSO> listWithLeastEntries = (new List<List<QSO>> { matchingQSOSpecific, matchingQSOsGeneral })
                   .OrderBy(x => x.Count())
                   .Take(1).FirstOrDefault().Where(q => q.InvalidEntity == false).ToList();

                // we have enough to vote
                if (listWithMostEntries.Count() > 2)
                {
                    // we want to take the list with the most entries and remove the entries in the least entries list
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
                                    entity = hitList[0].Province;
                                }
                                else if (Provinces.Contains(qso.ContactEntity))
                                {
                                    entity = hitList[0].Province;
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
                qso.InvalidEntity = true;
            }
        }

        /// <summary>
        /// Mark all QSOs that don't have the correct operator entity on one or more of their QSOs 
        /// entity set as invalid.
        /// This rarely finds that the operator made a mistake
        /// Finds where the entity is not consistent among all the QSOs made with that operator.
        /// </summary>
        /// <param name="qsoList"></param>
        /// <param name="entity"></param>
        private void MarkIncorrectSentEntity(List<QSO> qsoList)
        {
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
                        qsoList[0].InvalidSentEntity = true;
                        return;
                }
            }

            // find the operator entity used on the majority of qsos
            var mostUsedOperatorEntity = qsoList
                .GroupBy(q => q.OperatorEntity)
                .OrderByDescending(gp => gp.Count())
                .Take(5)
                .Select(g => g.Key).ToList();

            List<QSO> qsos = qsoList.Where(q => q.OperatorEntity != mostUsedOperatorEntity[0] && q.Status == QSOStatus.ValidQSO).ToList();

            if (qsos.Any())
            {
                _ = qsos.Select(c => { c.InvalidSentEntity = true; return c; })
                        .ToList();
            }
            else
            {
                // need to check every entry?
            }
        }

        private double DetermineModeFault(QSO qso)
        {
            ContestLog contestLog = qso.ParentLog;
            QSO previousQSO = null;
            QSO nextQSO = null;
            string frequency = qso.Frequency;
            double qsoPoints = 0;
            double counter = 0;

            // bonus point for having rig control frequency
            if (frequency != "1800" && frequency != "3500" && frequency != "7000" && frequency != "14000" && frequency != "21000" && frequency != "28000")
            {
                qsoPoints += 1;
                // if the log shows he was only on one band & rig control he wins
                if (contestLog.IsSingleMode)
                {
                    return 99.0;
                }
            }
            else
            {
                qsoPoints -= 1;
            }

            while (counter < 10)
            {
                counter += 1;

                int index = contestLog.QSOCollection.IndexOf(qso);

                if (index > 0)
                {
                    previousQSO = GetPrevious(contestLog.QSOCollection, qso);
                }

                if (previousQSO != null)
                {
                    if (previousQSO.IncorrectMode == true)
                    {
                        qsoPoints -= 1;
                    }

                    frequency = previousQSO.Frequency;

                    if (frequency != "1800" && frequency != "3500" && frequency != "7000" && frequency != "14000" && frequency != "21000" && frequency != "28000")
                    {
                        qsoPoints += 1;
                    }
                    else
                    {
                        qsoPoints -= 1;
                    }

                    if (previousQSO.Mode == qso.Mode)
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
                    // extra half point because he just ended and more likely to be correct
                    qsoPoints += 1.5;
                }
            }

            counter = 0;

            while (counter < 10)
            {
                counter += 1;

                int index = contestLog.QSOCollection.IndexOf(qso);
                if (index <= contestLog.QSOCollection.Count)
                {
                    nextQSO = GetNext(contestLog.QSOCollection, qso);
                }

                if (nextQSO != null)
                {
                    if (nextQSO.IncorrectBand == true)
                    {
                        qsoPoints -= 1;
                    }

                    frequency = nextQSO.Frequency;

                    if (frequency != "1800" && frequency != "3500" && frequency != "7000" && frequency != "14000" && frequency != "21000" && frequency != "28000")
                    {
                        qsoPoints += 1;
                    }
                    else
                    {
                        qsoPoints -= 1;
                    }

                    if (nextQSO.Mode == qso.Mode)
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
                    qsoPoints += 1.5;
                }
            }

            return qsoPoints;
        }


        #endregion

        #region CWOpen Only Code

        /// <summary>
        /// CWOpen
        /// See if the contact name is incorrect. Sometimes a person
        /// will send different names on different QSOS. Find out what
        /// the predominate name they used is.
        /// </summary>
        /// <param name="qso"></param>
        private void MarkIncorrectContactNames(QSO qso)
        {
            Tuple<string, int> majorityName = new Tuple<string, int>("", 1);

            // get list of name tuples
            var names = NameDictionary[qso.ContactCall];

            if (names.Count <= 1)
            { return; }
            else
            {
                majorityName = names.Aggregate((i1, i2) => i1.Item2 > i2.Item2 ? i1 : i2);
            }

            if (qso.ContactName == majorityName.Item1)
            {
                return;
            }

            qso.IncorrectValue = qso.ContactName + " --> " + majorityName.Item1;
            qso.IncorrectContactName = true;
        }

        /// <summary>
        /// Start trying to match up QSOS. Start with a search for a matching qso
        /// using all parameters. If that fails search by reducing parameters until 
        /// a match is found or the call is determined to be busted. Use IEnumerable
        /// for performance, .ToList() is expensive so don't use it unless something
        /// has been returned.
        /// </summary>
        /// <param name="qso"></param>
        private void FindCWOpenMatchingQsos(QSO qso)
        {
            IEnumerable<QSO> enumerable;
            List<QSO> matches;
            IEnumerable<ContestLog> contestLogs;
            IEnumerable<KeyValuePair<string, List<QSO>>> qsos;

            // it's already been flagged - no reason to evaluate
            if (qso.Status != QSOStatus.ValidQSO)
            {
                return;
            }

            // all logs with this operator call sign in them
            if (CallDictionary.ContainsKey(qso.OperatorCall))
            {
                contestLogs = CallDictionary[qso.OperatorCall];
            }
            else
            {
                Console.WriteLine("FindCWOpenMatchingQsos - 1 : " + qso.OperatorCall);
                // i.e. JF2IWL made 3 qsos and those three got in the CallDictionary
                // pointing at JF2IWL. However none of the 3 sent in a log so JF2IWL 
                // does not appear in the CallDictionary as there are no logs that
                // have a QSO for him so check the bad call list
                switch (CheckBadCallList(qso))
                {
                    case false:
                        qso.IsUniqueCall = true;
                        Console.WriteLine("FindCWOpenMatchingQsos - 2 : " + qso.OperatorCall);
                        break;
                    default:
                        qso.CallIsBusted = true;
                        Console.WriteLine("FindCWOpenMatchingQsos - 3 : " + qso.OperatorCall);
                        break;
                }
                return;
            }


            // if there is only one log this call is in then it is unique
            if (CallDictionary[qso.ContactCall].Count == 1)
            {
                Console.WriteLine("FindCWOpenMatchingQsos - 4 : " + qso.ContactCall);
                switch (CheckBadCallList(qso))
                {
                    case false:
                        qso.IsUniqueCall = true;
                        break;
                    default:
                        qso.CallIsBusted = true;
                        break;
                }
                return;
            }

            // there is no matching log for this call so we give him the qso
            // maybe should see if anyone else worked him?
            if (ContestLogList.Where(b => b.LogOwner == qso.ContactCall).Count() == 0)
            {
                Console.WriteLine("FindCWOpenMatchingQsos - 5 : " + qso.ContactCall);
                switch (CheckBadCallList(qso))
                {
                    case false:
                        qso.IsUniqueCall = true;
                        break;
                    default:
                        qso.CallIsBusted = true;
                        break;
                }
                qso.NoMatchingLog = true;
                return;
            }

            // no reason to process if it already has a match
            if (qso.HasBeenMatched)
            {
                return;
            }

            // list of key/value pairs from the list of contest logs that match this operator call sign
            qsos = contestLogs.SelectMany(z => z.QSODictionary).Where(x => x.Key == qso.OperatorCall);

            // this gets all the QSOs in a flattened list
            // do these parameters up front and I don't have to do them for every subsequent query
            IEnumerable<QSO> qsosFlattened = qsos.SelectMany(x => x.Value).Where(y => y.ContactCall == qso.OperatorCall && y.OperatorCall == qso.ContactCall);

            enumerable = CWOpenFullParameterSearch(qsosFlattened, qso);

            // if the qso has been matched at any previous level we don't need to continue
            if (qso.HasBeenMatched)
            {
                return;
            }

            matches = enumerable.ToList();

            switch (matches.Count)
            {
                case 0:
                    Console.WriteLine("FindCWOpenMatchingQsos - 6 : " + qso.ContactCall);
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
                    // duplicate incorrect QSOs - could this ever happen this deeply into the search?
                    break;
            }
        }

        /// <summary>
        /// Complete search looking for an exact match. If this is successful
        /// both QSOs are good.
        /// </summary>
        /// <param name="qsos"></param>
        /// <param name="qso"></param>
        /// <returns></returns>
        private List<QSO> CWOpenFullParameterSearch(IEnumerable<QSO> qsos, QSO qso)
        {
            IEnumerable<QSO> enumerable;
            List<QSO> matches;
            int timeInterval = 10; // because everything else must match
            int searchLevel = 1;

            enumerable = RefineCWOpenMatch(qsos, qso, timeInterval, searchLevel);

            matches = enumerable.ToList();

            switch (matches.Count)
            {
                case 0:
                    // match not found so lets search without serial number
                    matches = SearchWithoutSerialNumber(qsos, qso);
                    return matches;
                case 1:
                    // found one match so we mark both as matches and add matching QSO
                    qso.MatchingQSO = matches[0];
                    matches[0].MatchingQSO = qso;

                    qso.HasBeenMatched = true;
                    matches[0].HasBeenMatched = true;
                    return matches;
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
                    return matches;
                default:
                    // more than two so we have to do more analysis
                    Console.WriteLine("FindCWOpenMatches: 1");
                    return matches;
            }
        }

        /// <summary>
        /// Remove the serial number check. If we get a hit then one or both
        /// serial numbers are incorrect.
        /// </summary>
        /// <param name="qsos"></param>
        /// <param name="qso"></param>
        /// <returns></returns>
        private List<QSO> SearchWithoutSerialNumber(IEnumerable<QSO> qsos, QSO qso)
        {
            IEnumerable<QSO> enumerable;
            List<QSO> matches;
            int timeInterval = 10;
            int searchLevel = 2;

            enumerable = RefineCWOpenMatch(qsos, qso, timeInterval, searchLevel);

            matches = enumerable.ToList();

            switch (matches.Count)
            {
                case 0:
                    // search without name
                    matches = SearchWithoutNames(qsos, qso);
                    return matches;
                case 1:
                    qso.MatchingQSO = matches[0];
                    matches[0].MatchingQSO = qso;

                    qso.HasBeenMatched = true;
                    matches[0].HasBeenMatched = true;

                    // determine which serial number(s) are incorrect
                    // if already flagged for error don't change status
                    if (matches[0].Status != QSOStatus.InvalidQSO && matches[0].ReceivedSerialNumber != qso.SentSerialNumber)
                    {
                        matches[0].IncorrectSerialNumber = true;
                        matches[0].IncorrectValue = $"{matches[0].ReceivedSerialNumber} --> {qso.SentSerialNumber}";
                    }

                    if (qso.Status != QSOStatus.InvalidQSO && qso.ReceivedSerialNumber != matches[0].SentSerialNumber)
                    {
                        qso.IncorrectSerialNumber = true;
                        qso.IncorrectValue = $"{qso.ReceivedSerialNumber} --> {matches[0].SentSerialNumber}";
                    }
                    return matches;
                default:
                    // duplicate incorrect serial number QSOs
                    bool matchFound = false;

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
                            matchQSO.IncorrectValue += $"{matchQSO.ReceivedSerialNumber} --> {qso.SentSerialNumber}";
                        }

                        // we don't know the order they are evaluated so a later bad QSO could invalidate a good QSO
                        if (qso.Status != QSOStatus.InvalidQSO && qso.ReceivedSerialNumber != matchQSO.SentSerialNumber)
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
                    return matches;
            }
        }

        /// <summary>
        /// Search with the name component removed. Maybe one copied the name
        /// incorrectly.
        /// </summary>
        /// <param name="qsos"></param>
        /// <param name="qso"></param>
        /// <returns></returns>
        private List<QSO> SearchWithoutNames(IEnumerable<QSO> qsos, QSO qso)
        {
            IEnumerable<QSO> enumerable;
            List<QSO> matches;
            int timeInterval = 5;
            int searchLevel = 3;

            enumerable = RefineCWOpenMatch(qsos, qso, timeInterval, searchLevel); //.ToList();

            matches = enumerable.ToList();

            switch (matches.Count)
            {
                case 0:
                    // search without band
                    matches = SearchWithoutBandCWOpen(qsos, qso);
                    return matches;
                case 1:
                    qso.MatchingQSO = matches[0];
                    matches[0].MatchingQSO = qso;

                    qso.HasBeenMatched = true;
                    matches[0].HasBeenMatched = true;

                    if (qso.Status != QSOStatus.InvalidQSO)
                    {
                        qso.IncorrectContactName = true;
                        qso.IncorrectValue = $"{qso.ContactName} --> {matches[0].OperatorName}";
                    }
                    return matches;
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
                        }
                    }
                    Console.WriteLine("FindCWOpenMatches: 4");
                    return matches;
            }
        }

        /// <summary>
        /// Remove the band component. Sometimes one of them changes band
        /// but the band is recorded wrong. Especially if not using rig control.
        /// </summary>
        /// <param name="qsos"></param>
        /// <param name="qso"></param>
        /// <returns></returns>
        private List<QSO> SearchWithoutBandCWOpen(IEnumerable<QSO> qsos, QSO qso)
        {
            IEnumerable<QSO> enumerable;
            List<QSO> matches;
            int timeInterval = 5;
            int searchLevel = 5;
            double qsoPoints;
            double matchQsoPoints;

            enumerable = RefineCWOpenMatch(qsos, qso, timeInterval, searchLevel);

            matches = enumerable.ToList();

            // band mismatch
            switch (matches.Count)
            {
                case 0:
                    // search without time
                    matches = RefineCWOpenMatch(qsos, qso, 5, 5).ToList();
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
                        matches[0].IncorrectBand = true;
                        matches[0].IncorrectValue = $"{matches[0].Band} --> {qso.Band}";
                    }
                    else
                    {
                        qso.IncorrectBand = true;
                        qso.IncorrectValue = $"{qso} --> {matches[0].Band}";
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
                                matchQSO.IncorrectBand = true;
                                matchQSO.IncorrectValue = $"{matchQSO.Band} --> {qso.Band}";
                            }
                            else
                            {
                                qso.IncorrectBand = true;
                                qso.IncorrectValue = $"{qso} --> {matchQSO.Band}";
                            }
                        }
                    }
                    return matches;
            }
        }



        /// <summary>
        /// Do a search through the qsos collection depending on the parameters sent in.
        /// We already know the operator and contact calls match.
        /// </summary>
        /// <param name="contestLogs"></param>
        /// <param name="qso"></param>
        /// <param name="timeInterval"></param>
        /// <returns></returns>
        private IEnumerable<QSO> RefineCWOpenMatch(IEnumerable<QSO> qsos, QSO qso, int timeInterval, int queryLevel)
        {
            IEnumerable<QSO> matches;

            switch (queryLevel)
            {
                case 1: // exact match - does an entry in qsos match the qso 
                    matches = qsos
                   .Where(y => y.OperatorName == qso.ContactName &&
                   y.SentSerialNumber == qso.ReceivedSerialNumber &&
                   y.ReceivedSerialNumber == qso.SentSerialNumber &&
                   y.Band == qso.Band &&
                   Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= timeInterval);
                    return matches;
                case 2:
                    // catch incorrect serial number
                    matches = qsos
                    .Where(y => y.OperatorName == qso.ContactName &&
                    y.Band == qso.Band &&
                    Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= timeInterval);
                    return matches;
                case 3:
                    // incorrect name
                    matches = qsos
                    .Where(y => y.SentSerialNumber == qso.ReceivedSerialNumber &&
                    y.ReceivedSerialNumber == qso.SentSerialNumber &&
                    y.Band == qso.Band &&
                    Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= timeInterval);
                    return matches;
                case 4:
                    // incorrect band
                    matches = qsos
                   .Where(y => y.OperatorName == qso.ContactName &&
                   y.SentSerialNumber == qso.ReceivedSerialNumber &&
                   y.ReceivedSerialNumber == qso.SentSerialNumber &&
                   Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= timeInterval);
                    return matches;
                case 5:
                    // incorrect time
                    matches = qsos
                    .Where(y => y.OperatorName == qso.ContactName &&
                    y.SentSerialNumber == qso.ReceivedSerialNumber &&
                    y.ReceivedSerialNumber == qso.SentSerialNumber &&
                    y.Band == qso.Band);
                    return matches;
                default:
                    Console.WriteLine("Failed search: " + qso.RawQSO);
                    return new List<QSO>();

            }
        }

        /*
       I would simply accept ANY digital QSO where the software thinks there was a busted call. That may be the simplest solution. 
      Allowing FTx in the contest brings new challenges and hassles. However, at least for HQP I think it's a good thing overall.
       */

        private double DetermineBandFault(QSO qso)
        {
            ContestLog contestLog = qso.ParentLog;
            QSO previousQSO = null;
            QSO nextQSO = null;
            string frequency = qso.Frequency;
            double qsoPoints = 0;
            double counter = 0;

            // bonus point for having rig control frequency
            if (frequency != "1800" && frequency != "3500" && frequency != "7000" && frequency != "14000" && frequency != "21000" && frequency != "28000")
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

            while (counter < 10)
            {
                counter += 1;

                int index = contestLog.QSOCollection.IndexOf(qso);

                if (index > 0)
                {
                    previousQSO = GetPrevious(contestLog.QSOCollection, qso);
                }

                if (previousQSO != null)
                {
                    if (previousQSO.IncorrectBand == true)
                    {
                        qsoPoints -= 1;
                    }

                    frequency = previousQSO.Frequency;

                    if (frequency != "1800" && frequency != "3500" && frequency != "7000" && frequency != "14000" && frequency != "21000" && frequency != "28000")
                    {
                        qsoPoints += 1;
                    }
                    else
                    {
                        qsoPoints -= 1;
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
                    // extra half point because he just ended and more likely to be correct
                    qsoPoints += 1.5;
                }
            }

            counter = 0;

            while (counter < 10)
            {
                counter += 1;

                int index = contestLog.QSOCollection.IndexOf(qso);
                if (index <= contestLog.QSOCollection.Count)
                {
                    nextQSO = GetNext(contestLog.QSOCollection, qso);
                }

                if (nextQSO != null)
                {
                    if (nextQSO.IncorrectBand == true)
                    {
                        qsoPoints -= 1;
                    }

                    frequency = nextQSO.Frequency;

                    if (frequency != "1800" && frequency != "3500" && frequency != "7000" && frequency != "14000" && frequency != "21000" && frequency != "28000")
                    {
                        qsoPoints += 1;
                    }
                    else
                    {
                        qsoPoints -= 1;
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
                    qsoPoints += 1.5;
                }
            }

            return qsoPoints;
        }



        /// <summary>
        /// Mark all QSOs that don't have the correct name sent as invalid.
        /// This is very rare.
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

        #endregion

        #region Utility Code

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

        #endregion

    } // end class
}
