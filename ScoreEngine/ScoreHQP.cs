using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace W6OP.ContestLogAnalyzer
{
    public class ScoreHQP
    {
        public delegate void ProgressUpdate(ContestLog contestLog, int progress);
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
            int progress = 0;

            List<ContestLog> contestLogList = logList.OrderByDescending(o => o.LogOwner).ToList();

            foreach (ContestLog contestLog in contestLogList)
            {
                // clear so we can run scoring multiple times
                contestLog.Entities.Clear();
                contestLog.HQPTotalMultipliers = 0;
                contestLog.HQPMultipliers = 0;
                contestLog.NonHQPMultipliers = 0;
                contestLog.TotalPoints = 0;
                contestLog.ActualScore = 0;

                progress++;

                if (!contestLog.IsCheckLog && contestLog.IsValidLog)
                {
                    // first separate Hawaii and Non Hawaii
                    switch (contestLog.IsHQPEntity)
                    {
                        case true:
                            TotalMultipliersForHPQContestant(contestLog);
                            break;
                        default:
                            TotalMultipliersForNonHPQContestant(contestLog);
                            break;
                    }

                    ScoreContestant(contestLog);

                    OnProgressUpdate?.Invoke(contestLog, progress);
                }

                SetReportProperties(contestLog);
            }
        }

        /// <summary>
        /// Find all the entities. For each qso see if it is valid.
        /// If it is a valid call set it as a multiplier.
        /// </summary>
        /// <param name="contestLog"></param>
        private void TotalMultipliersForHPQContestant(ContestLog contestLog)
        {
            List<QSO> qsoList = contestLog.QSOCollection;
            List<QSO> multiList;
            string entity;
            string contactCallSign;
            string band;
            string mode;

            var query = qsoList.Where(item => item.Status == QSOStatus.ValidQSO || item.Status == QSOStatus.ReviewQSO);

            foreach (var qso in query)
            {
                contactCallSign = qso.ContactCall;
                band = qso.Band.ToString();
                mode = qso.OriginalMode;

                if (qso.ContactEntity == "DX")
                {
                    multiList = qsoList.Where(item => item.ContactCountry == qso.ContactCountry && (item.Status == QSOStatus.ValidQSO || item.Status == QSOStatus.ReviewQSO)).ToList();
                    entity = qso.ContactCountry;
                }
                else
                {
                    multiList = qsoList.Where(item => item.ContactEntity == qso.ContactEntity && (item.Status == QSOStatus.ValidQSO || item.Status == QSOStatus.ReviewQSO)).ToList();
                    entity = qso.ContactEntity;
                }

                if (multiList.Any())
                {
                    if (!contestLog.Entities.Contains(entity))
                    {
                        if (!qso.IsXQSO)
                        {
                            contestLog.Entities.Add(entity);
                            contestLog.EntitiesList[entity] = contactCallSign + "\t" + entity + " -- " + band + "m " + mode;
                            // now set the first one as a multiplier
                            multiList.First().IsMultiplier = true;
                            // for debugging
                            if (Enum.IsDefined(typeof(HQPMults), entity))
                            {
                                contestLog.HQPMultipliers += 1;
                            }
                            else
                            {
                                contestLog.NonHQPMultipliers += 1;
                            }
                        }
                    }
                }
            }

            // a little bit of a hack - we weren't allowing Hawaii stations to get a multiplier for
            // the first time they worked a Hawaiin station - this may be accounted for above ???
            if (contestLog.HQPMultipliers > 0)
            {
                contestLog.HQPMultipliers += 1;
            }
        }

        /// <summary>
        /// Multipliers are awarded for working each Hawai‘i Multiplier on each band, regardless 
        /// of mode.The maximum possible is 84 (6 Bands × 14 Hawai‘i Multipliers = 84).
        /// </summary>
        /// <param name="contestLog"></param>
        private void TotalMultipliersForNonHPQContestant(ContestLog contestLog)
        {
            List<QSO> qsoList = contestLog.QSOCollection;
            List<QSO> multiList;
            string entity;

            var query = qsoList.GroupBy(x => new { x.ContactEntity, x.Status, x.Band })
             .Where(g => g.Count() >= 1)
             .Select(y => y.Key).Where(item => (item.Status == QSOStatus.ValidQSO || item.Status == QSOStatus.ReviewQSO))
             .ToList();

            foreach (var qso in query)
            {
                multiList = qsoList.Where(item => item.Band == qso.Band && (item.Status == QSOStatus.ValidQSO || item.Status == QSOStatus.ReviewQSO)).ToList();

                if (multiList.Any())
                {
                    if (Enum.IsDefined(typeof(HQPMults), qso.ContactEntity))
                    {
                        entity = qso.ContactEntity;

                        if (!contestLog.Entities.Contains(entity))
                        {
                            // if not in hashset, add it
                            contestLog.Entities.Add(entity);
                            contestLog.EntitiesList[entity] = entity + " -- " + qso.Band.ToString() + "m ";
                            // now set the first one as a multiplier
                            multiList.First().IsMultiplier = true;
                            contestLog.HQPMultipliers += 1;

                            if (contestLog.HQPMultipliers > 84)
                            {
                                contestLog.HQPTotalMultipliers = 84;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Score the valid QSOs for a Hawaii contestant.
        /// </summary>
        /// <param name="contestLog"></param>
        private void ScoreContestant(ContestLog contestLog)
        {
            int pointTotal = contestLog.QSOCollection.Where(x => x.Status == QSOStatus.ValidQSO || x.Status == QSOStatus.ReviewQSO).Sum(item => item.HQPPoints);
            int multipliers = contestLog.HQPMultipliers + contestLog.NonHQPMultipliers;

            contestLog.TotalPoints = pointTotal;
            contestLog.HQPTotalMultipliers = multipliers;
            contestLog.ActualScore = pointTotal * multipliers;
        }

        /// <summary>
        /// Set the necessary report proerties for the CSV file
        /// PhoneTotal, CWTotal, DIGITotal
        /// 
        /// Later change this to use CategoryMode Enum
        /// </summary>
        /// <param name="contestLog"></param>
        private void SetReportProperties(ContestLog contestLog)
        {
            List<QSO> query = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO).ToList();

            // use IEnumerable and get count directly without ToList()
            List<QSO> phoneTotal = query.Where(q => q.Mode == QSOMode.PH).ToList();
            List<QSO> cwTotal = query.Where(q => q.Mode == QSOMode.CW).ToList();
            List<QSO> digiTotal = query.Where(q => q.Mode == QSOMode.RY).ToList();

            contestLog.PhoneTotal = phoneTotal.Count;
            contestLog.CWTotal = cwTotal.Count;
            contestLog.DIGITotal = digiTotal.Count;
        }

    } // end class
}
