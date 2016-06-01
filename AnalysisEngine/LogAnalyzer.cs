using System;
using System.Collections;
using System.Collections.Generic;
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
        /// This will probably go on another thread.
        /// Start processing individual logs.
        /// Find all of the logs that have the log owners call sign in them.
        /// Find all the logs that do not have a reference to this log.
        /// Now each log is self contained and can be operated on without opening other files.
        /// </summary>

        /// <summary>
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
            Int32 count = 0; // QSOs processed?
            Int32 progress = 0;

            foreach (ContestLog contestLog in contestLogList)
            {
                call = contestLog.LogOwner;

                progress++;

                // ReportProgress with Callsign
                if (OnProgressUpdate != null)
                {
                    OnProgressUpdate(call, contestLog.QSOCollection.Count.ToString(), progress);
                }

                if (!contestLog.IsCheckLog && contestLog.IsValidLog)
                {
                    count = CollateLogs(contestLogList, contestLog, count);
                }
            }

            // now I should every log with it's matching QSOs, QSOs to be checked and all the other logs with a reference
            FindLogsToReview(contestLogList);
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
                List<QSO> qsoList = contestLog.QSOCollection.Where(q =>  q.Status == QSOStatus.InvalidQSO).ToList();
                List<QSO> qsoList2 = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO).ToList();
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
