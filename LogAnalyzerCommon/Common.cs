using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace W6OP.ContestLogAnalyzer
{

    /// <summary>
    /// https://thomaslevesque.com/2019/11/18/using-foreach-with-index-in-c/
    /// </summary>
    public static class Extensions
    {
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
        {
            return source.Select((item, index) => (item, index));
        }

        public static T MostCommon<T>(this IEnumerable<T> list)
        {
            return list.GroupBy(i => i)
                .OrderByDescending(grp => grp.Count())
                .Select(grp => grp.Key).First();
        }
    }

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
