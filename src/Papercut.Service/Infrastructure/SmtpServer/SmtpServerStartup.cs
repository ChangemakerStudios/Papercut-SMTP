// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2018 Jaben Cargman
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
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Papercut.Common.Helper;
using Papercut.Core.Domain.Application;
using Papercut.Core.Infrastructure.Lifecycle;
using Papercut.Service.Helpers;

using Serilog;

using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Storage;

namespace Papercut.Service.Infrastructure.SmtpServer
{
    public class SmtpServerStartup : IStartupService
    {
        private readonly IAppMeta _applicationMetaData;

        private readonly ILogger _bridgeLogger;

        private readonly Serilog.ILogger _logger;

        private readonly MessageStore _messageStore;

        private readonly IServiceProvider _serviceProvider;

        private readonly PapercutServiceSettings _serviceSettings;

        public SmtpServerStartup(
            IServiceProvider serviceProvider,
            PapercutServiceSettings serviceSettings,
            IAppMeta applicationMetaData,
            Serilog.ILogger logger,
            MessageStore messageStore,
            ILogger bridgeLogger)
        {
            this._serviceProvider = serviceProvider;
            this._serviceSettings = serviceSettings;
            this._applicationMetaData = applicationMetaData;
            this._logger = logger;
            this._messageStore = messageStore;
            this._bridgeLogger = bridgeLogger;
        }

        private string ListenIpAddress
        {
            get
            {
                if (this._serviceSettings.IP.IsNullOrWhiteSpace()) return "0.0.0.0";
                    //|| this._serviceSettings.IP.CaseInsensitiveEquals("Any")) return "0.0.0.0";

                return this._serviceSettings.IP;
            }
        }

        public async Task Start(CancellationToken token)
        {
            ServicePointManager.ServerCertificateValidationCallback =
                this.IgnoreCertificateValidationFailureForTestingOnly;

            var options = new SmtpServerOptionsBuilder()
                .ServerName(this._applicationMetaData.AppName);
                //.MailboxFilter(new DelegatingMailboxFilter(this.CanAcceptMailbox))
                //.UserAuthenticator(new SimpleAuthentication())
                //.Logger(this._bridgeLogger)
                //.MessageStore(this._messageStore);

            foreach (var endpoint in this.GetEndpoints()) options = options.Endpoint(endpoint);

            var server = new global::SmtpServer.SmtpServer(options.Build(), this._serviceProvider);

            server.SessionCreated += this.OnSessionCreated;
            server.SessionCompleted += this.OnSessionCompleted;

            this._logger.Information(
                "Starting Smtp Server on {IP}:{Port}...",
                this.ListenIpAddress,
                this._serviceSettings.Port);

            await server.StartAsync(token);
        }

        private IEnumerable<IEndpointDefinition> GetEndpoints()
        {
            yield return new EndpointDefinitionBuilder()
                .Endpoint(
                    new IPEndPoint(
                        IPAddress.Parse(this.ListenIpAddress),
                        this._serviceSettings.Port))
                .IsSecure(false).Build();
        }

        //private MailboxFilterResult CanAcceptMailbox(
        //    ISessionContext sessionContext,
        //    IMailbox mailbox)
        //{
        //    return MailboxFilterResult.Yes;
        //}

        private void OnSessionCompleted(object sender, SessionEventArgs e)
        {
            this._logger.Information(
                "Completed SMTP connection from {EndpointDefinition}",
                e.Context.EndpointDefinition);
        }

        private void OnSessionCreated(object sender, SessionEventArgs e)
        {
            e.Context.CommandExecuting += (o, args) =>
            {
                this._logger.Verbose("SMTP Command {@SmtpCommand}", args.Command);
            };

            this._logger.Information(
                "New SMTP connection from {EndpointDefinition}",
                e.Context.EndpointDefinition);
        }

        private bool IgnoreCertificateValidationFailureForTestingOnly(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslpolicyerrors)
        {
            return true;
        }
    }
}