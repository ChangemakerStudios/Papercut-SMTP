// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2022 Jaben Cargman
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


using System;

using Papercut.Core.Domain.Settings;

namespace Papercut.App.WebApi
{
    public class PapercutHttpServerSettings : ISettingsTyped
    {
        public PapercutHttpServerSettings(ISettingStore settings)
        {
            this.Settings = settings;
        }

        const string DefaultHttpBaseAddress = "http://127.0.0.1";

        const int DefaultHttpPort = 37408;

        public bool HttpServerEnabled
        {
            get => this.Settings.GetOrSet("HttpServerEnabled", true, "Is the Papercut Web UI Server enabled? (Defaults to true)");
            set { if (this.HttpServerEnabled != value) this.Settings.Set("HttpServerEnabled", value); }
        }

        public int HttpPort
        {
            get => this.Settings.GetOrSet("HttpPort", DefaultHttpPort, $"The Papercut Web UI Server listening port (Defaults to {DefaultHttpPort}).");
            set { if (this.HttpPort != value) this.Settings.Set("HttpPort", value); }
        }

        public string HttpBaseAddress
        {
            get =>
                this.Settings.GetOrSet<string>(
                        "HttpBaseAddress",
                        DefaultHttpBaseAddress,
                        $"The Papercut Web UI Server listening address (Defaults to {DefaultHttpBaseAddress}).")
                    .Replace("*", "0.0.0.0");

            set { if (this.HttpBaseAddress != value) this.Settings.Set("HttpBaseAddress", value); }
        }

        internal string GetListeningUri()
        {
            var uri = new UriBuilder($"{HttpBaseAddress.Trim()}:{HttpPort}");

            return uri.ToString();
        }

        public ISettingStore Settings { get; set; }
    }
}