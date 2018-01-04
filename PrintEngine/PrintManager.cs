﻿//using PdfFileWriter;
using CsvHelper;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using W6OP.ContestLogAnalyzer;

namespace W6OP.PrintEngine
{
    public class PrintManager
    {
        private List<ContestLog> _ContestLogs;
        private ScoreList _ScoreList = new ScoreList();

        //private ExcelPackage _Excelpackage;
        //private ExcelWorkbook _Workbook;
        //private ExcelWorksheet _Worksheet;

        public string WorkingFolder { get; set; }
        public string InspectionFolder { get; set; }
        public string ReportFolder { get; set; }
        public string LogFolder { get; set; }
        public string ScoreFolder { get; set; }
        public string ReviewFolder { get; set; }
        private string _ContestDescription { get; set; }
        private string _Title { get; set; }
        private string _Subject { get; set; }
        private string _Keywords { get; set; }
        private string _Message { get; set; }
        private ContestName _ActiveContest { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public PrintManager(ContestName contestName)
        {
            _ActiveContest = contestName;

            switch (_ActiveContest)
            {
                case ContestName.CW_OPEN:
                    _ContestDescription = " CWO Box Scores Session ";
                    _Title = "CWOpen Score Sheet";
                    _Subject = "Final Score Sheet ";
                    _Keywords = "CW, CWOpen";
                    _Message = " CW Open Session ";
                    break;
                case ContestName.HQP:
                    _ContestDescription = " Hawaii QSO Party ";
                    _Title = "HQP Score Sheet";
                    _Subject = "Final Score Sheet ";
                    _Keywords = "HQP";
                    _Message = " Hawaii QSO Party ";
                    break;
            }
        }

        #region Print Scores

        public void PrintCsvFile(List<ContestLog> contestLogs)
        {
            ContestLog contestlog;
            List<QSO> validQsoList;
            string assisted = null;
            string reportFileName = null;
            string fileName = null;
            string session = null;
            string year = DateTime.Now.ToString("yyyy");

            session = contestLogs[0].Session.ToString();
            fileName = year + _ContestDescription + session + ".csv";
            reportFileName = Path.Combine(ScoreFolder, fileName);

            _ContestLogs = contestLogs.OrderByDescending(o => (int)o.ActualScore).ToList();

            using (var sw = new StreamWriter(reportFileName))
            {
                var writer = new CsvWriter(sw);
                for (int i = 0; i < _ContestLogs.Count; i++)
                {
                    contestlog = _ContestLogs[i];
                    if (contestlog != null)
                    {
                        assisted = "N";
                        //so2r = "N";

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

                        //writer.WriteHeader<ScoreList>();

                        _ScoreList.LogOwner = contestlog.LogOwner;
                        _ScoreList.Operator = contestlog.Operator;
                        _ScoreList.Station = contestlog.Station;
                        _ScoreList.OperatorName = contestlog.OperatorName;
                        _ScoreList.QSOCount = validQsoList.Count.ToString();
                        _ScoreList.Multipliers = contestlog.Multipliers.ToString();
                        _ScoreList.ActualScore = contestlog.ActualScore.ToString();
                        _ScoreList.Power = contestlog.LogHeader.Power.ToString();
                        _ScoreList.Assisted = assisted;

                        //Write entire current record
                        writer.WriteRecord(_ScoreList);
                    }
                }
            }
        }

        // using iTextSharp
        public void PrintCWOpenPdfScoreSheet(List<ContestLog> contestLogs)
        {
            Document doc = null;
            Paragraph para = null;
            string reportFileName = null;
            string fileName = null;
            string session = null;
            string year = DateTime.Now.ToString("yyyy");
            string message = null;
            ContestLog contestlog;
            string assisted = null;
            //string so2r = null;
            List<QSO> validQsoList;

            PrintCsvFile(contestLogs);


            // sort ascending by score
            _ContestLogs = contestLogs.OrderByDescending(o => (int)o.ActualScore).ToList();

            session = contestLogs[0].Session.ToString();
            fileName = year + _ContestDescription + session + ".pdf"; // later convert to PDF
            reportFileName = Path.Combine(ScoreFolder, fileName);

            try
            {
                if (File.Exists(reportFileName))
                {
                    File.Delete(reportFileName);
                }

                FileStream fs = new FileStream(reportFileName, FileMode.Create, FileAccess.Write, FileShare.None);

                // margins are set in points - iTextSharp uses 72 pts/inch (36 = .5 inch)
                doc = new Document(PageSize.LETTER, 36, 36, 36, 36); // L,R,T,B margins
                PdfWriter writer = PdfWriter.GetInstance(doc, fs);
                doc.Open();

                // set document meta data
                doc.AddTitle(_Title);
                doc.AddSubject(_Subject + session);
                doc.AddKeywords(_Keywords);
                doc.AddCreator("Contest Log Analyser");
                doc.AddAuthor("W6OP");
                //doc.AddHeader("Nothing", "No Header");

                // define fonts
                BaseFont bfTimes = BaseFont.CreateFont(BaseFont.COURIER, BaseFont.CP1252, false);
                iTextSharp.text.Font times = new iTextSharp.text.Font(bfTimes, 9, iTextSharp.text.Font.BOLD, iTextSharp.text.BaseColor.BLACK);

                PdfPTable table = BuildPdfTable();
                //PdfPCell cell = null;
                iTextSharp.text.Font fontTable = FontFactory.GetFont("Arial", 10, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);


                message = year + _Message + session;
                para = new Paragraph(message, times);
                //para.Font = new iTextSharp.text.Font(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED), 50);
                // Setting paragraph's text alignment using iTextSharp.text.Element class
                para.Alignment = Element.ALIGN_CENTER;
                // Adding this 'para' to the Document object
                doc.Add(para);

                message = message = "Call" + "          " + "Operator" + "     " + "Station" + "     " + "Name" + "         " + "QSOs" + "    " + "Mults" + "     " + "Final" + "     " + "Power" + "   " + "Assisted";
                para = new Paragraph(message, times);
                // Setting paragraph's text alignment using iTextSharp.text.Element class
                para.Alignment = Element.ALIGN_LEFT;
                // Adding this 'para' to the Document object
                doc.Add(para);

                Int32 line = 2; // 2 header lines initially
                bool firstPage = true;
                for (int i = 0; i < _ContestLogs.Count; i++)
                {
                    line++;
                    contestlog = _ContestLogs[i];
                    if (contestlog != null)
                    {
                        assisted = "N";
                        //so2r = "N";

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

                        table.AddCell(new Phrase(contestlog.LogOwner, fontTable));
                        table.AddCell(new Phrase(contestlog.Operator, fontTable));
                        table.AddCell(new Phrase(contestlog.Station, fontTable));
                        table.AddCell(new Phrase(contestlog.OperatorName, fontTable));
                        table.AddCell(new Phrase(validQsoList.Count.ToString(), fontTable));
                        table.AddCell(new Phrase(contestlog.Multipliers.ToString(), fontTable));
                        table.AddCell(new Phrase(contestlog.ActualScore.ToString(), fontTable));
                        table.AddCell(new Phrase(contestlog.LogHeader.Power.ToString(), fontTable));
                        table.AddCell(new Phrase(assisted, fontTable));

                        if (firstPage == true && line == 50)
                        {
                            line = 51;
                        }

                        if (line > 50)
                        {
                            firstPage = false;
                            doc.Add(table);
                            doc.NewPage();
                            table = BuildPdfTable();
                            line = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string a = ex.Message;
            }
            finally
            {
                doc.Close();
            }
        }

        // using iTextSharp
        public void PrintHQPPdfScoreSheet(List<ContestLog> contestLogs)
        {
            Document doc = null;
            Paragraph para = null;
            string reportFileName = null;
            string fileName = null;
            string year = DateTime.Now.ToString("yyyy");
            string message = null;
            ContestLog contestlog;
            List<QSO> validQsoList;

            PrintCsvFile(contestLogs);


            // sort ascending by score
            _ContestLogs = contestLogs.OrderByDescending(o => (int)o.ActualScore).ToList();

            fileName = year + _ContestDescription + ".pdf"; // later convert to PDF
            reportFileName = Path.Combine(ScoreFolder, fileName);

            try
            {
                if (File.Exists(reportFileName))
                {
                    File.Delete(reportFileName);
                }

                FileStream fs = new FileStream(reportFileName, FileMode.Create, FileAccess.Write, FileShare.None);

                // margins are set in points - iTextSharp uses 72 pts/inch (36 = .5 inch)
                doc = new Document(PageSize.LETTER, 36, 36, 36, 36); // L,R,T,B margins
                PdfWriter writer = PdfWriter.GetInstance(doc, fs);
                doc.Open();

                // set document meta data
                doc.AddTitle(_Title);
                doc.AddSubject(_Subject);
                doc.AddKeywords(_Keywords);
                doc.AddCreator("Contest Log Analyser");
                doc.AddAuthor("W6OP");
                //doc.AddHeader("Nothing", "No Header");

                // define fonts
                BaseFont bfTimes = BaseFont.CreateFont(BaseFont.COURIER, BaseFont.CP1252, false);
                iTextSharp.text.Font times = new iTextSharp.text.Font(bfTimes, 9, iTextSharp.text.Font.BOLD, iTextSharp.text.BaseColor.BLACK);

                PdfPTable table = BuildPdfTable();
                //PdfPCell cell = null;
                iTextSharp.text.Font fontTable = FontFactory.GetFont("Arial", 10, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);


                message = year + _Message;
                para = new Paragraph(message, times);
                //para.Font = new iTextSharp.text.Font(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED), 50);
                // Setting paragraph's text alignment using iTextSharp.text.Element class
                para.Alignment = Element.ALIGN_CENTER;
                // Adding this 'para' to the Document object
                doc.Add(para);

                message = message = "Call" + "          " + "Operator" + "     " + "Station" + "     " + "Entity" + "         " + "QSOs" + "    " + "Mults" + "     " + "Points" + "     " + "Final" + "   " + "";
                para = new Paragraph(message, times);
                // Setting paragraph's text alignment using iTextSharp.text.Element class
                para.Alignment = Element.ALIGN_LEFT;
                // Adding this 'para' to the Document object
                doc.Add(para);

                Int32 line = 2; // 2 header lines initially
                bool firstPage = true;
                for (int i = 0; i < _ContestLogs.Count; i++)
                {
                    line++;
                    contestlog = _ContestLogs[i];
                    if (contestlog != null)
                    {
                        // only look at valid QSOs
                        validQsoList = contestlog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO).ToList();

                        table.AddCell(new Phrase(contestlog.LogOwner, fontTable));
                        table.AddCell(new Phrase(contestlog.Operator, fontTable));
                        table.AddCell(new Phrase(contestlog.Station, fontTable));
                        table.AddCell(new Phrase(contestlog.OperatorName, fontTable));
                        table.AddCell(new Phrase(validQsoList.Count.ToString(), fontTable));
                        table.AddCell(new Phrase(contestlog.Multipliers.ToString(), fontTable));
                        table.AddCell(new Phrase(contestlog.TotalPoints.ToString(), fontTable));
                        table.AddCell(new Phrase(contestlog.ActualScore.ToString(), fontTable));
                        table.AddCell(new Phrase("", fontTable));
                        //table.AddCell(new Phrase(contestlog.LogHeader.Power.ToString(), fontTable));
                        //table.AddCell(new Phrase(assisted, fontTable));

                        if (firstPage == true && line == 50)
                        {
                            line = 51;
                        }

                        if (line > 50)
                        {
                            firstPage = false;
                            doc.Add(table);
                            doc.NewPage();
                            table = BuildPdfTable();
                            line = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string a = ex.Message;
            }
            finally
            {
                doc.Close();
            }
        }
        /// <summary>
        /// Build a table to hold the results.
        /// </summary>
        /// <returns></returns>
        private PdfPTable BuildPdfTable()
        {
            PdfPTable table = new PdfPTable(9);
            //actual width of table in points
            table.TotalWidth = 514;
            //fix the absolute width of the table
            table.LockedWidth = true;

            float[] widths = new float[] { 68f, 68f, 68f, 68f, 50f, 54f, 54f, 54f, 30f };
            table.SetWidths(widths);
            table.HorizontalAlignment = 0;
            //leave a gap before and after the table
            table.SpacingBefore = 20f;
            //table.SpacingAfter = 30f;

            return table;
        }

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
            Int32 session = contestLog.Session;
            //string message2 = null;

            // strip "/" from callsign
            if (callsign.IndexOf(@"/") != -1)
            {
                callsign = callsign.Substring(0, callsign.IndexOf(@"/"));
            }

            reportFileName = Path.Combine(ReportFolder, callsign + ".rpt");

            // only look at invalid QSOs
            List<QSO> inValidQsoList = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.InvalidQSO).ToList();
            // get list of valid qsos for dupes
            //List<QSO> validQsoList = contestLog.QSOCollection.Where(q => q.QSOHasDupes == true).ToList();

            try
            {
                using (StreamWriter sw = File.CreateText(reportFileName))
                {
                    // maybe add contest year later
                    switch (_ActiveContest)
                    {
                        case ContestName.CW_OPEN:
                            sw.WriteLine("CWOpen log checking results for " + callsign + " in session " + session.ToString());
                            break;
                        case ContestName.HQP:
                            sw.WriteLine("Hawaii QSO Party log checking results for " + callsign);
                            break;
                    }

                    sw.WriteLine("");

                    if (!String.IsNullOrEmpty(contestLog.LogHeader.SoapBox))
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
                            if (qso.HasBeenPrinted)
                            {
                                continue;
                            }

                            if (qso.QSOHasDupes == false && qso.QSOIsDupe == false)
                            {
                                message = "QSO: " + "\t" + qso.Frequency + "\t" + qso.Mode + "\t" + qso.QsoDate + "\t" + qso.QsoTime + "\t" + qso.OperatorCall + "\t" + qso.SentSerialNumber.ToString() + "\t" +
                                                                       qso.OperatorName + "\t" + qso.ContactCall + "\t" + qso.ReceivedSerialNumber.ToString() + "\t" + qso.ContactName;
                            }

                            // should only be one reason so lets change the collection type
                            foreach (var key in qso.RejectReasons.Keys)
                            {
                                if (key == RejectReason.OperatorName)
                                {
                                    if (qso.MatchingQSO != null)
                                    {
                                        value = qso.RejectReasons[key] + " - " + qso.IncorrectName + " --> " + qso.MatchingQSO.OperatorName;
                                    }
                                    else
                                    {
                                        value = qso.RejectReasons[key] + " - " + qso.IncorrectName;
                                    }
                                }
                                else if (key == RejectReason.EntityName)
                                {
                                    if (qso.MatchingQSO != null)
                                    {
                                        value = qso.RejectReasons[key] + " - " + qso.IncorrectName + " --> " + qso.MatchingQSO.OperatorName;
                                    }
                                    else
                                    {
                                        value = qso.RejectReasons[key] + " - " + qso.IncorrectName;
                                    }
                                }
                                else if (key == RejectReason.InvalidEntity)
                                {
                                    if (qso.MatchingQSO != null)
                                    {
                                        value = qso.RejectReasons[key] + " - " + qso.IncorrectName + " --> " + qso.MatchingQSO.OperatorName;
                                    }
                                    else
                                    {
                                        value = qso.RejectReasons[key] + " - " + qso.IncorrectName;
                                    }
                                }
                                else if (key == RejectReason.SerialNumber)
                                {
                                    if (qso.MatchingQSO != null)
                                    {
                                        value = qso.RejectReasons[key] + " - " + qso.ReceivedSerialNumber + " --> " + qso.MatchingQSO.SentSerialNumber;
                                    }
                                    else
                                    {
                                        value = qso.RejectReasons[key];
                                    }
                                }
                                else if (key == RejectReason.NotCounted)
                                {
                                    value = qso.RejectReasons[key] + " - " + "Non Hawaiian Station";
                                    break;
                                }
                                else if (key == RejectReason.BustedCallSign || key == RejectReason.NoQSO)
                                {
                                    if (qso.MatchingQSO != null)
                                    {
                                        value = qso.RejectReasons[key] + " - " + qso.ContactCall + " --> " + qso.MatchingQSO.OperatorCall;
                                    }
                                    else
                                    {
                                        value = qso.RejectReasons[key] + " - " + qso.ContactCall + " --> " + qso.BustedCallGuess;
                                        //value = qso.RejectReasons[key];
                                    }
                                }
                                else if (key == RejectReason.DuplicateQSO)
                                {
                                    message = null;

                                    if (qso.HasBeenPrinted == false)
                                    {
                                        //value = qso.RejectReasons[key];
                                        //originalQSO = qso.DupeListLocation;


                                        //message = "QSO: " + "\t" + originalQSO.Frequency + "\t" + originalQSO.Mode + "\t" + originalQSO.QsoDate + "\t" + originalQSO.QsoTime + "\t" + originalQSO.OperatorCall + "\t" + originalQSO.SentSerialNumber.ToString() + "\t" +
                                        //           originalQSO.OperatorName + "\t" + originalQSO.ContactCall + "\t" + originalQSO.ReceivedSerialNumber.ToString() + "\t" + originalQSO.ContactName;

                                        //sw.WriteLine(value.ToString());
                                        //sw.WriteLine(message);

                                        //message = String.Format("Here are the other QSOs that were made with {0} that match the mode and band", qso.DupeListLocation.ContactCall);
                                        message = "Duplicates ignored for scoring purposes:";
                                        sw.WriteLine(message);

                                        message = null;

                                        PrintDuplicates(qso.DupeListLocation, sw);
                                        sw.WriteLine("");
                                    }
                                }
                                else
                                {
                                    value = qso.RejectReasons[key];
                                }
                            }

                            if (message != null)
                            {
                                sw.WriteLine(value.ToString());
                                sw.WriteLine(message);

                                if (qso.MatchingQSO != null)
                                {
                                    message = "QSO: " + "\t" + qso.MatchingQSO.Frequency + "\t" + qso.MatchingQSO.Mode + "\t" + qso.MatchingQSO.QsoDate + "\t" + qso.MatchingQSO.QsoTime + "\t" + qso.MatchingQSO.OperatorCall + "\t" + qso.MatchingQSO.SentSerialNumber.ToString() + "\t" +
                                           qso.MatchingQSO.OperatorName + "\t" + qso.MatchingQSO.ContactCall + "\t" + qso.MatchingQSO.ReceivedSerialNumber.ToString() + "\t" + qso.MatchingQSO.ContactName;

                                    sw.WriteLine(message);
                                }
                            }

                            // print info on the original that was duped - the one that was counted
                            //if (qso.MatchingQSO != null || qso.DupeListLocation != null)
                            //{
                            //    //QSO dupeQSO = qso.MatchingQSO;
                            //    if (qso.DupeListLocation == null)
                            //    {
                            //        // if (dupeQSO.HasBeenPrinted == false)
                            //        // {
                            //        qso.MatchingQSO.HasBeenPrinted = true;
                            //        message = "QSO: " + "\t" + qso.MatchingQSO.Frequency + "\t" + qso.MatchingQSO.Mode + "\t" + qso.MatchingQSO.QsoDate + "\t" + qso.MatchingQSO.QsoTime + "\t" + qso.MatchingQSO.OperatorCall + "\t" + qso.MatchingQSO.SentSerialNumber.ToString() + "\t" +
                            //               qso.MatchingQSO.OperatorName + "\t" + qso.MatchingQSO.ContactCall + "\t" + qso.MatchingQSO.ReceivedSerialNumber.ToString() + "\t" + qso.MatchingQSO.ContactName;

                            //        sw.WriteLine(message);
                            //        // }
                            //    }
                            //    else
                            //    {
                            //        //sw.WriteLine(String.Format("Here are all the other QSOs that were made with {0} that match the mode and band", qso.DupeListLocation.ContactCall));

                            //        foreach (QSO item in qso.DupeListLocation.DuplicateQsoList)
                            //        {
                            //            //if (item.HasBeenPrinted == false)
                            //            //{
                            //            item.HasBeenPrinted = true;

                            //            message = "QSO: " + "\t" + item.Frequency + "\t" + item.Mode + "\t" + item.QsoDate + "\t" + item.QsoTime + "\t" + item.OperatorCall + "\t" + item.SentSerialNumber.ToString() + "\t" +
                            //                   item.OperatorName + "\t" + item.ContactCall + "\t" + item.ReceivedSerialNumber.ToString() + "\t" + item.ContactName;

                            //            sw.WriteLine(message);
                            //            //}
                            //        }
                            //    }
                            //}

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

                message = "QSO: " + "\t" + item.Frequency + "\t" + item.Mode + "\t" + item.QsoDate + "\t" + item.QsoTime + "\t" + item.OperatorCall + "\t" + item.SentSerialNumber.ToString() + "\t" +
                       item.OperatorName + "\t" + item.ContactCall + "\t" + item.ReceivedSerialNumber.ToString() + "\t" + item.ContactName;

                sw.WriteLine(message);
                //}
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
            var value = "";
            Int32 session = 1;

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
                            sw.WriteLine("Log checking results for " + callsign + " in session " + session.ToString() + ".");
                            sw.WriteLine("");

                            if (!String.IsNullOrEmpty(contestLog.LogHeader.SoapBox))
                            {
                                sw.WriteLine(contestLog.LogHeader.SoapBox);
                                sw.WriteLine("");
                            }

                            foreach (QSO qso in reviewQsoList)
                            {
                                // print QSO line and reject reason
                                message = "QSO: " + "\t" + qso.Frequency + "\t" + qso.Mode + "\t" + qso.QsoDate + "\t" + qso.QsoTime + "\t" + qso.OperatorCall + "\t" + qso.SentSerialNumber.ToString() + "\t" +
                                            qso.OperatorName + "\t" + qso.ContactCall + "\t" + qso.ReceivedSerialNumber.ToString() + "\t" + qso.ContactName;

                                // should only be one reason so lets change the collection type
                                foreach (var key in qso.RejectReasons.Keys)
                                {
                                    value = qso.RejectReasons[key];
                                }

                                sw.WriteLine(message + "\t" + value.ToString());
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
            Int32 totalQSOs = 0;
            Int32 totalValidQSOs = 0;
            Int32 totalInvalidQSOs = 0;
            Int32 totalPhoneQSOS = 0;
            Int32 totalCWQSOs = 0;
            Int32 totalDigiQSOS = 0;
            Int32 totalValidPhoneQSOS = 0;
            Int32 totalValidCWQSOs = 0;
            Int32 totalValidDigiQSOS = 0;
            Int32 multiplierCount = 0;
            Int32 score = 0;

            totalQSOs = contestLog.QSOCollection.Count;
            totalValidQSOs = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO).ToList().Count();
            totalInvalidQSOs = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.InvalidQSO).ToList().Count();

            totalPhoneQSOS = contestLog.QSOCollection.Where(q => q.Mode == "PH").ToList().Count();
            totalCWQSOs = contestLog.QSOCollection.Where(q => q.Mode == "CW").ToList().Count();
            totalDigiQSOS = contestLog.QSOCollection.Where(q => q.Mode == "RY").ToList().Count();

            totalValidPhoneQSOS = contestLog.QSOCollection.Where(q => (q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO) && q.Mode == "PH").ToList().Count();
            totalValidCWQSOs = contestLog.QSOCollection.Where(q => (q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO) && q.Mode == "CW").ToList().Count();
            totalValidDigiQSOS = contestLog.QSOCollection.Where(q => (q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO) && q.Mode == "RY").ToList().Count();

            //Int32 why = totalDigiQSOS = contestLog.QSOCollection.Where(q => (q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO) && q.Mode != "PH" && q.Mode != "CW" && q.Mode != "RY").ToList().Count();
            //if (why > 0)
            //{
            //    var e = 1;
            //}
            multiplierCount = contestLog.Multipliers;   //.QSOCollection.Where(q => q.IsMultiplier == true && q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO).ToList().Count();
            score = contestLog.ActualScore;

            sw.WriteLine(message);

            if (contestLog.IsCheckLog)
            {
                sw.WriteLine("CHECKLOG::: --------------------------");
                sw.WriteLine(message);
            }
            switch (_ActiveContest)
            {
                case ContestName.CW_OPEN:
                    message = String.Format(" Final:   Valid QSOs: {0}   Mults: {1}   Score: {2}", totalValidQSOs.ToString(), multiplierCount.ToString(), score.ToString());
                    sw.WriteLine(message);
                    break;
                case ContestName.HQP:
                    if (contestLog.IsHQPEntity)
                    {
                        //message = String.Format("Final:   Valid QSOs: {0}   HQP Mults: {1}   NonHQP Mults: {2}   Total Mults: {3}   Points: {4}   Score: {5}",
                        //totalValidQSOs.ToString(), contestLog.HQPMultipliers.ToString(), contestLog.NonHQPMultipliers.ToString(), multiplierCount.ToString(), contestLog.TotalPoints.ToString(), score.ToString());
                        message = String.Format(" Total QSOs: {0}   Valid QSOs: {1}  Invalid QSOs: {2}", totalQSOs.ToString(), totalValidQSOs.ToString(), totalInvalidQSOs.ToString());
                        sw.WriteLine(message);

                        message = String.Format(" Valid PH: {0}({1})  Valid CW: {2}({3})   Valid RY: {4}({5})", totalValidPhoneQSOS.ToString(), totalPhoneQSOS.ToString(), totalValidCWQSOs.ToString(), totalCWQSOs.ToString(), totalValidDigiQSOS.ToString(), totalDigiQSOS.ToString());
                        sw.WriteLine(message);

                        message = String.Format(" HQP Mults: {0}   NonHQP Mults: {1}   Total Mults: {2}", contestLog.HQPMultipliers.ToString(), contestLog.NonHQPMultipliers.ToString(), multiplierCount.ToString());
                        sw.WriteLine(message);

                        message = String.Format(" Points: {0}  Score: {1}", contestLog.TotalPoints.ToString(), score.ToString());
                        sw.WriteLine(message);
                    }
                    else
                    {
                        //message = String.Format(" Final:   Valid QSOs: {0}   HQP Mults: {1} Total Mults: {2}   Points: {3}   Score: {4}",
                        //totalValidQSOs.ToString(), contestLog.HQPMultipliers.ToString(), multiplierCount.ToString(), contestLog.TotalPoints.ToString(), score.ToString());
                        message = String.Format(" Final:   Valid QSOs: {0}  Invalid QSOs: {1}",
                       totalValidQSOs.ToString(), totalInvalidQSOs.ToString());
                        sw.WriteLine(message);

                        message = String.Format(" Valid PH: {0}({1})  Valid CW: {2}({3})   Valid RY: {4}({5})", totalValidPhoneQSOS.ToString(), totalPhoneQSOS.ToString(), totalValidCWQSOs.ToString(), totalCWQSOs.ToString(), totalValidDigiQSOS.ToString(), totalDigiQSOS.ToString());
                        sw.WriteLine(message);

                        message = String.Format(" HQP Mults: {0} Total Mults: {1}", contestLog.HQPMultipliers.ToString(), multiplierCount.ToString());
                        sw.WriteLine(message);

                        message = String.Format(" Points: {0}  Score: {1}", contestLog.TotalPoints.ToString(), score.ToString());
                        sw.WriteLine(message);
                    }
                    break;
            }

            message = String.Format(" Category:   {0}   Power: {1} ", contestLog.LogHeader.OperatorCategory, contestLog.LogHeader.Power);
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



        public void PrintInspectionReport(string fileName, string failReason)
        {
            string inspectFileName = Path.Combine(InspectionFolder, fileName);

            inspectFileName = Path.Combine(InspectionFolder, fileName);
            //create a text file with the reason for the rejection
            using (StreamWriter sw = File.CreateText(inspectFileName))
            {
                sw.WriteLine(failReason);
            }
        }


        #region Create Pre Analysis Reports

        /// <summary>
        /// 
        /// </summary>
        /// <param name="distinctQSOs"></param>
        /// <param name="reportPath"></param>
        /// <param name="session"></param>
        public void ListUniqueCallNamePairs(List<Tuple<string, string>> distinctQSOs, string reportPath, string session)
        {
            string fileName = reportPath + @"\Unique_Calls_" + session + ".xlsx";

            FileInfo newFile = new FileInfo(fileName);
            if (newFile.Exists)
            {
                newFile.Delete();  // ensures we create a new workbook
            }

            using (SpreadsheetDocument document = SpreadsheetDocument.Create(fileName, SpreadsheetDocumentType.Workbook))
            {
                // add workbookpart
                WorkbookPart workbookPart = CreateWorkBookPart(document);

                WorksheetPart worksheetPart = AddWorkSheetPart(workbookPart);

                AddWorkSheet(workbookPart, worksheetPart, "Unique Calls");

                // Get the sheetData cell table.
                SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

                for (int i = 0; i < distinctQSOs.Count; i++)
                {
                    // Add a row to the cell table.
                    Row row;
                    row = new Row() { RowIndex = (UInt32)i + 1 };
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

                    // Set the cell value to be a numeric value of 100.
                    newCell.CellValue = new CellValue(distinctQSOs[i].Item1);
                    newCell.DataType = new EnumValue<CellValues>(CellValues.String);

                    // Add the cell to the cell table at A2.
                    newCell = new Cell() { CellReference = "B" + row.RowIndex.ToString() };
                    row.InsertBefore(newCell, refCell);

                    // Set the cell value to be a numeric value of 100.
                    newCell.CellValue = new CellValue(distinctQSOs[i].Item2);
                    newCell.DataType = new EnumValue<CellValues>(CellValues.String);

                }

                // Test code
                //ThisWillPopulateAnotherWorksheet(workbookPart, worksheetPart);

                // Close the document.
                document.Close();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callNameCountList"></param>
        /// <param name="_BaseReportFolder"></param>
        /// <param name="v"></param>
        public void ListCallNameOccurences(List<Tuple<string, int, string, int>> callNameCountList, string reportPath, string session)
        {
            string fileName = reportPath + @"\Unique_Calls_" + session + ".xlsx";

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (SpreadsheetDocument document = SpreadsheetDocument.Open(fs, true))
                {
                    WorkbookPart workbookPart = document.WorkbookPart;

                    // Add another worksheet with data
                    WorksheetPart worksheetPart = AddWorkSheetPart(workbookPart);
                    AddWorkSheet(workbookPart, worksheetPart, "Call-Name Counts");

                    SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

                    for (int i = 0; i < callNameCountList.Count; i++)
                    {
                        // Add a row to the cell table.
                        Row row;
                        row = new Row() { RowIndex = (UInt32)i + 1 };
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

                        // Set the cell value to be a numeric value of 100.
                        newCell.CellValue = new CellValue(callNameCountList[i].Item1);
                        newCell.DataType = new EnumValue<CellValues>(CellValues.String);

                        // Add the cell to the cell table at A2.
                        newCell = new Cell() { CellReference = "B" + row.RowIndex.ToString() };
                        row.InsertBefore(newCell, refCell);

                        // Set the cell value to be a numeric value of 100.
                        if (callNameCountList[i].Item2 != 0)
                        {
                            newCell.CellValue = new CellValue(callNameCountList[i].Item2.ToString());
                        }
                        newCell.DataType = new EnumValue<CellValues>(CellValues.Number);

                        // Add the cell to the cell table at A3.
                        newCell = new Cell() { CellReference = "C" + row.RowIndex.ToString() };
                        row.InsertBefore(newCell, refCell);

                        // Set the cell value to be a numeric value of 100.
                        newCell.CellValue = new CellValue(callNameCountList[i].Item3);
                        newCell.DataType = new EnumValue<CellValues>(CellValues.String);

                        // Add the cell to the cell table at A4.
                        newCell = new Cell() { CellReference = "D" + row.RowIndex.ToString() };
                        row.InsertBefore(newCell, refCell);

                        // Set the cell value to be a numeric value of 100.
                        newCell.CellValue = new CellValue(callNameCountList[i].Item4.ToString());
                        newCell.DataType = new EnumValue<CellValues>(CellValues.Number);
                    }
                }
            }
        }


        private void ThisWillPopulateAnotherWorksheet(WorkbookPart workbookPart, WorksheetPart worksheetPart)
        {
            // Add another worksheet with data
            worksheetPart = AddWorkSheetPart(workbookPart);
            AddWorkSheet(workbookPart, worksheetPart, "More Stuff");

            SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
            //// Add a row to the cell table.
            Row row2;
            row2 = new Row() { RowIndex = (UInt32)1 };
            sheetData.Append(row2);



            Cell refCell2 = null;
            foreach (Cell cell2 in row2.Elements<Cell>())
            {
                if (string.Compare(cell2.CellReference.Value, "A1", true) > 0)
                {
                    refCell2 = cell2;
                    break;
                }
            }

            // Add the cell to the cell table at A1.
            Cell newCell2 = new Cell() { CellReference = "A" + row2.RowIndex.ToString() };
            row2.InsertBefore(newCell2, refCell2);

            // Set the cell value to be a numeric value of 100.
            newCell2.CellValue = new CellValue("Testing");
            newCell2.DataType = new EnumValue<CellValues>(CellValues.String);
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
            UInt32 sheetId = 1;
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

            // probably can put this in calling function
            //SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>(); 

            sheets.Append(sheet);

            //return sheetData;

        }



        /*
            Sheets sheets = workbookPart.Workbook.GetFirstChild<Sheets>();
            string relationshipId = workbookPart.GetIdOfPart(worksheetPart);

 

            if (sheets.Elements<Sheet>().Count() > 0)
            {
                sheetId = sheets.Elements<Sheet>().Select(s => s.SheetId.Value).Max() + 1;
            }

            string sheetName = name;

            // Append the new worksheet and associate it with the workbook.
            Sheet sheet = new Sheet() { Id = relationshipId, SheetId = sheetId, Name = sheetName };


         */


        #endregion

    } // end class
}
