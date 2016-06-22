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
        public delegate void ProgressUpdate(ContestLog contestLog);
        public event ProgressUpdate OnProgressUpdate;

        public ScoreCWOpen()
        {
            // matchingLogs.AddRange(_ContestLogs.Where(q => q.QSOCollection.Any(a => a.ContactCall == operatorCall && a.ReceivedSerialNumber == sent && a.Band == band && a.ContactName == sentName && a.Status == QSOStatus.InvalidQSO)).ToList()); // && a.IsValidQSO == false
            // DON'T SCORE CHECKLOGS
        }

        public void ScoreContestLogs(List<ContestLog> logList)
        {

            List<ContestLog> contestLogList = logList.OrderByDescending(o => o.LogOwner).ToList();

            //contestLogList.Sort((x, y) => string.Compare(x.LogOwner, y.LogOwner));

            foreach (ContestLog contestLog in contestLogList)
            {
                if (!contestLog.IsCheckLog && contestLog.IsValidLog)
                {
                    CalculateScore(contestLog);
                    // ReportProgress with Callsign
                    // ADD IF THE LOG NEEDS REVIEW AND SET IN LISTBOX IN RED
                    // probably make a little collection here to pass dupe and other info
                    OnProgressUpdate?.Invoke(contestLog);
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
