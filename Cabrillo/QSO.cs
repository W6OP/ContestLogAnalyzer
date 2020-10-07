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

        #region Common

        /// <summary>
        /// Indicates a QSO this operator does not get credit for
        /// but others do.
        /// </summary>
        private bool _IsXQSO;
        public bool IsXQSO { 
            get
            {
                //Console.WriteLine("xqso get");
                return _IsXQSO;
            }
            set {
                _IsXQSO = value;
                Console.WriteLine("xqso set: " + value);
            }
        }

        /// <summary>
        /// The log reference for a particuler QSO.
        /// </summary>
        public ContestLog ParentLog { get; set; }

        /// <summary>
        /// A collection of reasons a log was rejected.
        /// Only one reason is ever used so this can be changed to a single item.
        /// </summary>
       // private Dictionary<RejectReason, string> rejectReasons = new Dictionary<RejectReason, string>();
        //public Dictionary<RejectReason, string> GetRejectReasons()
        //{ return rejectReasons; }

        public RejectReason ReasonRejected { get; set; }

        /// <summary>
        /// The status of the QSO.
        /// </summary>
        private QSOStatus _Status = QSOStatus.ValidQSO;
        public QSOStatus Status
        {
            get => _Status;
            set => _Status = value;
        }

        /// <summary>
        /// The QSO in another log that matches this QSO.
        /// </summary>
        private QSO _MatchingQSO;
        public QSO MatchingQSO
        {
            get => _MatchingQSO;
            set => _MatchingQSO = value;
        }

        /// <summary>
        /// Location of duplicate QSOs.
        /// </summary>
        public QSO DupeListLocation { get; set; } = null;

        /// <summary>
        /// List of duplicates of this QSO.
        /// </summary>
        public List<QSO> DuplicateQsoList { get; set; } = new List<QSO>();

        /// <summary>
        /// Indicates this has been printed.
        /// </summary>
        public bool HasBeenPrinted { get; set; } = false;

        /// <summary>
        ///This is a duplicate QSO.
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
                    ReasonRejected = RejectReason.DuplicateQSO;
                    _Status = QSOStatus.InvalidQSO;     
                }
                else
                {
                    ReasonRejected = RejectReason.None;
                    _QSOIsDupe = false;
                    _Status = QSOStatus.ValidQSO;
                }
            }
        }

        /// <summary>
        /// This property allows the rejected qso report to know there are duplicates of this call
        /// It will only be true if it is the qso counted as the valid qso not the dupe
        /// </summary>
        public bool QSOHasDupes { get; set; }

        /// <summary>
        /// The operators call sign is invalid for this QSO
        /// </summary>
        public bool CallIsInValid
        {
            set
            {
                if (value == true)
                {
                    ReasonRejected = RejectReason.InvalidCall;
                    _Status = QSOStatus.InvalidQSO;
                }
                else
                {
                    ReasonRejected = RejectReason.None;
                    _Status = QSOStatus.ValidQSO;
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
                    ReasonRejected = RejectReason.BustedCallSign;
                    _Status = QSOStatus.InvalidQSO;
                }
                else
                {
                    ReasonRejected = RejectReason.None;
                    _Status = QSOStatus.ValidQSO;
                }
            }
        }

        /// <summary>
        /// Call sign of the operator.
        /// </summary>
        private string _OperatorCall;
        public string OperatorCall
        {
            get { return _OperatorCall; }
            set { _OperatorCall = value;}
        }

        /// <summary>
        /// Name of the operator.
        /// </summary>
        private string _OperatorName;
        public string OperatorName
        {
            get { return _OperatorName; }
            set { _OperatorName = value; }
        }

        /// <summary>
        /// Call sign of the contact.
        /// </summary>
        private string _ContactCall;
        public string ContactCall
        {
            get { return _ContactCall; }
            set { _ContactCall = value;}
        }

        /// <summary>
        /// This is the call that he should have copied.
        /// </summary>
        public string BustedCallGuess { get; set; }

        private string _ContactName;
        public string ContactName
        {
            get { return _ContactName; }
            set
            {
                _ContactName = value;
            }
        }

        /// <summary>
        /// This means an earlier QSO matched this one but this can't find the match because
        /// the call is busted and appears unique. I should probably analyse the list of calls 
        /// twice, ascending and descending to get all of these - 2 passes
        /// </summary>
        public bool HasMatchingQso { get; set; }

        #endregion

        #region CWOpen

        /// <summary>
        /// CWOpen only?
        /// The operators name does not match for this QSO.
        /// </summary>
        public bool OpNameIsInValid
        {
            set
            {
                if (value == true)
                {
                    ReasonRejected = RejectReason.OperatorName;
                    _Status = QSOStatus.InvalidQSO;
                }
                else
                {
                    ReasonRejected = RejectReason.None;
                    _Status = QSOStatus.ValidQSO;
                }
            }
        }

        public bool ContactNameIsInValid
        {
            set
            {
                if (value == true)
                {
                    ReasonRejected = RejectReason.ContactName;
                    _Status = QSOStatus.InvalidQSO;
                }
                else
                {
                    ReasonRejected = RejectReason.None;
                    _Status = QSOStatus.ValidQSO;
                }
            }
        }

        /// <summary>
        /// CWOpen only.
        /// Lists the incorrect name if available.
        /// </summary>
        private string _IncorrectName = null;
        public string IncorrectName
        {
            get => _IncorrectName;
            set => _IncorrectName = value;
        }

        /// <summary>
        /// Frequency of the exchange - may only need band.
        /// </summary>
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
        /// Indicates this QSO is the first one worked in this session and therefore a multiplier.
        /// </summary>
        public bool IsMultiplier { get; set; } = false;

        /// <summary>
        /// Band the QSO was on.
        /// </summary>
        public int Band { get; set; }

        /// <summary>
        /// Mode used for the QSO.
        /// this should be an enum
        /// Later change this to use CategoryMode Enum
        /// </summary>
        private string mode;
        public string Mode
        {
            get { return mode; }
            set
            {
                mode = value;
                SetHQPPoints();
            }
        }

        // determine points by mode for the HQP
        private void SetHQPPoints()
        {
            try
            {
                CategoryMode catMode = (CategoryMode)Enum.Parse(typeof(CategoryMode), Mode);

                switch (catMode)
                {
                    case CategoryMode.CW:
                        HQPPoints = 3;
                        break;
                    case CategoryMode.RTTY:
                        HQPPoints = 3;
                        break;
                    case CategoryMode.RY:
                        HQPPoints = 3;
                        break;
                    case CategoryMode.PH:
                        HQPPoints = 2;
                        break;
                    case CategoryMode.SSB:
                        HQPPoints = 2;
                        break;
                    default:
                        HQPPoints = 0;
                        break;
                }
            }
            catch (Exception)
            {
                throw new Exception("The mode " + Mode + " is not valid for this contest.");
            };
        }


        /// <summary>
        /// Date/Time of the QSO.
        /// Could this be a window?
        /// </summary>
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

        /// <summary>
        /// The amount of time a QSO is off by.
        /// </summary>
        public Int32 ExcessTimeSpan { get; set; }

        /// <summary>
        /// This incorporates two fields, QSO Date and QSO Time
        /// 
        /// CHANGE TO DATE AND TIME LATER
        /// </summary>
        public string QsoDate { get; set; }

        public string QsoTime { get; set; }

        /// <summary>
        /// CWOpen only.
        /// The sent serial number.
        /// </summary>
        public Int32 SentSerialNumber { get; set; }

        /// <summary>
        /// Not used for HQP
        /// </summary>
        public Int32 ReceivedSerialNumber { get; set; }

        /// <summary>
        /// CWOpen only.
        /// The serial number does not match the other log.
        /// </summary>
        public bool SerialNumberIsIncorrect
        {
            set
            {
                if (value == true)
                {
                    ReasonRejected = RejectReason.SerialNumber;
                    _Status = QSOStatus.InvalidQSO;
                }
                else
                {
                    ReasonRejected = RejectReason.None;
                    _Status = QSOStatus.ValidQSO;
                }
            }
        }


        /// <summary>
        /// CWOpen only.
        /// This QSO belongs to the current session.
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
                    ReasonRejected = RejectReason.InvalidSession;
                    _Status = QSOStatus.InvalidQSO;
                }
                else
                {
                    ReasonRejected = RejectReason.None;
                    _Status = QSOStatus.ValidQSO;
                }
            }
        }
        #endregion

        #region HQP (Hawaiin QSO Party)

        // the raw text of the qso line
        public string RawQSO { get; set; }

        public string OperatortPrefix { get; set; }
        public string OperatorSuffix { get; set; }
        public string ContactPrefix { get; set; }
        public string ContactSuffix { get; set; }

        /// <summary>
        /// HQP only.
        /// Lists the incorrect entity if available.
        /// </summary>
        private string _IncorrectDXEntity = null;
        public string IncorrectDXEntity
        {
            get => _IncorrectDXEntity;
            set => _IncorrectDXEntity = value;
        }

        /// <summary>
        /// The contact entity in the log before any lookups
        /// </summary>
        private string _OriginalContactEntity;
        public string OriginalContactEntity
        {
            get { return _OriginalContactEntity; }
            set
            {
                _OriginalContactEntity = value;
                ContactEntity = value;
            }
        }


        /// <summary>
        /// The operator entity in the log before any lookups
        /// </summary>
        private string _OriginalOperatorEntity;
        public string OriginalOperatorEntity
        {
            get { return _OriginalOperatorEntity; }
            set
            {
                _OriginalOperatorEntity = value;
                OperatorEntity = value;
            }
        }

        /// <summary>
        /// Operator country as defined by HQP
        /// Length is 2 characters for US and Canada
        /// or 3 characters if it is a HQP entity
        /// This is equivalent to the Operator Name for the CWOpen
        /// </summary>
        private string _OperatorEntity;
        public string OperatorEntity
        {
            get { return _OperatorEntity; }
            set { _OperatorEntity = value; }
        }

        /// <summary>
        /// The real or top level country of the  contact or DX station
        /// This is the long name. Cannot be null!
        /// </summary>
        private string _ContactCountry = "Unknown";
        public string ContactCountry
        {
            get => _ContactCountry;
            set {_ContactCountry = value;}
        }

        /// <summary>
        /// HQP only.
        /// The entity does not match this QSO.
        /// </summary>
        private bool _EntityIsInValid = false;
        public bool EntityIsInValid
        {
            set
            {
                _EntityIsInValid = value;
                if (value == true)
                {
                    ReasonRejected = RejectReason.InvalidEntity;
                    _Status = QSOStatus.InvalidQSO;
                }
                else
                {
                    ReasonRejected = RejectReason.None;
                    _Status = QSOStatus.ValidQSO;
                }
            }
            get
            {
                return _EntityIsInValid;
            }
        }

        /// <summary>
        /// Contact country - HQP - This can be state or Canadian province 2 letter code
        /// or 3 letter HQP entity code
        /// This is equivalent to the Contact Name for the CWOpen
        /// </summary>
        //public string DXEntity { get; set; }
        private string _ContactEntity;
        public string ContactEntity
        {
            get
            { return _ContactEntity; }
            set{_ContactEntity = value;}
        }
        /// <summary>
        /// For HQP contest
        /// </summary>
        public int HQPPoints { get; set; }

        /// <summary>
        /// For HQP Contest
        /// </summary>
        public string HQPEntity { get; set; }

        /// <summary>
        /// For HQP Contest
        /// </summary>
        public bool IsHQPEntity { get; set; }


        #endregion

    } // end class
}
