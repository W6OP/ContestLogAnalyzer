//using PdfFileWriter;
using CsvHelper;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using iTextSharp.text;
using iTextSharp.text.pdf;
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
        //private List<ContestLog> _ContestLogs;
        //private ScoreList _ScoreList = new ScoreList();

        //private ExcelPackage _Excelpackage;
        //private ExcelWorkbook _Workbook;
        //private ExcelWorksheet _Worksheet;

        public string WorkingFolder { get; set; }
        public string InspectionFolder { get; set; }
        public string ReportFolder { get; set; }
        public string LogFolder { get; set; }
        public string ScoreFolder { get; set; }
        public string ReviewFolder { get; set; }
        private string ContestDescription { get; set; }
        private string Title { get; set; }
        private string Subject { get; set; }
        private string Keywords { get; set; }
        private string Message { get; set; }
        private ContestName ActiveContest { get; set; }

        //private List<Tuple<string, string>> _DistinctCallNamePairs;
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
                    Title = "CWOpen Score Sheet";
                    Subject = "Final Score Sheet ";
                    Keywords = "CW, CWOpen";
                    Message = " CW Open Session ";
                    break;
                case ContestName.HQP:
                    ContestDescription = " Hawaii QSO Party ";
                    Title = "HQP Score Sheet";
                    Subject = "Final Score Sheet ";
                    Keywords = "HQP";
                    Message = " Hawaii QSO Party ";
                    break;
            }
        }

        #region Print Scores

        public void PrintCsvFile(List<ContestLog> contestLogs)
        {
            ScoreList scoreList = new ScoreList();
            List<ScoreList> scores = new List<ScoreList>();
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

                    scoreList = new ScoreList
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
            contestLogs = contestLogs.OrderByDescending(o => (int)o.ActualScore).ToList();

            session = contestLogs[0].Session.ToString();
            fileName = year + ContestDescription + session + ".pdf"; // later convert to PDF
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
                doc.AddTitle(Title);
                doc.AddSubject(Subject + session);
                doc.AddKeywords(Keywords);
                doc.AddCreator("Contest Log Analyser");
                doc.AddAuthor("W6OP");

                // define fonts
                BaseFont bfTimes = BaseFont.CreateFont(BaseFont.COURIER, BaseFont.CP1252, false);
                iTextSharp.text.Font times = new iTextSharp.text.Font(bfTimes, 9, iTextSharp.text.Font.BOLD, iTextSharp.text.BaseColor.BLACK);

                PdfPTable table = BuildPdfTable();
                iTextSharp.text.Font fontTable = FontFactory.GetFont("Arial", 10, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);


                message = year + Message + session;
                para = new Paragraph(message, times)
                {
                    // Setting paragraph's text alignment using iTextSharp.text.Element class
                    Alignment = Element.ALIGN_CENTER
                };
                // Adding this 'para' to the Document object
                doc.Add(para);

                message = message = "Call" + "          " + "Operator" + "     " + "Station" + "     " + "Name" + "         " + "QSOs" + "    " + "Mults" + "     " + "Final" + "     " + "Power" + "   " + "Assisted";
                para = new Paragraph(message, times)
                {
                    // Setting paragraph's text alignment using iTextSharp.text.Element class
                    Alignment = Element.ALIGN_LEFT
                };
                // Adding this 'para' to the Document object
                doc.Add(para);

                Int32 line = 2; // 2 header lines initially
                bool firstPage = true;
                for (int i = 0; i < contestLogs.Count; i++)
                {
                    line++;
                    contestlog = contestLogs[i];
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
            contestLogs = contestLogs.OrderByDescending(o => (int)o.ActualScore).ToList();

            fileName = year + ContestDescription + ".pdf"; // later convert to PDF
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
                doc.AddTitle(Title);
                doc.AddSubject(Subject);
                doc.AddKeywords(Keywords);
                doc.AddCreator("Contest Log Analyser");
                doc.AddAuthor("W6OP");
                //doc.AddHeader("Nothing", "No Header");

                // define fonts
                BaseFont bfTimes = BaseFont.CreateFont(BaseFont.COURIER, BaseFont.CP1252, false);
                iTextSharp.text.Font times = new iTextSharp.text.Font(bfTimes, 9, iTextSharp.text.Font.BOLD, iTextSharp.text.BaseColor.BLACK);

                PdfPTable table = BuildPdfTable();
                //PdfPCell cell = null;
                iTextSharp.text.Font fontTable = FontFactory.GetFont("Arial", 10, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);


                message = year + Message;
                para = new Paragraph(message, times)
                {
                    //para.Font = new iTextSharp.text.Font(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED), 50);
                    // Setting paragraph's text alignment using iTextSharp.text.Element class
                    Alignment = Element.ALIGN_CENTER
                };
                // Adding this 'para' to the Document object
                doc.Add(para);

                message = message = "Call" + "          " + "Operator" + "     " + "Station" + "     " + "Entity" + "         " + "QSOs" + "    " + "Mults" + "     " + "Points" + "     " + "Final" + "   " + "";
                para = new Paragraph(message, times)
                {
                    // Setting paragraph's text alignment using iTextSharp.text.Element class
                    Alignment = Element.ALIGN_LEFT
                };
                // Adding this 'para' to the Document object
                doc.Add(para);

                Int32 line = 2; // 2 header lines initially
                bool firstPage = true;
                for (int i = 0; i < contestLogs.Count; i++)
                {
                    line++;
                    contestlog = contestLogs[i];
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
            PdfPTable table = new PdfPTable(9)
            {
                //actual width of table in points
                TotalWidth = 514,
                //fix the absolute width of the table
                LockedWidth = true
            };

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

                            // should only be one reason so lets change the collection type
                            foreach (var key in qso.GetRejectReasons().Keys)
                            {
                                if (key == RejectReason.OperatorName)
                                {
                                    if (qso.MatchingQSO != null)
                                    {
                                        value = qso.GetRejectReasons()[key] + " - " + qso.IncorrectName + " --> " + qso.MatchingQSO.OperatorName;
                                    }
                                    else
                                    {
                                        value = qso.GetRejectReasons()[key] + " - " + qso.IncorrectName;
                                    }
                                }
                                else if (key == RejectReason.EntityName)
                                {
                                    if (qso.MatchingQSO != null)
                                    {
                                        value = qso.GetRejectReasons()[key] + " - " + qso.IncorrectDXEntity + " --> " + qso.MatchingQSO.OperatorEntity;
                                    }
                                    else
                                    {
                                        value = qso.GetRejectReasons()[key] + " - " + qso.IncorrectDXEntity;
                                    }
                                }
                                else if (key == RejectReason.InvalidEntity)
                                {
                                    if (qso.MatchingQSO != null)
                                    {
                                        value = qso.GetRejectReasons()[key] + " - " + qso.IncorrectDXEntity + " --> " + qso.MatchingQSO.OperatorEntity;
                                    }
                                    else
                                    {
                                        value = qso.GetRejectReasons()[key] + " - " + qso.IncorrectDXEntity;
                                    }
                                }
                                else if (key == RejectReason.SerialNumber)
                                {
                                    if (qso.MatchingQSO != null)
                                    {
                                        value = qso.GetRejectReasons()[key] + " - " + qso.ReceivedSerialNumber + " --> " + qso.MatchingQSO.SentSerialNumber;
                                    }
                                    else
                                    {
                                        value = qso.GetRejectReasons()[key];
                                    }
                                }
                                else if (key == RejectReason.NotCounted)
                                {
                                    value = qso.GetRejectReasons()[key] + " - " + "Non Hawaiian Station";
                                    break;
                                }
                                else if (key == RejectReason.BustedCallSign || key == RejectReason.NoQSO)
                                {
                                    if (qso.MatchingQSO != null)
                                    {
                                        value = qso.GetRejectReasons()[key] + " - " + qso.ContactCall + " --> " + qso.MatchingQSO.OperatorCall;
                                    }
                                    else
                                    {
                                        value = qso.GetRejectReasons()[key] + " - " + qso.ContactCall + " --> " + qso.BustedCallGuess;
                                        //value = qso.RejectReasons[key];
                                    }
                                }
                                else if (key == RejectReason.DuplicateQSO)
                                {
                                    message = null;

                                    if (qso.HasBeenPrinted == false)
                                    {
                                        message = "Duplicates ignored for scoring purposes:";
                                        sw.WriteLine(message);

                                        message = null;

                                        PrintDuplicates(qso.DupeListLocation, sw);
                                        sw.WriteLine("");
                                    }
                                }
                                else
                                {
                                    value = qso.GetRejectReasons()[key];
                                }
                            }

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

                            if (!String.IsNullOrEmpty(contestLog.LogHeader.SoapBox))
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
                                foreach (var key in qso.GetRejectReasons().Keys)
                                {
                                    value = qso.GetRejectReasons()[key];
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

            multiplierCount = contestLog.Multipliers;
            score = contestLog.ActualScore;

            sw.WriteLine(message);

            if (contestLog.IsCheckLog)
            {
                sw.WriteLine("CHECKLOG::: --------------------------");
                sw.WriteLine(message);
            }
            switch (ActiveContest)
            {
                case ContestName.CW_OPEN:
                    message = String.Format(" Final:   Valid QSOs: {0}   Mults: {1}   Score: {2}", totalValidQSOs.ToString(), multiplierCount.ToString(), score.ToString());
                    sw.WriteLine(message);
                    break;
                case ContestName.HQP:
                    if (contestLog.IsHQPEntity)
                    {
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
            //create a text file with the reason for the rejection
            using (StreamWriter sw = File.CreateText(Path.Combine(InspectionFolder, fileName)))
            {
                sw.WriteLine(failReason);
            }
        }


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

                    if (i == 0)
                    {
                        // Set the cell value to be a numeric value of 100.
                        newCell.CellValue = new CellValue(header1);
                        newCell.DataType = new EnumValue<CellValues>(CellValues.String);
                        //newCell.SetAttribute()

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


        //private void ThisWillPopulateAnotherWorksheet(WorkbookPart workbookPart, WorksheetPart worksheetPart)
        //{
        //    // Add another worksheet with data
        //    worksheetPart = AddWorkSheetPart(workbookPart);
        //    AddWorkSheet(workbookPart, worksheetPart, "More Stuff");

        //    SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
        //    //// Add a row to the cell table.
        //    Row row2;
        //    row2 = new Row() { RowIndex = (UInt32)1 };
        //    sheetData.Append(row2);



        //    Cell refCell2 = null;
        //    foreach (Cell cell2 in row2.Elements<Cell>())
        //    {
        //        if (string.Compare(cell2.CellReference.Value, "A1", true) > 0)
        //        {
        //            refCell2 = cell2;
        //            break;
        //        }
        //    }

        //    // Add the cell to the cell table at A1.
        //    Cell newCell2 = new Cell() { CellReference = "A" + row2.RowIndex.ToString() };
        //    row2.InsertBefore(newCell2, refCell2);

        //    // Set the cell value to be a numeric value of 100.
        //    newCell2.CellValue = new CellValue("Testing");
        //    newCell2.DataType = new EnumValue<CellValues>(CellValues.String);
        //}

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
        private List<Tuple<string, Int32, string, Int32>> CollectCallNameHitData(List<Tuple<string, string>> distinctCallNamePairs, List<ContestLog> contestLogList)
        {
            string currentCall = "";
            string previousCall = "";
            Int32 count = 0;

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

                Tuple<string, Int32, string, Int32> tuple = new Tuple<string, Int32, string, Int32>(currentCall, count, distinctCallNamePairs[i].Item2, nameCount.Count());

                _CallNameCountList.Add(tuple);
            }

            return _CallNameCountList;
        }

    } // end class
}
