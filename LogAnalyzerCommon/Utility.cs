using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace W6OP.ContestLogAnalyzer
{
    public static class Utility
    {
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

            //if (String.IsNullOrEmpty(description)) {
            //    throw new NullReferenceException("A required field is missing from the header. Possibly the Contest Name.");
            //}
            // remove tabs
            description = description.Replace("\t", " ");

            // remove extra embedded spaces between words
            Regex trimmer = new Regex(@"\s\s+");
            description = trimmer.Replace(description, " ");

            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                var contest = Attribute.GetCustomAttribute(field, typeof(ContestDescription)) as ContestDescription;

                if (contest == null)
                {
                    if (attribute != null)
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
                    return (T)field.GetValue(null);

                }
            }
            //throw new ArgumentException("Not found.", description);
            return default(T); // this returns the first or "=0" enum
        }

        /// <summary>
        /// Convert the raw fequency to a band number.
        /// </summary>
        /// <param name="frequency"></param>
        /// <returns></returns>
        public static Int32 ConvertFrequencyToBand(double frequency)
        {
            Int32 band = 0;

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
}
