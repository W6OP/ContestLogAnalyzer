using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W6OP.ContestLogAnalyzer
{
    [Serializable()]
    public class BadCalls
    {
        Dictionary<string, string> BadCallList;

        /// <summary>
        /// Constructor
        /// </summary>
        public BadCalls()
        {
            BadCallList = new Dictionary<string, string>();
        }

        
        //public bool GoodCall { get; set; }

        //public bool BadCall { get; set; }

    } // end class
}
