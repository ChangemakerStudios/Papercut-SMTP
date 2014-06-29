// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2014 Jaben Cargman
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//  
// http://www.apache.org/licenses/LICENSE-2.0
//  
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Papercut.Core.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Text;

    using Papercut.Core.Annotations;

    /// <summary>
    ///     The util.
    /// </summary>
    public static class GeneralExtensions
    {
        public static string AsString(this byte[] bytes, Encoding byteEncoding = null)
        {
            if (bytes == null) throw new ArgumentNullException("bytes");

            byteEncoding = byteEncoding ?? Encoding.UTF8;
            return byteEncoding.GetString(bytes);
        }

        /// <summary>
        ///     Gets a enum as a list
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <returns></returns>
        public static List<TEnum> EnumAsList<TEnum>()
            where TEnum : struct
        {
            Type enumType = typeof(TEnum);

            // Can't use type constraints on value types, so have to do check like this
            if (enumType.BaseType != typeof(Enum)) throw new ArgumentException("EnumAsList does not support non-enum types");

            Array enumValArray = Enum.GetValues(enumType);

            var enumValues = new List<TEnum>(enumValArray.Length);

            enumValues.AddRange(
                enumValArray.Cast<int>().Select(val => (TEnum)Enum.Parse(enumType, val.ToString())));

            return enumValues;
        }

        /// <summary>
        ///     The get ip address.
        /// </summary>
        /// <returns>
        ///     The get ip address.
        /// </returns>
        public static string GetIPAddress()
        {
            IPAddress ip = GetExternalIp();

            if (ip == null) return Dns.GetHostEntry(Dns.GetHostName()).HostName;

            return Dns.GetHostEntry(ip).HostName;
        }

        /// <summary>
        ///     To FileSizeFormat... Thank you to "deepee1" on StackOverflow for this elegent solution:
        ///     http://stackoverflow.com/a/4975942
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ToFileSizeFormat(this long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };

            if (bytes == 0) return string.Format("0{0}", suffixes[0]);

            var place = Convert.ToInt64(Math.Floor(Math.Log(bytes, 1024)));

            double roundedNumber = Math.Round(bytes / Math.Pow(1024, place), 1);

            return roundedNumber.ToString(CultureInfo.InvariantCulture) + suffixes[place];
        }

        /// <summary>
        ///     Truncates a string for readability.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="inputLimit"></param>
        /// <param name="cutOff"></param>
        /// <returns></returns>
        public static string Truncate(
            [CanBeNull] this string input,
            int inputLimit,
            [NotNull] string cutOff = "...")
        {
            if (cutOff == null) throw new ArgumentNullException("cutOff");

            string output = input;

            if (string.IsNullOrWhiteSpace(input)) return null;

            int limit = inputLimit - cutOff.Length;

            // Check if the string is longer than the allowed amount
            // otherwise do nothing
            if (output.Length > limit && limit > 0)
            {
                // cut the string down to the maximum number of characters
                output = output.Substring(0, limit);

                // Check if the space right after the truncate point 
                // was a space. if not, we are in the middle of a word and 
                // need to cut out the rest of it
                if (input.Substring(output.Length, 1) != " ")
                {
                    int lastSpace = output.LastIndexOf(" ");

                    // if we found a space then, cut back to that space
                    if (lastSpace != -1) output = output.Substring(0, lastSpace);
                }

                // Finally, add the the cut off string...
                output += cutOff;
            }

            return output;
        }

        /// <summary>
        ///     The get external ip.
        /// </summary>
        /// <returns>
        /// </returns>
        static IPAddress GetExternalIp()
        {
            try
            {
                string whatIsMyIp = "http://www.whatismyip.com/automation/n09230945.asp";
                var wc = new WebClient();
                string requestHtml = Encoding.UTF8.GetString(wc.DownloadData(whatIsMyIp));
                return IPAddress.Parse(requestHtml);
            }
            catch
            {
                return null;
            }
        }
    }
}