﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
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

            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                var contest = Attribute.GetCustomAttribute(field, typeof(ContestDescription)) as ContestDescription;

                if (contest == null)
                {
                    if (attribute != null)
                    {
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
            throw new ArgumentException("Not found.", "description");
            // or return default(T);
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
    } // end class
}
