// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2019 Jaben Cargman
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
namespace Papercut.Infrastructure.Smtp
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using Papercut.Common.Helper;
    using Papercut.Core.Domain.Application;
    using Papercut.Core.Domain.Network;
    using Papercut.Core.Domain.Settings;

    using SmtpServer;
    using SmtpServer.Mail;
    using SmtpServer.Storage;

    using ILogger = Serilog.ILogger;

    public class PapercutSmtpServer : IServer
    {
        readonly IAppMeta _applicationMetaData;

        private readonly global::SmtpServer.ILogger _bridgeLogger;

        readonly ILogger _logger;

        private readonly MessageStore _messageStore;

        readonly SmtpServerSettings _smtpServerSettings;

        private Task _smtpServerTask;

        private CancellationTokenSource _tokenSource;

        public PapercutSmtpServer(
            SmtpServerSettings smtpServerSettings,
            IAppMeta applicationMetaData,
            ILogger logger,
            MessageStore messageStore,
            global::SmtpServer.ILogger bridgeLogger)
        {
            this._smtpServerSettings = smtpServerSettings;
            this._applicationMetaData = applicationMetaData;
            this._logger = logger;
            this._messageStore = messageStore;
            this._bridgeLogger = bridgeLogger;
        }

        public bool IsActive => this._smtpServerTask != null;

        private string ListenIpAddress
        {
            get
            {
                if (this._smtpServerSettings.IP.IsNullOrWhiteSpace() || this._smtpServerSettings.IP.CaseInsensitiveEquals("Any"))
                {
                    return "0.0.0.0";
                }

                return this._smtpServerSettings.IP;
            }
        }

        public void Dispose()
        {
            this.Stop().Wait();
        }

        public async Task Stop()
        {
            if (this._smtpServerTask == null) return;

            try
            {
                this._tokenSource?.Cancel();
                await this._smtpServerTask;
                this._tokenSource?.Dispose();
            }
            catch (Exception ex) when (ex is AggregateException || ex is TaskCanceledException || ex is OperationCanceledException)
            {
            }
            finally
            {
                this._smtpServerTask = null;
                this._tokenSource = null;
            }
        }

        IEnumerable<IEndpointDefinition> GetEndpoints()
        {
            yield return new EndpointDefinitionBuilder()
                .Endpoint(
                    new IPEndPoint(
                        IPAddress.Parse(this.ListenIpAddress),
                        this._smtpServerSettings.Port))
                .IsSecure(false)
                .AllowUnsecureAuthentication(false)
                .Build();
        }

        public Task Start()
        {
            ServicePointManager.ServerCertificateValidationCallback = this.IgnoreCertificateValidationFailureForTestingOnly;

            var options = new SmtpServerOptionsBuilder()
                .ServerName(this._applicationMetaData.AppName)
                .UserAuthenticator(new SimpleAuthentication())
                .MailboxFilter(new DelegatingMailboxFilter(this.CanAcceptMailbox))
                .Logger(this._bridgeLogger)
                .MessageStore(this._messageStore);

            foreach (var endpoint in this.GetEndpoints())
            {
                options = options.Endpoint(endpoint);
            }

            var server = new SmtpServer(options.Build());

            server.SessionCreated += this.OnSessionCreated;
            server.SessionCompleted += this.OnSessionCompleted;

            this._logger.Information("Starting Smtp Server on {IP}:{Port}...", this.ListenIpAddress, this._smtpServerSettings.Port);

            this._tokenSource = new CancellationTokenSource();
            this._smtpServerTask = server.StartAsync(this._tokenSource.Token);

            return Task.CompletedTask;
        }

        private MailboxFilterResult CanAcceptMailbox(ISessionContext sessionContext, IMailbox mailbox)
        {
            return MailboxFilterResult.Yes;
        }

        private void OnSessionCompleted(object sender, SessionEventArgs e)
        {
            this._logger.Information("Completed SMTP connection from {EndpointDefinition}", e.Context.EndpointDefinition);
        }

        private void OnSessionCreated(object sender, SessionEventArgs e)
        {
            e.Context.CommandExecuting += (o, args) =>
            {
                this._logger.Verbose("SMTP Command {@SmtpCommand}", args.Command);
            };

            this._logger.Information("New SMTP connection from {EndpointDefinition}", e.Context.EndpointDefinition);
        }

        private bool IgnoreCertificateValidationFailureForTestingOnly(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}