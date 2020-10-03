using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using W6OP.ContestLogAnalyzer;

namespace W6OP.PrintEngine
{
    /// <summary>
    /// Helper class for printing CSV files.
    /// </summary>
    public class CWOpenScoreList
    {
        public CWOpenScoreList()
        {
        }

        public string LogOwner { get; internal set; }
        public string Operator { get; internal set; }
        public string Station { get; internal set; }
        public string OperatorName { get; internal set; }
        public string QSOCount { get; internal set; }
        public string Multipliers { get; internal set; }
        public string ActualScore { get; internal set; }
        public string Power { get; internal set; }
        public string Assisted { get; internal set; }
    } // end class

    /// <summary>
    /// Helper class for printing CSV files.
    /// Call QTH PH CW  DG QSOs    HI mults    Score
    /// </summary>
    public class HQPScoreList
    {
        public HQPScoreList()
        {
        }

        public string Call { get; internal set; }
        public string QTH { get; internal set; }
       // public string Country { get; internal set; }
        public string PH { get; internal set; }
        public string CW { get; internal set; }
        public string DG { get; internal set; }
        public string QSOs { get; internal set; }
        public string Mults { get; internal set; }

       
        //public string LogOwner { get; internal set; }
       // public string Operator { get; internal set; }
        //public string Station { get; internal set; }
       
       // public string Entity { get; internal set; }
       // public string QSOCount { get; internal set; }
        //public string Multipliers { get; internal set; }
        //public string Points { get; internal set; }
        public string Score { get; internal set; }
    } // end class
}
