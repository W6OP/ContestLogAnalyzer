﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

                    if (contestLog.IsHQPEntity)
                    {
                        // everthing is a multiplier
                        MarkMultipliersHQP(contestLog);
                    }
                    else
                    {
                        // one per band/mode (total 3 per band)
                        MarkMultipliersNonHQP(contestLog);
                    }

                    TotalHQPoints(contestLog);

                    CalculateScore(contestLog);

                    OnProgressUpdate?.Invoke(contestLog, progress);
                }

            }
        }

        /// <summary>
        /// Look through all the qsos and total up all the points.
        /// </summary>
        /// <param name="contestLog"></param>
        private void TotalHQPoints(ContestLog contestLog)
        {
            //Int32 totalPhoneQSOS = 0;
            //Int32 totalCWQSOs = 0;
            //Int32 totalDigiQSOS = 0;
            //Int32 totalPoints = 0;

            //totalPhoneQSOS = contestLog.QSOCollection.Where(q => (q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO) && q.Mode == "PH").ToList().Count();
            //totalCWQSOs = contestLog.QSOCollection.Where(q => (q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO) && q.Mode == "CW").ToList().Count();
            //totalDigiQSOS = contestLog.QSOCollection.Where(q => (q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO) && q.Mode == "RY").ToList().Count();

            //totalPoints = (totalPhoneQSOS * 2) + (totalCWQSOs * 3) + (totalDigiQSOS * 3);

            List<QSO> query = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO).ToList();

            foreach (var qso in query)
            {
                contestLog.TotalPoints += qso.HQPPoints;
            }
        }

        /// <summary>
        /// Make sure a guy doesn't get hit twice for the same bad QSO - count the good dupe and not the bad dupe
        /// QSO: 	14028	CW	2015-09-05	1211	AA3B	25	BUD	I5EFO	3	EMIL	The received serial number is incorrect - 3 --> 15
        /// QSO: 	14000	CW	2015-09-05	1313	I5EFO	15	EMIL AA3B	88	BUD
        /// 
        /// QSO: 	14033	CW	2015-09-05	1313	AA3B	145	BUD	I5EFO	15	EMIL	This is a duplicate QSO
        /// QSO: 	14000	CW	2015-09-05	1313	I5EFO	15	EMIL AA3B	88	BUD
        /// 
        /// MOVE THIS - should be right after we check for dupes in loganalyser.cs ???
        /// Each station can be worked three times per band using each mode - SSB, CW and Digital
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
                                                        q.Mode == q.MatchingQSO.Mode &&
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
        /// Find all the entities. For each qso see if it is valid.
        /// If it is a valid call set it as a multiplier.
        /// 
        /// </summary>
        /// <param name="qsoList"></param>
        private void MarkMultipliersHQP(ContestLog contestLog)
        {
            List<QSO> qsoList = contestLog.QSOCollection;
            List<QSO> multiList = new List<QSO>();
           // HashSet<string> entities = new HashSet<string>();
            string entity = "";

            contestLog.Multipliers = 0;
            contestLog.HQPMultipliers = 0;
            contestLog.NonHQPMultipliers = 0;
            contestLog.TotalPoints = 0;

            var query = qsoList.GroupBy(x => new { x.DXEntity, x.RealDXEntity, x.Status })
             .Where(g => g.Count() >= 1)
             .Select(y => y.Key)
             .ToList();

           //int multiplierCount = contestLog.QSOCollection.Where(q => q.IsMultiplier == true && (q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO)).ToList().Count();
           
            //List<QSO> qsoList2 = contestLog.QSOCollection.Where(q => q.IsMultiplier == true && (q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO)).ToList();

            foreach (var qso in query)
            {
                if (qso.DXEntity != "DX")
                {
                    multiList = qsoList.Where(item => item.DXEntity == qso.DXEntity && (item.Status == QSOStatus.ValidQSO || item.Status == QSOStatus.ReviewQSO)).ToList();
                    entity = qso.DXEntity;
                }
                else
                {
                    multiList = qsoList.Where(item => item.RealDXEntity == qso.RealDXEntity && (item.Status == QSOStatus.ValidQSO || item.Status == QSOStatus.ReviewQSO)).ToList();
                    entity = qso.RealDXEntity;
                }

                if (multiList.Any())
                {
                    if (Enum.IsDefined(typeof(HQPMults), entity))
                    {
                        if (!contestLog.Entities.Contains(entity))
                        {
                            // if not in hashset, add it
                            contestLog.Entities.Add(entity);
                            // now set the first one as a multiplier
                            multiList.First().IsMultiplier = true;
                            // for debugging
                            contestLog.HQPMultipliers += 1;
                        }
                    }
                    else
                    {
                        if (!contestLog.Entities.Contains(entity))
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

            contestLog.Multipliers = contestLog.NonHQPMultipliers + contestLog.HQPMultipliers;

            if (contestLog.Entities.Count != (contestLog.HQPMultipliers + contestLog.NonHQPMultipliers))
            {
                var b = 1;
            }
           
        }

        /// <summary>
        /// Multipliers are awarded for working each Hawai‘i Multiplier on each band, regardless 
        /// of mode.The maximum possible is 84 (6 Bands × 14 Hawai‘i Multipliers = 84).
        /// </summary>
        /// <param name="contestLog"></param>
        private void MarkMultipliersNonHQP(ContestLog contestLog)
        {
            List<QSO> qsoList = contestLog.QSOCollection;
           // HashSet<string> entities = new HashSet<string>();
            string consolidated = null;

            // target is HPQ
            var query = qsoList.GroupBy(x => new { x.DXEntity, x.Status, x.Band })
             .Where(g => g.Count() >= 1)
             .Select(y => y.Key)
             .ToList();

            foreach (var qso in query)
            {
                List<QSO> multiList = qsoList.Where(item => (item.Status == QSOStatus.ValidQSO || item.Status == QSOStatus.ReviewQSO) && item.Band == qso.Band).ToList();
                if (multiList.Any())
                {
                    if (Enum.IsDefined(typeof(HQPMults), qso.DXEntity))
                    {
                        // key for hashset
                        consolidated = qso.DXEntity + " - " + qso.Band + "m";
                        if (!contestLog.Entities.Contains(consolidated))
                        {
                            // if not in hashset, add it
                            contestLog.Entities.Add(consolidated);
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

                //contestLog.Multipliers = contestLog.HQPMultipliers;
            }
            contestLog.Multipliers = contestLog.HQPMultipliers;
        }

        /// <summary>
        /// Look at the matching logs first.
        /// Get the number of unique calls in the log. These are multipliers
        /// Really need to find the first instance of any call, not just uniques
        /// </summary>
        /// <returns></returns>
        private void CalculateScore(ContestLog contestLog)
        {
           // Int32 multiplierCount = 0;

            //multiplierCount = contestLog.QSOCollection.Where(q => q.IsMultiplier == true).ToList().Count();

            //contestLog.Multipliers = multiplierCount;
            if (contestLog.Multipliers != 0)
            {
                if (!contestLog.IsHQPEntity)
                {
                    if (contestLog.Multipliers > 84)
                    {
                        contestLog.Multipliers = 84;
                    }
                }
                contestLog.ActualScore = contestLog.Multipliers * contestLog.TotalPoints;
            }
        }
    } // end class
}
