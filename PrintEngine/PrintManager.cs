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
        public void PrintRejectReport(ContestLog contestLog)
        {

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
