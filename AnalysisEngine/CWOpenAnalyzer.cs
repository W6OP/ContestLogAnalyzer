using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W6OP.ContestLogAnalyzer
{
    internal class CWOpenAnalyzer
    {
        internal LogAnalyzer logAnalyzer;

        private const int TimeInterval = 10;

        internal CWOpenAnalyzer(LogAnalyzer logAnalyzerMain)
        {
            logAnalyzer = logAnalyzerMain;
        }

        /// <summary>
        /// Mark all QSOs that don't have the correct name sent as invalid.
        /// This is very rare.
        /// </summary>
        /// <param name="qsoList"></param>
        /// <param name="name"></param>
        internal void MarkIncorrectSentName(List<QSO> qsoList, string name)
        {
            List<QSO> qsos = qsoList.Where(q => q.OperatorName != name && q.Status == QSOStatus.ValidQSO).ToList();

            if (qsos.Any())
            {
                qsos.Select(c => { c.IsIncorrectOperatorName = false; return c; }).ToList();
            }
        }

        /// <summary>
        /// CWOpen
        /// See if the contact name is incorrect. Sometimes a person
        /// will send different names on different QSOS. Find out what
        /// the predominate name they used is.
        /// </summary>
        /// <param name="qso"></param>
        internal void MarkIncorrectContactNames(QSO qso)
        {
            Tuple<string, int> majorityName = new Tuple<string, int>("", 1);

            // get list of name tuples
            var names = logAnalyzer.NameDictionary[qso.ContactCall];

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

            qso.IncorrectValueMessage = qso.ContactName + " --> " + majorityName.Item1;
            qso.IsIncorrectContactName = true;
        }

        /// <summary>
        /// Start trying to match up QSOS. Start with a search for a matching qso
        /// using all parameters. If that fails search by reducing parameters until 
        /// a match is found or the call is determined to be busted. Use IEnumerable
        /// for performance, .ToList() is expensive so don't use it unless something
        /// has been returned.
        /// </summary>
        /// <param name="qso"></param>
        internal void FindCWOpenMatchingQsos(QSO qso)
        {
            IEnumerable<QSO> enumerable;
            List<QSO> matches;
            IEnumerable<ContestLog> contestLogs;
            IEnumerable<KeyValuePair<string, List<QSO>>> qsos;

            // all logs with this operator call sign in them
            if (logAnalyzer.CallDictionary.ContainsKey(qso.OperatorCall))
            {
                contestLogs = logAnalyzer.CallDictionary[qso.OperatorCall];
            }
            else
            {
                Console.WriteLine("FindCWOpenMatchingQsos - 1 : " + qso.OperatorCall);
                // i.e. JF2IWL made 3 qsos and those three got in the CallDictionary
                // pointing at JF2IWL. However none of the 3 sent in a log so JF2IWL 
                // does not appear in the CallDictionary as there are no logs that
                // have a QSO for him so check the bad call list
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


            // if there is only one log this call is in then it is unique
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

            // there is no matching log for this call so we give him the qso
            // maybe should see if anyone else worked him?
            if (logAnalyzer.ContestLogList.Where(b => b.LogOwner == qso.ContactCall).Count() == 0)
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

            enumerable = logAnalyzer.FullParameterSearch(qsosFlattened, qso);

            // if the qso has been matched at any previous level we don't need to continue
            if (qso.HasBeenMatched)
            {
                return;
            }

            matches = enumerable.ToList();

            switch (matches.Count)
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
                    break;
            }
        }


        /// <summary>
        /// Remove the serial number check. If we get a hit then one or both
        /// serial numbers are incorrect.
        /// </summary>
        /// <param name="qsos"></param>
        /// <param name="qso"></param>
        /// <returns></returns>
        internal List<QSO> SearchWithoutSerialNumber(IEnumerable<QSO> qsos, QSO qso)
        {
            IEnumerable<QSO> enumerable;
            List<QSO> matches;
            int searchLevel = 2;

            enumerable = RefineCWOpenMatch(qsos, qso, searchLevel);

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
                        matches[0].IsIncorrectSerialNumber = true;
                        matches[0].IncorrectValueMessage = $"{matches[0].ReceivedSerialNumber} --> {qso.SentSerialNumber}";
                    }

                    if (qso.Status != QSOStatus.InvalidQSO && qso.ReceivedSerialNumber != matches[0].SentSerialNumber)
                    {
                        qso.IsIncorrectSerialNumber = true;
                        qso.IncorrectValueMessage = $"{qso.ReceivedSerialNumber} --> {matches[0].SentSerialNumber}";
                    }
                    return matches;
                default:
                    // duplicate incorrect serial number QSOs
                    bool matchFound = false;

                    qso.MatchingQSO = matches[0];
                    qso.HasBeenMatched = true;

                    // This QSO is not in the other operators log or the call may be busted:
                    foreach (QSO matchQSO in matches)
                    {
                        matchQSO.MatchingQSO = qso;
                        matchQSO.HasBeenMatched = true;

                        // determine which serial number(s) are incorrect
                        if (matchQSO.ReceivedSerialNumber != qso.SentSerialNumber)
                        {
                            matchQSO.IsIncorrectSerialNumber = true;
                            matchQSO.IncorrectValueMessage += $"{matchQSO.ReceivedSerialNumber} --> {qso.SentSerialNumber}";
                        }

                        // we don't know the order they are evaluated so a later bad QSO could invalidate a good QSO
                        if (qso.Status != QSOStatus.InvalidQSO && qso.ReceivedSerialNumber != matchQSO.SentSerialNumber)
                        {
                            qso.IsIncorrectSerialNumber = true;
                            qso.IncorrectValueMessage = $"{qso.ReceivedSerialNumber} --> {matchQSO.SentSerialNumber}";
                        }
                        else
                        {
                            matchFound = true;
                        }
                    }

                    if (matchFound == true)
                    {
                        qso.IsIncorrectSerialNumber = false;
                        qso.IncorrectValueMessage = "";
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
            int searchLevel = 3;

            enumerable = RefineCWOpenMatch(qsos, qso, searchLevel); //.ToList();

            matches = enumerable.ToList();

            switch (matches.Count)
            {
                case 0:
                    // search without band
                    matches = logAnalyzer.SearchWithoutBand(qsos, qso);
                    return matches;
                case 1:
                    qso.MatchingQSO = matches[0];
                    matches[0].MatchingQSO = qso;

                    qso.HasBeenMatched = true;
                    matches[0].HasBeenMatched = true;

                    if (qso.Status != QSOStatus.InvalidQSO)
                    {
                        qso.IsIncorrectContactName = true;
                        qso.IncorrectValueMessage = $"{qso.ContactName} --> {matches[0].OperatorName}";
                    }
                    return matches;
                default:
                    // duplicate QSOs
                    if (qso.HasBeenMatched == false)
                    {
                        qso.MatchingQSO = matches[0];
                        qso.HasBeenMatched = true;
                    }

                    foreach (QSO matchQSO in matches)
                    {
                        if (qso.HasBeenMatched == false)
                        {
                            matchQSO.MatchingQSO = qso;
                            matchQSO.HasBeenMatched = true;

                            matchQSO.IsDuplicateMatch = true;
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
        /// <returns></returns>
        internal IEnumerable<QSO> RefineCWOpenMatch(IEnumerable<QSO> qsos, QSO qso, int queryLevel)
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
                   Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= TimeInterval);
                    return matches;
                case 2:
                    // catch incorrect serial number
                    matches = qsos
                    .Where(y => y.OperatorName == qso.ContactName &&
                    y.Band == qso.Band &&
                    Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= TimeInterval);
                    return matches;
                case 3:
                    // incorrect name
                    matches = qsos
                    .Where(y => y.SentSerialNumber == qso.ReceivedSerialNumber &&
                    y.ReceivedSerialNumber == qso.SentSerialNumber &&
                    y.Band == qso.Band &&
                    Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= TimeInterval);
                    return matches;
                case 4:
                    // incorrect band
                    matches = qsos
                   .Where(y => y.OperatorName == qso.ContactName &&
                   y.SentSerialNumber == qso.ReceivedSerialNumber &&
                   y.ReceivedSerialNumber == qso.SentSerialNumber &&
                   Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= TimeInterval);
                    return matches;
                case 5:
                    // incorrect time
                    matches = qsos
                    .Where(y => y.OperatorName == qso.ContactName &&
                    y.SentSerialNumber == qso.ReceivedSerialNumber &&
                    y.ReceivedSerialNumber == qso.SentSerialNumber &&
                    y.Band == qso.Band);
                    return matches;
                case 6:
                    matches = qsos
                    .Where(y => y.OperatorName == qso.ContactName &&
                    y.SentSerialNumber == qso.ReceivedSerialNumber &&
                    y.ReceivedSerialNumber == qso.SentSerialNumber &&
                    y.Band == qso.Band &&
                    Math.Abs(y.QSODateTime.Subtract(qso.QSODateTime).TotalMinutes) <= TimeInterval);
                    return matches;
                default:
                    Console.WriteLine("Failed search: " + qso.RawQSO);
                    return new List<QSO>();

            }
        }

        /// <summary>
        /// Check the 10 previous QSOs
        /// </summary>
        /// <param name="qso">QSO</param>
        /// <param name="contestLog">ContestLog</param>
        /// <param name="frequency">string</param>
        /// <returns>double</returns>
        internal double InspectPreviousQSOsForFrequency(QSO qso, ContestLog contestLog, string frequency)
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
                    if (previousQSO.IsIncorrectBand == true)
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

            return qsoPoints;
        }

        /// <summary>
        /// Check the 10 next QSOs
        /// </summary>
        /// <param name="qso">QSO</param>
        /// <param name="contestLog">ContestLog</param>
        /// <param name="frequency">string</param>
        /// <returns>double</returns>
        internal double InspectNextQSOsForFrequency(QSO qso, ContestLog contestLog, string frequency)
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
                    // extra half point because he just ended working the contest and more likely to be correct
                    qsoPoints += 1.5;
                }
            }

            return qsoPoints;
        }

    } // end class
}
