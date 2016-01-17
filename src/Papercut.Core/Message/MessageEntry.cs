// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2016 Jaben Cargman
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

namespace Papercut.Core.Message
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Text.RegularExpressions;

    using Papercut.Core.Annotations;
    using Papercut.Core.Helper;

    /// <summary>
    ///     The message entry.
    /// </summary>
    public class MessageEntry : INotifyPropertyChanged, IEquatable<MessageEntry>, IFile
    {
        static readonly Regex _nameFormat = new Regex(
            @"^(?<date>\d{14,16})(\-[A-Z0-9]{2})?\.eml$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        protected readonly FileInfo _info;

        protected DateTime? _created;

        bool _isSelected;

        public MessageEntry(FileInfo fileInfo)
        {
            _info = fileInfo;

            Match match = _nameFormat.Match(_info.Name);
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

        public DateTime ModifiedDate => _info.LastWriteTime;

        public string Name => _info.Name;

        public string FileSize => _info.Length.ToFileSizeFormat();

        public string DisplayText => $"{_created?.ToString("G") ?? _info.Name} ({(FileSize)})";

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public bool Equals(MessageEntry other)
        {
            return Equals(_info, other._info);
        }

        public string File => _info.FullName;

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return DisplayText;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((MessageEntry)obj);
        }

        public override int GetHashCode() => _info?.GetHashCode() ?? 0;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}