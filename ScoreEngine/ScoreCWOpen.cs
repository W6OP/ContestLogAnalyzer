using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using W6OP.ContestLogAnalyzer;

namespace W6OP.ContestLogAnalyzer
{
    public class ScoreCWOpen
    {
        public delegate void ProgressUpdate(ContestLog contestLog, Int32 progress);
        public event ProgressUpdate OnProgressUpdate;

        public ScoreCWOpen()
        {
            // DON'T SCORE CHECKLOGS
        }

        public void ScoreContestLogs(List<ContestLog> logList)
        {
            Int32 progress = 0;

            List<ContestLog> contestLogList = logList.OrderByDescending(o => o.LogOwner).ToList();

            foreach (ContestLog contestLog in contestLogList)
            {
                progress++;

                if (!contestLog.IsCheckLog && contestLog.IsValidLog)
                {
                    ValidateDuplicates(contestLog);

                    MarkMultipliers(contestLog);

                    CalculateScore(contestLog);
                    
                    OnProgressUpdate?.Invoke(contestLog, progress);
                }
                
            }
        }

        /// <summary>
        /// Make sure a guy doesn't get hit twice for the same bad QSO
        /// QSO: 	14028	CW	2015-09-05	1211	AA3B	25	BUD	I5EFO	3	EMIL	The received serial number is incorrect - 3 --> 15
        /// QSO: 	14000	CW	2015-09-05	1313	I5EFO	15	EMIL AA3B	88	BUD
        /// 
        /// QSO: 	14033	CW	2015-09-05	1313	AA3B	145	BUD	I5EFO	15	EMIL	This is a duplicate QSO
        /// QSO: 	14000	CW	2015-09-05	1313	I5EFO	15	EMIL AA3B	88	BUD
        /// 
        /// MOVE THIS - should be right after we check for dupes in loganalyser.cs
        /// </summary>
        /// <param name="contestLog"></param>
        private void ValidateDuplicates(ContestLog contestLog)
        {
            List<QSO> invalidQSOs = null;
            List<QSO> dupeQSOs = null;
            QSO qso = null;
            string call = null;

            invalidQSOs = contestLog.QSOCollection.Where(q => q.MatchingQSO != null &&
                                                        q.Band == q.MatchingQSO.Band &&
                                                        q.ContactCall == q.MatchingQSO.OperatorCall &&
                                                        q.OperatorCall == q.MatchingQSO.ContactCall &&
                                                        q.ContactName == q.MatchingQSO.OperatorName &&
                                                        q.OperatorName == q.MatchingQSO.ContactName &&
                                                        q.Status != QSOStatus.ValidQSO).ToList();

            if (invalidQSOs.Count > 1)
            {
                dupeQSOs = invalidQSOs.Where(q => q.QSOHasDupes == true).ToList();
                if (dupeQSOs.Count > 0)
                {
                    foreach (QSO contact in dupeQSOs)
                    {
                        call = contact.ContactCall;
                        qso = invalidQSOs.FirstOrDefault(q => q.ContactCall == call);
                        if (qso != null)
                        {
                            qso.CallIsInValid = false;
                            qso.RejectReasons.Clear();
                            qso.Status = QSOStatus.ValidQSO;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Find all the calls for the session. For each call see if it is valid.
        /// If it is a valid call set it as a multiplier.
        /// </summary>
        /// <param name="qsoList"></param>
        private void MarkMultipliers(ContestLog contestLog)
        {

            List<QSO> qsoList = contestLog.QSOCollection;

            var query = qsoList.GroupBy(x => new { x.ContactCall, x.Status })
             .Where(g => g.Count() >= 1)
             .Select(y => y.Key)
             .ToList();

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
        /// Look at the matching logs first.
        /// Get the number of unique calls in the log. These are multipliers
        /// Really need to find the first instance of any call, not just uniques
        /// </summary>
        /// <returns></returns>
        private void CalculateScore(ContestLog contestLog)
        {
            Int32 multiplierCount = 0;
            Int32 totalValidQSOs = 0;

            totalValidQSOs = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO).ToList().Count();
            multiplierCount = contestLog.QSOCollection.Where(q => q.IsMultiplier== true && (q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO)).ToList().Count();

            //if (!qso.HasMatchingQso)
            //{
            //}
                // need to subtract dupes and invalid logs
                //uniqueCount = contestLog.QSOCollection
                //    .Where(q => q.Status == QSOStatus.ValidQSO)
                //    .GroupBy(p=> p.ContactCall, StringComparer.OrdinalIgnoreCase)
                //    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase).Count();

                contestLog.Multipliers = multiplierCount;
            if (multiplierCount != 0)
            {
                contestLog.ActualScore = totalValidQSOs * multiplierCount;
            } else
            {
                contestLog.ActualScore = totalValidQSOs;
            }
            
        }
    } // end class
}
