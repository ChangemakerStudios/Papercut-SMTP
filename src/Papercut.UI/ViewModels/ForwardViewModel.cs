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

namespace Papercut.ViewModels
{
    using System.Text.RegularExpressions;
    using System.Windows;

    using Caliburn.Micro;

    using Common;

    using Core;

    using Papercut.Properties;

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
            get => _fromSetting;
            set
            {
                _fromSetting = value;
                NotifyOfPropertyChange(() => FromSetting);
            }
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                _windowTitle = value;
                NotifyOfPropertyChange(() => WindowTitle);
            }
        }

        public string Server
        {
            get => _server;
            set
            {
                _server = value;
                NotifyOfPropertyChange(() => Server);
            }
        }

        public string To
        {
            get => _to;
            set
            {
                _to = value;
                NotifyOfPropertyChange(() => To);
            }
        }

        public string From
        {
            get => _from;
            set
            {
                _from = value;
                NotifyOfPropertyChange(() => From);
            }
        }

        void Load()
        {
            // Load previous settings
            Server = Settings.Default.ForwardServer;
            To = Settings.Default.ForwardTo;
            From = Settings.Default.ForwardFrom;
        }

        public void Cancel()
        {
            TryClose(false);
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);

            if (FromSetting) Load();
        }

        public void Send()
        {
            if (string.IsNullOrEmpty(Server) || string.IsNullOrEmpty(From)
                || string.IsNullOrEmpty(To))
            {
                MessageBox.Show(
                    "All the text boxes are required, fill them in please.",
                    AppConstants.ApplicationName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!_emailRegex.IsMatch(From) || !_emailRegex.IsMatch(To))
            {
                MessageBox.Show(
                    "You need to enter valid email addresses.",
                    AppConstants.ApplicationName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (FromSetting)
            {
                // Save settings for the next time
                Settings.Default.ForwardServer = Server.Trim();
                Settings.Default.ForwardTo = To.Trim();
                Settings.Default.ForwardFrom = From.Trim();
                Settings.Default.Save();
            }

            TryClose(true);
        }
    }
}