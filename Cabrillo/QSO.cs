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
        /// Constructor. Initialize properties.
        /// </summary>
        public QSO()
        {
            QSOIsDupe = false;
            CallIsValid = true;
            SessionIsValid = true;
        }

        private Dictionary<RejectReason, QSOStatus> _RejectReasons = new Dictionary<RejectReason, QSOStatus>();
        public Dictionary<RejectReason, QSOStatus> RejectReasons
        {
            get { return _RejectReasons; }
            //set { }
        }

        //private QSOStatus _Status = QSOStatus.ValidQSO;
        public QSOStatus Status { get; set; } = QSOStatus.ValidQSO;

        /// <summary>
        ///
        /// </summary>
        public bool QSOIsDupe
        {
            set
            {
                if (value == true)
                {
                    if (!RejectReasons.ContainsKey(RejectReason.Duplicate))
                    {
                        _RejectReasons.Add(RejectReason.Duplicate, QSOStatus.InvalidQSO);
                        Status = QSOStatus.InvalidQSO;
                    }
                }
                else
                {
                    if (RejectReasons.ContainsKey(RejectReason.Duplicate))
                    {
                        RejectReasons.Remove(RejectReason.Duplicate);
                        if (RejectReasons.Count == 0)
                        {
                            Status = QSOStatus.ValidQSO;
                        }
                    }
                }
            }
        }

        public bool CallIsValid
        {
            //get { return CallIsValid; }
            set
            {
                if (value == false)
                {
                    if (!RejectReasons.ContainsKey(RejectReason.InvalidCall))
                    {
                        _RejectReasons.Add(RejectReason.InvalidCall, QSOStatus.InvalidQSO);
                        Status = QSOStatus.InvalidQSO;
                    }
                }
                else
                {
                    if (RejectReasons.ContainsKey(RejectReason.InvalidCall))
                    {
                        RejectReasons.Remove(RejectReason.InvalidCall);
                        if (RejectReasons.Count == 0)
                        {
                            Status = QSOStatus.ValidQSO;
                        }
                    }
                }
            }
        }

        public bool NameIsValid
        {
            set
            {
                if (value == false)
                {
                    if (!RejectReasons.ContainsKey(RejectReason.OpName))
                    {
                        _RejectReasons.Add(RejectReason.OpName, QSOStatus.InvalidQSO);
                        Status = QSOStatus.InvalidQSO;
                    }
                }
                else
                {
                    if (RejectReasons.ContainsKey(RejectReason.OpName))
                    {
                        RejectReasons.Remove(RejectReason.OpName);
                        if (RejectReasons.Count == 0)
                        {
                            Status = QSOStatus.ValidQSO;
                        }
                    }
                }
            }
        }

        public bool SessionIsValid
        {
            set
            {
                if (value == false)
                {
                    if (!RejectReasons.ContainsKey(RejectReason.InvalidSession))
                    {
                        _RejectReasons.Add(RejectReason.InvalidSession, QSOStatus.InvalidQSO);
                        Status = QSOStatus.InvalidQSO;
                    }
                }
                else
                {
                    if (RejectReasons.ContainsKey(RejectReason.InvalidSession))
                    {
                        RejectReasons.Remove(RejectReason.InvalidSession);
                        if (RejectReasons.Count == 0)
                        {
                            Status = QSOStatus.ValidQSO;
                        }
                    }
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
                Band = Utility.ConvertFrequencyToBand(Convert.ToDouble(_Frequency));
            }
        }

        /// <summary>
        /// Indicates this QSO is the first one worked in this session and therefore a multiplier
        /// </summary>
        public bool IsMultiplier { get; set; } = false;

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
