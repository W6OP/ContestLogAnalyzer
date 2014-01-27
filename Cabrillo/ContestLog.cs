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
        /// The header for this log.
        /// </summary>
        private LogHeader _LogHeader;
        public LogHeader LogHeader
        {
            get { return _LogHeader; }
            set 
            { 
                _LogHeader = value;
                if (_LogHeader.OperatorCategory == CategoryOperator.CheckLog)
                {
                    _LogIsCheckLog = true;
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
        /// A list of all of the logs that have a reference to the call represented by this log.
        /// </summary>
        private List<ContestLog> _MatchingLogs;
        public List<ContestLog> MatchingLogs
        {
            get { return _MatchingLogs; }
            set { _MatchingLogs = value; }
        }

        private bool _LogIsValid;
        public bool LogIsValid
        {
            get { return _LogIsValid; }
            set { _LogIsValid = value; }
        }

        private bool _IsCheckLog;
        public bool IsCheckLog
        {
            get { return _IsCheckLog; }
            set { _IsCheckLog = value; }
        }

        private bool _LogIsCheckLog;
        public bool LogIsCheckLog
        {
            get { return _LogIsCheckLog; }
            set { _LogIsCheckLog = value; }
        }


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

        /// <summary>
        /// 
        /// </summary>
        public ContestLog()
        {
            _MatchingLogs = new List<ContestLog>();
        }
    } // end class
}
