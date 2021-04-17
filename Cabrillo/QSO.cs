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
        /// Constructor.
        /// </summary>
        public QSO()
        {
        }

        #region Common Fields

        private bool qsoIsDupe = false;
        private DateTime qsoDateTime;
        private string frequency;
        private QSOMode mode;

        #endregion

        #region Common Properties

        // Indicates a log for this call does not exist
        public bool NoMatchingLog { get; set; }

        public bool IsUniqueCall { get; set; }

        // Indicates a QSO this operator does not get credit for but others do.
        public bool IsXQSO { get; set; }

        // The log reference for a particuler QSO.
        public ContestLog ParentLog { get; set; }

        // The reason a qso was rejected.
        public RejectReason ReasonRejected { get; set; }

        // The status of the QSO.
        public QSOStatus Status { get; set; }

        // The QSO in another log that matches this QSO.
        public QSO MatchingQSO { get; set; }

        public QSO FirstMatchingQSO { get; set; }

        public List<QSO> NearestMatches { get; set; } = new List<QSO>();

        // This means an earlier QSO matched this one.
        public bool HasBeenMatched { get; set; }

        // This property allows the rejected qso report to know there are duplicates of this call
        // It will only be true if it is the qso counted as the valid qso not the dupe
        public bool QSOHasDupes { get; set; }

        // Indicates this has been printed.
        public bool HasBeenPrinted { get; set; }

        // This incorporates two fields, QSO Date and QSO Time
        // CHANGE TO DATE AND TIME LATER
        // ARE THES REALLY NEEDED SINCE WE HAVE COMBINDED DATE/TIME
        // </summary>
        public string QsoDate { get; set; }
        public string QsoTime { get; set; }

        // Date/Time of the QSO.
        public DateTime QSODateTime
        {
            get
            {
                string qtime = QsoTime.Insert(2, ":");

                DateTime.TryParse(QsoDate + " " + qtime, out qsoDateTime);
                return qsoDateTime;
            }
        }

        // Marks this qso as a duplicate QSO.
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
                    if (ReasonRejected == RejectReason.DuplicateQSO)
                    {
                        qsoIsDupe = false;
                        ReasonRejected = RejectReason.None;
                        Status = QSOStatus.ValidQSO;
                    }
                }
            }
        }

        // The operators call sign is invalid for this QSO
        public bool IncorrectOperatorCall
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
                    if (ReasonRejected == RejectReason.InvalidCall)
                    {
                        ReasonRejected = RejectReason.None;
                        Status = QSOStatus.ValidQSO;
                    }
                }
            }
        }

        // The calsign does not match the call sign in the other log
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
                    if (ReasonRejected == RejectReason.BustedCallSign)
                    {
                        ReasonRejected = RejectReason.None;
                        Status = QSOStatus.ValidQSO;
                    }
                }
            }
        }

        // Call sign of the operator.
        public string OperatorCall { get; set; }

        // Name of the operator.
        public string OperatorName { get; set; }
        
        // Call sign of the contact.
        public string ContactCall { get; set; }

        public string ContactName { get; set; }

        // This is the call that he should have copied.
        public string BustedCallGuess { get; set; }

        // Frequency of the exchange - may only need band.
        public string Frequency
        {
            get { return frequency; }
            set
            {
                frequency = value;
                Band = Utility.ConvertFrequencyToBand(Convert.ToDouble(frequency));
            }
        }

        // Indicates this QSO is the first one worked in this session and therefore a multiplier.
        public bool IsMultiplier { get; set; }

        // Band the QSO was on.
        public int Band { get; set; }

        // Mode used for the QSO.
        public QSOMode Mode
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

        public bool IncorrectBand
        {
            get { return incorrectBandHQP; }
            set
            {
                incorrectBandHQP = value;
                if (incorrectBandHQP == true)
                {
                    ReasonRejected = RejectReason.Band;
                    Status = QSOStatus.InvalidQSO;
                }
                else
                {
                    if (ReasonRejected == RejectReason.Band)
                    {
                        ReasonRejected = RejectReason.None;
                        Status = QSOStatus.ValidQSO;
                    }
                }
            }
        }

        public bool IncorrectMode
        {
            get { return incorrectModeHQP; }
            set
            {
                incorrectModeHQP = value;
                if (value == true)
                {
                    ReasonRejected = RejectReason.Mode;
                    Status = QSOStatus.InvalidQSO;
                }
                else
                {
                    if (ReasonRejected == RejectReason.Mode)
                    {
                        ReasonRejected = RejectReason.None;
                        Status = QSOStatus.ValidQSO;
                    }
                }
            }
        }

        // The operators name does not match for this QSO.
        public bool IncorrectOperatorName
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
                    if (ReasonRejected == RejectReason.OperatorName)
                    {
                        ReasonRejected = RejectReason.None;
                        Status = QSOStatus.ValidQSO;
                    }
                }
            }
        }

        public bool IncorrectContactName
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
                    if (ReasonRejected == RejectReason.ContactName)
                    {
                        ReasonRejected = RejectReason.None;
                        Status = QSOStatus.ValidQSO;
                    }
                }
            }
        }

        // Lists the incorrect name if available.
        public string IncorrectValue { get; set; }

        // The sent serial number.
        public int SentSerialNumber { get; set; }

        public int ReceivedSerialNumber { get; set; }

        // The serial number does not match the other log.
        public bool IncorrectSerialNumber
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
                    if (ReasonRejected == RejectReason.SerialNumber)
                    {
                        ReasonRejected = RejectReason.None;
                        Status = QSOStatus.ValidQSO;
                    }
                }
            }
        }

        // This QSO belongs to the current session.
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
                    if (ReasonRejected == RejectReason.InvalidSession)
                    {
                        ReasonRejected = RejectReason.None;
                        Status = QSOStatus.ValidQSO;
                    }
                }
            }
        }
        #endregion

        #region HQP Fields

        private bool invalidEntityHQP = false;
        private bool incorrectBandHQP = false;
        private bool incorrectModeHQP = false;
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

        // Lists the incorrect entity if available.
        public string IncorrectDXEntity { get; set; }

        // Actual country of the operator.
        public string OperatorCountry { get; set; }

        // Operator entity as defined by HQP
        // Length is 2 characters for US and Canada
        // or 3 characters if it is a HQP entity
        // This is equivalent to the Operator Name for the CWOpen
        public string OperatorEntity { get; set; }

        // The real or top level country of the  contact or DX station
        // This is the long name. Cannot be null!
        public string ContactCountry { get; set; }

        // The entity does not match this QSO.
        public bool InvalidEntity
        {
            set
            {
                invalidEntityHQP = value;
                if (value == true)
                {
                    ReasonRejected = RejectReason.InvalidEntity;
                    Status = QSOStatus.InvalidQSO;
                }
                else
                {
                    if (ReasonRejected == RejectReason.InvalidEntity)
                    {
                        ReasonRejected = RejectReason.None;
                        Status = QSOStatus.ValidQSO;
                    }
                }
            }
            get
            {
                return invalidEntityHQP;
            }
        }

        // The entity does not match this QSO.
        public bool InvalidSentEntity
        {
            set
            {
                //invalidSentEntity = value;
                if (value == true)
                {
                    ReasonRejected = RejectReason.InvalidSentEntity;
                    Status = QSOStatus.InvalidQSO;
                }
                else
                {
                    if (ReasonRejected == RejectReason.InvalidSentEntity)
                    {
                        ReasonRejected = RejectReason.None;
                        Status = QSOStatus.ValidQSO;
                    }
                }
            }
        }

        // Contact country - HQP - This can be state or Canadian province 2 letter code
        // or 3 letter HQP entity code
        // This is equivalent to the Contact Name for the CWOpen
        public string ContactEntity { get; set; }

        public int HQPPoints { get; set; }

        public string HQPEntity { get; set; }

        public bool IsHQPEntity { get; set; }
        public string OperatorOriginalCall { get; set; }

        #endregion

        #region Methods

        // determine points by mode for the HQP
        private void SetHQPPoints()
        {
            try
            {
                HQPPoints = mode switch
                {
                    QSOMode.CW => 3,
                    QSOMode.RTTY => 3,
                    QSOMode.RY => 3,
                    QSOMode.PH => 2,
                    QSOMode.SSB => 2,
                    _ => 0,
                };
            }
            catch (Exception)
            {
                throw new Exception("The mode " + Mode + " is not valid for this contest.");
            };
        }

        #endregion
    } // end class
}
