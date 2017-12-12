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
            CallIsInValid = false;
            SessionIsValid = true;
        }

        private Dictionary<RejectReason, String> _RejectReasons = new Dictionary<RejectReason, String>();
        public Dictionary<RejectReason, String> RejectReasons
        {
            get { return _RejectReasons; }
        }

        private QSOStatus _Status = QSOStatus.ValidQSO;
        public QSOStatus Status
        {
            get => _Status;
            set => _Status = value;
        }
        //public QSOStatus Status { get; set; } = QSOStatus.ValidQSO;

        public QSO MatchingQSO { get; set; } = null;

        public QSO DupeListLocation { get; set; } = null;

        public List<QSO> DuplicateQsoList { get; set; } = new List<QSO>();

        public bool HasBeenPrinted { get; set; } = false;

        /// <summary>
        ///This is a duplicate QSO
        /// </summary>
        private bool _QSOIsDupe = false;
        public bool QSOIsDupe
        {
            get
            {
                return _QSOIsDupe;
            }
            set
            {
                if (value == true)
                {
                    _QSOIsDupe = true;
                    if (!RejectReasons.ContainsKey(RejectReason.DuplicateQSO))
                    {
                        _RejectReasons.Add(RejectReason.DuplicateQSO, EnumHelper.GetDescription(RejectReason.DuplicateQSO));
                        Status = QSOStatus.InvalidQSO;
                    }
                }
                else
                {
                    if (RejectReasons.ContainsKey(RejectReason.DuplicateQSO))
                    {
                        RejectReasons.Remove(RejectReason.DuplicateQSO);
                        _QSOIsDupe = false;
                        if (RejectReasons.Count == 0)
                        {
                            Status = QSOStatus.ValidQSO;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This property allows the rejected qso report to know there are duplicates of this call
        /// It will only be true if it is the qso counted as the valid qso not the dupe)
        /// </summary>
        public bool QSOHasDupes { get; set; }

        public string IncorrectName { get; set; }

        /// <summary>
        /// The operators call sign is invalid for this QSO
        /// </summary>
        public bool CallIsInValid
        {
            set
            {
                if (value == true)
                {
                    if (!RejectReasons.ContainsKey(RejectReason.InvalidCall))
                    {
                        _RejectReasons.Add(RejectReason.InvalidCall, EnumHelper.GetDescription(RejectReason.InvalidCall));
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
        public bool OpNameIsInValid
        {
            set
            {
                if (value == true)
                {
                    if (!RejectReasons.ContainsKey(RejectReason.OperatorName))
                    {
                        _RejectReasons.Add(RejectReason.OperatorName, EnumHelper.GetDescription(RejectReason.OperatorName));
                        Status = QSOStatus.InvalidQSO;
                    }
                }
                else
                {
                    if (RejectReasons.ContainsKey(RejectReason.OperatorName))
                    {
                        RejectReasons.Remove(RejectReason.OperatorName);
                        if (RejectReasons.Count == 0)
                        {
                            Status = QSOStatus.ValidQSO;
                        }
                    }
                }
            }
        }

        public bool EntityIsInValid
        {
            set
            {
                if (value == true)
                {
                    if (!RejectReasons.ContainsKey(RejectReason.InvalidEntity))
                    {
                        _RejectReasons.Add(RejectReason.InvalidEntity, EnumHelper.GetDescription(RejectReason.InvalidEntity));
                        Status = QSOStatus.InvalidQSO;
                    }
                }
                else
                {
                    if (RejectReasons.ContainsKey(RejectReason.InvalidEntity))
                    {
                        RejectReasons.Remove(RejectReason.InvalidEntity);
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
        /// The calsign does not match the call sign in the other log
        /// </summary>
        public bool CallIsBusted
        {
            set
            {
                if (value == true)
                {
                    if (!RejectReasons.ContainsKey(RejectReason.BustedCallSign))
                    {
                        _RejectReasons.Add(RejectReason.BustedCallSign, EnumHelper.GetDescription(RejectReason.BustedCallSign));
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
                if (value == true)
                {
                    if (!RejectReasons.ContainsKey(RejectReason.SerialNumber))
                    {
                        _RejectReasons.Add(RejectReason.SerialNumber, EnumHelper.GetDescription(RejectReason.SerialNumber));
                        Status = QSOStatus.InvalidQSO;
                    }
                }
                else
                {
                    if (RejectReasons.ContainsKey(RejectReason.SerialNumber))
                    {
                        RejectReasons.Remove(RejectReason.SerialNumber);
                        if (RejectReasons.Count == 0)
                        {
                            Status = QSOStatus.ValidQSO;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// This QSO belongs to the current session
        /// </summary>
        private bool _SessionIsValid = true;
        public bool SessionIsValid
        {
            get { return _SessionIsValid; }
            set
            {
                _SessionIsValid = value;
                if (value == false)
                {
                    if (!RejectReasons.ContainsKey(RejectReason.InvalidSession))
                    {
                        _RejectReasons.Add(RejectReason.InvalidSession, EnumHelper.GetDescription(RejectReason.InvalidSession));
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
            get
            {
                string qtime = QsoTime.Insert(2, ":");

                DateTime.TryParse(QsoDate + " " + qtime, out _QSODateTime);
                return _QSODateTime;
            }
        }

        public Int32 ExcessTimeSpan { get; set; }

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

        private string _OperatorCall;
        public string OperatorCall
        {
            get { return _OperatorCall.ToUpper(); }
            set { _OperatorCall = value; }
        }

        //later add Entity for HQP
        private string _OperatorName;
        public string OperatorName
        {
            get { return _OperatorName.ToUpper(); }
            set { _OperatorName = value; }
        }

        /// <summary>
        /// Not used for HQP
        /// </summary>
        public Int32 ReceivedSerialNumber { get; set; }
        
        /// <summary>
        /// Top level country - Used in HQP
        /// Applied to US and Canadian stations so I can check the state or province
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// The real or top level country of the  contact or DX station
        /// </summary>
        public string RealDXCountry { get; set; }

        /// <summary>
        /// Contact country - HQP - This can be state or Canadian province 2 letter code
        /// </summary>
        public string DXCountry { get; set; }

        /// <summary>
        /// Operator country as defined by HQP
        /// </summary>
        public string OperatorCountry { get; set; }

        /// <summary>
        /// For HQP contest
        /// </summary>
        public Int32 HQPPoints { get; set; }

        /// <summary>
        /// For HQP Contest
        /// </summary>
        public string HQPEntity { get; set; }

        /// <summary>
        /// For HQP Contest
        /// </summary>
        public bool IsHQPEntity { get; set; }

        private string _ContactCall;
        public string ContactCall
        {
            get
            { return _ContactCall.ToUpper(); }
            set
            {
                //strip "/" as in R7RF/6 or /QRP, etc.
                if (value.IndexOf("/") == -1)
                {
                    _ContactCall = value;
                }
                else
                {
                    _ContactCall = value.Substring(0, value.IndexOf("/"));
                }
            }
        }

        /// <summary>
        /// This is the call that he should have copied.
        /// </summary>
        public string BustedCallGuess { get; set; }

        private string _ContactName;
        public string ContactName
        {
            get { return _ContactName.ToUpper(); }
            set { _ContactName = value; }
        }

        /// <summary>
        /// This means an earlier QSO matched this one but this can't find the match because
        /// the call is busted and appears unique. I should probably analyse the list of calls 
        /// twice, ascending and descending to get all of these - 2 passes
        /// </summary>
        public bool HasMatchingQso { get; set; }
    } // end class
}
