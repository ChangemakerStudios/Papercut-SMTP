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


using System.ComponentModel;

using Autofac;

using MimeKit;

using Papercut.Common.Extensions;
using Papercut.Core.Domain.Rules;
using Papercut.Rules.Domain.Rules;

namespace Papercut.Rules.Domain.Relaying;

[Serializable]
public class RelayRule : RuleBase
{
    string _smtpPassword;

    int _smtpPort = 25;

    string _smtpServer = "10.0.0.1";

    string _smtpUsername;

    bool _smtpUseSsl;

    private string _toBcc;

    [Category("Settings")]
    [DisplayName("SMTP Password")]
    [PasswordPropertyText]
    public string SmtpPassword
    {
        get => this._smtpPassword;
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
        get => this._smtpUsername;
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
        get => this._smtpPort;
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
        get => this._smtpUseSsl;
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
        get => this._smtpServer;
        set
        {
            if (value == this._smtpServer) return;
            this._smtpServer = value;
            this.OnPropertyChanged(nameof(this.SmtpServer));
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
            this.OnPropertyChanged(nameof(this.ToBcc));
        }
    }

    public virtual void PopulateFromRule(MimeMessage message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        if (string.IsNullOrWhiteSpace(this.ToBcc))
        {
            return;
        }

        foreach (var bcc in this.ToBcc.Split(new[] { ',', '|', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()))
        {
            message.Bcc.Add(new MailboxAddress(bcc, bcc));
        }
    }

    protected override IEnumerable<KeyValuePair<string, Lazy<object>>> GetPropertiesForDescription()
    {
        return base.GetPropertiesForDescription().Concat(this.GetProperties());
    }

    #region Begin Static Container Registrations

    /// <summary>
    /// Called dynamically from the RegisterStaticMethods() call in the container module.
    /// </summary>
    /// <param name="builder"></param>
    [UsedImplicitly]
    static void Register(ContainerBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.RegisterType<RelayRule>().AsSelf().As<IRule>().InstancePerDependency();
    }

    #endregion
}