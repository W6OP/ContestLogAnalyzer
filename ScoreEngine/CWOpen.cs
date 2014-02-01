using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W6OP.ContestLogAnalyzer
{
    public class CWOpen
    {
        public CWOpen()
        {
            // matchingLogs.AddRange(_ContestLogs.Where(q => q.QSOCollection.Any(a => a.ContactCall == operatorCall && a.ReceivedSerialNumber == sent && a.Band == band && a.ContactName == sentName && a.Status == QSOStatus.InvalidQSO)).ToList()); // && a.IsValidQSO == false
            // DON'T SCORE CHECKLOGS
        }

        /// <summary>
        /// Look at the matching logs first.
        /// Get the number of unique calls in the log.
        /// </summary>
        /// <returns></returns>
        public ContestLog CalculateScore(ContestLog contestLog)
        {
            Int32 uniqueCount = 0;

            uniqueCount = contestLog.QSOCollection
                .GroupBy(p=> p.ContactCall, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase).Count();

            contestLog.Multipliers = uniqueCount;
           
            return contestLog;
        }
    } // end class
}
