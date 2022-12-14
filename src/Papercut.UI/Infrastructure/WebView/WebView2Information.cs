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


namespace Papercut.Infrastructure.WebView
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;

    using Autofac;

    using Microsoft.Web.WebView2.Core;

    using Papercut.Core.Annotations;

    using Serilog;

    public enum WebView2InstallType
    {
        WebView2, EdgeChromiumBeta, EdgeChromiumCanary, EdgeChromiumDev, NotInstalled
    }

    /// <summary>
    /// Code from https://github.com/mortenbrudvik/KioskBrowser/blob/main/src/KioskBrowser/WebView2Install.cs
    /// THANK YOU
    /// </summary>
    public class WebView2Information
    {
        #region Constructors and Destructors

        static WebView2Information()
        {
            var loaderPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", $@"runtimes\win-{RuntimeInformation.ProcessArchitecture}\native");

            Log.Information("Setting WebView2 Loader Path to {LoaderPath}", loaderPath);

            try
            {
                CoreWebView2Environment.SetLoaderDllFolderPath(loaderPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failure Settings WebView2 Loader Path");
            }
        }

        public WebView2Information()
        {
            this.Version = this.GetWebView2Version();
            this.InstallType = this.GetInstallType(this.Version);
        }

        #endregion

        #region Public Properties

        public WebView2InstallType InstallType { get; }

        public bool IsInstalled => this.InstallType != WebView2InstallType.NotInstalled;

        public string Version { get; }

        public Exception WebView2LoadException { get; private set; }

        #endregion

        #region Methods

        #region Begin Static Container Registrations

        /// <summary>
        /// Called dynamically from the RegisterStaticMethods() call in the container module.
        /// </summary>
        /// <param name="builder"></param>
        [UsedImplicitly]
        static void Register([NotNull] ContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.RegisterType<WebView2Information>().AsSelf().SingleInstance();
        }

        #endregion

        private WebView2InstallType GetInstallType(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                return WebView2InstallType.NotInstalled;
            }

            if (version.Contains("dev"))
                return WebView2InstallType.EdgeChromiumDev;

            if (version.Contains("beta"))
                return WebView2InstallType.EdgeChromiumBeta;

            if (version.Contains("canary"))
                return WebView2InstallType.EdgeChromiumCanary;

            return WebView2InstallType.WebView2;
        }

        private string GetWebView2Version()
        {
            try
            {
                return CoreWebView2Environment.GetAvailableBrowserVersionString();
            }
            catch (Exception ex)
            {
                this.WebView2LoadException = ex;
                return "";
            }
        }

        #endregion
    }
}