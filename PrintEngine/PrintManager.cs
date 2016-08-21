﻿//using PdfFileWriter;
using CsvHelper;
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
        public string WorkingFolder { get; set; }
        public string InspectionFolder { get; set; }
        public string ReportFolder { get; set; }
        public string LogFolder { get; set; }
        public string ScoreFolder { get; set; }
        public string ReviewFolder { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public PrintManager()
        {

        }

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
            fileName = year + " CWO Box Scores Session " + session + ".csv";
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
                        validQsoList = contestlog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO).ToList();

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

        //private void PopulateScoreList(List<ContestLog> contestLogs)
        //{
        //    ContestLog contestlog;
        //    List<QSO> validQsoList;
        //    string assisted = null;

        //    // sort ascending by score
        //    _ContestLogs = contestLogs.OrderByDescending(o => (int)o.ActualScore).ToList();


        //    for (int i = 0; i < _ContestLogs.Count; i++)
        //    {
        //        contestlog = _ContestLogs[i];
        //        if (contestlog != null)
        //        {
        //            assisted = "N";
        //            //so2r = "N";

        //            // only look at valid QSOs
        //            validQsoList = contestlog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO).ToList();

        //            if (contestlog.LogHeader.Assisted == CategoryAssisted.Assisted)
        //            {
        //                assisted = "Y";
        //            }

        //            if (contestlog.SO2R == true)
        //            {
        //                //so2r = "Y";
        //            }

        //            _ScoreList.LogOwner = contestlog.LogOwner;
        //            _ScoreList.Operator = contestlog.Operator;
        //            _ScoreList.Station = contestlog.Station;
        //            _ScoreList.OperatorName = contestlog.OperatorName;
        //            _ScoreList.QSOCount = validQsoList.Count.ToString();
        //            _ScoreList.Multipliers = contestlog.Multipliers.ToString();
        //            _ScoreList.ActualScore = contestlog.ActualScore.ToString();
        //            _ScoreList.Power = contestlog.LogHeader.Power.ToString();
        //            _ScoreList.Assisted = assisted;
        //        }
        //    }
        //}

        // using iTextSharp
        public void PrintPdfScoreSheet(List<ContestLog> contestLogs)
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
            fileName = year + " CWO Box Scores Session " + session + ".pdf"; // later convert to PDF
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
                doc.AddTitle("CWOpen Score Sheet");
                doc.AddSubject("Final Score Sheet " + session);
                doc.AddKeywords("CW, CWOpen");
                doc.AddCreator("Contest Log Analyser");
                doc.AddAuthor("W6OP");
                //doc.AddHeader("Nothing", "No Header");

                // define fonts
                BaseFont bfTimes = BaseFont.CreateFont(BaseFont.COURIER, BaseFont.CP1252, false);
                iTextSharp.text.Font times = new iTextSharp.text.Font(bfTimes, 9, iTextSharp.text.Font.BOLD, iTextSharp.text.BaseColor.BLACK);

                PdfPTable table = BuildPdfTable();
                //PdfPCell cell = null;
                iTextSharp.text.Font fontTable = FontFactory.GetFont("Arial", 10, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);


                message = year + " CW Open Session " + session;
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
                        validQsoList = contestlog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO).ToList();

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

        /// <summary>
        /// Print a report for each log listing the QSOs rejected and the reason for rejection.
        /// 
        /// CWO log checking results for 4K9W
        /// 
        /// Final: Valid QSOs:  2  Mults:  2  Score:  4 
        ///Category: LOW Checked:  2 
        /// </summary>
        /// <param name="contestLog"></param>
        public void PrintRejectReport(ContestLog contestLog, string callsign)
        {
            string reportFileName = null;
            string message = null;
            var value = "";
            //string message2 = null;

            // strip "/" from callsign
            if (callsign.IndexOf(@"/") != -1)
            {
                callsign = callsign.Substring(0, callsign.IndexOf(@"/"));
            }

            reportFileName = Path.Combine(ReportFolder, callsign + ".rpt");

            // only look at invalid QSOs
            List<QSO> inValidQsoList = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.InvalidQSO || q.Status == QSOStatus.ReviewQSO).ToList();
            // get list of valid qsos for dupes
            //List<QSO> validQsoList = contestLog.QSOCollection.Where(q => q.QSOHasDupes == true).ToList();

            try
            {
                using (StreamWriter sw = File.CreateText(reportFileName))
                {
                    // maybe add contest year later
                    sw.WriteLine("CWO log checking results for " + callsign);
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
                            // print QSO line and reject reason
                            message = "QSO: " + "\t" + qso.Frequency + "\t" + qso.Mode + "\t" + qso.QsoDate + "\t" + qso.QsoTime + "\t" + qso.OperatorCall + "\t" + qso.SentSerialNumber.ToString() + "\t" +
                                        qso.OperatorName + "\t" + qso.ContactCall + "\t" + qso.ReceivedSerialNumber.ToString() + "\t" + qso.ContactName;

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
                                else if (key == RejectReason.BustedCallSign || key == RejectReason.NoQSO)
                                {
                                    if (qso.MatchingQSO != null)
                                    {
                                        value = qso.RejectReasons[key] + " - " + qso.ContactCall + " --> " + qso.MatchingQSO.OperatorCall;
                                    }
                                    else
                                    {
                                        value = qso.RejectReasons[key];
                                    }
                                }
                                else
                                {
                                    value = qso.RejectReasons[key];
                                }
                            }


                            sw.WriteLine(message + "\t" + value.ToString());

                            // print info on the original that was duped - the one that was counted
                            if (qso.MatchingQSO != null)
                            {
                                QSO dupeQSO = qso.MatchingQSO;

                                message = "QSO: " + "\t" + dupeQSO.Frequency + "\t" + dupeQSO.Mode + "\t" + dupeQSO.QsoDate + "\t" + dupeQSO.QsoTime + "\t" + dupeQSO.OperatorCall + "\t" + dupeQSO.SentSerialNumber.ToString() + "\t" +
                                       dupeQSO.OperatorName + "\t" + dupeQSO.ContactCall + "\t" + dupeQSO.ReceivedSerialNumber.ToString() + "\t" + dupeQSO.ContactName;

                                //if (qso.ExcessTimeSpan > 0)
                                //{
                                //    message = message + "\t" + qso.ExcessTimeSpan.ToString() + " minutes difference";
                                //}

                                sw.WriteLine(message);
                            }

                            sw.WriteLine("");
                        }
                    }
                    else
                    {
                        sw.WriteLine("Golden log with zero errors. Congratulations!");

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
        /// Print a report for each log listing the QSOs rejected and the reason for rejection.
        /// 
        /// CWO log checking results for 4K9W
        /// 
        /// Final: Valid QSOs:  2  Mults:  2  Score:  4 
        ///Category: LOW Checked:  2 
        /// </summary>
        /// <param name="contestLog"></param>
        public void PrintReviewReport(ContestLog contestLog, string callsign)
        {
            string reportFileName = null;
            string message = null;
            var value = "";

            // strip "/" from callsign
            if (callsign.IndexOf(@"/") != -1)
            {
                callsign = callsign.Substring(0, callsign.IndexOf(@"/"));
            }

            reportFileName = Path.Combine(ReviewFolder, callsign + "_Review.rpt");

            // only look at invalid QSOs
            List<QSO> reviewQsoList = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.ReviewQSO).ToList();

            try
            {
                if (reviewQsoList.Count > 0)
                {
                    using (StreamWriter sw = File.CreateText(reportFileName))
                    {
                        // maybe add contest year later
                        sw.WriteLine("CWO log checking results for " + callsign);
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
            Int32 totalValidQSOs = 0;
            Int32 multiplierCount = 0;
            Int32 score = 0;

            totalValidQSOs = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO).ToList().Count();
            multiplierCount = contestLog.QSOCollection.Where(q => q.IsMultiplier == true && q.Status == QSOStatus.ValidQSO).ToList().Count();
            score = contestLog.ActualScore;

            sw.WriteLine(message);

            message = String.Format("Final:   Valid QSOs: {0}   Mults: {1}   Score: {2}", totalValidQSOs.ToString(), multiplierCount.ToString(), score.ToString());
            sw.WriteLine(message);

            message = String.Format("Category:   {0}   Power: {1} ", contestLog.LogHeader.OperatorCategory, contestLog.LogHeader.Power);
            sw.WriteLine(message);
        }

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

    } // end class
}
