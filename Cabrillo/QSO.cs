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
        public bool IsXQSO { get; set; }

        /// <summary>
        /// The log reference for a particuler QSO.
        /// </summary>
        public ContestLog ParentLog { get; set; }

        /// <summary>
        /// A collection of reasons a log was rejected.
        /// Only one reason is ever used so this can be changed to a single item.
        /// </summary>
        private Dictionary<RejectReason, String> _RejectReasons = new Dictionary<RejectReason, String>();
        public Dictionary<RejectReason, String> RejectReasons
        {
            get { return _RejectReasons; }
        }

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
                    if (!_RejectReasons.ContainsKey(RejectReason.DuplicateQSO))
                    {
                        _RejectReasons.Add(RejectReason.DuplicateQSO, EnumHelper.GetDescription(RejectReason.DuplicateQSO));
                        _Status = QSOStatus.InvalidQSO;
                    }
                }
                else
                {
                    if (_RejectReasons.ContainsKey(RejectReason.DuplicateQSO))
                    {
                        _RejectReasons.Remove(RejectReason.DuplicateQSO);
                        _QSOIsDupe = false;
                        if (_RejectReasons.Count == 0)
                        {
                            _Status = QSOStatus.ValidQSO;
                        }
                    }
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
                    if (!_RejectReasons.ContainsKey(RejectReason.InvalidCall))
                    {
                        _RejectReasons.Add(RejectReason.InvalidCall, EnumHelper.GetDescription(RejectReason.InvalidCall));
                        _Status = QSOStatus.InvalidQSO;
                    }
                }
                else
                {
                    if (_RejectReasons.ContainsKey(RejectReason.InvalidCall))
                    {
                        _RejectReasons.Remove(RejectReason.InvalidCall);
                        if (_RejectReasons.Count == 0)
                        {
                            _Status = QSOStatus.ValidQSO;
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
                    if (!_RejectReasons.ContainsKey(RejectReason.BustedCallSign))
                    {
                        _RejectReasons.Add(RejectReason.BustedCallSign, EnumHelper.GetDescription(RejectReason.BustedCallSign));
                        _Status = QSOStatus.InvalidQSO;
                    }
                }
                else
                {
                    if (_RejectReasons.ContainsKey(RejectReason.BustedCallSign))
                    {
                        _RejectReasons.Remove(RejectReason.BustedCallSign);
                        if (_RejectReasons.Count == 0)
                        {
                            _Status = QSOStatus.ValidQSO;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Call sign of the operator.
        /// </summary>
        private string _OperatorCall;
        public string OperatorCall
        {
            get { return _OperatorCall.ToUpper(); }
            set { _OperatorCall = value; }
        }

        /// <summary>
        /// Name of the operator.
        /// </summary>
        private string _OperatorName;
        public string OperatorName
        {
            get { return _OperatorName; }
            set { _OperatorName = value.ToUpper(); }
        }

        /// <summary>
        /// Call sign of the contact.
        /// </summary>
        private string _ContactCall;
        public string ContactCall
        {
            get
            { return _ContactCall.ToUpper(); }
            set
            {
                _ContactCall = value;
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
                    if (!_RejectReasons.ContainsKey(RejectReason.OperatorName))
                    {
                        _RejectReasons.Add(RejectReason.OperatorName, EnumHelper.GetDescription(RejectReason.OperatorName));
                        _Status = QSOStatus.InvalidQSO;
                    }
                }
                else
                {
                    if (_RejectReasons.ContainsKey(RejectReason.OperatorName))
                    {
                        _RejectReasons.Remove(RejectReason.OperatorName);
                        if (_RejectReasons.Count == 0)
                        {
                            _Status = QSOStatus.ValidQSO;
                        }
                    }
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
        public Int32 Band { get; set; }

        /// <summary>
        /// Mode used for the QSO.
        /// this should be an enum
        /// </summary>
        public string Mode { get; set; }

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
                    if (!_RejectReasons.ContainsKey(RejectReason.SerialNumber))
                    {
                        _RejectReasons.Add(RejectReason.SerialNumber, EnumHelper.GetDescription(RejectReason.SerialNumber));
                        _Status = QSOStatus.InvalidQSO;
                    }
                }
                else
                {
                    if (_RejectReasons.ContainsKey(RejectReason.SerialNumber))
                    {
                        _RejectReasons.Remove(RejectReason.SerialNumber);
                        if (_RejectReasons.Count == 0)
                        {
                            _Status = QSOStatus.ValidQSO;
                        }
                    }
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
                    if (!_RejectReasons.ContainsKey(RejectReason.InvalidSession))
                    {
                        _RejectReasons.Add(RejectReason.InvalidSession, EnumHelper.GetDescription(RejectReason.InvalidSession));
                        _Status = QSOStatus.InvalidQSO;
                    }
                }
                else
                {
                    if (_RejectReasons.ContainsKey(RejectReason.InvalidSession))
                    {
                        _RejectReasons.Remove(RejectReason.InvalidSession);
                        if (_RejectReasons.Count == 0)
                        {
                            _Status = QSOStatus.ValidQSO;
                        }
                    }
                }
            }
        }
        #endregion
        
        #region HQP (Hawaiin QSO Party)

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
        public string OriginalContactEntityEntry { get; set; }

        /// <summary>
        /// The operator entity in the log before any lookups
        /// </summary>
        public string OriginalOperatorEntityEntry { get; set; }

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
            set { _OperatorEntity = value.ToUpper(); }
        }

        /// <summary>
        /// The real or top level country of the  contact or DX station
        /// This is the long name
        /// </summary>
        private string _ContactTerritory;
        public string ContactTerritory
        {
            get { return _ContactTerritory.ToUpper(); }
            set
            {
                _ContactTerritory = value;
            }
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
                    if (!_RejectReasons.ContainsKey(RejectReason.InvalidEntity))
                    {
                        _RejectReasons.Add(RejectReason.InvalidEntity, EnumHelper.GetDescription(RejectReason.InvalidEntity));
                        _Status = QSOStatus.InvalidQSO;
                    }
                }
                else
                {
                    if (_RejectReasons.ContainsKey(RejectReason.InvalidEntity))
                    {
                        _RejectReasons.Remove(RejectReason.InvalidEntity);
                        if (_RejectReasons.Count == 0)
                        {
                            _Status = QSOStatus.ValidQSO;
                        }
                    }
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
            set
            {
                _ContactEntity = value.ToUpper();
            }
        }
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


        #endregion

    } // end class
}
