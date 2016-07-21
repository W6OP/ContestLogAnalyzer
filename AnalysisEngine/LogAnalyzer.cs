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
        public delegate void ProgressUpdate(string value, string qsoCount, Int32 progress);
        public event ProgressUpdate OnProgressUpdate;

        //private SortedDictionary<string, string> _CallTable;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public LogAnalyzer()
        {
            // need case insensitive BOB and Bob
            //_CallTable = new SortedDictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Start processing individual logs.
        /// Find all of the logs that have the log owners call sign in them.
        /// Find all the logs that do not have a reference to this log.
        /// Now each log is self contained and can be operated on without opening other files.
        /// </summary>

        /// <summary>
        /// At this point we have already marked duplicate QSOs as invalid and
        /// any in the wrong session or those with an incorrect call sign format invalid
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
            string call = null;
            string name = null;
            //Int32 count = 0; // QSOs processed?
            Int32 progress = 0;


            foreach (ContestLog contestLog in contestLogList)
            {
                List<QSO> qsoList;
                call = contestLog.LogOwner;
                name = contestLog.LogHeader.NameSent.ToUpper();

                progress++;

                // ReportProgress with Callsign
                OnProgressUpdate?.Invoke(call, contestLog.QSOCollection.Count.ToString(), progress);

                if (!contestLog.IsCheckLog && contestLog.IsValidLog)
                {
                    qsoList = contestLog.QSOCollection;

                    MarkDuplicateQSOs(qsoList);

                    MarkIncorrectCallSigns(qsoList, call);

                    MarkIncorrectName(qsoList, name);

                    MatchQSOs(qsoList, contestLogList, call, name);

                    MarkMultipliers(qsoList);


                    // count = CollateLogs(contestLogList, contestLog, count);
                }
                else
                {
                    // what do I do now?
                }
            }

            // now I should every log with it's matching QSOs, QSOs to be checked and all the other logs with a reference
            FindLogsToReview(contestLogList);
        }

        /// <summary>
        /// http://stackoverflow.com/questions/16197290/checking-for-duplicates-in-a-list-of-objects-c-sharp
        /// Find duplicate QSOs in a log and mark the as dupes. Be sure
        /// to allow the first QSO to be marked as valid, though.
        /// </summary>
        /// <param name="qsoList"></param>
        private void MarkDuplicateQSOs(List<QSO> qsoList)
        {
            var query = qsoList.GroupBy(x => new { x.ContactCall, x.Band })
             .Where(g => g.Count() > 1)
             .Select(y => y.Key)
             .ToList();

            foreach (var qso in query)
            {
                List<QSO> dupeList = qsoList.Where(item => item.ContactCall == qso.ContactCall && item.Band == qso.Band).ToList();

                if (dupeList.Any())
                {
                    // set all as dupes
                    dupeList.Select(c => { c.QSOIsDupe = true; return c; }).ToList();
                    // now reset the first one as not a dupe
                    dupeList.First().QSOIsDupe = false;
                    // let me know it has dupes for the rejected qso report
                    dupeList.First().QSOHasDupes = true;
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
            List<QSO> qsos = qsoList.Where(q => q.OperatorCall.ToUpper() != call).ToList();

            if (qsos.Any())
            {
                qsos.Select(c => { c.CallIsValid = false; return c; }).ToList();
            }
        }

        /// <summary>
        /// This may not be the correct place for this.
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
            ContestLog contestLog = null;
            QSO matchQSO = null;

            // only look at valid QSOs
            List<QSO> validQsoList = qsoList.Where(q => q.Status == QSOStatus.ValidQSO).ToList();

            foreach (QSO qso in validQsoList)
            {
                // get the other log that matches this QSO contact call
                contestLog = (ContestLog)contestLogList.FirstOrDefault(q => q.LogOwner == qso.ContactCall);

                if (contestLog != null)
                {
                    // now see if a QSO matches this QSO
                    //matchQSO = (QSO)contestLog.QSOCollection.FirstOrDefault(q => q.Band == qso.Band && q.OperatorName == qso.ContactName &&
                    //                        q.ReceivedSerialNumber == qso.SentSerialNumber && DateTime.Compare(q.QSODateTime, qso.QSODateTime) == 0);

                    matchQSO = (QSO)contestLog.QSOCollection.FirstOrDefault(q => q.Band == qso.Band && q.OperatorName == qso.ContactName && q.OperatorCall == qso.ContactCall &&
                                            q.SentSerialNumber == qso.ReceivedSerialNumber && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).Minutes) <= 5);

                    if (matchQSO != null) // found it
                    {
                        // store the matching QSO
                        qso.MatchingQSO = matchQSO;
                    }
                    else
                    {
                        // narrow down search
                        RejectReason reason = FindRejectReason(contestLog, qso);
                        qso.Status = QSOStatus.InvalidQSO;
                        qso.RejectReasons.Add(reason, EnumHelper.GetDescription(reason));
                    }
                }
                else
                {
                    // can't find a matching log
                    // need to see if call is in any logs
                    List<ContestLog> tempLog = contestLogList.Where(q => q.QSOCollection.Any(a => a.ContactCall == operatorCall && a.Status == QSOStatus.ValidQSO)).ToList();
                    if (tempLog == null || tempLog.Count <= 1) // 1 would mean its only in this log
                    {
                        // the call is not in any other log
                        qso.Status = QSOStatus.InvalidQSO;
                        qso.RejectReasons.Add(RejectReason.NoQSO, EnumHelper.GetDescription(RejectReason.NoQSO));
                    }
                    else
                    {
                        qso.Status = QSOStatus.ValidQSO;
                    } 
                }
            }


            // get the first QSO - lets say W1XXX
            // look for the log for W1XXX
            // List<QSO> validQsoList = qsoList.Where(q => q.Status == QSOStatus.ValidQSO).ToList();
            //matchingLogs.AddRange(contestLogList.Where(q => q.QSOCollection.Any(a => String.Equals(a.ContactCall, operatorCall, StringComparison.CurrentCultureIgnoreCase) && a.ReceivedSerialNumber == sentSerialNumber && a.Band == band && String.Equals(a.ContactName, sentName, StringComparison.CurrentCultureIgnoreCase) && a.Status == QSOStatus.InvalidQSO)).ToList());


            // this might be tough
            // for each QSO I need to query every other log for an exact match of name and serial number - if match then good and quit
            // if no match then search by serial number, band, mode, date/time? (may not be exact)
            // if no match then search by sent name, band, mode, date/time? (may not be exact)


            // for each QSO find the matching log
            //foreach (ContestLog contestLog in contestLogList)
            //{
            //    // List<QSO> qsoList = contestLog.QSOCollection.Where(q => q.ContactCall == operatorCall && q.ReceivedSerialNumber == sent && q.Band == band && q.ContactName == sentName && q.Status == QSOStatus.InvalidQSO).ToList(); 
            //    //matchingLogs.AddRange(contestLogList.Where(q => q.QSOCollection.Any(a => String.Equals(a.ContactCall, operatorCall, StringComparison.CurrentCultureIgnoreCase) && a.ReceivedSerialNumber == sentSerialNumber && a.Band == band && String.Equals(a.ContactName, sentName, StringComparison.CurrentCultureIgnoreCase) && a.Status == QSOStatus.InvalidQSO)).ToList());
            //}
        }

        private RejectReason FindRejectReason(ContestLog contestLog, QSO qso)
        {
            QSO matchQSO = null;
            RejectReason reason = RejectReason.NoQSO;
            TimeSpan ts;

            // query for time difference
            matchQSO = (QSO)contestLog.QSOCollection.FirstOrDefault(q => q.Band == qso.Band && q.OperatorName == qso.ContactName && q.SentSerialNumber == qso.ReceivedSerialNumber && q.OperatorCall == qso.ContactCall && Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).Minutes) > 5 && q.Status == QSOStatus.ValidQSO);
            if (matchQSO != null)
            {
                // store the matching QSO
                qso.MatchingQSO = matchQSO;

                ts = qso.QSODateTime.Subtract(matchQSO.QSODateTime);
                qso.Status = QSOStatus.InvalidQSO;
                qso.ExcessTimeSpan = Math.Abs(ts.Minutes);
                return RejectReason.InvalidTime;
            }

            // query for incoreect serial number
            matchQSO = (QSO)contestLog.QSOCollection.FirstOrDefault(q => q.Band == qso.Band && q.OperatorName == qso.ContactName && q.OperatorCall == qso.ContactCall && q.Status == QSOStatus.ValidQSO); //&& Math.Abs(q.QSODateTime.Subtract(qso.QSODateTime).Minutes) > 5
            if (matchQSO != null)
            {
                // store the matching QSO
                qso.MatchingQSO = matchQSO;
                return RejectReason.SerialNumber;
            }

            // query for incorrect band
            matchQSO = (QSO)contestLog.QSOCollection.FirstOrDefault(q => q.OperatorName == qso.ContactName && q.SentSerialNumber == qso.ReceivedSerialNumber && q.OperatorCall == qso.ContactCall && q.Status == QSOStatus.ValidQSO);
            if (matchQSO != null)
            {
                // store the matching QSO
                qso.MatchingQSO = matchQSO;
                return RejectReason.Band;
            }

            // query for incorrect name
            matchQSO = (QSO)contestLog.QSOCollection.FirstOrDefault(q => q.Band == qso.Band && q.SentSerialNumber == qso.ReceivedSerialNumber && q.OperatorCall == qso.ContactCall && q.Status == QSOStatus.ValidQSO);
            if (matchQSO != null)
            {
                // store the matching QSO
                qso.MatchingQSO = matchQSO;
                return RejectReason.OperatorName;
            }

            // query for incoreect call
            matchQSO = (QSO)contestLog.QSOCollection.FirstOrDefault(q => q.Band == qso.Band && q.OperatorName == qso.ContactName && q.SentSerialNumber == qso.ReceivedSerialNumber && q.Status == QSOStatus.ValidQSO);
            if (matchQSO != null)
            {
                // store the matching QSO
                qso.MatchingQSO = matchQSO;
                return RejectReason.BustedCallSign;
            }

            return reason;
        }

        /// <summary>
        /// Mark all QSOs that don't have the correct name sent as invalid.
        /// </summary>
        /// <param name="qsoList"></param>
        /// <param name="name"></param>
        private void MarkIncorrectName(List<QSO> qsoList, string name)
        {
            List<QSO> qsos = qsoList.Where(q => q.OperatorName.ToUpper() != name).ToList();

            if (qsos.Any())
            {
                qsos.Select(c => { c.CallIsValid = false; return c; }).ToList();
            }
        }



        /// <summary>
        /// Find all the calls for the session. For each call see if it is valid.
        /// If it is a valid call set it as a multiplier.
        /// </summary>
        /// <param name="qsoList"></param>
        private void MarkMultipliers(List<QSO> qsoList)
        {
            var query = qsoList.GroupBy(x => new { x.ContactCall, x.Status })
             .Where(g => g.Count() >= 1)
             .Select(y => y.Key)
             .ToList();


            // THIS NEEDS TESTING !!!

            foreach (var qso in query)
            {
                List<QSO> multiList = qsoList.Where(item => item.ContactCall == qso.ContactCall && item.Status == QSOStatus.ValidQSO).ToList();

                if (multiList.Any())
                {
                    // now set the first one as a multiplier
                    multiList.First().IsMultiplier = true;
                }
            }
        }



        /// <summary>
        /// This is the first pass and I am mostly interested in collating the logs.
        /// For each QSO in the log build a collection of all of the logs that have at least one exact match.
        /// Next, build a collection of all of the logs that have a call sign match but where the other information
        /// does not match. Later in the process we may get an exact match and then will want to remove that QSO from 
        /// the Review collection.
        /// Then get a collection of all logs where there was not a match at all. On the first pass there will be a lot 
        /// of duplicates because a log is added for each QSO that matches. Later I'll remove the duplicates.
        /// </summary>
        /// <param name="contestLog"></param>
        private Int32 CollateLogs(List<ContestLog> contestLogList, ContestLog contestLog, Int32 count)
        {
            List<ContestLog> matchingLogs = new List<ContestLog>();
            List<ContestLog> reviewLogs = new List<ContestLog>();
            List<ContestLog> otherLogs = new List<ContestLog>();
            string operatorCall = contestLog.LogOwner;
            string sentName = null;
            Int32 sentSerialNumber = 0;
            Int32 received = 0;
            Int32 band = 0;
            Int32 qsoCount = 0;
            Int32 otherCount = 0;

            foreach (QSO qso in contestLog.QSOCollection)
            {





                List<QSO> InvalidQsoList = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.InvalidQSO).ToList();
                List<QSO> ValidQsoList = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO).ToList();
                //if (!_CallTable.ContainsKey(qso.ContactCall.ToString() + qso.ContactName.ToString()))
                //{
                //    // Chas and Chuck same guy, also AJ and Anthony
                //    _CallTable.Add(qso.ContactCall.ToString() + qso.ContactName.ToString(), qso.ContactCall.ToString() + " - " + qso.ContactName);
                //}


                // query all the other logs for a match
                // if there is a match, mark each QSO as valid.



                // AM I ONLY CHECKING THE ONES I ALREADY MARKED INVALID HERE OR TRYING TO FIND NEW ONES TO INVALIDATE ????
                // 



                if (qso.Status == QSOStatus.InvalidQSO)
                {
                    sentSerialNumber = qso.SentSerialNumber;
                    received = qso.ReceivedSerialNumber;
                    band = qso.Band;
                    sentName = qso.OperatorName;

                    // get logs that have at least a partial match
                    // List<ContestLog> partialMatch = contestLog.Where(q => q.QSOCollection.Any(a => a.ContactCall == call)).ToList();

                    // get all the QSOs that match
                    //List<QSO> qsoList = contestLog.QSOCollection.Where(q => q.ContactCall == operatorCall && q.ReceivedSerialNumber == sent && q.Band == band && q.ContactName == sentName && q.Status == QSOStatus.InvalidQSO).ToList(); 
                    // get all the logs that have at least one exact match
                    //List<ContestLog> list = _ContestLogs.Where(q => q.QSOCollection.Any(a =>  a.Band == band)).ToList();

                    // need to add way to check if log is valid
                    //matchingLogs.AddRange(contestLogList.Where(q => q.QSOCollection.Any(a => a.ContactCall == operatorCall && a.ReceivedSerialNumber == sentSerialNumber && a.Band == band && a.ContactName == sentName && a.Status == QSOStatus.InvalidQSO)).ToList()); // && a.IsValidQSO == false
                    // case insensitive
                    matchingLogs.AddRange(contestLogList.Where(q => q.QSOCollection.Any(a => String.Equals(a.ContactCall, operatorCall, StringComparison.CurrentCultureIgnoreCase) && a.ReceivedSerialNumber == sentSerialNumber && a.Band == band && String.Equals(a.ContactName, sentName, StringComparison.CurrentCultureIgnoreCase) && a.Status == QSOStatus.InvalidQSO)).ToList()); // && a.IsValidQSO == false

                    //StringComparer.CurrentCultureIgnoreCase

                    // some of these will be marked valid as we go along and need to be removed from this collection
                    //reviewLogs.AddRange(_ContestLogs.Where(q => q.QSOCollection.Any(a => a.ContactCall == operatorCall && (a.ReceivedSerialNumber != sentSerialNumber || a.Band == band || a.ContactName == sentName && a.Status == QSOStatus.InvalidQSO))).ToList());
                    // logs where there was no match at all - exclude this logs owner too

                    // StringComparison.CurrentCultureIgnoreCase
                    //otherLogs.AddRange(contestLogList.Where(q => q.QSOCollection.All(a => a.ContactCall != operatorCall && a.OperatorCall != operatorCall)).ToList());
                    otherLogs.AddRange(contestLogList.Where(q => q.QSOCollection.All(a => !String.Equals(a.ContactCall, operatorCall, StringComparison.CurrentCultureIgnoreCase) && !String.Equals(a.OperatorCall, operatorCall, StringComparison.CurrentCultureIgnoreCase))).ToList());

                    // need to determine if the matching log count went up, if it did, mark the QSO as valid
                    if (matchingLogs.Count > qsoCount)
                    {
                        qso.Status = QSOStatus.ValidQSO;
                        qsoCount++;

                    }

                    //if (reviewLogs.Count > reviewCount)
                    //{
                    //    if (qso.Status != QSOStatus.ValidQSO)
                    //    {
                    //        qso.Status = QSOStatus.ReviewQSO;
                    //        reviewCount++;
                    //    }
                    //}

                    if (otherLogs.Count > otherCount)
                    {
                        if (qso.Status != QSOStatus.ReviewQSO)
                        {
                            qso.Status = QSOStatus.ValidQSO;
                            otherCount++;
                        }
                    }

                    count++;

                }
            }

            //reviewLogs.AddRange(otherLogs.Where(q => q.QSOCollection.Any(a => a.ContactCall == operatorCall && a.Status == QSOStatus.InvalidQSO)).ToList());

            // http://stackoverflow.com/questions/3319016/convert-list-to-dictionary-using-linq-and-not-worrying-about-duplicates
            // this gives me a dictionary with a unique log even if several QSOs
            contestLog.MatchLogs = matchingLogs
                .GroupBy(p => p.LogOwner, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            // THIS DOES THE SAME AS ABOVE BUT THE KEY IS THE LOG INSTEAD OF LogOwner
            //// http://social.msdn.microsoft.com/Forums/vstudio/en-US/c0f0141c-1f98-422e-89af-406638c4403f/how-to-write-linq-query-to-convert-to-dictionaryintlistint-in-c?forum=linqprojectgeneral
            //// this converts the list to a dictionary and lists how many logs were matching
            //var match = matchingLogs
            //    .Select((n, i) => new { Value = n, Index = i })
            //    .GroupBy(a => a.Value)
            //    .ToDictionary(
            //        g => g.Key,
            //        g => g.Select(a => a.Index).ToList()
            //     );

            // now cleanup - THIS NEEDS TO BE CHECKED TO SEE IF IT WORKS
            //if (reviewLogs.Count > 0)
            //{
            //foreach (ContestLog log in reviewLogs)
            //{
            //    Int32 asd = log.QSOCollection.Count;
            //    log.QSOCollection.RemoveAll(x => x.Status == QSOStatus.ValidQSO);

            //}


            //contestLog.ReviewLogs = reviewLogs
            //   .GroupBy(p => p.LogOwner, StringComparer.OrdinalIgnoreCase)
            //       .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);


            // this also contains the LogOwner's log so I want to remove it in a bit
            contestLog.OtherLogs = otherLogs
               .GroupBy(p => p.LogOwner, StringComparer.OrdinalIgnoreCase)
                   .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            return count;
        }

        /// <summary>
        /// I have a collection of logs where some the QSOs match and another collection
        /// where they don't match at all. Now I need a collection of QSOs that need review.
        /// These will be logs where at least one QSO is marked InValid.
        /// 
        /// SO WHAT AM I DOING HERE? Don't return anything or updtae anything from what I can see
        /// </summary>
        private void FindLogsToReview(List<ContestLog> contestLogList)
        {
            List<QSO> reviewQsoList = new List<QSO>();
            Dictionary<string, QSO> review;



            // This gives me a list of all the QSOs in the Contest Log collection that need reviewing
            reviewQsoList = contestLogList.SelectMany(q => q.QSOCollection).Where(a => a.Status == QSOStatus.InvalidQSO).ToList();

            // this eliminates the dupes - needs testing
            review = reviewQsoList
              .GroupBy(p => p.OperatorCall, StringComparer.OrdinalIgnoreCase)
              .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);




            //var seasonSpots = from s in _ContestLogs
            //                  where s.QSOCollection != null
            //                  where QSO(s.QSOCollection)
            //                  select s.;
        }


    } // end class
}
