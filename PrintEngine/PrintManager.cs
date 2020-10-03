//using PdfFileWriter;
using CsvHelper;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
//using iTextSharp.text;
//using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using W6OP.ContestLogAnalyzer;

namespace W6OP.PrintEngine
{
    public class PrintManager
    {
        public string WorkingFolder { get; set; }
        public string InspectionFolder { get; set; }
        public string ReportFolder { get; set; }
        public string LogFolder { get; set; }
        public string ScoreFolder { get; set; }
        public string ReviewFolder { get; set; }
        private string ContestDescription { get; set; }
        private ContestName ActiveContest { get; set; }

        private List<Tuple<string, int, string, int>> _CallNameCountList;

        /// <summary>
        /// Constructor
        /// </summary>
        public PrintManager(ContestName contestName)
        {
            ActiveContest = contestName;

            switch (ActiveContest)
            {
                case ContestName.CW_OPEN:
                    ContestDescription = " CWO Box Scores Session ";
                    break;
                case ContestName.HQP:
                    ContestDescription = " Hawaii QSO Party ";
                    break;
            }
        }

        #region Print Scores

        /// <summary>
        /// Print the final score CSV file for the CWOpen.
        /// </summary>
        /// <param name="contestLogs"></param>
        public void PrintCWOpenCsvFile(List<ContestLog> contestLogs)
        {
            CWOpenScoreList scoreList;
            List<CWOpenScoreList> scores = new List<CWOpenScoreList>();
            ContestLog contestlog;
            List<QSO> validQsoList;
            string assisted = null;
            string reportFileName = null;
            string fileName = null;
            string session = null;
            string year = DateTime.Now.ToString("yyyy");

            if (contestLogs[0].Session == 0)
            {
                session = "";
            }
            else
            {
                session = contestLogs[0].Session.ToString();
            }

            fileName = year + ContestDescription + session + ".csv";
            reportFileName = Path.Combine(ScoreFolder, fileName);

            contestLogs = contestLogs.OrderByDescending(o => (int)o.ActualScore).ToList();

            for (int i = 0; i < contestLogs.Count; i++)
            {
                contestlog = contestLogs[i];
                if (contestlog != null)
                {
                    assisted = "N";

                    // only look at valid QSOs
                    validQsoList = contestlog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO).ToList();

                    if (contestlog.LogHeader.Assisted == CategoryAssisted.Assisted)
                    {
                        assisted = "Y";
                    }

                    if (contestlog.SO2R == true)
                    {
                        //so2r = "Y";
                    }

                    scoreList = new CWOpenScoreList
                    {
                        LogOwner = contestlog.LogOwner,
                        Operator = contestlog.Operator,
                        Station = contestlog.Station,
                        OperatorName = contestlog.OperatorName,
                        QSOCount = validQsoList.Count.ToString(),
                        Multipliers = contestlog.Multipliers.ToString(),
                        ActualScore = contestlog.ActualScore.ToString(),
                        Power = contestlog.LogHeader.Power.ToString(),
                        Assisted = assisted
                    };

                    scores.Add(scoreList);
                }
            }

