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
            int progress = 0;

            List<ContestLog> contestLogList = logList.OrderByDescending(o => o.LogOwner).ToList();

            foreach (ContestLog contestLog in contestLogList)
            {
                // clear so we can run scoring multiple times
                contestLog.Entities.Clear();
                contestLog.Multipliers = 0;
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
                            //ScoreHPQContestant(contestLog);
                            break;
                        default:
                            TotalMultipliersForNonHPQContestant(contestLog);
                            //ScoreHPQContestant(contestLog);
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

            var query = qsoList.Where(item => item.Status == QSOStatus.ValidQSO || item.Status == QSOStatus.ReviewQSO);

           // var query = qsoList.GroupBy(x => new { x.ContactEntity, x.ContactCountry, x.Status, x.IsXQSO })
           //.Where(g => g.Count() >= 1)
           //.Select(y => y.Key).Where(item => item.Status == QSOStatus.ValidQSO || item.Status == QSOStatus.ReviewQSO)
           //.ToList();


            //  var entities = qsoList
            // .GroupBy(x => new { x.ContactEntity, x.ContactCountry, x.Status, x.IsXQSO })
            // .Select(g => new
            // {
            //     entity = g.Key,
            //     qsos = g.Select(c => c).Where(item => item.Status == QSOStatus.ValidQSO || item.Status == QSOStatus.ReviewQSO)
            // });


            foreach (var qso in query)
            {
                    if (qso.ContactEntity == "XX")
                    {
                        var a = 1;
                    }

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
                    if (Enum.IsDefined(typeof(HQPMults), entity))
                    {
                        if (!contestLog.Entities.Contains(entity))
                        {
                            if (!qso.IsXQSO)
                            {
                                // if not in hashset, add it
                                contestLog.Entities.Add(entity);
                                // now set the first one as a multiplier
                                multiList.First().IsMultiplier = true;
                                // for debugging
                                contestLog.HQPMultipliers += 1;
                            }
                        }
                    }
                    else
                    {
                        if (!contestLog.Entities.Contains(entity))
                        {
                            if (!qso.IsXQSO)
                            {
                                // if not in hashset, add it
                                contestLog.Entities.Add(entity);
                                // now set the first one as a multiplier
                                multiList.First().IsMultiplier = true;
                                // for debugging
                                contestLog.NonHQPMultipliers += 1;
                            }
                        }
                    }
                }
            }

            // a little bit of a hack - we weren't allowing Hawaii stations to get a multiplier for
            // the first time they worked a Hawaiin station
            if (contestLog.HQPMultipliers > 0)
            {
                contestLog.HQPMultipliers += 1;
            }

            // need to have ContestLog object manage this
            contestLog.Multipliers = contestLog.NonHQPMultipliers + contestLog.HQPMultipliers;
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
                        entity = qso.ContactEntity + " - " + qso.Band + "m";

                        if (!contestLog.Entities.Contains(entity))
                        {
                            // if not in hashset, add it
                            contestLog.Entities.Add(entity);
                            // now set the first one as a multiplier
                            multiList.First().IsMultiplier = true;
                            // for debugging
                            contestLog.HQPMultipliers += 1;

                            if (contestLog.HQPMultipliers > 84)
                            {
                                contestLog.HQPMultipliers = 84;
                            }
                        }
                    }
                }
            }

            // need to have ContestLog object manage this
            contestLog.Multipliers = contestLog.HQPMultipliers;
        }

        /// <summary>
        /// Look through all the qsos and total up all the points.
        /// </summary>
        /// <param name="contestLog"></param>
        //private void TotalHQPoints(ContestLog contestLog)
        //{
        //    List<QSO> query = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO).ToList();

        //    foreach (var qso in query)
        //    {
        //        contestLog.TotalPoints += qso.HQPPoints;
        //    }
        //}

        /// <summary>
        /// HQP only. Find all the entities. For each qso see if it is valid.
        /// If it is a valid call set it as a multiplier.
        /// </summary>
        /// <param name="qsoList"></param>
        //private void MarkMultipliersHQP(ContestLog contestLog)
        //{
        //    List<QSO> qsoList = contestLog.QSOCollection;
        //    List<QSO> multiList = new List<QSO>();
        //    string entity = "";

        //    contestLog.Multipliers = 0;
        //    contestLog.HQPMultipliers = 0;
        //    contestLog.NonHQPMultipliers = 0;
        //    //contestLog.TotalPoints = 0;

        //    var query = qsoList.GroupBy(x => new { x.ContactEntity, x.ContactCountry, x.Status })
        //    .Where(g => g.Count() >= 1)
        //    .Select(y => y.Key).Where(item => (item.Status == QSOStatus.ValidQSO || item.Status == QSOStatus.ReviewQSO))
        //    .ToList();


        //    foreach (var qso in query)
        //    {
        //        if (qso.ContactEntity != "DX")
        //        {
        //            multiList = qsoList.Where(item => item.ContactEntity == qso.ContactEntity && (item.Status == QSOStatus.ValidQSO || item.Status == QSOStatus.ReviewQSO)).ToList();
        //            entity = qso.ContactEntity;
        //        }
        //        else
        //        {
        //            multiList = qsoList.Where(item => item.ContactCountry == qso.ContactCountry && (item.Status == QSOStatus.ValidQSO || item.Status == QSOStatus.ReviewQSO)).ToList();
        //            entity = qso.ContactCountry;
        //        }

        //        if (multiList.Any())
        //        {
        //            if (Enum.IsDefined(typeof(HQPMults), entity))
        //            {
        //                if (!contestLog.Entities.Contains(entity))
        //                {
        //                    // if not in hashset, add it
        //                    contestLog.Entities.Add(entity);
        //                    // now set the first one as a multiplier
        //                    multiList.First().IsMultiplier = true;
        //                    // for debugging
        //                    contestLog.HQPMultipliers += 1;
        //                }
        //            }
        //            else
        //            {
        //                if (!contestLog.Entities.Contains(entity))
        //                {
        //                    // if not in hashset, add it
        //                    contestLog.Entities.Add(entity);
        //                    // now set the first one as a multiplier
        //                    multiList.First().IsMultiplier = true;
        //                    // for debugging
        //                    contestLog.NonHQPMultipliers += 1;
        //                }
        //            }
        //        }
        //    }

        //    // a little bit of a hack - we weren't allowing Hawaii stations to get a multiplier for
        //    // the first time they worked a Hawaiin station
        //    if (contestLog.HQPMultipliers > 0)
        //    {
        //        contestLog.HQPMultipliers += 1;
        //    }

        //    contestLog.Multipliers = contestLog.NonHQPMultipliers + contestLog.HQPMultipliers;
        //    //contestLog.ActualScore = contestLog.Multipliers * contestLog.TotalPoints;
        //}

        /// <summary>
        /// Score the valid QSOs for a Non Hawaii contestant.
        /// </summary>
        /// <param name="contestLog"></param>
        //private void ScoreNonHQPContestant(ContestLog contestLog)
        //{
        //    int pointTotal = contestLog.QSOCollection.Where(x => x.Status == QSOStatus.ValidQSO || x.Status == QSOStatus.ReviewQSO).Sum(item => item.HQPPoints);
        //    int multipliers = contestLog.HQPMultipliers + contestLog.NonHQPMultipliers;

        //    contestLog.TotalPoints = pointTotal;
        //    contestLog.Multipliers = multipliers;
        //    contestLog.ActualScore = pointTotal * multipliers;
        //}

        /// <summary>
        /// Score the valid QSOs for a Hawaii contestant.
        /// </summary>
        /// <param name="contestLog"></param>
        private void ScoreContestant(ContestLog contestLog)
        {
            int pointTotal = contestLog.QSOCollection.Where(x => x.Status == QSOStatus.ValidQSO || x.Status == QSOStatus.ReviewQSO).Sum(item => item.HQPPoints);
            int multipliers = contestLog.HQPMultipliers + contestLog.NonHQPMultipliers;

            contestLog.TotalPoints = pointTotal;
            contestLog.Multipliers = multipliers;
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

            // use IEnumerable and get count directlt without ToList()
            List<QSO> phoneTotal = query.Where(q => q.Mode == "PH").ToList();
            List<QSO> cwTotal = query.Where(q => q.Mode == "CW").ToList();
            List<QSO> digiTotal = query.Where(q => q.Mode != "PH" && q.Mode != "CW").ToList();

            contestLog.PhoneTotal = phoneTotal.Count;
            contestLog.CWTotal = cwTotal.Count;
            contestLog.DIGITotal = digiTotal.Count;
        }

        /// <summary>
        /// Multipliers are awarded for working each Hawai‘i Multiplier on each band, regardless 
        /// of mode.The maximum possible is 84 (6 Bands × 14 Hawai‘i Multipliers = 84).
        /// </summary>
        /// <param name="contestLog"></param>
        //private void MarkMultipliersNonHQP(ContestLog contestLog)
        //{
        //    List<QSO> qsoList = contestLog.QSOCollection;
        //    string consolidated = null;

        //    // target is HPQ
        //    var query = qsoList.GroupBy(x => new { x.ContactEntity, x.Status, x.Band })
        //     .Where(g => g.Count() >= 1)
        //     .Select(y => y.Key).Where(item => (item.Status == QSOStatus.ValidQSO || item.Status == QSOStatus.ReviewQSO))
        //     .ToList();

        //    foreach (var qso in query)
        //    {
        //        List<QSO> multiList = qsoList.Where(item => item.Band == qso.Band && (item.Status == QSOStatus.ValidQSO || item.Status == QSOStatus.ReviewQSO)).ToList();
        //        if (multiList.Any())
        //        {
        //            if (Enum.IsDefined(typeof(HQPMults), qso.ContactEntity))
        //            {
        //                // key for hashset
        //                consolidated = qso.ContactEntity + " - " + qso.Band + "m";
        //                if (!contestLog.Entities.Contains(consolidated))
        //                {
        //                    // if not in hashset, add it
        //                    contestLog.Entities.Add(consolidated);
        //                    // now set the first one as a multiplier
        //                    multiList.First().IsMultiplier = true;
        //                    // for debugging
        //                    contestLog.HQPMultipliers += 1;

        //                    if (contestLog.HQPMultipliers > 84)
        //                    {
        //                        contestLog.HQPMultipliers = 84;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    contestLog.Multipliers = contestLog.HQPMultipliers;
        //}

        /// <summary>
        /// Look at the matching logs first.
        /// Get the number of unique calls in the log. These are multipliers
        /// Really need to find the first instance of any call, not just uniques
        /// </summary>
        /// <returns></returns>
        //private void CalculateScore(ContestLog contestLog)
        //{
        //    // Int32 multiplierCount = 0;

        //    //multiplierCount = contestLog.QSOCollection.Where(q => q.IsMultiplier == true).ToList().Count();

        //    //contestLog.Multipliers = multiplierCount;
        //    if (contestLog.Multipliers != 0)
        //    {
        //        if (!contestLog.IsHQPEntity)
        //        {
        //            if (contestLog.Multipliers > 84)
        //            {
        //                contestLog.Multipliers = 84;
        //            }
        //        }
        //        contestLog.ActualScore = contestLog.Multipliers * contestLog.TotalPoints;
        //    }
        //}

    } // end class
}
