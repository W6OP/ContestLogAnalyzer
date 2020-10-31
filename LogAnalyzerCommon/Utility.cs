using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace W6OP.ContestLogAnalyzer
{
    public static class Utility
    {

        public static T GetValueFromDescriptionEx<T>(string description) where T : Enum
        {
            foreach (var field in typeof(T).GetFields())
            {
                if (Attribute.GetCustomAttribute(field,
                typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
                {
                    if (attribute.Description == description)
                        return (T)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (T)field.GetValue(null);
                }
            }

            throw new ArgumentException("Not found.", nameof(description));
            // Or return default(T);
        }

        /// <summary>
        /// http://stackoverflow.com/questions/4367723/get-enum-from-description-attribute
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="description"></param>
        /// <returns></returns>
        public static T GetValueFromDescription<T>(string description)
        {
            var type = typeof(T);
            if (!type.IsEnum)
            {
                throw new InvalidOperationException();
            }

            // remove tabs
            description = description.Replace("\t", " ");

            // remove extra embedded spaces between words
            Regex trimmer = new Regex(@"\s\s+");
            description = trimmer.Replace(description, " ");

            foreach (var field in type.GetFields())
            {
                // var attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                var cwopenContest = Attribute.GetCustomAttribute(field, typeof(CWOPENContestDescription)) as CWOPENContestDescription;
                var hqpContest = Attribute.GetCustomAttribute(field, typeof(HQPContestDescription)) as HQPContestDescription;

                if (cwopenContest == null && hqpContest == null)
                {
                    if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
                    {
                        // maybe need to look at the TYPE and then do indexOf or something
                        if (attribute.Description == description)
                        {
                            return (T)field.GetValue(null);
                        }
                    }
                    else
                    {
                        if (field.Name == description)
                        {

                            return (T)field.GetValue(null);
                        }
                    }
                }
                else
                {
                   if (cwopenContest != null)
                    {
                        //return (T)field.GetValue(null);
                        Attribute[] result = field.GetCustomAttributes().ToArray();
                        CWOPENContestDescription co = (CWOPENContestDescription)result[0];
                        if (co.ContestNameOne == description)
                        {
                            return (T)field.GetValue(null);
                        }

                        if (co.ContestNameTwo == description)
                        {
                            return (T)field.GetValue(null);
                        }

                        if (co.ContestNameThree == description)
                        {
                            return (T)field.GetValue(null);
                        }

                        if (co.ContestNameFour == description)
                        {
                            return (T)field.GetValue(null);
                        }
                    }

                   if (hqpContest != null)
                    {
                        Attribute[] result = field.GetCustomAttributes().ToArray();
                        HQPContestDescription co = (HQPContestDescription)result[0];
                        if (co.ContestNameOne == description)
                        {
                            return (T)field.GetValue(null);
                        }
                        //return (T)field.GetValue(null);
                    }

                    //if (field.GetValue(null).ToString() == description)
                    //{
                    //    return (T)field.GetValue(null);
                    //} else
                    //{
                    //    //return (T)field.GetValue(null);
                    //}
                }
            }
            //throw new ArgumentException("Not found.", description);
            return default; // this returns the first or "=0" enum
        }

        /// <summary>
        /// Convert the raw fequency to a band number.
        /// </summary>
        /// <param name="frequency"></param>
        /// <returns></returns>
        public static int ConvertFrequencyToBand(double frequency)
        {
            int band = 0;

            if (frequency >= 50000.0 && frequency <= 54000.0)
            {
                return 6;
            }

            if (frequency >= 28000.0 && frequency <= 29700.0)
            {
                return 10;
            }

            if (frequency >= 24890.0 && frequency <= 24990.0)
            {
                return 12;
            }

            if (frequency >= 21000.0 && frequency <= 21450.0)
            {
                return 15;
            }
            if (frequency >= 18068 && frequency <= 18168.0)
            {
                return 17;
            }
            if (frequency >= 14000.0 && frequency <= 14350.0)
            {
                return 20;
            }
            if (frequency >= 10100.0 && frequency <= 10150.0)
            {
                return 30;
            }
            if (frequency >= 7000.0 && frequency <= 7300.0)
            {
                return 40;
            }
            if (frequency >= 5000.0 && frequency <= 6000.0)
            {
                return 60;
            }
            if (frequency >= 3500.0 && frequency <= 4000.0)
            {
                return 80;
            }

            if (frequency >= 1800.0 && frequency <= 2000.0)
            {
                return 160;
            }

            return band;
        }

        public static TCollection MakeRigCollectionSimple<TCollection, TItem>(this IEnumerable<TItem> items, TCollection collection)
            where TCollection : ICollection<TItem>
        {
            foreach (var myObj in items)
                collection.Add(myObj);
            return collection;
        }


        /// <summary>
        /// http://stackoverflow.com/questions/3595583/can-i-use-linq-to-strip-repeating-spaces-from-a-string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string RemoveRepeatedSpaces(string s)
        {
            StringBuilder sb = new StringBuilder(s.Length);
            char lastChar = '\0';
            foreach (char c in s)
                if (c != ' ' || lastChar != ' ')
                    sb.Append(lastChar = c);
            return sb.ToString().Trim();
        }
    } // end class

    /// <summary>
    /// Contains approximate string matching
    /// </summary>
    public static class LevenshteinDistance
    {
        /// <summary>
        /// Compute the distance between two strings.
        /// </summary>
        public static int Compute(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }
    } // end class

}
