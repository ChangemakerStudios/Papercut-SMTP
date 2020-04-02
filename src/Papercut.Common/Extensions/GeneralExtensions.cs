// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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

namespace Papercut.Common.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Papercut.Common.Helper;
    using Papercut.Core.Annotations;

    /// <summary>
    ///     The util.
    /// </summary>
    public static class GeneralExtensions
    {
        public static string AsString(this byte[] bytes, Encoding byteEncoding = null)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            byteEncoding = byteEncoding ?? Encoding.UTF8;
            return byteEncoding.GetString(bytes);
        }

        public static string ToBase64String(this string value, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            
            encoding = encoding ?? Encoding.UTF8;
            return Convert.ToBase64String(encoding.GetBytes(value));
        }

        /// <summary>
        ///     To FileSizeFormat... Thank you to "deepee1" on StackOverflow for this elegant solution:
        ///     http://stackoverflow.com/a/4975942
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ToFileSizeFormat(this long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };

            if (bytes == 0) return $"0{suffixes[0]}";

            var place = Convert.ToInt64(Math.Floor(Math.Log(bytes, 1024)));

            double roundedNumber = Math.Round(bytes / Math.Pow(1024, place), 1);

            return roundedNumber.ToString(CultureInfo.InvariantCulture) + suffixes[place];
        }

        public static string GetOriginalFileName([NotNull] string path, [NotNull] string fileName)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            return
                GenerateFormattedFileNames(fileName)
                    .Select(f => Path.Combine(path, f))
                    .FirstOrDefault(f => !File.Exists(f));
        }

        static IEnumerable<string> GenerateFormattedFileNames([NotNull] string fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            var fileSansExtension = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            bool isFirst = true;

            while (true)
            {
                string randomString;

                if (!isFirst)
                    randomString = "-" + StringHelpers.SmallRandomString();
                else
                {
                    randomString = string.Empty;
                    isFirst = false;
                }

                yield return "{0}{1}{2}".FormatWith(fileSansExtension, randomString, extension);
            }
        }
    }
}
