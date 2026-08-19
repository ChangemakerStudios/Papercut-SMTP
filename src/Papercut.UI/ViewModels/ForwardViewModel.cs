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


using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

using Papercut.AppLayer.Rules;
using Papercut.Core;
using Papercut.Core.Domain.Rules;
using Papercut.Domain.UiCommands;
using Papercut.Rules.Domain.Forwarding;
using Papercut.Rules.Domain.Relaying;

namespace Papercut.ViewModels;

public class ForwardViewModel : Screen
{
    static readonly Regex _emailRegex =
        new Regex(
            @"(\A(\s*)\Z)|(\A([^@\s]+)@((?:[-a-z0-9]+\.)+[a-z]{2,})\Z)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    readonly IUiCommandHub _uiCommandHub;

    readonly RuleService _ruleService;

    string _from;

    bool _fromSetting;

    string _server;

    string _to;

    string _username;

    string _password;

    int _port = 25;

    bool _useSsl;

    bool _useAuthentication;

    string _windowTitle = "Forward Message";

    RelayRuleListItem _selectedRule;

    public ForwardViewModel(IUiCommandHub uiCommandHub, RuleService ruleService)
    {
        _uiCommandHub = uiCommandHub;
        _ruleService = ruleService;
        AvailableRules = new ObservableCollection<RelayRuleListItem>();
    }

    public bool FromSetting
    {
        get => this._fromSetting;
        set
        {
            this._fromSetting = value;
            this.NotifyOfPropertyChange(() => this.FromSetting);
        }
    }

    public string WindowTitle
    {
        get => this._windowTitle;
        set
        {
            this._windowTitle = value;
            this.NotifyOfPropertyChange(() => this.WindowTitle);
        }
    }

    public string Server
    {
        get => this._server;
        set
        {
            this._server = value;
            this.NotifyOfPropertyChange(() => this.Server);
        }
    }

    public string To
    {
        get => this._to;
        set
        {
            this._to = value;
            this.NotifyOfPropertyChange(() => this.To);
        }
    }

    public string From
    {
        get => this._from;
        set
        {
            this._from = value;
            this.NotifyOfPropertyChange(() => this.From);
        }
    }

    public string Username
    {
        get => this._username;
        set
        {
            this._username = value;
            this.NotifyOfPropertyChange(() => this.Username);
        }
    }

    public string Password
    {
        get => this._password;
        set
        {
            this._password = value;
            this.NotifyOfPropertyChange(() => this.Password);
        }
    }

    public int Port
    {
        get => this._port;
        set
        {
            this._port = value;
            this.NotifyOfPropertyChange(() => this.Port);
        }
    }

    public bool UseSsl
    {
        get => this._useSsl;
        set
        {
            this._useSsl = value;
            this.NotifyOfPropertyChange(() => this.UseSsl);
        }
    }

    public bool UseAuthentication
    {
        get => this._useAuthentication;
        set
        {
            this._useAuthentication = value;
            this.NotifyOfPropertyChange(() => this.UseAuthentication);

            if (!value)
            {
                this.Username = string.Empty;
                this.Password = string.Empty;
            }
        }
    }

    public ObservableCollection<RelayRuleListItem> AvailableRules { get; }

    public bool HasAvailableRules => AvailableRules.Count > 0;

    public RelayRuleListItem SelectedRule
    {
        get => this._selectedRule;
        set
        {
            this._selectedRule = value;
            this.NotifyOfPropertyChange(() => this.SelectedRule);

            if (value?.Rule != null)
            {
                PopulateFromRule(value.Rule);
            }
        }
    }

    void PopulateFromRule(RelayRule rule)
    {
        this.Server = rule.SmtpServer;
        this.Port = rule.SmtpPort;
        this.UseSsl = rule.SmtpUseSSL;

        var hasCredentials = !string.IsNullOrEmpty(rule.SmtpUsername) || !string.IsNullOrEmpty(rule.SmtpPassword);
        this.UseAuthentication = hasCredentials;
        this.Username = rule.SmtpUsername;
        this.Password = rule.SmtpPassword;

        if (rule is ForwardRule forwardRule)
        {
            this.From = forwardRule.FromEmail;
            this.To = forwardRule.ToEmail;
        }
    }

    void LoadAvailableRules()
    {
        AvailableRules.Clear();

        var relayRules = _ruleService.Rules.OfType<ForwardRule>();
        foreach (var rule in relayRules)
        {
            AvailableRules.Add(new RelayRuleListItem(rule));
        }

        this.NotifyOfPropertyChange(() => this.HasAvailableRules);
    }

    void Load()
    {
        // Load previous settings
        this.Server = Settings.Default.ForwardServer;
        this.To = Settings.Default.ForwardTo;
        this.From = Settings.Default.ForwardFrom;
        this.Username = Settings.Default.ForwardSmtpUsername;
        // Password is intentionally not persisted to settings for security
        this.Port = Settings.Default.ForwardSmtpPort;
        this.UseSsl = Settings.Default.ForwardSmtpUseSsl;
    }

    public async Task Cancel()
    {
        await this.TryCloseAsync(false);
    }

    protected override void OnViewLoaded(object view)
    {
        base.OnViewLoaded(view);

        LoadAvailableRules();

        if (this.FromSetting) this.Load();
    }

    public async Task Send()
    {
        if (string.IsNullOrEmpty(this.Server) || string.IsNullOrEmpty(this.From)
                                              || string.IsNullOrEmpty(this.To))
        {
            _uiCommandHub.ShowMessage(
                "All the text boxes are required, fill them in please.",
                AppConstants.ApplicationName);
            return;
        }

        if (!_emailRegex.IsMatch(this.From) || !_emailRegex.IsMatch(this.To))
        {
            _uiCommandHub.ShowMessage(
                "You need to enter valid email addresses.",
                AppConstants.ApplicationName);
            return;
        }

        if (this.Port < 1 || this.Port > 65535)
        {
            _uiCommandHub.ShowMessage(
                "SMTP port must be between 1 and 65535.",
                AppConstants.ApplicationName);
            return;
        }

        if (this.FromSetting)
        {
            // Save settings for the next time
            Settings.Default.ForwardServer = this.Server.Trim();
            Settings.Default.ForwardTo = this.To.Trim();
            Settings.Default.ForwardFrom = this.From.Trim();
            Settings.Default.ForwardSmtpUsername = this.Username?.Trim() ?? string.Empty;
            // Password is intentionally not persisted to settings for security
            Settings.Default.ForwardSmtpPort = this.Port;
            Settings.Default.ForwardSmtpUseSsl = this.UseSsl;
            Settings.Default.Save();
        }

        await this.TryCloseAsync(true);
    }
}

public class RelayRuleListItem
{
    public RelayRuleListItem(RelayRule rule)
    {
        Rule = rule;
    }

    public RelayRule Rule { get; }

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Rule.Name))
                return Rule.Name;

            var type = Rule.Type;
            var server = Rule.SmtpServer;
            if (Rule is ForwardRule fwd && !string.IsNullOrWhiteSpace(fwd.ToEmail))
                return $"{type} - {server} -> {fwd.ToEmail}";

            return $"{type} - {server}";
        }
    }

    public override string ToString() => DisplayName;
}
