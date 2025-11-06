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


namespace Papercut.Infrastructure.WebView;

public class WebView2Reporter(Lazy<ILogger> logger, WebView2Information webView2Information)
    : IStartable
{
    protected ILogger Logger => logger.Value;

    public void Start()
    {
        if (!webView2Information.IsInstalled)
        {
            this.Logger.Error(
                webView2Information.WebView2LoadException,
                "Failure Loading 'WebView2' or Required Component 'WebView2' is not installed. Visit this URL to download: https://go.microsoft.com/fwlink/p/?LinkId=2124703");
        }
        else
        {
            this.Logger.Information(
                "WebView2 Installed Version {WebView2:l} {WebView2InstallType}",
                webView2Information.Version,
                webView2Information.InstallType);
        }
    }

    #region Begin Static Container Registrations

    /// <summary>
    /// Called dynamically from the RegisterStaticMethods() call in the container module.
    /// </summary>
    /// <param name="builder"></param>
    [UsedImplicitly]
    static void Register(ContainerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RegisterType<WebView2Reporter>().AsImplementedInterfaces().SingleInstance();
    }

    #endregion
}