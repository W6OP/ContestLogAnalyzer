using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W6OP.ContestLogAnalyzer
{
   public class QSO
    {
       private string _Frequency;
       public string Frequency
        {
            get { return _Frequency; }
            set { _Frequency = value; }
        }

        private string _Mode;
        public string Mode
        {
            get { return _Mode; }
            set { _Mode = value; }
        }

        /// <summary>
        /// This incorporates two fields, QSO Date and QSO Time
        /// 
        /// CHNGE TO DATE AND TIME LATER
        /// </summary>
        private string _QsoDate;
        public string QsoDate
        {
            get { return _QsoDate; }
            set { _QsoDate = value; }
        }

        private string _QsoTime;
        public string QsoTime
        {
            get { return _QsoTime; }
            set { _QsoTime = value; }
        }

        private string _OperatorSerialNumber;
        public string OperatorSerialNumber
        {
            get { return _OperatorSerialNumber; }
            set { _OperatorSerialNumber = value; }
        }

       private string _OperatorCall;
       public string OperatorCall
       {
           get { return _OperatorCall; }
           set { _OperatorCall = value; }
       }

       private string _OperatorName;

       public string OperatorName
       {
           get { return _OperatorName; }
           set { _OperatorName = value; }
       }

       private string _ContactSerialNumber;
       public string ContactSerialNumber
       {
           get { return ContactSerialNumber; }
           set { _ContactSerialNumber = value; }
       }

       private string _ContactCall;
       public string ContactCall
       {
           get { return _ContactCall; }
           set { _ContactCall = value; }
       }

       private string _ContactName;
       public string ContactName
       {
           get { return _ContactName; }
           set { _ContactName = value; }
       }


       /// <summary>
       /// Constructor.
       /// </summary>
       public QSO()
       {

       }
    } // end class
}
