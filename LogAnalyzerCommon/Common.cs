using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W6OP.ContestLogAnalyzer
{
    public class Common
    {
        public Common()
        {

        }

        /// <summary>
        /// Get a list of all distinct call/name pairs by grouped by call sign. This is used
        /// for the bad call list.
        /// </summary>
        /// <param name="contestLogList"></param>
        //public List<Tuple<string, string>> CollectAllCallNamePairs(List<ContestLog> contestLogList)
        //{
        //    // list of all distinct call/name pairs
        //    List<Tuple<string, string>> distinctCallNamePairs = contestLogList.SelectMany(z => z.QSOCollection)
        //      .Select(r => new Tuple<string, string>(r.ContactCall, r.ContactName))
        //      .GroupBy(p => new Tuple<string, string>(p.Item1, p.Item2))
        //      .Select(g => g.First())
        //      .OrderBy(q => q.Item1)
        //      .ToList();

        //    return distinctCallNamePairs;
        //}


    } // end class
}
