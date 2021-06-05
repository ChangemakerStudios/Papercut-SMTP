// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2021 Jaben Cargman
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


namespace Papercut.AppLayer.HtmlPreviews
{
    using System;
    using System.IO;
    using System.Text;

    using Autofac;

    using MimeKit;

    using Papercut.Core.Annotations;
    using Papercut.Core.Domain.Application;
    using Papercut.Domain.HtmlPreviews;
    using Papercut.Helpers;

    using Serilog;

    public class HtmlPreviewService : IHtmlPreviewGenerator
    {
        readonly IAppMeta _appMeta;

        readonly ILogger _logger;

        public HtmlPreviewService(ILogger logger, IAppMeta appMeta)
        {
            this._logger = logger;
            this._appMeta = appMeta;
        }

        public string CreateFile(MimeMessage mailMessageEx)
        {
            if (mailMessageEx == null) throw new ArgumentNullException(nameof(mailMessageEx));

            var tempDir = this.CreateUniqueTempDirectory();
            var visitor = new HtmlPreviewVisitor(tempDir);
            mailMessageEx.Accept(visitor);

            string htmlFile = Path.Combine(tempDir, "index.html");

            this._logger.Verbose("Writing HTML Preview file {HtmlFile}", htmlFile);

            File.WriteAllText(htmlFile, visitor.HtmlBody, Encoding.Unicode);

            return htmlFile;
        }

        string CreateUniqueTempDirectory()
        {
            string tempDir;
            do
            {
                // find unique temp directory
                tempDir = Path.Combine(Path.GetTempPath(), $"{this._appMeta.AppName}-{Guid.NewGuid()}");
            }
            while (Directory.Exists(tempDir));

            Directory.CreateDirectory(tempDir);

            return tempDir;
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

            builder.RegisterType<HtmlPreviewService>().AsImplementedInterfaces()
                .InstancePerLifetimeScope();
        }

        #endregion
    }
}