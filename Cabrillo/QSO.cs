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
        }

        //private QSOStatus _Status = QSOStatus.ValidQSO;
        public QSOStatus Status { get; set; } = QSOStatus.ValidQSO;

        /// <summary>
        ///Is this a duplicate QSO
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

        /// <summary>
        /// The operators call sign is invalid for this QSO
        /// </summary>
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

        /// <summary>
        /// The operators name does not match for this QSO
        /// </summary>
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

        /// <summary>
        /// The call is busted - do I want a sub reason, is it call or serial number?
        /// The calsign does not mact the call sign in the ther log
        /// </summary>
        public bool CallIsBusted
        {
            set
            {
                if (value == false)
                {
                    if (!RejectReasons.ContainsKey(RejectReason.BustedCallSign))
                    {
                        _RejectReasons.Add(RejectReason.BustedCallSign, QSOStatus.InvalidQSO);
                        Status = QSOStatus.InvalidQSO;
                    }
                }
                else
                {
                    if (RejectReasons.ContainsKey(RejectReason.BustedCallSign))
                    {
                        RejectReasons.Remove(RejectReason.BustedCallSign);
                        if (RejectReasons.Count == 0)
                        {
                            Status = QSOStatus.ValidQSO;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The serial number does not match the other log
        /// </summary>
        public bool SerialNumberIsIncorrect
        {
            set
            {
                if (value == false)
                {
                    if (!RejectReasons.ContainsKey(RejectReason.BustedSerialNumber))
                    {
                        _RejectReasons.Add(RejectReason.BustedSerialNumber, QSOStatus.InvalidQSO);
                        Status = QSOStatus.InvalidQSO;
                    }
                }
                else
                {
                    if (RejectReasons.ContainsKey(RejectReason.BustedSerialNumber))
                    {
                        RejectReasons.Remove(RejectReason.BustedSerialNumber);
                        if (RejectReasons.Count == 0)
                        {
                            Status = QSOStatus.ValidQSO;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// This QSO does not belong to the current session
        /// </summary>
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

        
        private DateTime _QSODateTime;
        public DateTime QSODateTime
        {
            get {
                string qtime = QsoTime.Insert(2, ":");

                DateTime.TryParse(QsoDate + " " + qtime, out _QSODateTime);
                return _QSODateTime;
            }
        }


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
