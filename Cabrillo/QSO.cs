using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W6OP.ContestLogAnalyzer
{
    [Serializable()]
    public class QSO
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public QSO()
        {

        }

        // need to track how many unique calls - multipliers
       // need way to track totally unique calls
       // some way to find similar calls

        private QSOStatus _Status = QSOStatus.ValidQSO;
        public QSOStatus Status
        {
            get { return _Status; }
            set { _Status = value; }
        }

       /// <summary>
       /// Later make this RejectReasonDescription and have an enum for RejectReason
       /// </summary>
        private string _RejectReason;
        public string RejectReason
        {
            get { return _RejectReason; }
            set { _RejectReason = value; }
        }

        private bool _QSOIsDupe = false;
        public bool QSOIsDupe
        {
            get { return _QSOIsDupe; }
            set 
            { 
                _QSOIsDupe = value;
                if (_QSOIsDupe == true)
                {
                    _Status = QSOStatus.InvalidQSO;
                    _RejectReason = "Duplicate QSO.";
                }
            }
        }

        private bool _CallIsValid = true;
        public bool CallIsValid
        {
            get { return _CallIsValid; }
            set 
            { 
                _CallIsValid = value;
                if (_CallIsValid == false)
                {
                    _Status = QSOStatus.InvalidQSO;
                    _RejectReason = "Invalid call sign.";
                }
            }
        }

       private string _Frequency;
       public string Frequency
        {
            get { return _Frequency; }
            set 
            { 
                _Frequency = value;
                _Band = Utility.ConvertFrequencyToBand(Convert.ToDouble(_Frequency));
            }
        }

       private Int32 _Band;
       public Int32 Band
       {
           get { return _Band; }
           set { _Band = value; }
       }

        // this should be an enum
        private string _Mode;
        public string Mode
        {
            get { return _Mode; }
            set { _Mode = value; }
        }

        /// <summary>
        /// This incorporates two fields, QSO Date and QSO Time
        /// 
        /// CHANGE TO DATE AND TIME LATER
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
       ///// ////////////////////////////////////////////////////

        private Int32 _SentSerialNumber;
        public Int32 SentSerialNumber
        {
            get { return _SentSerialNumber; }
            set { _SentSerialNumber = value; }
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

        private Int32 _ReceivedSerialNumber;
        public Int32 ReceivedSerialNumber
        {
            get { return _ReceivedSerialNumber; }
            set { _ReceivedSerialNumber = value; }
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
    } // end class
}
