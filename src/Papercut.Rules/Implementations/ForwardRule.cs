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


namespace Papercut.Rules.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    using MimeKit;

    using Papercut.Common.Extensions;

    [Serializable]
    public class ForwardRule : RelayRule
    {
        private string _fromEmail;

        private string _toEmail;

        [Category("Information")]
        public override string Type => "Forward";

        [Category("Settings")]
        [DisplayName("From Email")]
        [Description("Forward From Email")]
        public string FromEmail
        {
            get => _fromEmail;
            set
            {
                if (value == _fromEmail) return;
                _fromEmail = value;
                OnPropertyChanged(nameof(FromEmail));
            }
        }

        [Category("Settings")]
        [DisplayName("To Email")]
        [Description("Foward To Email")]
        public string ToEmail
        {
            get => _toEmail;
            set
            {
                if (value == _toEmail) return;
                _toEmail = value;
                OnPropertyChanged(nameof(ToEmail));
            }
        }

        public override void PopulateFromRule(MimeMessage mimeMessage)
        {
            if (mimeMessage == null) throw new ArgumentNullException(nameof(mimeMessage));

            if (!string.IsNullOrWhiteSpace(this.FromEmail))
            {
                mimeMessage.From.Clear();
                mimeMessage.From.Add(
                    new MailboxAddress(this.FromEmail, this.FromEmail));
            }

            if (!string.IsNullOrWhiteSpace(this.ToEmail))
            {
                mimeMessage.To.Clear();
                mimeMessage.Bcc.Clear();
                mimeMessage.Cc.Clear();
                mimeMessage.To.Add(new MailboxAddress(this.ToEmail, this.ToEmail));
            }

            base.PopulateFromRule(mimeMessage);
        }

        protected override IEnumerable<KeyValuePair<string, Lazy<object>>> GetPropertiesForDescription()
        {
            return base.GetPropertiesForDescription().Concat(this.GetProperties());
        }
    }
}