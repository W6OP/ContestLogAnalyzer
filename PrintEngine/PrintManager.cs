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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contestLog"></param>
        public void PrintRejectReport(ContestLog contestLog, string callsign)
        {
            string reportFileName = Path.Combine(InspectionFolder, callsign);
            string message = null;

            using (StreamWriter sw = File.CreateText(reportFileName))
            {
                foreach (QSO qso in contestLog.QSOCollection)
                {
                    // print QSO line and reject reason
                    // QSO: Freq, mode, date, time, op, sent s/n, opname, contact call, rx s/n, contact name, rejectReason
                    message = "QSO: " + "\t" + qso.Frequency + "\t" + qso.Mode + "\t" + qso.QsoDate + "\t" + qso.QsoTime + "\t" + qso.OperatorCall + "\t" + qso.SentSerialNumber.ToString() +
                                qso.OperatorName + "\t" + qso.ContactCall + "\t" + qso.ReceivedSerialNumber.ToString() + "\t" + qso.ContactName; // + qso.RejectReasons[0];

                    // maybe do rejectReason(s) first
                    //foreach ( RejectReason reason in qso.RejectReasons)
                    //{
                    //    // need to enumerate dictionary
                    //}


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
