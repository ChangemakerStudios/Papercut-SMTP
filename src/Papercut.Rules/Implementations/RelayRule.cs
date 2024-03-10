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


namespace Papercut.Rules.Implementations;

[Serializable]
public class RelayRule : RuleBase
{
    string _smtpPassword;

    int _smtpPort = 25;

    string _smtpServer = "10.0.0.1";

    string _smtpUsername;

    bool _smtpUseSsl;

    [Category("Settings")]
    [DisplayName("SMTP Password")]
    // [PasswordPropertyText] .NET Core does not provide this attribute
    public string SmtpPassword
    {
        get { return this._smtpPassword; }
        set
        {
            if (value == this._smtpPassword) return;
            this._smtpPassword = value;
            this.OnPropertyChanged(nameof(this.SmtpPassword));
        }
    }

    [Category("Settings")]
    [DisplayName("SMTP Username")]
    public string SmtpUsername
    {
        get { return this._smtpUsername; }
        set
        {
            if (value == this._smtpUsername) return;
            this._smtpUsername = value;
            this.OnPropertyChanged(nameof(this.SmtpUsername));
        }
    }

    [Category("Settings")]
    [DisplayName("SMTP Port")]
    public int SmtpPort
    {
        get { return this._smtpPort; }
        set
        {
            if (value == this._smtpPort) return;
            this._smtpPort = value;
            this.OnPropertyChanged(nameof(this.SmtpPort));
        }
    }

    [Category("Settings")]
    [DisplayName("SMTP Use SSL")]
    public bool SmtpUseSSL
    {
        get { return this._smtpUseSsl; }
        set
        {
            if (value.Equals(this._smtpUseSsl)) return;
            this._smtpUseSsl = value;
            this.OnPropertyChanged(nameof(this.SmtpUseSSL));
        }
    }

    [Category("Information")]
    public override string Type => "Relay";

    [Category("Settings")]
    [DisplayName("SMTP Server")]
    public string SmtpServer
    {
        get { return this._smtpServer; }
        set
        {
            if (value == this._smtpServer) return;
            this._smtpServer = value;
            this.OnPropertyChanged(nameof(this.SmtpServer));
        }
    }

    protected override IEnumerable<KeyValuePair<string, Lazy<object>>> GetPropertiesForDescription()
    {
        return base.GetPropertiesForDescription().Concat(this.GetProperties());
    }
}