using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using W6OP.ContestLogAnalyzer;

namespace W6OP.PrintEngine
{
    /// <summary>
    /// Class with scores so CSV generator can write file.
    /// </summary>
    public class ScoreList
    {
        public ScoreList()
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
}
