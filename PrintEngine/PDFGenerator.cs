using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using W6OP.ContestLogAnalyzer;

namespace W6OP.PrintEngine
{
    public class PDFGenerator
    {
        private string ContestDescription { get; set; }
        public string ScoreFolder { get; set; }

        private string Title { get; set; }
        private string Subject { get; set; }
        private string Keywords { get; set; }
        private string Message { get; set; }

        private ContestName ActiveContest { get; set; }

        public PDFGenerator(ContestName contestName)
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

        public void PrintCWOpenPdfScoreSheet(List<ContestLog> contestLogs)
        {
            string reportFileName = null;
            string fileName = null;
            string session = null;
            string year = DateTime.Now.ToString("yyyy");
            string message = null;
            ContestLog contestlog;
            string assisted = null;
            //string so2r = null;
            List<QSO> validQsoList;

            // sort ascending by score
            contestLogs = contestLogs.OrderByDescending(o => (int)o.ActualScore).ToList();

            session = contestLogs[0].Session.ToString();
            message = year + Message + session;
            fileName = year + ContestDescription + session + ".pdf";
            reportFileName = Path.Combine(ScoreFolder, fileName);

            if (File.Exists(reportFileName))
            {
                File.Delete(reportFileName);
            }

            Document document = new Document(new PdfDocument(new PdfWriter(reportFileName)));

            Paragraph header = new Paragraph(message)
                .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA))
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetBackgroundColor(ColorConstants.LIGHT_GRAY);

            Table table = new Table(UnitValue.CreatePercentArray(9)).UseAllAvailableWidth();

            table.AddHeaderCell("Call");//.SetFontSize(12);
            table.AddHeaderCell("Operator");//.SetFontSize(12);
            table.AddHeaderCell("Station");//.SetFontSize(12);
            table.AddHeaderCell("Name");//.SetFontSize(12);
            table.AddHeaderCell("QSOs");//.SetFontSize(12);
            table.AddHeaderCell("Mults");//.SetFontSize(12);
            table.AddHeaderCell("Score");//.SetFontSize(12);
            table.AddHeaderCell("Power");//.SetFontSize(12);
            table.AddHeaderCell("Assisted");//.SetFontSize(12);

            for (int i = 0; i < contestLogs.Count; i++)
            {
                contestlog = contestLogs[i];
                assisted = "N";

                if (contestlog.LogHeader.Assisted == CategoryAssisted.Assisted)
                {
                    assisted = "Y";
                }

                if (contestlog != null)
                {
                    // only look at valid QSOs
                    validQsoList = contestlog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO).ToList();
                    table.AddCell(contestlog.LogOwner).SetFontSize(10);
                    table.AddCell(contestlog.Operator).SetFontSize(10);
                    table.AddCell(contestlog.Station).SetFontSize(10);
                    table.AddCell(contestlog.OperatorName).SetFontSize(10);
                    table.AddCell(validQsoList.Count.ToString()).SetFontSize(10);
                    table.AddCell(contestlog.Multipliers.ToString()).SetFontSize(10);
                    //table.AddCell(contestlog.TotalPoints.ToString()).SetFontSize(10);
                    table.AddCell(contestlog.ActualScore.ToString()).SetFontSize(10);
                    table.AddCell(contestlog.LogHeader.Power.ToString()).SetFontSize(10);
                    table.AddCell(assisted).SetFontSize(10);
                }
            }

            document.Add(table);

            document.Close();
        }


        public void PrintHQPPdfScoreSheet(List<ContestLog> contestLogs)
        {
            string reportFileName = null;
            string fileName = null;
            string session = null;
            string year = DateTime.Now.ToString("yyyy");
            string message = null;
            ContestLog contestlog;
            List<QSO> validQsoList;

            // sort ascending by score
            contestLogs = contestLogs.OrderByDescending(o => (int)o.ActualScore).ToList();

            message = year + Message;
            session = contestLogs[0].Session.ToString();
            fileName = year + ContestDescription + ".pdf"; // later convert to PDF
            reportFileName = Path.Combine(ScoreFolder, fileName);

            if (File.Exists(reportFileName))
            {
                File.Delete(reportFileName);
            }

            Document document = new Document(new PdfDocument(new PdfWriter(reportFileName)));

            Paragraph header = new Paragraph(message)
               .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA))
               .SetFontSize(14)
               .SetTextAlignment(TextAlignment.CENTER)
               .SetBackgroundColor(ColorConstants.LIGHT_GRAY);
            
            document.Add(header);

            Table table = new Table(UnitValue.CreatePercentArray(8)).UseAllAvailableWidth();

            table.AddHeaderCell("Call").SetFontSize(12);
            table.AddHeaderCell("Operator").SetFontSize(12);
            table.AddHeaderCell("Station").SetFontSize(12);
            table.AddHeaderCell("Entity").SetFontSize(12);
            table.AddHeaderCell("QSOs").SetFontSize(12);
            table.AddHeaderCell("Mults").SetFontSize(12);
            table.AddHeaderCell("Points").SetFontSize(12);
            table.AddHeaderCell("Score").SetFontSize(12);

            for (int i = 0; i < contestLogs.Count; i++)
            {
                contestlog = contestLogs[i];
                if (contestlog != null)
                {
                    // only look at valid QSOs
                    validQsoList = contestlog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO).ToList();
                    table.AddCell(contestlog.LogOwner).SetFontSize(10);
                    table.AddCell(contestlog.Operator).SetFontSize(10);
                    table.AddCell(contestlog.Station).SetFontSize(10);
                    table.AddCell(contestlog.QSOCollection[0].OperatorEntity).SetFontSize(10);
                    table.AddCell(validQsoList.Count.ToString()).SetFontSize(10);
                    table.AddCell(contestlog.Multipliers.ToString()).SetFontSize(10);
                    table.AddCell(contestlog.TotalPoints.ToString()).SetFontSize(10);
                    table.AddCell(contestlog.ActualScore.ToString()).SetFontSize(10);
                }
            }

            document.Add(table);

            document.Close();
        }

    } // end class
}
