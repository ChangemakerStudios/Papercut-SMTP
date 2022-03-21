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

using Autofac;

using Papercut.Core.Annotations;

using Serilog;

namespace Papercut.Infrastructure.WebView
{
    public class WebView2InformationReporter : IStartable
    {
        private readonly Lazy<ILogger> _logger;

        private readonly WebView2Information _webView2Information;

        public WebView2InformationReporter(WebView2Information webView2Information, Lazy<ILogger> logger)
        {
            this._webView2Information = webView2Information;
            this._logger = logger;
        }

        public void Start()
        {
            if (this._webView2Information.IsInstalled)
            {
                this._logger.Value.Error(
                    "Required Component 'WebView2' is NOT installed. Visit this url to download: https://go.microsoft.com/fwlink/p/?LinkId=2124703");
            }
            else
            {
                this._logger.Value.Information(
                    "WebView2 Installed Version {WebView2:l} {WebView2InstallType}",
                    this._webView2Information.Version,
                    this._webView2Information.InstallType);
            }
        }

        #region Begin Static Container Registrations

        /// <summary>
        /// Called dynamically from the RegisterStaticMethods() call in the container module.
        /// </summary>
        /// <param name="builder"></param>
        [UsedImplicitly]
        static void Register([NotNull] ContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.RegisterType<WebView2InformationReporter>().AsImplementedInterfaces().SingleInstance();
        }

        #endregion
    }
}