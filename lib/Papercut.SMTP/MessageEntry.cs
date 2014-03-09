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
    using System.Globalization;
    using System.IO;
    using System.Text.RegularExpressions;

    #endregion

    /// <summary>
    /// The message entry.
    /// </summary>
    public class MessageEntry
    {
        #region Constants and Fields

        /// <summary>
        /// The name format.
        /// </summary>
        private static readonly Regex nameFormat = new Regex(@"^(?<date>\d{14,16})(\-[A-Z0-9]{2})?\.eml$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// The info.
        /// </summary>
        private readonly FileInfo info;

        /// <summary>
        /// The created.
        /// </summary>
        private DateTime? created;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageEntry"/> class.
        /// </summary>
        /// <param name="file">
        /// The file.
        /// </param>
        public MessageEntry(string file)
        {
            this.info = new FileInfo(file);

            var match = nameFormat.Match(this.info.Name);

            if (match.Success)
            {
                this.created = DateTime.ParseExact(match.Groups["date"].Value, "yyyyMMddHHmmssFF", CultureInfo.InvariantCulture);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets File.
        /// </summary>
        public string File
        {
            get
            {
                return this.info.FullName;
            }
        }

        /// <summary>
        /// Gets ModifiedDate.
        /// </summary>
        public DateTime ModifiedDate
        {
            get
            {
                return this.info.LastWriteTime;
            }
        }

        /// <summary>
        /// Gets Name.
        /// </summary>
        public string Name
        {
            get
            {
                return this.info.Name;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The to string.
        /// </summary>
        /// <returns>
        /// The to string.
        /// </returns>
        public override string ToString()
        {
            return string.Format
                ("{0} ({1})", this.created.HasValue ? this.created.Value.ToString("G") : this.info.Name, (this.info.Length.ToFileSizeFormat()));
        }

        #endregion
    }
}