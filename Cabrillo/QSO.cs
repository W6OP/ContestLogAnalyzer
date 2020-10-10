using System;
using System.Collections.Generic;
using System.Dynamic;
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
            //QSOIsDupe = false;
            //CallIsInValid = false;
            //SessionIsValid = true;
        }

        #region Common Fields

        private bool qsoIsDupe = false;
        private DateTime qsoDateTime;
        private string frequency;
        private string mode;

        #endregion

        #region Common Properties

        // Indicates a log for this call does not exist
        public bool NoMatchingLog { get; set; }

        // additional qsos found when no matching log exists - just for reference right now
        public List<QSO> AdditionalQSOs { get; set; } = new List<QSO>();

        public bool IsUniqueCall { get; set; }
        /// <summary>
        /// Indicates a QSO this operator does not get credit for
        /// but others do.
        /// </summary>
        public bool IsXQSO { get; set; }

        /// <summary>
        /// The log reference for a particuler QSO.
        /// </summary>
        public ContestLog ParentLog { get; set; }

        /// <summary>
        /// The reason a qso was rejected.
        /// </summary>
        public RejectReason ReasonRejected { get; set; }

        /// <summary>
        /// The status of the QSO.
        /// </summary>
        public QSOStatus Status { get; set; }

        /// <summary>
        /// The QSO in another log that matches this QSO.
        /// </summary>
        public QSO MatchingQSO { get; set; }

        public QSO FirstMatchingQSO { get; set; }


        public List<QSO> NearestMatches { get; set; } = new List<QSO>();

        /// <summary>
        /// This means an earlier QSO matched this one.
        /// </summary>
        public bool HasBeenMatched { get; set; }

        /// <summary>
        /// This property allows the rejected qso report to know there are duplicates of this call
        /// It will only be true if it is the qso counted as the valid qso not the dupe
        /// </summary>
        public bool QSOHasDupes { get; set; }

        /// <summary>
        /// Indicates this has been printed.
        /// </summary>
        public bool HasBeenPrinted { get; set; }

        /// <summary>
        /// This incorporates two fields, QSO Date and QSO Time
        /// 
        /// CHANGE TO DATE AND TIME LATER
        /// ARE THES REALLY NEEDED SINCE WE HAVE COMBINDED DATE/TIME
        /// </summary>
        public string QsoDate { get; set; }

        public string QsoTime { get; set; }


        /// <summary>
        /// Date/Time of the QSO.
        /// Could this be a window?
        /// </summary>
        public DateTime QSODateTime
        {
            get
            {
                string qtime = QsoTime.Insert(2, ":");

                DateTime.TryParse(QsoDate + " " + qtime, out qsoDateTime);
                return qsoDateTime;
            }
        }

       

        /// <summary>
        ///Marks this qso as a duplicate QSO.
        /// </summary>
        public bool IsDuplicateMatch
        {
            get
            {
                return qsoIsDupe;
            }
            set
            {
                if (value == true)
                {
                    qsoIsDupe = true;
                    ReasonRejected = RejectReason.DuplicateQSO;
                    Status = QSOStatus.InvalidQSO;     
                }
                else
                {
                    ReasonRejected = RejectReason.None;
                    qsoIsDupe = false;
                    Status = QSOStatus.ValidQSO;
                }
            }
        }

       

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
                    Status = QSOStatus.InvalidQSO;
                }
                else
                {
                    ReasonRejected = RejectReason.None;
                    Status = QSOStatus.ValidQSO;
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
                    Status = QSOStatus.InvalidQSO;
                }
                else
                {
                    ReasonRejected = RejectReason.None;
                    Status = QSOStatus.ValidQSO;
                }
            }
        }

        /// <summary>
        /// Call sign of the operator.
        /// </summary>
        public string OperatorCall { get; set; }


        /// <summary>
        /// Name of the operator.
        /// </summary>
        public string OperatorName { get; set; }
        

        /// <summary>
        /// Call sign of the contact.
        /// </summary>
        public string ContactCall { get; set; }


        /// <summary>
        /// This is the call that he should have copied.
        /// </summary>
        public string BustedCallGuess { get; set; }

        public string ContactName { get; set; }

        /// <summary>
        /// Frequency of the exchange - may only need band.
        /// </summary>
        public string Frequency
        {
            get { return frequency; }
            set
            {
                frequency = value;
                Band = Utility.ConvertFrequencyToBand(Convert.ToDouble(frequency));
            }
        }

        /// <summary>
        /// Indicates this QSO is the first one worked in this session and therefore a multiplier.
        /// </summary>
        public bool IsMultiplier { get; set; }

        /// <summary>
        /// Band the QSO was on.
        /// </summary>
        public int Band { get; set; }

        /// <summary>
        /// Mode used for the QSO.
        /// this should be an enum
        /// Later change this to use CategoryMode Enum
        /// </summary>
        public string Mode
        {
            get { return mode; }
            set
            {
                mode = value;
                SetHQPPoints();
            }
        }

        #endregion

        #region CWOpen Fields

       
        private bool sessionIsValid = true;

        #endregion

        #region CWOpen Properties

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
                    Status = QSOStatus.InvalidQSO;
                }
                else
                {
                    ReasonRejected = RejectReason.None;
                    Status = QSOStatus.ValidQSO;
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
                    Status = QSOStatus.InvalidQSO;
                }
                else
                {
                    ReasonRejected = RejectReason.None;
                    Status = QSOStatus.ValidQSO;
                }
            }
        }

        /// <summary>
        /// CWOpen only.
        /// Lists the incorrect name if available.
        /// THIS NEEDS CHECKING
        /// </summary>
        public string IncorrectName { get; set; }
       
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
                    Status = QSOStatus.InvalidQSO;
                }
                else
                {
                    ReasonRejected = RejectReason.None;
                    Status = QSOStatus.ValidQSO;
                }
            }
        }

        /// <summary>
        /// CWOpen only.
        /// This QSO belongs to the current session.
        /// </summary>
        public bool SessionIsValid
        {
            get { return sessionIsValid; }
            set
            {
                sessionIsValid = value;
                if (value == false)
                {
                    ReasonRejected = RejectReason.InvalidSession;
                    Status = QSOStatus.InvalidQSO;
                }
                else
                {
                    ReasonRejected = RejectReason.None;
                    Status = QSOStatus.ValidQSO;
                }
            }
        }
        #endregion

        #region HQP Fields

        private string originalContactEntity;
        private string originalOperatorEntity;
        private bool entityIsInValid = false;
        private bool sentEntityIsInValid = false;

        #endregion

        #region HQP Properties

        // the raw text of the qso line
        public string RawQSO { get; set; }
        public string OperatorPrefix { get; set; }
        public string OperatorSuffix { get; set; }
        public string ContactPrefix { get; set; }
        public string ContactSuffix { get; set; }
        public string SentReport { get; set; }
        public string ReceivedReport { get; set; }

        /// <summary>
        /// HQP only.
        /// Lists the incorrect entity if available.
        /// </summary>
        public string IncorrectDXEntity { get; set; }
        
        /// <summary>
        /// CHECK ON THIS
        /// The contact entity in the log before any lookups
        /// </summary>
       
        //public string OriginalContactEntity
        //{
        //    get { return originalContactEntity; }
        //    set
        //    {
        //        originalContactEntity = value;
        //        ContactEntity = value;
        //    }
        //}


        /// <summary>
        /// The operator entity in the log before any lookups
        /// </summary>

        //public string OriginalOperatorEntity
        //{
        //    get { return originalOperatorEntity; }
        //    set
        //    {
        //        originalOperatorEntity = value;
        //        OperatorEntity = value;
        //    }
        //}

        /// <summary>
        /// Operator country as defined by HQP
        /// Length is 2 characters for US and Canada
        /// or 3 characters if it is a HQP entity
        /// This is equivalent to the Operator Name for the CWOpen
        /// </summary>
        public string OperatorEntity { get; set; }

        /// <summary>
        /// The real or top level country of the  contact or DX station
        /// This is the long name. Cannot be null!
        /// </summary>
        public string ContactCountry { get; set; }

        public string OperatorCountry { get; set; }

        /// <summary>
        /// HQP only.
        /// The entity does not match this QSO.
        /// </summary>

        public bool EntityIsInValid
        {
            set
            {
                entityIsInValid = value;
                if (value == true)
                {
                    ReasonRejected = RejectReason.InvalidEntity;
                    Status = QSOStatus.InvalidQSO;
                }
                else
                {
                    ReasonRejected = RejectReason.None;
                    Status = QSOStatus.ValidQSO;
                }
            }
            get
            {
                return entityIsInValid;
            }
        }

        /// <summary>
        /// HQP only.
        /// The entity does not match this QSO.
        /// </summary>
        public bool SentEntityIsInValid
        {
            set
            {
                sentEntityIsInValid = value;
                if (value == true)
                {
                    ReasonRejected = RejectReason.InvalidSentEntity;
                    Status = QSOStatus.InvalidQSO;
                }
                else
                {
                    ReasonRejected = RejectReason.None;
                    Status = QSOStatus.ValidQSO;
                }
            }
            get
            {
                return sentEntityIsInValid;
            }
        }

        /// <summary>
        /// Contact country - HQP - This can be state or Canadian province 2 letter code
        /// or 3 letter HQP entity code
        /// This is equivalent to the Contact Name for the CWOpen
        /// </summary>
        //public string DXEntity { get; set; }
        public string ContactEntity { get; set; }

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

        #region Methods

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

        #endregion
    } // end class
}
