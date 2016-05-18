using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public void ScoreContestLogs(List<ContestLog> contestLogList)
        {

            List<ContestLog> newcontestLogList = contestLogList.OrderByDescending(o => o.LogOwner).ToList();

            //contestLogList.Sort((x, y) => string.Compare(x.LogOwner, y.LogOwner));

            foreach (ContestLog contestLog in newcontestLogList)
            {
                CalculateScore(contestLog);
                // ReportProgress with Callsign
                // ADD IF THE LOG NEEDS REVIEW AND SET IN LISTBOX IN RED
                // probably make a little collection here to pass dupe and other info
                OnProgressUpdate?.Invoke(contestLog);
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
            Int32 uniqueCount = 0;

            // need to subtract dupes and invalid logs
            uniqueCount = contestLog.QSOCollection
                .GroupBy(p=> p.ContactCall, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase).Count();

            contestLog.Multipliers = uniqueCount;
        }
    } // end class
}
