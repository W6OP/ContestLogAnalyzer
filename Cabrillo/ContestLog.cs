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

        /// <summary>
        /// 
        /// </summary>
        public ContestLog()
        {

        }
    } // end class
}
