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
            // matchingLogs.AddRange(_ContestLogs.Where(q => q.QSOCollection.Any(a => a.ContactCall == operatorCall && a.ReceivedSerialNumber == sent && a.Band == band && a.ContactName == sentName && a.Status == QSOStatus.InvalidQSO)).ToList()); // && a.IsValidQSO == false
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
                    MarkMultipliers(contestLog);

                    CalculateScore(contestLog);
                    
                    OnProgressUpdate?.Invoke(contestLog, progress);
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
        /// Look at the matching logs first.
        /// Get the number of unique calls in the log. These are multipliers
        /// Really need to find the first instance of any call, not just uniques
        /// </summary>
        /// <returns></returns>
        private void CalculateScore(ContestLog contestLog)
        {
            Int32 multiplierCount = 0;
            Int32 totalValidQSOs = 0;

            totalValidQSOs = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO).ToList().Count();
            multiplierCount = contestLog.QSOCollection.Where(q => q.IsMultiplier== true && q.Status == QSOStatus.ValidQSO).ToList().Count();

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
