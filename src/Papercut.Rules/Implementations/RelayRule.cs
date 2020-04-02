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
    using Papercut.Core.Annotations;

    [Serializable]
    public class RelayRule : RuleBase
    {
        string _smtpPassword;

        int _smtpPort = 25;

        string _smtpServer = "10.0.0.1";

        bool _smtpUseSsl;

        string _smtpUsername;

        private string _toBcc;

        [Category("Settings")]
        [DisplayName("SMTP Password")]
        [PasswordPropertyText]
        public string SmtpPassword
        {
            get => _smtpPassword;
            set
            {
                if (value == _smtpPassword) return;
                _smtpPassword = value;
                OnPropertyChanged(nameof(SmtpPassword));
            }
        }

        [Category("Settings")]
        [DisplayName("SMTP Username")]
        public string SmtpUsername
        {
            get => _smtpUsername;
            set
            {
                if (value == _smtpUsername) return;
                _smtpUsername = value;
                OnPropertyChanged(nameof(SmtpUsername));
            }
        }

        [Category("Settings")]
        [DisplayName("SMTP Port")]
        public int SmtpPort
        {
            get => _smtpPort;
            set
            {
                if (value == _smtpPort) return;
                _smtpPort = value;
                OnPropertyChanged(nameof(SmtpPort));
            }
        }

        [Category("Settings")]
        [DisplayName("SMTP Use SSL")]
        public bool SmtpUseSSL
        {
            get => _smtpUseSsl;
            set
            {
                if (value.Equals(_smtpUseSsl)) return;
                _smtpUseSsl = value;
                OnPropertyChanged(nameof(SmtpUseSSL));
            }
        }

        [Category("Information")]
        public override string Type => "Relay";

        [Category("Settings")]
        [DisplayName("SMTP Server")]
        public string SmtpServer
        {
            get => _smtpServer;
            set
            {
                if (value == _smtpServer) return;
                _smtpServer = value;
                OnPropertyChanged(nameof(SmtpServer));
            }
        }

        [Category("Settings")]
        [DisplayName("To Bcc Email(s)")]
        [Description("To Bcc Email(s) (comma delimited)")]
        public string ToBcc
        {
            get => this._toBcc;
            set
            {
                if (value == this._toBcc) return;
                this._toBcc = value;
                OnPropertyChanged(nameof(this.ToBcc));
            }
        }

        public override void PopulateFromRule([NotNull] MimeMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            if (string.IsNullOrWhiteSpace(ToBcc))
            {
                return;
            }

            foreach (var bcc in ToBcc.Split(new[] { ',', '|', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()))
            {
                message.Bcc.Add(new MailboxAddress(bcc, bcc));
            }
        }

        protected override IEnumerable<KeyValuePair<string, Lazy<object>>> GetPropertiesForDescription()
        {
            return base.GetPropertiesForDescription().Concat(this.GetProperties());
        }
    }
}