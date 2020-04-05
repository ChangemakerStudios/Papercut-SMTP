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

namespace Papercut.Helpers
{
    using System;
    using System.Linq;
    using System.Windows.Media.Effects;

    using Message.Helpers;

    using MimeKit;

    using Papercut.Core.Domain.Message;
    using Papercut.Message;

    public class MimeMessageEntry : MessageEntry
    {
        string _subject;
        private MessagePriority _priority;
        private int _attachmentsCount;

        public MimeMessageEntry(MessageEntry entry, MimeMessageLoader loader)
            : base(entry.File)
        {
            IsSelected = entry.IsSelected;

            loader.GetObservable(this).Subscribe(m =>
                {
                    Subject = m.Subject;
                    Priority = m.Priority;
                    AttachmentsCount = m.Attachments.Count();
                },
                e =>
                {
                    Subject = $"Failure loading message: {e.Message}";
                });
        }
        
        public int AttachmentsCount
        {
            get => _attachmentsCount;
            set
            {
                if (value == _attachmentsCount) return;
                _attachmentsCount = value;
                OnPropertyChanged(nameof(AttachmentsCount));
            }
        }

        public MessagePriority Priority
        {
            get => _priority;
            protected set
            {
                if (value == _priority) return;
                _priority = value;
                OnPropertyChanged(nameof(Priority));
            }
        }

        public string Subject
        {
            get => _subject;
            protected set
            {
                if (value == _subject) return;
                _subject = value;
                OnPropertyChanged(nameof(Subject));
            }
        }
    }
}