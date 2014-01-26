using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W6OP.ContestLogAnalyzer
{
    public class ContestLog
    {
        private LogHeader _LogHeader;
        public LogHeader LogHeader
        {
            get { return _LogHeader; }
            set { _LogHeader = value; }
        }

        private List<QSO> _QSOCollection;
        public List<QSO> QSOCollection
        {
            get { return _QSOCollection; }
            set { _QSOCollection = value; }
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

        /// <summary>
        /// A list of all of the logs that have a reference to the call represented by this log.
        /// </summary>
        private List<ContestLog> _MatchingLogs;
        public List<ContestLog> MatchingLogs
        {
            get { return _MatchingLogs; }
            set { _MatchingLogs = value; }
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
