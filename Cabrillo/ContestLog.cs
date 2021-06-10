using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;

namespace W6OP.ContestLogAnalyzer
{
    public class ContestLog
    {
        /// <summary>
        /// 
        /// </summary>
        public ContestLog()
        {
            HQPMultipliers = 0;
            NonHQPMultipliers = 0;

            QSOCollection = new List<QSO>();
            QSODictionary = new Dictionary<string, List<QSO>>();
        }

        /// <summary>
        /// Call sign of the log owner.
        /// </summary>
        public string LogOwner { get; set; }

        /// <summary>
        /// Call sign of the operator.
        /// </summary>
        public string Operator { get; set; }

        /// <summary>
        /// Name of the operator.
        /// </summary>
        public string OperatorName { get; set; }

        /// <summary>
        /// Call sign of station.
        /// </summary>
        public string Station { get; set; }

        /// <summary>
        /// The header for this log.
        /// </summary>
        private LogHeader _LogHeader;
        public LogHeader LogHeader
        {
            get { return _LogHeader; }
            set 
            { 
                _LogHeader = value;
                LogOwner = _LogHeader.OperatorCallSign;
                if (_LogHeader.OperatorCategory == CategoryOperator.CheckLog)
                {
                    IsCheckLog = true;
                }

                // THIS NEEDs TO BE CORRECTED
                if (_LogHeader.OperatorCategory == CategoryOperator.SingleOp)
                {
                    SO2R = true;
                }

                ClaimedScore = LogHeader.ClaimedScore;
            }
        }

        public int Session { get; set; }

        public bool SO2R { get; set; }

        // used for scoring HQP and CWOpen band faults
        public bool IsSingleBand { get; set; }

        // used for scoring HQP mode faults
        public bool IsSingleMode { get; set; }

        /// <summary>
        /// List of all the QSOs in this log.
        /// </summary>
        public List<QSO> QSOCollection { get; set; }

        public Dictionary<string, List<QSO>> QSODictionary { get; set; }

        /// <summary>
        /// Indicates the log meets the criteria necessary to be analysed.
        /// </summary>
        //private bool _IsValidLog;
        public bool IsValidLog { get; set; } = true;
        

        /// <summary>
        /// Indicates this is a check log and should not be scored.
        /// </summary>
        //private bool _IsCheckLog;
        public bool IsCheckLog { get; set; }

        #region Scoring

        public int ClaimedScore { get; set; }

        public int ActualScore { get; set; }
        public int Multipliers { get; set; }

        public int PhoneTotal { get; set; }
        public int CWTotal { get; set; }
        public int DIGITotal { get; set; }

        #endregion

        #region Points and Multipliers

        public int HQPMultipliers { get; set; }

        public int NonHQPMultipliers { get; set; }

        // used for the HQP
        public int TotalPoints { get; set; }

        #endregion

        // for determining multipliers for HQP
        public bool  IsHQPEntity { get; set; }

        /// <summary>
        /// Holds the soap box comment the log owner made
        /// </summary>
        //public string SoapBox { get; set; }

        private HashSet<string> _Entities = new HashSet<string>();
        public HashSet<string> Entities { get => _Entities; set => _Entities = value; }

        /// <summary>
        /// For printing multiplier list in reports.
        /// </summary>
        private SortedList<string, string> _EntitiesList = new SortedList<string, string>();
        public SortedList<string, string> EntitiesList { get => _EntitiesList; set => _EntitiesList = value; }

    } // end class
}
