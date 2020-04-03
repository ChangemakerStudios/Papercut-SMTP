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


namespace Papercut.Infrastructure.Smtp
{
    using System;
    using System.Net;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using Core.Domain.Application;
    using Core.Domain.Network;

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

        private EndpointDefinition _currentEndpoint;

        private Task _smtpServerTask;

        private CancellationTokenSource _tokenSource;

        public PapercutSmtpServer(
            IAppMeta applicationMetaData,
            ILogger logger,
            MessageStore messageStore,
            global::SmtpServer.ILogger bridgeLogger)
        {
            _applicationMetaData = applicationMetaData;
            _logger = logger;
            _messageStore = messageStore;
            _bridgeLogger = bridgeLogger;
        }

        public bool IsActive => _smtpServerTask != null;

        public IPAddress ListenIpAddress => _currentEndpoint?.Address;

        public int ListenPort => _currentEndpoint?.Port ?? 0;

        public void Dispose()
        {
            Stop();
        }

        public void Stop()
        {
            if (_smtpServerTask == null) return;

            try
            {
                _tokenSource?.Cancel();
                _smtpServerTask.Wait();
                _tokenSource?.Dispose();
            }
            catch (Exception ex) when (ex is AggregateException || ex is TaskCanceledException || ex is OperationCanceledException)
            {
            }
            finally
            {
                _smtpServerTask = null;
                _tokenSource = null;
            }
        }

        public void Start(EndpointDefinition smtpEndpoint)
        {
            ServicePointManager.ServerCertificateValidationCallback = IgnoreCertificateValidationFailureForTestingOnly;

            _currentEndpoint = smtpEndpoint;

            var options = new SmtpServerOptionsBuilder()
                .ServerName(_applicationMetaData.AppName)
                .UserAuthenticator(new SimpleAuthentication())
                .MailboxFilter(new DelegatingMailboxFilter(CanAcceptMailbox))
                .Logger(_bridgeLogger)
                .MessageStore(_messageStore);

            options = options.Endpoint(new EndpointDefinitionBuilder()
                .Endpoint(smtpEndpoint.ToIPEndPoint())
                .IsSecure(false)
                .AllowUnsecureAuthentication(false)
                .Build());

            var server = new SmtpServer(options.Build());

            server.SessionCreated += OnSessionCreated;
            server.SessionCompleted += OnSessionCompleted;

            _logger.Information("Starting Smtp Server on {IP}:{Port}...", ListenIpAddress, ListenPort);

            _tokenSource = new CancellationTokenSource();
            _smtpServerTask = server.StartAsync(_tokenSource.Token);
        }

        private MailboxFilterResult CanAcceptMailbox(ISessionContext sessionContext, IMailbox mailbox)
        {
            return MailboxFilterResult.Yes;
        }

        private void OnSessionCompleted(object sender, SessionEventArgs e)
        {
            _logger.Information("Completed SMTP connection from {EndpointAddress}", e.Context.EndpointDefinition.Endpoint.Address.ToString());
        }

        private void OnSessionCreated(object sender, SessionEventArgs e)
        {
            e.Context.CommandExecuting += (o, args) =>
            {
                _logger.Verbose("SMTP Command {@SmtpCommand}", args.Command);
            };

            _logger.Information("New SMTP connection from {EndpointAddress}", e.Context.EndpointDefinition.Endpoint.Address.ToString());
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