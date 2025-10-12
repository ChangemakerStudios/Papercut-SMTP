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


using System.Text.RegularExpressions;

using Papercut.Core;

namespace Papercut.ViewModels;

public class ForwardViewModel : Screen
{
    static readonly Regex _emailRegex =
        new Regex(
            @"(\A(\s*)\Z)|(\A([^@\s]+)@((?:[-a-z0-9]+\.)+[a-z]{2,})\Z)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    string _from;

    bool _fromSetting;

    string _server;

    string _to;

    string _windowTitle = "Forward Message";

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

    void Load()
    {
        // Load previous settings
        this.Server = Settings.Default.ForwardServer;
        this.To = Settings.Default.ForwardTo;
        this.From = Settings.Default.ForwardFrom;
    }

    public async Task Cancel()
    {
        await this.TryCloseAsync(false);
    }

    protected override void OnViewLoaded(object view)
    {
        base.OnViewLoaded(view);

        if (this.FromSetting) this.Load();
    }

    public async Task Send()
    {
        if (string.IsNullOrEmpty(this.Server) || string.IsNullOrEmpty(this.From)
                                              || string.IsNullOrEmpty(this.To))
        {
            MessageBox.Show(
                "All the text boxes are required, fill them in please.",
                AppConstants.ApplicationName,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (!_emailRegex.IsMatch(this.From) || !_emailRegex.IsMatch(this.To))
        {
            MessageBox.Show(
                "You need to enter valid email addresses.",
                AppConstants.ApplicationName,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (this.FromSetting)
        {
            // Save settings for the next time
            Settings.Default.ForwardServer = this.Server.Trim();
            Settings.Default.ForwardTo = this.To.Trim();
            Settings.Default.ForwardFrom = this.From.Trim();
            Settings.Default.Save();
        }

        await this.TryCloseAsync(true);
    }
}