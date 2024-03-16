// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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


using MimeKit;

using Papercut.Core.Domain.Message;
using Papercut.Message;

namespace Papercut.Helpers
{
    public class MimeMessageEntry : MessageEntry
    {
        private int _attachmentsCount;

        private MessagePriority _priority;

        string _subject;

        public MimeMessageEntry(MessageEntry entry, MimeMessageLoader loader)
            : base(entry.File)
        {
            this.IsSelected = entry.IsSelected;
            this.Subject = "Loading...";

            loader.GetMessageCallback(this, this.LoadMessageDetails);
        }

        public int AttachmentsCount
        {
            get => this._attachmentsCount;
            set
            {
                if (value == this._attachmentsCount) return;
                this._attachmentsCount = value;
                this.OnPropertyChanged(nameof(this.AttachmentsCount));
                this.OnPropertyChanged(nameof(this.HasAttachments));
            }
        }

        public MessagePriority Priority
        {
            get => this._priority;
            protected set
            {
                if (value == this._priority) return;
                this._priority = value;
                this.OnPropertyChanged(nameof(this.Priority));
            }
        }

        public string Subject
        {
            get => this._subject;
            protected set
            {
                if (value == this._subject) return;
                this._subject = value;
                this.OnPropertyChanged(nameof(this.Subject));
            }
        }

        public bool HasAttachments
        {
            get => this.AttachmentsCount > 0;
        }

        private void LoadMessageDetails(MimeMessage message)
        {
            this.Subject = message.Subject;
            this.Priority = message.Priority;
            this.AttachmentsCount = message.Attachments.Count();
        }
    }
}