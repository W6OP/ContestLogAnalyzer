using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace W6OP.ContestLogAnalyzer
{
    public class ScoreHQP
    {
        public delegate void ProgressUpdate(ContestLog contestLog, Int32 progress);
        public event ProgressUpdate OnProgressUpdate;
        /// <summary>
        /// 
        /// </summary>
        public ScoreHQP()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logList"></param>
        public void ScoreContestLogs(List<ContestLog> logList)
        {
            Int32 progress = 0;

            List<ContestLog> contestLogList = logList.OrderByDescending(o => o.LogOwner).ToList();

            foreach (ContestLog contestLog in contestLogList)
            {
                progress++;

                if (!contestLog.IsCheckLog && contestLog.IsValidLog)
                {
                    // I don't think this is necessary right now
                    // not unless I have multiple reject reasons
                    //ValidateDuplicates(contestLog);

                    // first separate Hawaii and Non Hawaii
                    switch (contestLog.IsHQPEntity)
                    {
                        case true:
                            ScoreHPQContestant(contestLog);
                            break;
                        default:
                            ScoreNonHQPContestant(contestLog);
                            break;
                    }

                    OnProgressUpdate?.Invoke(contestLog, progress);
                }

            }
        }

        /// <summary>
        /// Score the valid QSOs for a Non Hawaii contestant.
        /// </summary>
        /// <param name="contestLog"></param>
        private void ScoreNonHQPContestant(ContestLog contestLog)
        {
            int pointTotal = contestLog.QSOCollection.Where(x => x.Status == QSOStatus.ValidQSO || x.Status == QSOStatus.ReviewQSO).Sum(item => item.HQPPoints);
            
            int multipliers = contestLog.QSOCollection
                .Select(x => new { x.Band, x.ContactEntity, x.Status })
                .Where(x => x.Status == QSOStatus.ValidQSO || x.Status == QSOStatus.ReviewQSO)
                .Distinct().Count();

            contestLog.TotalPoints = pointTotal;
            contestLog.Multipliers = multipliers;
            contestLog.ActualScore = pointTotal * multipliers;

            Console.WriteLine(contestLog.LogOwner + " - QSOs: " + contestLog.QSOCollection.Count.ToString() + " Points:" + pointTotal.ToString() + " Mults:" + multipliers.ToString() + " Log Total: " + contestLog.TotalPoints.ToString());
        }

        /// <summary>
        /// Score the valid QSOs for a Hawaii contestant.
        /// </summary>
        /// <param name="contestLog"></param>
        private void ScoreHPQContestant(ContestLog contestLog)
        {
            int pointTotal = contestLog.QSOCollection.Where(x => x.Status == QSOStatus.ValidQSO || x.Status == QSOStatus.ReviewQSO).Sum(item => item.HQPPoints);

            int multipliers = contestLog.QSOCollection
              .Select(x => new { x.ContactEntity, x.Status })
              .Where(x => x.Status == QSOStatus.ValidQSO || x.Status == QSOStatus.ReviewQSO)
              .Distinct().Count();

            contestLog.TotalPoints = pointTotal;
            contestLog.Multipliers = multipliers;
            contestLog.ActualScore = pointTotal * multipliers;

            Console.WriteLine(contestLog.LogOwner + " - QSOs: " + contestLog.QSOCollection.Count.ToString() + " Points:" + pointTotal.ToString() + " Mults:" + multipliers.ToString() + " Log Total: " + contestLog.TotalPoints.ToString());
        }

    } // end class
}
