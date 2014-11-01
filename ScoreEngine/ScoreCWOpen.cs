using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W6OP.ContestLogAnalyzer
{
    public class ScoreCWOpen
    {
        public delegate void ProgressUpdate(string logOwner, string claimed, string actual);
        public event ProgressUpdate OnProgressUpdate;

        public ScoreCWOpen()
        {
            // matchingLogs.AddRange(_ContestLogs.Where(q => q.QSOCollection.Any(a => a.ContactCall == operatorCall && a.ReceivedSerialNumber == sent && a.Band == band && a.ContactName == sentName && a.Status == QSOStatus.InvalidQSO)).ToList()); // && a.IsValidQSO == false
            // DON'T SCORE CHECKLOGS
        }

        public void ScoreContestLogs(List<ContestLog> contestLogList)
        {
            foreach (ContestLog contestLog in contestLogList)
            {
                CalculateScore(contestLog);
                // ReportProgress with Callsign
                if (OnProgressUpdate != null)
                {
                    // ADD IF THE LOG NEEDS REVIEW AND SET IN LISTBOX IN RED
                    // probably make a little collection here to pass dupe and other info
                    OnProgressUpdate(contestLog.LogOwner, contestLog.ClaimedScore.ToString(), contestLog.ActualScore.ToString());
                }
            }
        }

        /// <summary>
        /// Look at the matching logs first.
        /// Get the number of unique calls in the log.
        /// </summary>
        /// <returns></returns>
        private void CalculateScore(ContestLog contestLog)
        {
            Int32 uniqueCount = 0;

            // need to subtract dupes and invalid logs
            uniqueCount = contestLog.QSOCollection
                .GroupBy(p=> p.ContactCall, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase).Count();

            contestLog.Multipliers = uniqueCount;
           
            //return contestLog;
        }
    } // end class
}
