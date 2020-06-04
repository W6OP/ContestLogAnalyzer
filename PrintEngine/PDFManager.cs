using Spire.Pdf;
using Spire.Pdf.Graphics;
using Spire.Pdf.Tables;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using W6OP.ContestLogAnalyzer;

namespace W6OP.PrintEngine
{
    public class PDFManager
    {
        private string ContestDescription { get; set; }
        public string ScoreFolder { get; set; }

        private string Title { get; set; }
        private string Subject { get; set; }
        private string Keywords { get; set; }
        private string Message { get; set; }

        private ContestName ActiveContest { get; set; }

        public PDFManager(ContestName contestName)
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

        public void PrintHQPPdfScoreSheet(List<ContestLog> contestLogs)
        {
            string reportFileName = null;
            string fileName = null;
            string session = null;
            string year = DateTime.Now.ToString("yyyy");
            string message = null;
            ContestLog contestlog;
            string assisted = null;
            List<QSO> validQsoList;

            //PrintCsvFile(contestLogs);

           // BuildDocument();

            // sort ascending by score
            contestLogs = contestLogs.OrderByDescending(o => (int)o.ActualScore).ToList();

            session = contestLogs[0].Session.ToString();
            fileName = year + ContestDescription + ".pdf"; // later convert to PDF
            reportFileName = Path.Combine(ScoreFolder, fileName);

            //Create a pdf document.
            PdfDocument doc = new PdfDocument();

            try
            {
                if (File.Exists(reportFileName))
                {
                    File.Delete(reportFileName);
                }

                //FileStream fs = new FileStream(reportFileName, FileMode.Create, FileAccess.Write, FileShare.None);

                ////Create a pdf document.
                //PdfDocument doc = new PdfDocument();
                PdfSection sec = doc.Sections.Add();
                sec.PageSettings.Width = PdfPageSize.A4.Width;
                PdfPageBase page = sec.Pages.Add();
                float y = 10;
                //title
                PdfBrush brush1 = PdfBrushes.Black;
                PdfTrueTypeFont font1 = new PdfTrueTypeFont(new Font("Arial", 16f, FontStyle.Bold));
                PdfStringFormat format1 = new PdfStringFormat(PdfTextAlignment.Center);
                page.Canvas.DrawString("Part Sales Information", font1, brush1, page.Canvas.ClientSize.Width / 2, y, format1);
                y += font1.MeasureString("Country List", format1).Height;
                y += 5;

             //   string[] data
             //= {
             //  "Call;Operator;Station;QSOs;Mults;Points;Final"
               
             //      };

                List<string> list = new List<string>();
                list.Add("Call;Operator;Station;QSOs;Mults;Points;Final");

                for (int a = 0; a < contestLogs.Count; a++)
                {
                    contestlog = contestLogs[a];
                    if (contestlog != null)
                    {
                        // only look at valid QSOs
                        validQsoList = contestlog.QSOCollection.Where(q => q.Status == QSOStatus.ValidQSO || q.Status == QSOStatus.ReviewQSO).ToList();
                        string line = contestlog.LogOwner
                                      + ";"
                                      + contestlog.Operator
                                      + ";"
                                      + contestlog.Station
                                      + ";"
                                      + contestlog.OperatorName
                                      + ";"
                                      + validQsoList.Count.ToString()
                                      + ";"
                                      + contestlog.Multipliers.ToString()
                                      + ";"
                                      + contestlog.TotalPoints.ToString()
                                      + ";"
                                      + contestlog.ActualScore.ToString();

                        list.Add(line);
                    }
                }

                //-------------------------------------

                //            String[] data
                //= {
                //    "Name;Capital;Continent;Area;Population",
                //    "Argentina;Buenos Aires;South America;2777815;32300003",
                //    "Bolivia;La Paz;South America;1098575;7300000",
                //    "Brazil;Brasilia;South America;8511196;150400000",
                //    "Canada;Ottawa;North America;9976147;26500000",
                //    };
                //            String[][] dataSource
                //                = new String[data.Length][];
                //            for (int i = 0; i < data.Length; i++)
                //            {
                //                dataSource[i] = data[i].Split(';');
                //            }


                //---------------------------------------

                string[] data = list.ToArray();

                string[][] dataSource = new string[data.Length][];

                for (int i = 0; i < data.Length; i++)
                {
                    dataSource[i] = data[i].Split(';');
                }

                PdfTable table = new PdfTable();
                table.Style.CellPadding = 2;
                table.Style.BorderPen = new PdfPen(brush1, 0.75f);
                table.Style.HeaderStyle.StringFormat = new PdfStringFormat(PdfTextAlignment.Center);
                table.Style.HeaderSource = PdfHeaderSource.Rows;
                table.Style.HeaderRowCount = 1;
                table.Style.ShowHeader = true;
                table.Style.HeaderStyle.BackgroundBrush = PdfBrushes.CadetBlue;
                table.DataSource = dataSource;
                foreach (PdfColumn column in table.Columns)
                {
                    column.StringFormat = new PdfStringFormat(PdfTextAlignment.Center, PdfVerticalAlignment.Middle);
                }
                table.Draw(page, new PointF(0, y));

                doc.SaveToFile(reportFileName);

            }
            catch (Exception ex)
            {
                string a = ex.Message;
            }
            finally
            {
                //doc.SaveToFile(reportFileName);
            }
        }

        //private void BuildDocument()
        //{
        //    // Create a new PDF document
        //    PdfDocument doc = new PdfDocument();
        //    PdfUnitConvertor unitCvtr = new PdfUnitConvertor();
        //    PdfMargins margin = new PdfMargins();
        //    margin.Top = unitCvtr.ConvertUnits(2.54f, PdfGraphicsUnit.Centimeter, PdfGraphicsUnit.Point);
        //    margin.Bottom = margin.Top;
        //    margin.Left = unitCvtr.ConvertUnits(3.17f, PdfGraphicsUnit.Centimeter, PdfGraphicsUnit.Point);
        //    margin.Right = margin.Left;
        //    PdfPageBase page = doc.Pages.Add(PdfPageSize.Letter, margin);
        //    //PdfBrush brush1 = PdfBrushes.Black;
        //    PdfTrueTypeFont font1 = new PdfTrueTypeFont(new Font("Arial", 16f, FontStyle.Bold));
        //    PdfStringFormat format1 = new PdfStringFormat(PdfTextAlignment.Center);

       
        //}

        private void PrintCsvFile(List<ContestLog> contestLogs)
        {
            throw new NotImplementedException();
        }
    } // end class
}
