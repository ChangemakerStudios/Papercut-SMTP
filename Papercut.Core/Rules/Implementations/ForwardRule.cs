// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2015 Jaben Cargman
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

namespace Papercut.Core.Rules.Implementations
{
    using System;
    using System.ComponentModel;

    [Serializable]
    public class ForwardRule : RuleBase
    {
        string _fromEmail;

        string _smtpPassword;

        int _smtpPort = 25;

        string _smtpServer = "10.0.0.1";

        bool _smtpUseSsl;

        string _smtpUsername;

        string _toEmail;

        [Category("Settings")]
        [DisplayName("SMTP Password")]
        [PasswordPropertyTextAttribute]
        public string SMTPPassword
        {
            get { return _smtpPassword; }
            set
            {
                if (value == _smtpPassword) return;
                _smtpPassword = value;
                OnPropertyChanged(nameof(SMTPPassword));
            }
        }

        [Category("Settings")]
        [DisplayName("SMTP Username")]
        public string SMTPUsername
        {
            get { return _smtpUsername; }
            set
            {
                if (value == _smtpUsername) return;
                _smtpUsername = value;
                OnPropertyChanged(nameof(SMTPUsername));
            }
        }

        [Category("Settings")]
        [DisplayName("SMTP Port")]
        public int SMTPPort
        {
            get { return _smtpPort; }
            set
            {
                if (value == _smtpPort) return;
                _smtpPort = value;
                OnPropertyChanged(nameof(SMTPPort));
            }
        }

        [Category("Settings")]
        [DisplayName("SMTP Use SSL")]
        public bool SmtpUseSSL
        {
            get { return _smtpUseSsl; }
            set
            {
                if (value.Equals(_smtpUseSsl)) return;
                _smtpUseSsl = value;
                OnPropertyChanged(nameof(SmtpUseSSL));
            }
        }

        [Category("Information")]
        public override string Type => "Forward";

        [Category("Settings")]
        [DisplayName("From Email")]
        [Description("Forward From Email")]
        public string FromEmail
        {
            get { return _fromEmail; }
            set
            {
                if (value == _fromEmail) return;
                _fromEmail = value;
                OnPropertyChanged(nameof(FromEmail));
            }
        }

        [Category("Settings")]
        [DisplayName("SMTP Server")]
        [Description("Foward to SMTP Server")]
        public string SMTPServer
        {
            get { return _smtpServer; }
            set
            {
                if (value == _smtpServer) return;
                _smtpServer = value;
                OnPropertyChanged(nameof(SMTPServer));
            }
        }

        [Category("Settings")]
        [DisplayName("To Email")]
        [Description("Foward To Email")]
        public string ToEmail
        {
            get { return _toEmail; }
            set
            {
                if (value == _toEmail) return;
                _toEmail = value;
                OnPropertyChanged(nameof(ToEmail));
            }
        }

        public override string ToString()
        {
            return
                string.Format(
                    "SMTP Server: {0}:{1}\r\nSMTP Username: {2}\r\nSMTP Use SSL: {3}\r\nFrom Email: {4}\r\nTo Email: {5}",
                    _smtpServer,
                    _smtpPort,
                    _smtpUsername,
                    _smtpUseSsl,
                    _fromEmail,
                    _toEmail);
        }
    }
}