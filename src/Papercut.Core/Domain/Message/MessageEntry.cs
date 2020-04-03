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

namespace Papercut.Core.Domain.Message
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Papercut.Common.Extensions;
    using Papercut.Core.Annotations;

    /// <summary>
    ///     The message entry.
    /// </summary>
    public class MessageEntry : INotifyPropertyChanged, IEquatable<MessageEntry>, IFile
    {
        // You can start by using another timestamp format. An example below.
        //public const string DateTimeFormat = "yy.MMdd-HH.mm.ssfff";
        // Bug. It is sucks to use FFF instead of fff.
        // I.e. for 170ms we will get the string 17 and we will be forced to use RegEx dirty coding.
        //public const string DateTimeFormat = "yyyyMMddHHmmssFFF";
        public const string DateTimeFormat = "yyyyMMddHHmmssfff";

        protected readonly FileInfo _info;

        protected DateTime? _created;

        bool _isSelected;

        public MessageEntry(FileInfo fileInfo)
        {
            this._info = fileInfo;

            var firstBit = this._info.Name.Split(' ').FirstOrDefault();

            if (firstBit?.Length == DateTimeFormat.Length)
            {
                _created = DateTime.ParseExact(
                    firstBit,
                    DateTimeFormat,
                    CultureInfo.InvariantCulture);
            }
            else
            {
                _created = this._info.CreationTime;
            }
        }

        public MessageEntry(string file)
            : this(new FileInfo(file))
        {
        }

        public DateTime ModifiedDate => this._info.LastWriteTime;

        public string Name => this._info.Name;

        public string FileSize => this._info.Length.ToFileSizeFormat();

        public string DisplayText => $"{this._created?.ToString("G") ?? this._info.Name} ({(FileSize)})";

        public bool IsSelected
        {
            get => this._isSelected;
            set
            {
                this._isSelected = value;
                this.OnPropertyChanged(nameof(this.IsSelected));
            }
        }

        public bool Equals(MessageEntry other)
        {
            return Equals(this._info, other._info);
        }

        public string File => this._info.FullName;

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return this.DisplayText;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return this.Equals((MessageEntry)obj);
        }

        public override int GetHashCode() => this._info?.GetHashCode() ?? 0;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}