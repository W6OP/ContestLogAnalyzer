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
        }

        /// <summary>
        /// Comments I add while I am anlyzing the logs. May be from when I first load
        /// or anywhere along the line.
        /// </summary>
        private List<string> _AnalyzerComments;
        public List<string> AnalyzerComments
        {
            get { return _AnalyzerComments; }
            set { _AnalyzerComments = value; }
        }

        /// <summary>
        /// Call sign of the log owner.
        /// </summary>
        private string _LogOwner;
        public string LogOwner
        {
            get { return _LogOwner; }
            set { _LogOwner = value; }
        }
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
                _LogOwner = _LogHeader.OperatorCallSign;
                if (_LogHeader.OperatorCategory == CategoryOperator.CheckLog)
                {
                    _IsCheckLog = true;
                }
                _ClaimedScore = LogHeader.ClaimedScore;
            }
        }

        /// <summary>
        /// List of all the QSOs in this log.
        /// </summary>
        private List<QSO> _QSOCollection;
        public List<QSO> QSOCollection
        {
            get { return _QSOCollection; }
            set { _QSOCollection = value; }
        }

        /// <summary>
        /// List of QSOs from other logs that fully match QSOs in this log.
        /// </summary>
        private List<QSO> _FullMatchQSOS;
        public List<QSO> FullMatchQSOS
        {
            get { return _FullMatchQSOS; }
            set { _FullMatchQSOS = value; }
        }

        /// <summary>
        /// List of QSOs from other logs that match at least the call sign for this log.
        /// </summary>
        private List<QSO> _PartialMatchQSOS;
        public List<QSO> PartialMatchQSOS
        {
            get { return _PartialMatchQSOS; }
            set { _PartialMatchQSOS = value; }
        }

        /// List of QSOs from other logs that do not match any QSOs in this log.
        /// </summary>
        private List<QSO> _OtherQSOS;
        public List<QSO> OtherQSOS
        {
            get { return _OtherQSOS; }
            set { _OtherQSOS = value; }
        }
        /// <summary>
        /// A list of all of the logs that have a reference to the call represented by this log.
        /// </summary>
        private Dictionary<string, ContestLog> _MatchLogs;
        public Dictionary<string, ContestLog> MatchLogs
        {
            get { return _MatchLogs; }
            set { _MatchLogs = value; }
        }

        /// <summary>
        /// List of logs that do not have a QSO with this operator.
        /// </summary>
        private Dictionary<string, ContestLog> _OtherLogs;
        public Dictionary<string, ContestLog> OtherLogs
        {
            get { return _OtherLogs; }
            set { _OtherLogs = value; }
        }

        /// <summary>
        /// Logs that need review.
        /// </summary>
        private Dictionary<string, ContestLog> _ReviewLogs;
        public Dictionary<string, ContestLog> ReviewLogs
        {
            get { return _ReviewLogs; }
            set { _ReviewLogs = value; }
        }

        /// <summary>
        /// Indicates the log meets the criteria necessary to be analysed.
        /// </summary>
        private bool _IsValidLog;
        public bool IsValidLog
        {
            get { return _IsValidLog; }
            set { _IsValidLog = value; }
        }

        /// <summary>
        /// Indicates this is a check log and should not be scored.
        /// </summary>
        private bool _IsCheckLog;
        public bool IsCheckLog
        {
            get { return _IsCheckLog; }
            set { _IsCheckLog = value; }
        }

        //private bool _LogIsCheckLog;
        //public bool LogIsCheckLog
        //{
        //    get { return _LogIsCheckLog; }
        //    set { _LogIsCheckLog = value; }
        //}


        private Int32 _ClaimedScore;
        public Int32 ClaimedScore
        {
            get { return _ClaimedScore; }
            set { _ClaimedScore = value; }
        }

        private Int32 _ActualScore;
        public Int32 ActualScore
        {
            get { return _ActualScore; }
            set { _ActualScore = value; }
        }

        private Int32 _Multipliers;
        public Int32 Multipliers
        {
            get { return _Multipliers; }
            set 
            { 
                _Multipliers = value;
                // this actuall needs to be a link query - where QSO.IsValid
                Int32 count = QSOCollection.Where(q =>  q.Status == QSOStatus.ValidQSO).ToList().Count();
                _ActualScore = _Multipliers * count;
            }
        }

    } // end class
}
