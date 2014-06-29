// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2014 Jaben Cargman
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

namespace Papercut.Core.Rules
{
    using System;

    [Serializable]
    public class ForwardRule : RuleBase
    {
        string _fromEmail;

        string _smtpServer;

        string _toEmail;

        public ForwardRule()
        {
        }

        public ForwardRule(string smtpServer, string fromEmail, string toEmail)
            : this()
        {
            FromEmail = fromEmail;
            SmtpServer = smtpServer;
            ToEmail = toEmail;
        }

        public string FromEmail
        {
            get { return _fromEmail; }
            set
            {
                if (value == _fromEmail) return;
                _fromEmail = value;
                OnPropertyChanged("FromEmail");
            }
        }

        public string SmtpServer
        {
            get { return _smtpServer; }
            set
            {
                if (value == _smtpServer) return;
                _smtpServer = value;
                OnPropertyChanged("SmtpServer");
            }
        }

        public string ToEmail
        {
            get { return _toEmail; }
            set
            {
                if (value == _toEmail) return;
                _toEmail = value;
                OnPropertyChanged("ToEmail");
            }
        }

        public override string ToString()
        {
            return string.Format(
                "Smtp Server: {0}, From Email: {1}, To Email: {2}",
                SmtpServer,
                FromEmail,
                ToEmail);
        }
    }
}