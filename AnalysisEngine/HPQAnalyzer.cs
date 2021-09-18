using System;
using System.Collections.Generic;
using System.Linq;
using W6OP.CallParser;

namespace W6OP.ContestLogAnalyzer
{
    internal class HPQAnalyzer
    {
        internal LogAnalyzer logAnalyzer;

        readonly string[] States = { "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "DC", "FL", "GA", "HI", "ID", "IL", "IN", "IA", "KS", "KY", "LA", "ME", "MD", "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ", "NM", "NY", "NC", "ND", "OH", "OK", "OR", "PA", "RI", "SC", "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY", };
        readonly string[] Provinces = { "NL", "NS", "PE", "NB", "QC", "ON", "MB", "SK", "AB", "BC", "YT", "NT", "NU" };

        private const int TimeInterval = 10;

        internal HPQAnalyzer(LogAnalyzer logAnalyzerMain)
        {
            logAnalyzer = logAnalyzerMain;
        }

        /// <summary>
        /// Mark all QSOs that don't have the correct operator entity on one or more of their QSOs 
        /// entity set as invalid.
        /// This rarely finds that the operator made a mistake
        /// Finds where the entity is not consistent among all the QSOs made with that operator.
        /// </summary>
        /// <param name="qsoList"></param>
        /// <param name="entity"></param>
        internal void MarkIncorrectSentEntity(List<QSO> qsoList)
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
                    case string _ when Enum.IsDefined(typeof(ALTHQPMults), qsoList[0].OperatorEntity):
                        return;
                    default:
                        qsoList[0].IsInvalidSentEntity = true;
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
                _ = qsos.Select(c => { c.IsInvalidSentEntity = true; return c; })
                        .ToList();
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
        internal void MarkIncorrectContactEntities(QSO qso)
        {
            int matchCount = 0;
            List<QSO> matchingQSOSpecific = null;
            List<QSO> matchingQSOsGeneral = null;
            string entity = null;

            // using a CallDictionary speeds up from 28 sec to 12 sec
            List<ContestLog> tempLog = logAnalyzer.CallDictionary[qso.ContactCall];

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
                   .Take(1).FirstOrDefault().Where(q => q.IsInvalidEntity == false).ToList();

                List<QSO> listWithLeastEntries = (new List<List<QSO>> { matchingQSOSpecific, matchingQSOsGeneral })
                   .OrderBy(x => x.Count())
                   .Take(1).FirstOrDefault().Where(q => q.IsInvalidEntity == false).ToList();

                // we have enough to vote
                if (listWithMostEntries.Count() > 2)
                {
                    // we want to take the list with the most entries and remove the entries in the least entries list
                    QSO firstMatch = listWithMostEntries.Except(listWithLeastEntries).FirstOrDefault();
                    entity = firstMatch.ContactEntity;
                }
                else
                { // two entries, either could be correct
                    IEnumerable<Hit> hitCollection = logAnalyzer.CallLookUp.LookUpCall(qso.ContactCall);
                    List<Hit> hitList = hitCollection.ToList();
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
                qso.IncorrectDXEntityMessage = $"{qso.ContactEntity} --> {entity}";
                qso.IsInvalidEntity = true;
            }
        }

        /// <summary>
        /// Find duplicate QSOs.
        /// Do a Select for call, mode and band.
        /// </summary>
        /// <param name="qsoList"></param>
        internal void MarkDuplicates(List<QSO> qsoList)
        {
            List<QSO> matchList;
            //int count = 0;

            //Console.WriteLine("Dupe list:" + qsoList[0].OperatorCall);
            foreach (QSO qso in qsoList)
            {
                matchList = qsoList
                      .Where(y => 
                      y.Status == QSOStatus.ValidQSO &&
                      y.Band == qso.Band && 
                      y.Mode == qso.Mode && 
                      y.ContactCall == qso.ContactCall).ToList();

                if (matchList.Count > 1)
                {
                    if (qso.Status == QSOStatus.ValidQSO)
                    {
                        qso.HasBeenMatched = true;
                        qso.MatchingQSO = matchList[0];
                        qso.QSOHasDupes = true;

                        matchList[0].HasBeenMatched = true;
                        matchList[0].MatchingQSO = qso;

                       // Console.WriteLine("Match: " + matchList[0].ContactCall + " " + matchList[0].Band + " " + matchList[0].Mode + " " + matchList[0].QsoID);

                        foreach (QSO dupe in matchList.Skip(1))
                        {
                            //count++;
                            //Console.WriteLine("Dupe: " + dupe.ContactCall + " " + dupe.Band + " " + dupe.Mode + " " + dupe.QsoID);
                            dupe.HasBeenMatched = true;
                            dupe.MatchingQSO = qso;
                            dupe.FirstMatchingQSO = matchList[0];
                            dupe.IsDuplicateMatch = true;
                        }
                    }
                }
            }
            //Console.WriteLine("Number of dupes: " + count.ToString());
            //var a = 1;
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
        internal void FindHQPMatchingQsos(QSO qso)
        {
            IEnumerable<ContestLog> contestLogs;

            // all logs with this operator call sign
            if (logAnalyzer.CallDictionary.ContainsKey(qso.OperatorCall))
            {
                contestLogs = logAnalyzer.CallDictionary[qso.OperatorCall];

                // if there is only one log this call is in then it is unique
                // look for partial match
                if (logAnalyzer.CallDictionary[qso.ContactCall].Count == 1)
                {
                    switch (logAnalyzer.CheckBadCallList(qso))
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
                    return;
                }
            }
            else
            {
                // i.e. JF2IWL made 3 qsos and those three got in the CallDictionary
                // pointing at JF2IWL. However none of the 3 sent in a log so JF2IWL 
                // does not appear in the CallDictionary as there are no logs that
                // have a QSO for him so consider his valid
                switch (logAnalyzer.CheckBadCallList(qso))
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
                return;
            }

            // there is no matching log for this call so we give him the qso
            if (logAnalyzer.ContestLogList.Where(b => b.LogOwner == qso.ContactCall).Count() == 0)
            {
                qso.NoMatchingLog = true;
                qso.HasBeenMatched = true;
                return;
            }

            // no reason to process if it already has a match
            if (qso.HasBeenMatched)
            {
                return;
            }

            // List of List<QSO> from the list of contest logs that match this operator call sign
            SelectQSOs(contestLogs, qso);
        }

        /// <summary>
        /// We have a list of all logs from the CallDictionary that have this call sign in it.
        /// Now we can query the QSODictionary for each QSO that may match.
        /// We can also flag some duplicates here.
        /// </summary>
        /// <param name="contestLogs"></param>
        /// <param name="qso"></param>
        private void SelectQSOs(IEnumerable<ContestLog> contestLogs, QSO qso)
        {
            IEnumerable<QSO> enumerable;
            List<QSO> matches;
            IEnumerable<KeyValuePair<string, List<QSO>>> qsos;

            qsos = contestLogs.SelectMany(z => z.QSODictionary).Where(x => x.Key == qso.OperatorCall);

            // this gets all the QSOs in a flattened list
            // do these parameters up front and I don't have to do them for every subsequent query
            IEnumerable<QSO> qsosFlattened = qsos.SelectMany(x => x.Value).Where(y => y.ContactCall == qso.OperatorCall && y.OperatorCall == qso.ContactCall);

            // Start the search process
            enumerable = logAnalyzer.FullParameterSearch(qsosFlattened, qso);

#if DEBUG
            if (qso.ParentLog.LogOwner == "AH6KO")
            {
                var invalidQsos = qso.ParentLog.QSOCollection.Where(q => q.Status == QSOStatus.InvalidQSO).ToList();
            }
#endif

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
                    logAnalyzer.LastChanceMatch(qso);
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
        /// Check to see if the Entity is incorrect.
        /// </summary>
        /// <param name="qsos"></param>
        /// <param name="qso"></param>
        /// <returns></returns>
        internal List<QSO> SearchWithoutEntity(IEnumerable<QSO> qsos, QSO qso, bool isOperator)
        {
            IEnumerable<QSO> enumerable;
            List<QSO> matches;
            int queryLevel = 2;

            if (isOperator)
            {
                queryLevel = 3;
            }

            enumerable = RefineHQPMatch(qsos, qso, queryLevel);
            matches = enumerable.ToList();

            // this is hit only if previous switch hit case 0: so we up the time interval
            switch (matches.Count)
            {
                case 0:
                    if (isOperator)
                    {
                        matches = logAnalyzer.SearchWithoutBand(qsos, qso);
                    }
                    else
                    {
                        matches = SearchWithoutEntity(qsos, qso, true);
                    }
                    return matches;
                case 1:
                    qso.HasBeenMatched = true;
                    qso.MatchingQSO = matches[0];

                    if (matches[0].Status == QSOStatus.ValidQSO)
                    {
                        matches[0].MatchingQSO = qso;
                        matches[0].HasBeenMatched = true;
                        if (isOperator)
                        {
                            matches[0].IncorrectDXEntityMessage = $"{matches[0].OperatorEntity} --> {qso.ContactEntity}";
                        }
                        else
                        {
                            matches[0].IncorrectDXEntityMessage = $"{matches[0].ContactEntity} --> {qso.OperatorEntity}";
                        }
                        matches[0].IsInvalidEntity = true;
                    }
                    return matches;
                case 2:
                    qso.HasBeenMatched = true;
                    qso.MatchingQSO = matches[0];

                    if (matches[0].Status == QSOStatus.ValidQSO)
                    {
                        matches[0].MatchingQSO = qso;
                        matches[0].HasBeenMatched = true;
                        if (isOperator)
                        {
                            matches[0].IncorrectDXEntityMessage = $"{matches[0].OperatorEntity} --> {qso.ContactEntity}";
                        }
                        else
                        {
                            matches[0].IncorrectDXEntityMessage = $"{matches[0].ContactEntity} --> {qso.OperatorEntity}";
                        }
                        matches[0].IsInvalidEntity = true;
                    }

                    if (matches[1].Status == QSOStatus.ValidQSO)
                    {
                        matches[1].MatchingQSO = qso;
                        matches[1].HasBeenMatched = true;
                        if (isOperator)
                        {
                            matches[1].IncorrectDXEntityMessage = $"{matches[1].OperatorEntity} --> {qso.ContactEntity}";
                        }
                        else
                        {
                            matches[1].IncorrectDXEntityMessage = $"{matches[1].ContactEntity} --> {qso.OperatorEntity}";
                        }
                        matches[1].IsInvalidEntity = true;
                    }
                    return matches;
                default:
                    Console.WriteLine("FindHQPMatches: 2");
                    return matches;
            }
        }

        /// <summary>
        /// Search without useing the mode.
        /// </summary>
        /// <param name="qsos"></param>
        /// <param name="qso"></param>
        /// <returns></returns>
        internal List<QSO> SearchWithoutModeHQP(IEnumerable<QSO> qsos, QSO qso)
        {
            IEnumerable<QSO> enumerable;
            List<QSO> matches;
            int queryLevel = 5;
            double qsoPoints;
            double matchQsoPoints;

            enumerable = RefineHQPMatch(qsos, qso, queryLevel);

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
                        matches[0].IsIncorrectMode = true;
                        matches[0].IncorrectValueMessage = $"{matches[0].Mode} --> {qso.Mode}";
                    }
                    else
                    {
                        qso.IsIncorrectMode = true;
                        qso.IncorrectValueMessage = $"{qso.Mode} --> {matches[0].Mode}";
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
                                matchQSO.IsIncorrectMode = true;
                                matchQSO.IncorrectValueMessage = $"{matchQSO.Mode} --> {qso.Mode}";

                            }
                            else
                            {
                                qso.IsIncorrectMode = true;
                                qso.IncorrectValueMessage = $"{qso.Mode} --> {matchQSO.Mode}";
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
        /// <returns></returns>
        internal IEnumerable<QSO> RefineHQPMatch(IEnumerable<QSO> qsos, QSO qso, int queryLevel)
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
                                && Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= TimeInterval);
                    return matches;
                case 2:
                    // without y.ContactEntity == qso.OperatorEntity - 
                    // QSO: 14034 CW 2020-08-22 1732 KH6TU 599 MAU W7JMM 599 OR  
                    // QSO: 21000 CW 2020-08-22 1732 W7JMM 599 OR WH6TU 599 MAU
                    matches = qsos
                    .Where(y => y.Band == qso.Band
                                && y.Mode == qso.Mode
                                && y.OperatorEntity == qso.ContactEntity
                                && Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= TimeInterval);
                    return matches;
                case 3:
                    // without y.OperatorCall == qso.ContactCall -
                    //  QSO: 14034 CW 2020-08-22 1732 KH6TU 599 MAU W7JMM 599 OR  
                    //  QSO: 21000 CW 2020-08-22 1732 W7JMM 599 OR WH6TU 599 MAU
                    matches = qsos
                    .Where(y => y.Band == qso.Band
                                && y.Mode == qso.Mode
                                && y.ContactEntity == qso.OperatorEntity
                                && Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= TimeInterval);
                    return matches;
                case 4:
                    // without y.Band == qso.Band -
                    //  QSO: 14034 CW 2020-08-22 1732 KH6TU 599 MAU W7JMM 599 OR  
                    //  QSO: 21000 CW 2020-08-22 1732 W7JMM 599 OR WH6TU 599 MAU
                    matches = qsos
                    .Where(y => y.Mode == qso.Mode
                                && y.ContactEntity == qso.OperatorEntity
                                && y.OperatorEntity == qso.ContactEntity
                                && Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= TimeInterval);
                    return matches;
                case 5:
                    // general search
                    matches = qsos
                    .Where(y => y.Band == qso.Band
                                && y.ContactEntity == qso.OperatorEntity
                                && y.OperatorEntity == qso.ContactEntity
                                && Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= TimeInterval);
                    return matches;
                case 6:
                    // this is a full search only with mismatching contact and operator calls
                    matches = qsos
                   .Where(y => y.Band == qso.Band
                               && y.Mode == qso.Mode
                               && y.ContactEntity == qso.OperatorEntity
                               && y.OperatorEntity == qso.ContactEntity
                               && Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= TimeInterval);

                    switch (matches.ToList().Count)
                    {
                        case 0:
                            matches = RefineHQPMatch(qsos, qso, 7);
                            break;
                        case 1:
                            return matches;
                        default:
                            matches = RefineHQPMatch(qsos, qso, 8);
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
                               && Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= TimeInterval);
                    return matches;
                case 8:
                    // this is a full search only with mismatching entities
                    matches = qsos
                   .Where(y => y.Band == qso.Band
                               && y.Mode == qso.Mode
                               && Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= TimeInterval);
                    return matches;
                default:
                    Console.WriteLine("Failed search: " + qso.RawQSO);
                    return new List<QSO>();
            }
        }

        /// <summary>
        /// Determine which operator has the mode incorrect.
        /// </summary>
        /// <param name="qso">QSO</param>
        /// <returns>double</returns>
        private double DetermineModeFault(QSO qso)
        {
            ContestLog contestLog = qso.ParentLog;
            string frequency = qso.Frequency;
            double qsoPoints = 0;

            // bonus point for having rig control frequency
            if (logAnalyzer.UsesRigControl(frequency))
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

            qsoPoints += InspectPreviousQSOsForMode(qso, contestLog, frequency);

            qsoPoints += InspectNextQSOsForMode(qso, contestLog, frequency);

            return qsoPoints;
        }

        /// <summary>
        /// Check the 10 previous QSOs
        /// </summary>
        /// <param name="qso">QSO</param>
        /// <param name="contestLog">ContestLog</param>
        /// <param name="frequency">string</param>
        /// <returns>double</returns>
        private double InspectPreviousQSOsForMode(QSO qso, ContestLog contestLog, string frequency)
        {
            double counter = 0;
            double qsoPoints = 0;
            QSO previousQSO = null;

            while (counter < 10)
            {
                counter += 1;

                int index = contestLog.QSOCollection.IndexOf(qso);

                if (index > 0)
                {
                    // https://stackoverflow.com/questions/24799820/get-previous-next-item-of-a-given-item-in-a-list
                    previousQSO = contestLog.QSOCollection.TakeWhile(x => x != qso)
                                                          .DefaultIfEmpty(contestLog.QSOCollection[contestLog.QSOCollection.Count - 1])
                                                          .LastOrDefault();
                }

                if (previousQSO != null)
                {
                    if (previousQSO.IsIncorrectMode == true)
                    {
                        qsoPoints -= 1;
                    }

                    frequency = previousQSO.Frequency;

                    if (logAnalyzer.UsesRigControl(frequency))
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

            return qsoPoints;
        }

        /// <summary>
        /// Check the 10 next QSOs
        /// </summary>
        /// <param name="qso">QSO</param>
        /// <param name="contestLog">ContestLog</param>
        /// <param name="frequency">string</param>
        /// <returns>double</returns>
        private double InspectNextQSOsForMode(QSO qso, ContestLog contestLog, string frequency)
        {
            double counter = 0;
            double qsoPoints = 0;
            QSO nextQSO = null;

            while (counter < 10)
            {
                counter += 1;

                int index = contestLog.QSOCollection.IndexOf(qso);
                if (index <= contestLog.QSOCollection.Count)
                {
                    // https://stackoverflow.com/questions/24799820/get-previous-next-item-of-a-given-item-in-a-list
                    nextQSO = contestLog.QSOCollection.SkipWhile(x => x != qso)
                        .Skip(1)
                        .DefaultIfEmpty(defaultValue: null)
                        .FirstOrDefault();
                }

                if (nextQSO != null)
                {
                    if (nextQSO.IsIncorrectBand == true)
                    {
                        qsoPoints -= 1;
                    }

                    frequency = nextQSO.Frequency;

                    if (logAnalyzer.UsesRigControl(frequency))
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



    } // end class
}