            using (var writer = new StreamWriter(reportFileName))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(scores);
            }
        }

        /// <summary>
        /// Print the final score CSV file for the HQP.
        /// LogOwner,Operator,Station,Entity,QSOCount,Multipliers,Points,Score
        /// KH6TU,KH6TU,KH6TU,MAU,1586,139,0,522858
        /// AH6KO,AH6KO,AH6KO,HIL,733,65,0,202308
        /// 
        /// LogOwner,Operator,Station,Entity,QSOCount,Multipliers,Points,Score
        /// Call QTH PH CW  DG QSOs    HI mults    Score
        /// </summary>
        /// <param name="contestLogs"></param>
        public void PrintHQPCsvFileEx(List<ContestLog> contestLogs)
        {
            HQPScoreList scoreList;
            List<HQPScoreList> scores = new List<HQPScoreList>();
            ContestLog contestlog;
            List<QSO> validQsoList;
            string reportFileName = null;
            string fileName = null;
            string year = DateTime.Now.ToString("yyyy");

            string qth;

            fileName = year + ContestDescription + ".csv";
            reportFileName = Path.Combine(ScoreFolder, fileName);

            contestLogs = contestLogs.OrderByDescending(o => (int)o.ActualScore).ToList();

            for (int i = 0; i < contestLogs.Count; i++)
            {
                contestlog = contestLogs[i];

                if (contestlog.LogHeader.QTH != "")
                {
                    qth = contestlog.LogHeader.QTH;
                } else
                {
                    qth = contestlog.LogHeader.Country;
                }

                    if (contestlog != null)
                {
                    // only look at valid QSOs
                    validQsoList = contestlog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO).ToList();

                    scoreList = new HQPScoreList
                    {
                        Call = contestlog.LogOwner,
                        QTH = qth, // contestlog.LogHeader.QTH, 
                        PH = contestlog.PhoneTotal.ToString(), // Phone total
                        CW = contestlog.CWTotal.ToString(), // CW total
                        DG = contestlog.DIGITotal.ToString(), // DIGI total
                        QSOs = validQsoList.Count.ToString(),
                        Mults = contestlog.Multipliers.ToString(),
                        Score = contestlog.ActualScore.ToString(),
                    };

                    scores.Add(scoreList);
                }
            }

            using (var writer = new StreamWriter(reportFileName))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(scores);
            }
        }

        /// <summary>
        /// Print the final score CSV file for the HQP.
        /// /// LogOwner,Operator,Station,Entity,QSOCount,Multipliers,Points,Score
        /// KH6TU,KH6TU,KH6TU,MAU,1586,139,0,522858
        /// AH6KO,AH6KO,AH6KO,HIL,733,65,0,202308
        /// 
        /// 
        /// Call QTH PH CW  DG QSOs    HI mults    Score
        /// </summary>
        /// <param name="contestLogs"></param>
        //public void PrintHQPCsvFile(List<ContestLog> contestLogs)
        //{
        //    HQPScoreList scoreList;
        //    List<HQPScoreList> scores = new List<HQPScoreList>();
        //    ContestLog contestlog;
        //    List<QSO> validQsoList;
        //    string reportFileName = null;
        //    string fileName = null;
        //    string year = DateTime.Now.ToString("yyyy");

        //    fileName = year + ContestDescription + ".csv";
        //    reportFileName = Path.Combine(ScoreFolder, fileName);

        //    contestLogs = contestLogs.OrderByDescending(o => (int)o.ActualScore).ToList();

        //    for (int i = 0; i < contestLogs.Count; i++)
        //    {
        //        contestlog = contestLogs[i];
        //        if (contestlog != null)
        //        {
                   
        //            // only look at valid QSOs
        //            validQsoList = contestlog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO).ToList();

        //            scoreList = new HQPScoreList
        //            {
        //                LogOwner = contestlog.LogOwner,
        //                Operator = contestlog.Operator,
        //                Station = contestlog.Station,
        //                Entity = contestlog.QSOCollection[0].OperatorEntity,
        //                QSOCount = validQsoList.Count.ToString(),
        //                Multipliers = contestlog.Multipliers.ToString(),
        //                Points = contestlog.TotalPoints.ToString(),
        //                Score = contestlog.ActualScore.ToString(),
        //            };

        //            scores.Add(scoreList);
        //        }
        //    }

        //    using (var writer = new StreamWriter(reportFileName))
        //    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        //    {
        //        csv.WriteRecords(scores);
        //    }
        //}

        #endregion

        #region Print Reject Report

        /// <summary>
        /// Print a report for each log listing the QSOs rejected and the reason for rejection.
        /// 
        /// CWO log checking results for 4K9W
        /// 
        /// Final: Valid QSOs:  2  Mults:  2  Score:  4 
        ///Category: LOW Checked:  2 
        /// </summary>
        /// <param name="contestLog"></param>
        public void PrintLogSummaryReport(ContestLog contestLog, string callsign)
        {
            string reportFileName = null;
            string message = null;
            var value = "";
            int session = contestLog.Session;

            // strip "/" from callsign
            if (callsign.IndexOf(@"/") != -1)
            {
                callsign = callsign.Substring(0, callsign.IndexOf(@"/"));
            }

            reportFileName = Path.Combine(ReportFolder, callsign + ".rpt");

            // only look at invalid QSOs
            List<QSO> inValidQsoList = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.InvalidQSO).ToList();
            
            try
            {
                using (StreamWriter sw = File.CreateText(reportFileName))
                {
                    // maybe add contest year later
                    switch (ActiveContest)
                    {
                        case ContestName.CW_OPEN:
                            sw.WriteLine("CWOpen log checking results for " + callsign + " in session " + session.ToString());
                            break;
                        case ContestName.HQP:
                            sw.WriteLine("Hawaii QSO Party log checking results for " + callsign);
                            break;
                    }

                    sw.WriteLine("");

                    if (!string.IsNullOrEmpty(contestLog.LogHeader.SoapBox))
                    {
                        sw.WriteLine(contestLog.LogHeader.SoapBox);
                        sw.WriteLine("");
                    }

                    if (inValidQsoList.Count > 0)
                    {
                        foreach (QSO qso in inValidQsoList)
                        {
                            qso.HasBeenPrinted = false;
                        }

                        foreach (QSO qso in inValidQsoList)
                        {
                            //qso.HasBeenPrinted = false;
                            message = null;

                            if (qso.HasBeenPrinted)
                            {
                                continue;
                            }

                            if (qso.QSOHasDupes == false && qso.QSOIsDupe == false)
                            {
                                switch (ActiveContest)
                                {
                                    case ContestName.CW_OPEN:
                                        message = "QSO: " + "\t" + qso.Frequency + "\t" + qso.Mode + "\t" + qso.QsoDate + "\t" + qso.QsoTime + "\t" + qso.OperatorCall + "\t" + qso.SentSerialNumber.ToString() + "\t" +
                                                                       qso.OperatorName + "\t" + qso.ContactCall + "\t" + qso.ReceivedSerialNumber.ToString() + "\t" + qso.ContactName;
                                        break;
                                    case ContestName.HQP:
                                        message = "QSO: " + "\t" + qso.Frequency + "\t" + qso.Mode + "\t" + qso.QsoDate + "\t" + qso.QsoTime + "\t" + qso.OperatorCall + "\t" + qso.SentSerialNumber.ToString() + "\t" +
                                                                       qso.OperatorEntity + "\t" + qso.ContactCall + "\t" + qso.ReceivedSerialNumber.ToString() + "\t" + qso.ContactEntity;
                                        break;
                                }
                            }

                            switch (qso.ReasonRejected)
                            {
                                case RejectReason.OperatorName:
                                    if (qso.MatchingQSO != null)
                                    {
                                        value = EnumHelper.GetDescription(qso.ReasonRejected) + " - " + qso.IncorrectName + " --> " + qso.MatchingQSO.OperatorName;
                                    }
                                    else
                                    {
                                        value = EnumHelper.GetDescription(qso.ReasonRejected) + " - " + qso.IncorrectName;
                                    }
                                    break;
                                case RejectReason.Band:
                                    break;
                                case RejectReason.Mode:
                                    break;
                                case RejectReason.NoQSOMatch:
                                    break;
                                case RejectReason.NoQSO:
                                    break;
                                case RejectReason.BustedCallSign:
                                    if (qso.MatchingQSO != null)
                                    {
                                        value = EnumHelper.GetDescription(qso.ReasonRejected) + " - " + qso.ContactCall + " --> " + qso.MatchingQSO.OperatorCall;
                                    }
                                    else
                                    {
                                        value = EnumHelper.GetDescription(qso.ReasonRejected) + " - " + qso.ContactCall + " --> " + qso.BustedCallGuess;
                                        //value = qso.RejectReasons[key];
                                    }
                                    break;
                                case RejectReason.SerialNumber:
                                    if (qso.MatchingQSO != null)
                                    {
                                        value = EnumHelper.GetDescription(qso.ReasonRejected) + " - " + qso.ReceivedSerialNumber + " --> " + qso.MatchingQSO.SentSerialNumber;
                                    }
                                    else
                                    {
                                        value = EnumHelper.GetDescription(qso.ReasonRejected);
                                    }
                                    break;
                                case RejectReason.EntityName:
                                    if (qso.MatchingQSO != null)
                                    {
                                        value = EnumHelper.GetDescription(qso.ReasonRejected) + " - " + qso.IncorrectDXEntity + " --> " + qso.MatchingQSO.OperatorEntity;
                                    }
                                    else
                                    {
                                        value = EnumHelper.GetDescription(qso.ReasonRejected) + " - " + qso.IncorrectDXEntity;
                                    }
                                    break;
                                case RejectReason.DuplicateQSO:
                                    if (qso.HasBeenPrinted == false)
                                    {
                                        message = "Duplicates ignored for scoring purposes:";
                                        sw.WriteLine(message);

                                        PrintDuplicates(qso.DupeListLocation, sw);
                                        sw.WriteLine("");
                                    }

                                    message = null;
                                    break;
                                case RejectReason.InvalidCall:
                                    break;
                                case RejectReason.InvalidTime:
                                    break;
                                case RejectReason.InvalidSession:
                                    break;
                                case RejectReason.InvalidEntity:
                                    if (qso.MatchingQSO != null)
                                    {
                                        value = EnumHelper.GetDescription(qso.ReasonRejected) + " - " + qso.IncorrectDXEntity + " --> " + qso.MatchingQSO.OperatorEntity;
                                    }
                                    else
                                    {
                                        value = EnumHelper.GetDescription(qso.ReasonRejected) + " - " + qso.IncorrectDXEntity;
                                    }
                                    break;
                                case RejectReason.NotCounted:
                                    value = EnumHelper.GetDescription(qso.ReasonRejected) + " - " + "Non Hawaiian Station";
                                    break;
                                case RejectReason.Marked_XQSO:
                                    break;
                                case RejectReason.MissingColumn:
                                    break;
                                case RejectReason.None:
                                    break;
                                default:
                                    if (qso.ReasonRejected != RejectReason.None)
                                    {
                                        value = EnumHelper.GetDescription(qso.ReasonRejected);
                                    }
                                    break;
                            }



                            // should only be one reason so lets change the collection type
                            //foreach (var key in qso.GetRejectReasons().Keys)
                            //{
                            //    if (key == RejectReason.OperatorName)
                            //    {
                            //        //if (qso.MatchingQSO != null)
                            //        //{
                            //        //    value = qso.GetRejectReasons()[key] + " - " + qso.IncorrectName + " --> " + qso.MatchingQSO.OperatorName;
                            //        //}
                            //        //else
                            //        //{
                            //        //    value = qso.GetRejectReasons()[key] + " - " + qso.IncorrectName;
                            //        //}
                            //    }
                            //    else if (key == RejectReason.EntityName)
                            //    {
                            //        //if (qso.MatchingQSO != null)
                            //        //{
                            //        //    value = qso.GetRejectReasons()[key] + " - " + qso.IncorrectDXEntity + " --> " + qso.MatchingQSO.OperatorEntity;
                            //        //}
                            //        //else
                            //        //{
                            //        //    value = qso.GetRejectReasons()[key] + " - " + qso.IncorrectDXEntity;
                            //        //}
                            //    }
                            //    else if (key == RejectReason.InvalidEntity)
                            //    {
                            //        //if (qso.MatchingQSO != null)
                            //        //{
                            //        //    value = qso.GetRejectReasons()[key] + " - " + qso.IncorrectDXEntity + " --> " + qso.MatchingQSO.OperatorEntity;
                            //        //}
                            //        //else
                            //        //{
                            //        //    value = qso.GetRejectReasons()[key] + " - " + qso.IncorrectDXEntity;
                            //        //}
                            //    }
                            //    else if (key == RejectReason.SerialNumber)
                            //    {
                            //        //if (qso.MatchingQSO != null)
                            //        //{
                            //        //    value = qso.GetRejectReasons()[key] + " - " + qso.ReceivedSerialNumber + " --> " + qso.MatchingQSO.SentSerialNumber;
                            //        //}
                            //        //else
                            //        //{
                            //        //    value = qso.GetRejectReasons()[key];
                            //        //}
                            //    }
                            //    else if (key == RejectReason.NotCounted)
                            //    {
                            //        //value = qso.GetRejectReasons()[key] + " - " + "Non Hawaiian Station";
                            //        break;
                            //    }
                            //    else if (key == RejectReason.BustedCallSign || key == RejectReason.NoQSO)
                            //    {
                            //        //if (qso.MatchingQSO != null)
                            //        //{
                            //        //    value = qso.GetRejectReasons()[key] + " - " + qso.ContactCall + " --> " + qso.MatchingQSO.OperatorCall;
                            //        //}
                            //        //else
                            //        //{
                            //        //    value = qso.GetRejectReasons()[key] + " - " + qso.ContactCall + " --> " + qso.BustedCallGuess;
                            //        //    //value = qso.RejectReasons[key];
                            //        //}
                            //    }
                            //    else if (key == RejectReason.DuplicateQSO)
                            //    {
                            //        //message = null;

                            //        //if (qso.HasBeenPrinted == false)
                            //        //{
                            //        //    message = "Duplicates ignored for scoring purposes:";
                            //        //    sw.WriteLine(message);

                            //        //    message = null;

                            //        //    PrintDuplicates(qso.DupeListLocation, sw);
                            //        //    sw.WriteLine("");
                            //        //}
                            //    }
                            //    else
                            //    {
                            //        value = qso.GetRejectReasons()[key];
                            //    }
                            //}

                            if (message != null)
                            {
                                sw.WriteLine(value.ToString());
                                sw.WriteLine(message);

                                if (qso.MatchingQSO != null)
                                {
                                    switch (ActiveContest)
                                    {
                                        case ContestName.CW_OPEN:
                                            message = "QSO: " + "\t" + qso.MatchingQSO.Frequency + "\t" + qso.MatchingQSO.Mode + "\t" + qso.MatchingQSO.QsoDate + "\t" + qso.MatchingQSO.QsoTime + "\t" + qso.MatchingQSO.OperatorCall + "\t" + qso.MatchingQSO.SentSerialNumber.ToString() + "\t" +
                                          qso.MatchingQSO.OperatorName + "\t" + qso.MatchingQSO.ContactCall + "\t" + qso.MatchingQSO.ReceivedSerialNumber.ToString() + "\t" + qso.MatchingQSO.ContactName;
                                            break;
                                        case ContestName.HQP:
                                            message = "QSO: " + "\t" + qso.MatchingQSO.Frequency + "\t" + qso.MatchingQSO.Mode + "\t" + qso.MatchingQSO.QsoDate + "\t" + qso.MatchingQSO.QsoTime + "\t" + qso.MatchingQSO.OperatorCall + "\t" + qso.MatchingQSO.SentSerialNumber.ToString() + "\t" +
                                          qso.MatchingQSO.OperatorEntity + "\t" + qso.MatchingQSO.ContactCall + "\t" + qso.MatchingQSO.ReceivedSerialNumber.ToString() + "\t" + qso.MatchingQSO.ContactEntity;
                                            break;
                                    }

                                    sw.WriteLine(message);
                                }
                            }

                            sw.WriteLine("");
                        }
                    }
                    else
                    {
                        sw.WriteLine("Golden log with zero errors in session " + session.ToString() + ". Congratulations!");
                    }

                    AddFooter(contestLog, sw);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dupeListLocation"></param>
        /// <param name="sw"></param>
        private void PrintDuplicates(QSO dupeListLocation, StreamWriter sw)
        {
            string message = null;

            foreach (QSO item in dupeListLocation.DuplicateQsoList)
            {

                item.HasBeenPrinted = true;

                switch (ActiveContest)
                {
                    case ContestName.CW_OPEN:
                        message = "QSO: " + "\t" + item.Frequency + "\t" + item.Mode + "\t" + item.QsoDate + "\t" + item.QsoTime + "\t" + item.OperatorCall + "\t" + item.SentSerialNumber.ToString() + "\t" +
                       item.OperatorName + "\t" + item.ContactCall + "\t" + item.ReceivedSerialNumber.ToString() + "\t" + item.ContactName;
                        break;
                    case ContestName.HQP:
                        message = "QSO: " + "\t" + item.Frequency + "\t" + item.Mode + "\t" + item.QsoDate + "\t" + item.QsoTime + "\t" + item.OperatorCall + "\t" + item.SentSerialNumber.ToString() + "\t" +
                        item.OperatorEntity + "\t" + item.ContactCall + "\t" + item.ReceivedSerialNumber.ToString() + "\t" + item.ContactEntity;
                        break;
                }

                sw.WriteLine(message);
            }
        }


        /// <summary>
        /// Print a report for each log listing the QSOs rejected and the reason for rejection.
        /// 
        /// CWO log checking results for 4K9W
        /// 
        /// Final: Valid QSOs:  2  Mults:  2  Score:  4 
        ///Category: LOW Checked:  2 
        /// </summary>
        /// <param name="contestLog"></param>
        public void PrintReviewReport(List<ContestLog> contestLogs)
        {
            string reportFileName = null;
            string message = null;
            string callsign = null;
            //var value = "";
            int session = 1;

            reportFileName = Path.Combine(ReviewFolder, "Review.rpt");

            try
            {
                using (StreamWriter sw = File.CreateText(reportFileName))
                {
                    foreach (ContestLog contestLog in contestLogs)
                    {

                        session = contestLog.Session;
                        callsign = contestLog.LogOwner;

                        // strip "/" from callsign
                        if (callsign.IndexOf(@"/") != -1)
                        {
                            callsign = callsign.Substring(0, callsign.IndexOf(@"/"));
                        }

                        // only look at invalid QSOs
                        List<QSO> reviewQsoList = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.ReviewQSO).ToList();

                        if (reviewQsoList.Count > 0)
                        {

                            // maybe add contest year later
                            switch (ActiveContest)
                            {
                                case ContestName.CW_OPEN:
                                    sw.WriteLine("Log checking results for " + callsign + " in session " + session.ToString());
                                    break;
                                case ContestName.HQP:
                                    sw.WriteLine("Log checking results for " + callsign);
                                    break;
                            }
                            
                            sw.WriteLine("");

                            if (!string.IsNullOrEmpty(contestLog.LogHeader.SoapBox))
                            {
                                sw.WriteLine(contestLog.LogHeader.SoapBox);
                                sw.WriteLine("");
                            }

                            foreach (QSO qso in reviewQsoList)
                            {
                                switch (ActiveContest)
                                {
                                    case ContestName.CW_OPEN:
                                        message = "QSO: " + "\t" + qso.Frequency + "\t" + qso.Mode + "\t" + qso.QsoDate + "\t" + qso.QsoTime + "\t" + qso.OperatorCall + "\t" + qso.SentSerialNumber.ToString() + "\t" +
                                           qso.OperatorName + "\t" + qso.ContactCall + "\t" + qso.ReceivedSerialNumber.ToString() + "\t" + qso.ContactName;
                                        break;
                                    case ContestName.HQP:
                                        message = "QSO: " + "\t" + qso.Frequency + "\t" + qso.Mode + "\t" + qso.QsoDate + "\t" + qso.QsoTime + "\t" + qso.OperatorCall + "\t" + qso.SentSerialNumber.ToString() + "\t" +
                                           qso.OperatorEntity + "\t" + qso.ContactCall + "\t" + qso.ReceivedSerialNumber.ToString() + "\t" + qso.ContactEntity;
                                        break;
                                }

                                // should only be one reason so lets change the collection type
                                //foreach (var key in qso.GetRejectReasons().Keys)
                                //{
                                 //   value = EnumHelper.GetDescription(qso.ReasonRejected);
                                //}

                                sw.WriteLine(message + "\t" + EnumHelper.GetDescription(qso.ReasonRejected));
                                sw.WriteLine("");
                            }
                            sw.WriteLine("---------------------------------------");
                            sw.WriteLine("");
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Add the footer (summary) to the report
        /// </summary>
        /// <param name="contestLog"></param>
        /// <param name="sw"></param>
        /// <returns></returns>
        private void AddFooter(ContestLog contestLog, StreamWriter sw)
        {
            string message = "";
            int totalQSOs = 0;
            int totalValidQSOs = 0;
            int totalInvalidQSOs = 0;
            int totalPhoneQSOS = 0;
            int totalCWQSOs = 0;
            int totalDigiQSOS = 0;
            int totalValidPhoneQSOS = 0;
            int totalValidCWQSOs = 0;
            int totalValidDigiQSOS = 0;
            int multiplierCount = 0;
            int score = 0;
            int totalPoints;

            totalQSOs = contestLog.QSOCollection.Count;
            totalValidQSOs = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO).ToList().Count();
            totalInvalidQSOs = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.InvalidQSO).ToList().Count();

            totalPhoneQSOS = contestLog.QSOCollection.Where(q => q.Mode == "PH").ToList().Count();
            totalCWQSOs = contestLog.QSOCollection.Where(q => q.Mode == "CW").ToList().Count();
            totalDigiQSOS = contestLog.QSOCollection.Where(q => q.Mode == "RY").ToList().Count();

            totalValidPhoneQSOS = contestLog.QSOCollection.Where(q => (q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO) && q.Mode == "PH").ToList().Count();
            totalValidCWQSOs = contestLog.QSOCollection.Where(q => (q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO) && q.Mode == "CW").ToList().Count();
            totalValidDigiQSOS = contestLog.QSOCollection.Where(q => (q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO) && q.Mode == "RY").ToList().Count();

            multiplierCount = contestLog.Multipliers;
            totalPoints = contestLog.TotalPoints;

            // this should really be in Scoring but it doesn't work there
            score = multiplierCount * totalPoints; //contestLog.ActualScore;

            sw.WriteLine(message);

            if (contestLog.IsCheckLog)
            {
                sw.WriteLine("CHECKLOG::: --------------------------");
                sw.WriteLine(message);
            }
            switch (ActiveContest)
            {
                case ContestName.CW_OPEN:
                    message = string.Format(" Final:   Valid QSOs: {0}   Mults: {1}   Score: {2}", totalValidQSOs.ToString(), multiplierCount.ToString(), score.ToString());
                    sw.WriteLine(message);
                    break;
                case ContestName.HQP:
                    if (contestLog.IsHQPEntity)
                    {
                        message = string.Format(" Total QSOs: {0}   Valid QSOs: {1}  Invalid QSOs: {2}", totalQSOs.ToString(), totalValidQSOs.ToString(), totalInvalidQSOs.ToString());
                        sw.WriteLine(message);

                        message = string.Format(" Valid PH: {0}({1})  Valid CW: {2}({3})   Valid RY: {4}({5})", totalValidPhoneQSOS.ToString(), totalPhoneQSOS.ToString(), totalValidCWQSOs.ToString(), totalCWQSOs.ToString(), totalValidDigiQSOS.ToString(), totalDigiQSOS.ToString());
                        sw.WriteLine(message);

                        message = string.Format(" HQP Mults: {0}   NonHQP Mults: {1}   Total Mults: {2}", contestLog.HQPMultipliers.ToString(), contestLog.NonHQPMultipliers.ToString(), multiplierCount.ToString());
                        sw.WriteLine(message);

                        message = string.Format(" Points: {0}  Score: {1}", totalPoints.ToString(), score.ToString());
                        sw.WriteLine(message);
                    }
                    else
                    {
                        message = string.Format(" Final:   Valid QSOs: {0}  Invalid QSOs: {1}",
                       totalValidQSOs.ToString(), totalInvalidQSOs.ToString());
                        sw.WriteLine(message);

                        message = string.Format(" Valid PH: {0}({1})  Valid CW: {2}({3})   Valid RY: {4}({5})", totalValidPhoneQSOS.ToString(), totalPhoneQSOS.ToString(), totalValidCWQSOs.ToString(), totalCWQSOs.ToString(), totalValidDigiQSOS.ToString(), totalDigiQSOS.ToString());
                        sw.WriteLine(message);

                        message = string.Format(" HQP Mults: {0} Total Mults: {1}", contestLog.HQPMultipliers.ToString(), multiplierCount.ToString());
                        sw.WriteLine(message);

                        message = string.Format(" Points: {0}  Score: {1}", totalPoints.ToString(), score.ToString());
                        sw.WriteLine(message);
                    }
                    break;
            }

            message = string.Format(" Category:   {0}   Power: {1} ", contestLog.LogHeader.OperatorCategory, contestLog.LogHeader.Power);
            sw.WriteLine(message);

            sw.WriteLine("");
            sw.WriteLine(" Multipliers:");

            if (contestLog.Entities != null)
            {
                foreach (string entity in contestLog.Entities)
                {
                    sw.WriteLine("--" + entity);
                }
            }
        }

        #endregion

        #region PrintInspectionReport

        public void PrintInspectionReport(string fileName, string failReason)
        {
            //create a text file with the reason for the rejection
            using (StreamWriter sw = File.CreateText(Path.Combine(InspectionFolder, fileName)))
            {
                sw.WriteLine(failReason);
            }
        }

        #endregion

        #region Create Pre Analysis Reports

        /// <summary>
        /// Print the PreAnalysis report. Unique call signs how many times each call sign
        /// is listed in logs.
        /// </summary>
        /// <param name="reportPath"></param>
        /// <param name="session"></param>
        /// <param name="contestLogs"></param>
        public void PrintPreAnalysisReport(string reportPath, string session, List<ContestLog> contestLogs)
        {
            ListUniqueCallNamePairs(reportPath, session, contestLogs);
        }

        /// <summary>
        /// Create the unique callsign/(name or entity) pair worksheet.
        /// </summary>
        /// <param name="distinctQSOs"></param>
        /// <param name="reportPath"></param>
        /// <param name="session"></param>
        private void ListUniqueCallNamePairs(string reportPath, string session, List<ContestLog> contestLogs)
        {
            string worksheetName;
            string header1 = "Call Sign";
            string header2 = null;
            string workbookName = null;
            string fileName = null;

            List<Tuple<string, string>> distinctCallNamePairs;

            switch (ActiveContest)
            {
                case ContestName.HQP:
                    worksheetName = @"\Unique_Calls_Entities_";
                    header2 = "Entity";
                    workbookName = "Unique Call Entity Pairs";
                    fileName = reportPath + worksheetName + ".xlsx";
                    break;
                case ContestName.CW_OPEN:
                    worksheetName = @"\Unique_Call_Names_";
                    header2 = "Operator Name";
                    workbookName = "Unique Call Name Pairs";
                    fileName = reportPath + worksheetName + session + ".xlsx";
                    break;
            }

            FileInfo newFile = new FileInfo(fileName);

            if (newFile.Exists)
            {
                newFile.Delete();  // ensures we create a new workbook
            }

            distinctCallNamePairs = CollectAllCallNamePairs(contestLogs);

            using (SpreadsheetDocument document = SpreadsheetDocument.Create(fileName, SpreadsheetDocumentType.Workbook))
            {
                // add workbookpart
                WorkbookPart workbookPart = CreateWorkBookPart(document);

                WorksheetPart worksheetPart = AddWorkSheetPart(workbookPart);

                AddWorkSheet(workbookPart, worksheetPart, workbookName);

                // Get the sheetData cell table.
                SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

                for (int i = 0; i < distinctCallNamePairs.Count; i++)
                {
                    // Add a row to the cell table.
                    Row row;
                    row = new Row() { RowIndex = (uint)i + 1 };
                    sheetData.Append(row);

                    // In the new row, find the column location to insert a cell in A1.  
                    Cell refCell = null;
                    foreach (Cell cell in row.Elements<Cell>())
                    {
                        if (string.Compare(cell.CellReference.Value, "A1", true) > 0)
                        {
                            refCell = cell;
                            break;
                        }
                    }

                    // Add the cell to the cell table at A1.
                    Cell newCell = new Cell() { CellReference = "A" + row.RowIndex.ToString() };
                    row.InsertBefore(newCell, refCell);

                    if (i == 0)
                    {
                        // Set the cell value to be a numeric value of 100.
                        newCell.CellValue = new CellValue(header1);
                        newCell.DataType = new EnumValue<CellValues>(CellValues.String);

                        // Add the cell to the cell table at A2.
                        newCell = new Cell() { CellReference = "B" + row.RowIndex.ToString() };
                        row.InsertBefore(newCell, refCell);

                        // Set the cell value to be a numeric value of 100.
                        newCell.CellValue = new CellValue(header2);
                        newCell.DataType = new EnumValue<CellValues>(CellValues.String);
                    }
                    else
                    {
                        // Set the cell value to be a numeric value of 100.
                        newCell.CellValue = new CellValue(distinctCallNamePairs[i].Item1);
                        newCell.DataType = new EnumValue<CellValues>(CellValues.String);

                        // Add the cell to the cell table at A2.
                        newCell = new Cell() { CellReference = "B" + row.RowIndex.ToString() };
                        row.InsertBefore(newCell, refCell);

                        // Set the cell value to be a numeric value of 100.
                        newCell.CellValue = new CellValue(distinctCallNamePairs[i].Item2);
                        newCell.DataType = new EnumValue<CellValues>(CellValues.String);
                    }
                }

                ListCallNameOccurences(contestLogs, document, distinctCallNamePairs);
            }
        }

        /// <summary>
        /// Add to the workbook and worksheet.
        /// This lists every call sign, the total number of times that cal was referenced
        /// and a count of each different entity that call sign has listed.
        /// </summary>
        /// <param name="callNameCountList"></param>
        /// <param name="_BaseReportFolder"></param>
        /// <param name="v"></param>
        private void ListCallNameOccurences(List<ContestLog> contestLogs, SpreadsheetDocument document, List<Tuple<string, string>> distinctCallNamePairs)
        {
            string worksheetName = null;
            string header1 = "Call Sign";
            string header2 = null;
            string header3 = null;
            string header4 = null;

            switch (ActiveContest)
            {
                case ContestName.HQP:
                    worksheetName = @"Call-Entity Counts";
                    header2 = "Total";
                    header3 = "Entity";
                    header4 = "Count";
                    break;
                case ContestName.CW_OPEN:
                    worksheetName = @"Call-Name Counts";
                    header2 = "Total";
                    header3 = "Operator Name";
                    header4 = "Count";
                    break;
            }

            _CallNameCountList = CollectCallNameHitData(distinctCallNamePairs, contestLogs);

            using (document)
            {
                WorkbookPart workbookPart = document.WorkbookPart;

                // Add another worksheet with data
                WorksheetPart worksheetPart = AddWorkSheetPart(workbookPart);
                AddWorkSheet(workbookPart, worksheetPart, worksheetName);

                SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

                for (int i = 0; i < _CallNameCountList.Count; i++)
                {
                    // Add a row to the cell table.
                    Row row;
                    row = new Row() { RowIndex = (uint)i + 1 };
                    sheetData.Append(row);

                    // In the new row, find the column location to insert a cell in A1.  
                    Cell refCell = null;
                    foreach (Cell cell in row.Elements<Cell>())
                    {
                        if (string.Compare(cell.CellReference.Value, "A1", true) > 0)
                        {
                            refCell = cell;
                            break;
                        }
                    }

                    // Add the cell to the cell table at A1.
                    Cell newCell = new Cell() { CellReference = "A" + row.RowIndex.ToString() };
                    row.InsertBefore(newCell, refCell);

                    if (i == 0)
                    {
                        // Set the cell value
                        newCell.CellValue = new CellValue(header1);
                        newCell.DataType = new EnumValue<CellValues>(CellValues.String);

                        // Add the cell to the cell table at A2.
                        newCell = new Cell() { CellReference = "B" + row.RowIndex.ToString() };
                        row.InsertBefore(newCell, refCell);

                        newCell.CellValue = new CellValue(header2);
                        newCell.DataType = new EnumValue<CellValues>(CellValues.String);

                        // Add the cell to the cell table at A3.
                        newCell = new Cell() { CellReference = "C" + row.RowIndex.ToString() };
                        row.InsertBefore(newCell, refCell);

                        newCell.CellValue = new CellValue(header3);
                        newCell.DataType = new EnumValue<CellValues>(CellValues.String);

                        // Add the cell to the cell table at A4.
                        newCell = new Cell() { CellReference = "D" + row.RowIndex.ToString() };
                        row.InsertBefore(newCell, refCell);

                        newCell.CellValue = new CellValue(header4);
                        newCell.DataType = new EnumValue<CellValues>(CellValues.String);
                    }
                    else
                    {
                        // Set the cell value
                        newCell.CellValue = new CellValue(_CallNameCountList[i].Item1);
                        newCell.DataType = new EnumValue<CellValues>(CellValues.String);

                        // Add the cell to the cell table at A2.
                        newCell = new Cell() { CellReference = "B" + row.RowIndex.ToString() };
                        row.InsertBefore(newCell, refCell);

                        // Set the cell value to be a numeric value.
                        if (_CallNameCountList[i].Item2 != 0)
                        {
                            newCell.CellValue = new CellValue(_CallNameCountList[i].Item2.ToString());
                        }
                        newCell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        // Add the cell to the cell table at A3.
                        newCell = new Cell() { CellReference = "C" + row.RowIndex.ToString() };
                        row.InsertBefore(newCell, refCell);

                        newCell.CellValue = new CellValue(_CallNameCountList[i].Item3);
                        newCell.DataType = new EnumValue<CellValues>(CellValues.String);

                        // Add the cell to the cell table at A4.
                        newCell = new Cell() { CellReference = "D" + row.RowIndex.ToString() };
                        row.InsertBefore(newCell, refCell);

                        newCell.CellValue = new CellValue(_CallNameCountList[i].Item4.ToString());
                        newCell.DataType = new EnumValue<CellValues>(CellValues.Number);
                    }
                }
            }
        }

        #endregion

        #region Excel Functions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private WorkbookPart CreateWorkBookPart(SpreadsheetDocument document)
        {
            WorkbookPart workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            return workbookPart;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="workbookPart"></param>
        /// <returns></returns>
        private WorksheetPart AddWorkSheetPart(WorkbookPart workbookPart)
        {
            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());

            return worksheetPart;
        }

        /// <summary>
        /// http://stackoverflow.com/questions/9120544/openxml-multiple-sheets
        /// </summary>
        /// <param name="workbookPart"></param>
        /// <param name="worksheetPart"></param>
        /// <param name="name"></param>
        private void AddWorkSheet(WorkbookPart workbookPart, WorksheetPart worksheetPart, string sheetName)
        {
            uint sheetId = 1;
            Sheet sheet = null;

            Sheets sheets = workbookPart.Workbook.GetFirstChild<Sheets>();

            if (sheets != null && sheets.Elements<Sheet>().Count() > 0)
            {
                sheetId = sheets.Elements<Sheet>().Select(s => s.SheetId.Value).Max() + 1;
                string relationshipId = workbookPart.GetIdOfPart(worksheetPart);
                sheet = new Sheet() { Id = relationshipId, SheetId = sheetId, Name = sheetName };
            }
            else
            {
                sheets = workbookPart.Workbook.AppendChild(new Sheets());
                sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = sheetId, Name = sheetName };
            }
         
            sheets.Append(sheet);
        }

        #endregion

        /// <summary>
        /// Get a list of all distinct call/name pairs by grouped by call sign. This is used
        /// for the bad call list.
        /// </summary>
        /// <param name="contestLogList"></param>
        private List<Tuple<string, string>> CollectAllCallNamePairs(List<ContestLog> contestLogList)
        {
            // list of all distinct call/name pairs
            List<Tuple<string, string>> distinctCallNamePairs = contestLogList.SelectMany(z => z.QSOCollection)
              .Select(r => new Tuple<string, string>(r.ContactCall, r.ContactName))
              .GroupBy(p => new Tuple<string, string>(p.Item1, p.Item2))
              .Select(g => g.First())
              .OrderBy(q => q.Item1)
              .ToList();

            return distinctCallNamePairs;
        }

        /// <summary>
        /// Take the list of distinct call signs and a list of all call/name pairs. For every
        /// call sign see how many times it was used. Also, get the call and name combination
        /// and see how many times each name was used.
        /// </summary>
        /// <param name="distinctCallNamePairs"></param>
        /// <param name="contestLogList"></param>
        /// <returns></returns>
        private List<Tuple<string, int, string, int>> CollectCallNameHitData(List<Tuple<string, string>> distinctCallNamePairs, List<ContestLog> contestLogList)
        {
            string currentCall = "";
            string previousCall = "";
            int count = 0;

            _CallNameCountList = new List<Tuple<string, int, string, int>>();

            List<Tuple<string, string>> allCallNamePairs = contestLogList.SelectMany(z => z.QSOCollection)
                .Select(r => new Tuple<string, string>(r.ContactCall, r.ContactName))
                .ToList();

            for (int i = 0; i < distinctCallNamePairs.Count; i++)
            {
                IEnumerable<Tuple<string, string>> callCount = allCallNamePairs.Where(t => t.Item1 == distinctCallNamePairs[i].Item1);
                IEnumerable<Tuple<string, string>> nameCount = allCallNamePairs.Where(t => t.Item1 == distinctCallNamePairs[i].Item1 && t.Item2 == distinctCallNamePairs[i].Item2);

                if (previousCall != distinctCallNamePairs[i].Item1)
                {
                    previousCall = distinctCallNamePairs[i].Item1;
                    currentCall = distinctCallNamePairs[i].Item1;
                    count = callCount.Count();
                }
                else
                {
                    currentCall = "";
                    count = 0;
                }

                Tuple<string, int, string, int> tuple = new Tuple<string, int, string, int>(currentCall, count, distinctCallNamePairs[i].Item2, nameCount.Count());

                _CallNameCountList.Add(tuple);
            }

            return _CallNameCountList;
        }

    } // end class
}
