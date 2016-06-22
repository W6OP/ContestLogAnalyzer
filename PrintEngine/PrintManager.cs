using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            // MAKE PDF




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
