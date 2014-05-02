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

namespace Papercut.Core.Message
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text.RegularExpressions;

    /// <summary>
    ///     The message entry.
    /// </summary>
    public class MessageEntry
    {
        static readonly Regex _nameFormat = new Regex(
            @"^(?<date>\d{14,16})(\-[A-Z0-9]{2})?\.eml$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        protected readonly FileInfo _info;

        protected DateTime? _created;

        public MessageEntry(FileInfo fileInfo)
        {
            _info = fileInfo;

            var match = _nameFormat.Match(_info.Name);
            if (match.Success)
            {
                _created = DateTime.ParseExact(
                    match.Groups["date"].Value,
                    "yyyyMMddHHmmssFF",
                    CultureInfo.InvariantCulture);
            }
        }

        public MessageEntry(string file)
            : this(new FileInfo(file))
        {
        }

        public string File
        {
            get
            {
                return _info.FullName;
            }
        }

        public DateTime ModifiedDate
        {
            get
            {
                return _info.LastWriteTime;
            }
        }

        public string Name
        {
            get
            {
                return _info.Name;
            }
        }

        public override string ToString()
        {
            return string.Format(
                "{0} ({1})",
                _created.HasValue ? _created.Value.ToString("G") : _info.Name,
                (_info.Length.ToFileSizeFormat()));
        }

        protected bool Equals(MessageEntry other)
        {
            return Equals(_info, other._info);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((MessageEntry)obj);
        }

        public override int GetHashCode()
        {
            return (_info != null ? _info.GetHashCode() : 0);
        }
    }
}