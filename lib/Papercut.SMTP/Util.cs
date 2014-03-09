/*  
 * Papercut
 *
 *  Copyright © 2008 - 2012 Ken Robertson
 *  Copyright © 2013 - 2014 Jaben Cargman
 *  
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *  
 *  http://www.apache.org/licenses/LICENSE-2.0
 *  
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *  
 */

namespace Papercut.SMTP
{
    #region Using

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;

    #endregion

    /// <summary>
    /// The util.
    /// </summary>
    public static class Util
    {
        #region Public Methods and Operators

        /// <summary>
        /// The add range.
        /// </summary>
        /// <param name="destinationCollection">
        /// The destination collection.
        /// </param>
        /// <param name="sourceCollection">
        /// The source collection.
        /// </param>
        /// <typeparam name="TValue">
        /// </typeparam>
        public static void AddRange<TValue>(
            this ICollection<TValue> destinationCollection, IEnumerable<TValue> sourceCollection)
        {
            if (destinationCollection == null)
            {
                throw new ArgumentNullException("destinationCollection");
            }

            if (sourceCollection == null)
            {
                throw new ArgumentNullException("sourceCollection");
            }

            foreach (var item in sourceCollection.ToList())
            {
                destinationCollection.Add(item);
            }
        }

        /// <summary>
        /// The add range.
        /// </summary>
        /// <param name="destinationList">
        /// The destination list.
        /// </param>
        /// <param name="sourceList">
        /// The source list.
        /// </param>
        public static void AddRange(this IList destinationList, IEnumerable sourceList)
        {
            if (destinationList == null)
            {
                throw new ArgumentNullException("destinationList");
            }

            if (sourceList == null)
            {
                throw new ArgumentNullException("sourceList");
            }

            foreach (var item in sourceList.Cast<object>().ToList())
            {
                destinationList.Add(item);
            }
        }

        /// <summary>
        /// The for each.
        /// </summary>
        /// <param name="source">
        /// The source. 
        /// </param>
        /// <param name="act">
        /// The act. 
        /// </param>
        /// <typeparam name="T">
        /// </typeparam>
        /// <returns>
        /// </returns>
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> act)
        {
            foreach (T element in source.ToList())
            {
                act(element);
            }

            return source;
        }

        /// <summary>
        /// The get ip address.
        /// </summary>
        /// <returns>
        /// The get ip address. 
        /// </returns>
        public static string GetIPAddress()
        {
            IPAddress ip = GetExternalIp();

            if (ip == null)
            {
                return Dns.GetHostEntry(Dns.GetHostName()).HostName;
            }

            return Dns.GetHostEntry(ip).HostName;
        }

        private static readonly Regex _timeZoneRegex = new Regex(@"\s?(\((?<tz>[A-Z]{3,4})\))?$", RegexOptions.Compiled);

        /// <summary>
        /// Try parse date time.
        /// </summary>
        /// <param name="dateTimeParse">The date time parse.</param>
        /// <returns>
        /// .
        /// </returns>
        public static DateTime? TryParseSTMPDateTime(string dateTimeParse)
        {
            if (string.IsNullOrWhiteSpace(dateTimeParse))
            {
                return null;
            }

            DateTime dateTime;

            // clean the timezone off
            dateTimeParse = _timeZoneRegex.Replace(dateTimeParse.Trim().Replace("−", "-"), string.Empty).Trim();

            return DateTime.TryParse(dateTimeParse, out dateTime) ? (DateTime?)dateTime : null;
        }

        #endregion

        #region Methods

        /// <summary>
        /// The get external ip.
        /// </summary>
        /// <returns>
        /// </returns>
        private static IPAddress GetExternalIp()
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

        /// <summary>
        /// To FileSizeFormat... Thank you to "deepee1" on StackOverflow for this elegent solution:
        /// http://stackoverflow.com/a/4975942
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ToFileSizeFormat(this long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };

            if (bytes == 0)
            {
                return string.Format("0{0}", suffixes[0]);
            }

            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));

            double roundedNumber = Math.Round(bytes / Math.Pow(1024, place), 1);

            return roundedNumber.ToString(CultureInfo.InvariantCulture) + suffixes[place];            
        }

        public static bool IsDefault<TIn>(this TIn value)
        {
            // from the master, J. Skeet:
            return EqualityComparer<TIn>.Default.Equals(value, default(TIn));
        }

        public static TOut IfNotNull<TIn, TOut>(this TIn value, Func<TIn, TOut> continueFunc)
        {
            return value.IsDefault() ? default(TOut) : continueFunc(value);
        }

        /// <summary>
        /// Converts to an IEnumerable'T if obj is not default.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static IEnumerable<T> ToEnumerable<T>(this T obj)
        {
            if (!obj.IsDefault())
            {
                yield return obj;
            }

            yield break;
        }

        #endregion
    }
}