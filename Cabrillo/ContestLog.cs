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
            _MatchLogs = new List<ContestLog>();
            _ReviewLogs = new List<ContestLog>();
            _OtherLogs = new List<ContestLog>();
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
        private List<ContestLog> _MatchLogs;
        public List<ContestLog> MatchLogs
        {
            get { return _MatchLogs; }
            set { _MatchLogs = value; }
        }

        /// <summary>
        /// List of logs that do not have a QSO with this operator.
        /// </summary>
        private List<ContestLog> _OtherLogs;
        public List<ContestLog> OtherLogs
        {
            get { return _OtherLogs; }
            set { _OtherLogs = value; }
        }

        /// <summary>
        /// Logs that need review.
        /// </summary>
        private List<ContestLog> _ReviewLogs;
        public List<ContestLog> ReviewLogs
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

    } // end class
}
