using PdfFileWriter;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using W6OP.ContestLogAnalyzer;

namespace W6OP.PrintEngine
{
    public class PrintManager
    {
        private PdfDocument _Document;
        private Font _DefaultFont;
        private Int32 _PageNo;
        Int32 _LineNo = 1;

        private List<ContestLog> _ContestLogs;

        public string WorkingFolder { get; set; }

        public string InspectionFolder { get; set; }

        public string ReportFolder { get; set; }

        public string LogFolder { get; set; }

        public string ScoreFolder { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public PrintManager()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contestLog"></param>
        public void PrintLog(ContestLog contestLog)
        {
            // create a text file with the reason for the rejection
            //using (StreamWriter sw = File.CreateText(inspectReasonFileName))
            //{
            //    sw.WriteLine(FailReason);
            //}
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contestLog"></param>
        public void PrintHeader(ContestLog contestLog)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contestLog"></param>
        public void PrintQSOs(ContestLog contestLog)
        {

        }

        public void PrintScoreSheet(List<ContestLog> contestLogs)
        {
            string reportFileName = null;
            string fileName = null;
            string session = null;
            string message = null;
            string assisted = null;
            string so2r = null;
            string year = DateTime.Now.ToString("yyyy");
            List<QSO> validQsoList;

            session = contestLogs[0].Session.ToString();
            fileName = year + " CWO Box Scores Session " + session + ".txt"; // later convert to PDF
            reportFileName = Path.Combine(ScoreFolder, fileName);



            // LETS SORT SO IS IN SAME ORDER AS DISPLAY
            contestLogs = contestLogs.OrderBy(o => o.LogOwner).ToList();


            using (StreamWriter sw = File.CreateText(reportFileName))
            {
                // Add Header ie. 2014 CW Open Session 1 
                message = year +  " CW Open Session " + session;

                sw.WriteLine(message + "\r\n");
                //sw.WriteLine("");

                message = "Call" + "\t\t" + "Operator" + "\t" + "Station" + "\t" + "Name" + "\t" + "QSOs" + "\t" + "Mults" + "\t" + "Final" + "\t" + "Power" + "\t" + "SO2R" + "\t" + "Assisted";
                sw.WriteLine(message);


                foreach (ContestLog contestlog in contestLogs)
                {
                    assisted = "N";
                    so2r = "N";

                    // only look at valid QSOs
                    validQsoList = contestlog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO).ToList();

                    if (contestlog.LogHeader.Assisted == CategoryAssisted.Assisted)
                    {
                        assisted = "Y";
                    }

                    if(contestlog.SO2R == true)
                    {
                        so2r = "Y";
                    }

                    message = contestlog.LogOwner + "\t\t" + contestlog.Operator + "\t" + contestlog.Station + "\t" + contestlog.OperatorName + "\t" + validQsoList.Count.ToString() + "\t";

                    message = message + contestlog.Multipliers.ToString() + "\t" + contestlog.ActualScore.ToString() + "\t" + contestlog.LogHeader.Power + "\t" + so2r + "\t" + assisted;

                    sw.WriteLine(message);
                }
            }
        }

        /// <summary>
        /// Print the scores as a PDF file.
        /// </summary>
        /// <param name="contestLogs"></param>
        /// <param name="pdf"></param>
        public void PrintScoreSheet(List<ContestLog> contestLogs, bool pdf)
        {
            string reportFileName = null;
            string fileName = null;
            string session = null;
            //string message = null;
            //string assisted = null;
            //string so2r = null;
            string year = DateTime.Now.ToString("yyyy");
            //List<QSO> validQsoList;

            // sort ascending by score
            _ContestLogs = contestLogs.OrderByDescending(o => (int)o.ActualScore).ToList();
            //contestLogs.Sort((s1, s2) => s1.ActualScore.CompareTo(s2.ActualScore));

            //_ContestLogs = contestLogs;

            session = contestLogs[0].Session.ToString();
            fileName = year + " CWO Box Scores Session " + session + ".pdf"; // later convert to PDF
            reportFileName = Path.Combine(ScoreFolder, fileName);

            if (File.Exists(reportFileName))
            {
                File.Delete(reportFileName);
            }

            _Document = new PdfDocument(PaperType.Letter, false, UnitOfMeasure.Inch, reportFileName);
            _DefaultFont = new Font("Helvetica", 9.0F, FontStyle.Regular);
            _PageNo = 1;

            // create PrintPdfDocument
            PdfImageControl ImageControl = new PdfImageControl();
            ImageControl.Resolution = 300.0;
            ImageControl.SaveAs = SaveImageAs.BWImage;
            PdfPrintDocument Print = new PdfPrintDocument(_Document, ImageControl);

            // the method that will print one page at a time to PrintDocument
            Print.PrintPage += PrintPage;

            // set margins 
            Print.SetMargins(1.0, 1.0, 1.0, 1.0);

            // crop the page image result to reduce PDF file size
            Print.PageCropRect = new RectangleF(0.95F, 0.95F, 6.6F, 9.1F);

            // initiate the printing process (calling the PrintPage method)
            // after the document is printed, add each page an an image to PDF file.
            Print.AddPagesToPdfDocument();

            // dispose of the PrintDocument object
            Print.Dispose();

            // create the PDF file
            _Document.CreateFile();
        }

        ////////////////////////////////////////////////////////////////////
        // Print each page of the document to PrintDocument class
        // You can use standard PrintDocument.PrintPage(...) method.
        // NOTE: The graphics origin is top left and Y axis is pointing down.
        // In other words this is not PdfContents printing.
        ////////////////////////////////////////////////////////////////////

        public void PrintPage(object sender, PrintPageEventArgs e)
        {
            ContestLog contestlog;
            string session = null;
            string message = null;
            string assisted = null;
            string so2r = null;
            string year = DateTime.Now.ToString("yyyy");
            List<QSO> validQsoList;

            // graphics object short cut
            Graphics G = e.Graphics;

            // Set everything to high quality
            G.SmoothingMode = SmoothingMode.HighQuality;
            G.InterpolationMode = InterpolationMode.HighQualityBicubic;
            G.PixelOffsetMode = PixelOffsetMode.HighQuality;
            G.CompositingQuality = CompositingQuality.HighQuality;

            // print area within margins
            Rectangle PrintArea = e.MarginBounds;

            // draw rectangle around print area
            //G.DrawRectangle(Pens.Black, PrintArea);

            // line height
            Int32 LineHeight = _DefaultFont.Height + 8;
            Rectangle TextRect = new Rectangle(PrintArea.X + 4, PrintArea.Y + 4, PrintArea.Width - 8, LineHeight);

            // display page bounds
            //String text = String.Format("Page Bounds: Left {0}, Top {1}, Right {2}, Bottom {3}", e.PageBounds.Left, e.PageBounds.Top, e.PageBounds.Right, e.PageBounds.Bottom);
            //G.DrawString(text, _DefaultFont, Brushes.Black, TextRect);
           // TextRect.Y += LineHeight;

            // display print area
            //text = String.Format("Page Margins: Left {0}, Top {1}, Right {2}, Bottom {3}", PrintArea.Left, PrintArea.Top, PrintArea.Right, PrintArea.Bottom);
            //G.DrawString(text, _DefaultFont, Brushes.Black, TextRect);
            //TextRect.Y += LineHeight;

            session = _ContestLogs[0].Session.ToString();
            message = year + " CW Open Session " + session;
            G.DrawString(message, _DefaultFont, Brushes.Black, TextRect);
            TextRect.Y += LineHeight;

            message = "Call" + "\t\t" + "Operator" + "\t" + "Station" + "\t" + "Name" + "\t" + "QSOs" + "\t" + "Mults" + "\t" + "Final" + "\t" + "Power" + "\t" + "Assisted"; // + "SO2R" + "\t"
            G.DrawString(message, _DefaultFont, Brushes.Black, TextRect);
            TextRect.Y += LineHeight;

            // print some lines
            for (; ; _LineNo++)
            //foreach (ContestLog contestlog in contestLogs)
            {
                if (_LineNo == _ContestLogs.Count)
                {
                    e.HasMorePages = false;
                    return;
                }

                contestlog = _ContestLogs[_LineNo - 1];
                if (contestlog != null)
                {
                    assisted = "N";
                    so2r = "N";

                    // only look at valid QSOs
                    validQsoList = contestlog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO).ToList();

                    if (contestlog.LogHeader.Assisted == CategoryAssisted.Assisted)
                    {
                        assisted = "Y";
                    }

                    if (contestlog.SO2R == true)
                    {
                        so2r = "Y";
                    }

                    message = contestlog.LogOwner + "\t\t" + contestlog.Operator + "\t\t" + contestlog.Station + "\t" + contestlog.OperatorName + "\t" + validQsoList.Count.ToString() + "\t";
                    message = message + contestlog.Multipliers.ToString() + "\t" + contestlog.ActualScore.ToString() + "\t" + contestlog.LogHeader.Power + "\t" + assisted; // + "\t" + so2r

                    G.DrawString(message, _DefaultFont, Brushes.Black, TextRect);
                    TextRect.Y += LineHeight;
                }
                else
                {
                    return;
                }

                if (TextRect.Bottom > PrintArea.Bottom)
                {
                     break;
                }
            }
           

            // move on to next page
            _PageNo++;
            e.HasMorePages = true;  // _PageNo <= 150;
            return;
        }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="contestLog"></param>
    public void PrintRejectReport(ContestLog contestLog, string callsign)
        {
            string reportFileName = Path.Combine(ReportFolder, callsign + ".rpt");
            string message = null;

            // only look at valid QSOs
            List<QSO> inValidQsoList = contestLog.QSOCollection.Where(q => q.Status == QSOStatus.InvalidQSO).ToList();

            using (StreamWriter sw = File.CreateText(reportFileName))
            {
                foreach (QSO qso in inValidQsoList)
                {


                    // print QSO line and reject reason
                    // QSO: Freq, mode, date, time, op, sent s/n, opname, contact call, rx s/n, contact name, rejectReason
                    message = "QSO: " + "\t" + qso.Frequency + "\t" + qso.Mode + "\t" + qso.QsoDate + "\t" + qso.QsoTime + "\t" + qso.OperatorCall + "\t" + qso.SentSerialNumber.ToString() + "\t" +
                                qso.OperatorName + "\t" + qso.ContactCall + "\t" + qso.ReceivedSerialNumber.ToString() + "\t" + qso.ContactName; // + qso.RejectReasons[0];

                    foreach (var key in qso.RejectReasons.Keys)
                    {
                        var value = qso.RejectReasons[key];
                        message = message + "\t" + value.ToString();
                    }

                    if (qso.ExcessTimeSpan > 0)
                    {
                        message = message + "\t" + qso.ExcessTimeSpan.ToString() + "minutes";
                    }


                    sw.WriteLine(message);
                }
                //
            }


        }

        public void PrintInspectionReport(string fileName, string failReason)
        {
            //string fileNameWithPath = Path.Combine(InspectionFolder, inspectReasonFileName);

            string inspectFileName = Path.Combine(InspectionFolder, fileName);
            // string inspectReasonFileName = Path.Combine(InspectionFolder, fileName + ".txt");



            //create a text file with the reason for the rejection
            using (StreamWriter sw = File.CreateText(inspectFileName))
            {
                sw.WriteLine(failReason);
            }
        }

    } // end class
}
