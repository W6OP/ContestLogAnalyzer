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
            //_SessionIsValid = ValidateSession(session);
        }

        // need to track how many unique calls - multipliers
        // need way to track totally unique calls
        // some way to find similar calls

        //private QSOStatus _Status = QSOStatus.ValidQSO;
        public QSOStatus Status { get; set; }
        //{
        //    get { return _Status; }
        //    set { _Status = value; }
        //}

        /// <summary>
        /// Later make this RejectReasonDescription and have an enum for RejectReason
        /// </summary>
        public string RejectReason { get; set; }

        private bool _QSOIsDupe = false;
        public bool QSOIsDupe
        {
            get { return _QSOIsDupe; }
            set
            {
                _QSOIsDupe = value;
                if (_QSOIsDupe == true)
                {
                    Status = QSOStatus.InvalidQSO;
                    RejectReason = "Duplicate QSO.";
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
                    Status = QSOStatus.InvalidQSO;
                    RejectReason = "Invalid call sign.";
                }
            }
        }

        private bool _SessionIsValid = true;
        public bool SessionIsValid
        {
            get { return _SessionIsValid; }
            set
            {
                _SessionIsValid = value;
                if (_SessionIsValid == false)
                {
                    Status = QSOStatus.InvalidQSO;
                    RejectReason = "Invalid session date time.";
                }
            }
        }

        /// <summary>
        /// Indicates this QSO is the first one worked in this session and therefore a multiplier
        /// </summary>
        public bool IsMultiplier { get; set; }


        private string _Frequency;
        public string Frequency
        {
            get { return _Frequency; }
            set
            {
                _Frequency = value;
                Band = Utility.ConvertFrequencyToBand(Convert.ToDouble(_Frequency));
            }
        }

        //private Int32 _Band;
        public Int32 Band { get; set; }
        //{
        //    get { return _Band; }
        //    set { _Band = value; }
        //}

        // this should be an enum
        //private string _Mode;
        public string Mode { get; set; }
        //{
        //    get { return _Mode; }
        //    set { _Mode = value; }
        //}

        /// <summary>
        /// This incorporates two fields, QSO Date and QSO Time
        /// 
        /// CHANGE TO DATE AND TIME LATER
        /// </summary>
        //private string _QsoDate;
        public string QsoDate { get; set; }
        //{
        //    get { return _QsoDate; }
        //    set { _QsoDate = value; }
        //}

        //private string _QsoTime;
        public string QsoTime { get; set; }
        //{
        //    get { return _QsoTime; }
        //    set { _QsoTime = value; }
        //}
        ///// ////////////////////////////////////////////////////

        //private Int32 _SentSerialNumber;
        public Int32 SentSerialNumber { get; set; }
        //{
        //    get { return _SentSerialNumber; }
        //    set { _SentSerialNumber = value; }
        //}

        //private string _OperatorCall;
        public string OperatorCall { get; set; }
        //{
        //    get { return _OperatorCall; }
        //    set { _OperatorCall = value; }
        //}

        //private string _OperatorName;
        public string OperatorName { get; set; }
        //{
        //    get { return _OperatorName; }
        //    set { _OperatorName = value; }
        //}

        //private Int32 _ReceivedSerialNumber;
        public Int32 ReceivedSerialNumber { get; set; }
        //{
        //    get { return _ReceivedSerialNumber; }
        //    set { _ReceivedSerialNumber = value; }
        //}

        //private string _ContactCall;
        public string ContactCall { get; set; }
        //{
        //    get { return _ContactCall; }
        //    set { _ContactCall = value; }
        //}

        //private string _ContactName;
        public string ContactName { get; set; }
        //{
        //    get { return _ContactName; }
        //    set { _ContactName = value; }
        //}
    } // end class
}
