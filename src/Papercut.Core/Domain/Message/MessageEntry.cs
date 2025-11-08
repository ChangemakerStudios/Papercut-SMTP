// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
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


using System.ComponentModel;
using System.Globalization;

using Papercut.Common.Extensions;
using Papercut.Core.Infrastructure.Identities;

namespace Papercut.Core.Domain.Message;

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

    private bool _hasBeenSeen;

    private bool _isSelected;

    public MessageEntry(FileInfo fileInfo)
    {
        _info = fileInfo;

        Id = HashHelpers.GenerateUniqueId(_info.Name);

        var firstBit = _info.Name.Split(' ').FirstOrDefault();

        if (firstBit?.Length == DateTimeFormat.Length)
        {
            _created = DateTime.ParseExact(
                firstBit,
                DateTimeFormat,
                CultureInfo.InvariantCulture);
        }
        else
        {
            _created = _info.CreationTime;
        }

        if (_created > DateTime.Now.Add(-TimeSpan.FromMinutes(5)))
        {
            // anything under 5 minutes old is "new" still by default
            _hasBeenSeen = false;
        }
        else
        {
            // everything else has been seen by default
            _hasBeenSeen = true;
        }
    }

    public MessageEntry(string file)
        : this(new FileInfo(file))
    {
    }

    public DateTime ModifiedDate => _info.LastWriteTime;

    public long SortTicks => (_created ?? ModifiedDate).Ticks;

    public string Name => _info.Name;

    public string Id { get; }

    public long FileSizeBytes => _info.Length;

    public string FileSize => _info.Length.ToFileSizeFormat();

    public string DisplayText => $"{_created?.ToString("G") ?? _info.Name} ({FileSize})";

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;

            if (value)
            {
                HasBeenSeen = true;
            }
            OnPropertyChanged(nameof(IsSelected));
        }
    }

    public bool HasBeenSeen
    {
        get => _hasBeenSeen;
        set
        {
            _hasBeenSeen = value;
            OnPropertyChanged(nameof(HasBeenSeen));
        }
    }

    public bool Equals(MessageEntry? other)
    {
        return Equals(File, other?.File);
    }

    public string File => _info.FullName;

    public event PropertyChangedEventHandler? PropertyChanged;

    public override string ToString()
    {
        return DisplayText;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;

        if (obj is MessageEntry entry)
        {
            return Equals(entry);
        }

        return false;
    }

    public override int GetHashCode() => File.GetHashCode();

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged(string propertyName)
    {
        var propertyChangedEventHandler = PropertyChanged;
        if (propertyChangedEventHandler != null)
        {
            propertyChangedEventHandler.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}