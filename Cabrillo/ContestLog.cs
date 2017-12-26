using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        }

        /// <summary>
        /// Comments I add while I am anlyzing the logs. May be from when I first load
        /// or anywhere along the line.
        /// </summary>
        public List<string> AnalyzerComments { get; set; } = new List<string>();
        //private List<string> _AnalyzerComments;
        //public List<string> AnalyzerComments
        //{
        //    get { return _AnalyzerComments; }
        //    set { _AnalyzerComments = value; }
        //}

        /// <summary>
        /// Call sign of the log owner.
        /// </summary>
        public string LogOwner { get; set; }
        //private string _LogOwner;
        //public string LogOwner
        //{
        //    get { return _LogOwner; }
        //    set { _LogOwner = value; }
        //}

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
        
        /// <summary>
        /// List of all the QSOs in this log.
        /// </summary>
        public List<QSO> QSOCollection { get; set; } = new List<QSO>();

        /// <summary>
        /// List of all the XQSOs in this log.
        /// </summary>
        public List<QSO> QSOCollectionX { get; set; } = new List<QSO>();

        /// <summary>
        /// A list of all of the logs that have a reference to the call represented by this log.
        /// </summary>
        //private Dictionary<string, ContestLog> _MatchLogs;
        public Dictionary<string, ContestLog> MatchLogs { get; set; } = new Dictionary<string, ContestLog>();
       

        /// <summary>
        /// List of logs that do not have a QSO with this operator.
        /// </summary>
        //private Dictionary<string, ContestLog> _OtherLogs;
        public Dictionary<string, ContestLog> OtherLogs { get; set; } = new Dictionary<string, ContestLog>();
        

        /// <summary>
        /// Logs that need review.
        /// </summary>
        //private Dictionary<string, ContestLog> _ReviewLogs;
        public Dictionary<string, ContestLog> ReviewLogs { get; set; } = new Dictionary<string, ContestLog>();
       

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
       

        //private Int32 _ClaimedScore;
        public Int32 ClaimedScore { get; set; }

        //private Int32 _ActualScore;
        public Int32 ActualScore { get; set; }
        //{
        //    get { return _ActualScore; }
        //    set { _ActualScore = value; }
        //}

        private Int32 _Multipliers;
        public Int32 Multipliers
        {
            get { return _Multipliers; }
            set 
            { 
                _Multipliers = value;
                // IS THIS CORRECT???
                //Int32 count = QSOCollection.Where(q =>  q.Status == QSOStatus.ValidQSO).ToList().Count();
                //ActualScore = _Multipliers * count;
            }
        }

        // For debugging use in HQP
        public Int32 HQPMultipliers { get; set; }
        public Int32 NonHQPMultipliers { get; set; }

        // used for the HQP
        public int TotalPoints { get; set; }

        // for determining multipliers for HQP
        public bool  IsHQPEntity { get; set; }

        /// <summary>
        /// Holds the soap box comment the log owner made
        /// </summary>
        public string SoapBox { get; set; }

        private HashSet<string> _Entities = new HashSet<string>();
        public HashSet<string> Entities { get => _Entities; set => _Entities = value; }

        


    } // end class
}
