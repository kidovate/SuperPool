// -----
// Copyright 2010 Deyan Timnev
// This file is part of the Matrix Platform (www.matrixplatform.com).
// The Matrix Platform is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, 
// either version 3 of the License, or (at your option) any later version. The Matrix Platform is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with the Matrix Platform. If not, see http://www.gnu.org/licenses/lgpl.html
// -----
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Matrix.Common.Core
{
    /// <summary>
    /// Class stores helper operations related to string operations.
    /// </summary>
    public static class StringHelper
    {
        public const string NoVallueString = "NA";
        public const string NoVallueAssignedString = NoVallueString;

        /// <summary>
        /// 
        /// </summary>
        public static string ToString(DateTime? dateTime)
        {
            if (dateTime.HasValue == false)
            {
                return NoVallueAssignedString;
            }

            return dateTime.Value.ToString();
        }

        /// <summary>
        /// Helper, takes care of null or string.empty values.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ToString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return NoVallueAssignedString;
            }

            return input;
        }

        /// <summary>
        /// Helper, will convert to NaN if value is null;
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToString(long? value)
        {
            if (value.HasValue == false)
            {
                return NoVallueAssignedString;
            }

            return value.Value.ToString();
        }

        /// <summary>
        /// Helper, will convert to NaN if value is null;
        /// </summary>
        public static string ToString(double? value, string format)
        {
            if (value.HasValue == false)
            {
                return NoVallueAssignedString;
            }

            return value.Value.ToString(format);
        }

        /// <summary>
        /// Helper, will convert to NaN if value is null;
        /// </summary>
        public static string ToString(double? value)
        {
            if (value.HasValue == false)
            {
                return NoVallueAssignedString;
            }

            return value.Value.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        public static string ToStringSafe(object value)
        {
            if (value == null)
            {
                return NoVallueAssignedString;
            }

            return value.ToString();
        }

        /// <summary>
        /// Helper, will convert to NaN if value is null;
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        public static string ToString(decimal? value)
        {
            if (value.HasValue == false)
            {
                return NoVallueAssignedString;
            }
            return value.Value.ToString();
        }

        /// <summary>
        /// Helper, will convert to NaN if value is null;
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToString(decimal? value, string format)
        {
            if (value.HasValue == false)
            {
                return NoVallueAssignedString;
            }

            return value.Value.ToString(format);
        }

        public static string GetLimitedSubstring(string input, string startLimiter, string endLimiter, int occurenceIndex)
        {
            int start = -1;
            int countFound = 0;
            do
            {
                start = input.IndexOf(startLimiter, start + 1);
                if (start >= 0)
                {
                    int end = input.IndexOf(endLimiter, start + 1);
                    if (start >= 0 && end >= 0 && end > start)
                    {
                        if (countFound == occurenceIndex)
                        {
                            return input.Substring(start + 1, end - start - 1);
                        }
                        countFound++;
                    }
                }
            }
            while (start >= 0);

            return null;
        }

        static public string ToString<ItemType>(IEnumerable<ItemType> items, string separator)
        {
            if (items == null)
            {
                return NoVallueAssignedString;
            }

            StringBuilder builder = new StringBuilder();
            bool isFirst = true;
            foreach (ItemType item in items)
            {
                if (isFirst == false && string.IsNullOrEmpty(separator) == false)
                {
                    builder.Append(separator);
                }

                isFirst = false;
                builder.Append(item.ToString());
            }

            return builder.ToString();
        }

        static public string IntsToString(int[] values, string separator)
        {
            StringBuilder sb = new StringBuilder(values.Length);
            foreach (int value in values)
            {
                sb.Append(value);
                sb.Append(separator);
            }
            // Remove last separator
            if (sb.Length > 1)
            {
                sb.Remove(sb.Length - separator.Length, separator.Length);
            }
            return sb.ToString();
        }

        static public List<string> ToStrings<ItemType>(IEnumerable<ItemType> items)
        {
            List<string> result = new List<string>();
            foreach (ItemType item in items)
            {
                result.Add(item.ToString());
            }

            return result;
        }

    }
}
