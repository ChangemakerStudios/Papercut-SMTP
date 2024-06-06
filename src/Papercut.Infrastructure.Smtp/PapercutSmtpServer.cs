﻿// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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


using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

using Autofac.Util;

using Papercut.Core.Domain.Application;
using Papercut.Core.Domain.Network;

using SmtpServer;

namespace Papercut.Infrastructure.Smtp
{
    using ILogger = Serilog.ILogger;

    public class PapercutSmtpServer : Disposable, IServer
    {
        readonly IAppMeta _applicationMetaData;

        readonly ILogger _logger;

        private readonly Func<ISmtpServerOptions, SmtpServer.SmtpServer> _smtpServerFactory;

        private EndpointDefinition _currentEndpoint;

        private SmtpServer.SmtpServer? _server;

        private CancellationTokenSource? _tokenSource;

        public PapercutSmtpServer(
            IAppMeta applicationMetaData,
            ILogger logger,
            Func<ISmtpServerOptions, SmtpServer.SmtpServer> smtpServerFactory)
        {
            this._applicationMetaData = applicationMetaData;
            this._logger = logger;
            this._smtpServerFactory = smtpServerFactory;
        }

        public bool IsActive => this._server != null;

        public IPAddress? ListenIpAddress => this._currentEndpoint?.Address;

        public int ListenPort => this._currentEndpoint?.Port ?? 0;

        public async Task StopAsync()
        {
            try
            {
                if (this._tokenSource != null)
                    await this._tokenSource.CancelAsync();

                if (this._server != null)
                {
                    this._logger.Information("Stopping Smtp Server");

                    await this._server.ShutdownTask;
                }

                this._tokenSource?.Dispose();
            }
            catch (Exception ex) when (ex is AggregateException or TaskCanceledException or OperationCanceledException)
            {
            }
            finally
            {
                this._tokenSource = null;
                this._server = null;
            }
        }

        public Task StartAsync(EndpointDefinition smtpEndpoint)
        {
            if (this.IsActive)
            {
                return Task.CompletedTask;
            }

            ServicePointManager.ServerCertificateValidationCallback = this.IgnoreCertificateValidationFailureForTestingOnly;

            this._currentEndpoint = smtpEndpoint;

            var options = new SmtpServerOptionsBuilder()
                .ServerName(this._applicationMetaData.AppName)
                .Endpoint(
                    new EndpointDefinitionBuilder()
                        .Endpoint(smtpEndpoint.ToIPEndPoint())
                        .IsSecure(false)
                        .AllowUnsecureAuthentication(false)
                        .Build());

            this._server = this._smtpServerFactory(options.Build());

            this._server.SessionCreated += this.OnSessionCreated;
            this._server.SessionCompleted += this.OnSessionCompleted;

            this._logger.Information("Starting Smtp Server on {IP}:{Port}...", this.ListenIpAddress, this.ListenPort);
            this._tokenSource = new CancellationTokenSource();

#pragma warning disable 4014
            // server will block -- just let it run
            Task.Factory.StartNew(
                async () =>
                {
                    try
                    {
                        await this._server.StartAsync(this._tokenSource.Token);
                    }
                    catch (Exception ex)
                    {
                        this._logger.Error(ex, "Smtp Server Error");
                    }
                },
                this._tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
#pragma warning restore 4014

            return Task.CompletedTask;
        }

        protected override async ValueTask DisposeAsync(bool disposing)
        {
            if (disposing)
            {
                await this.StopAsync();
            }
        }

        private void OnSessionCompleted(object? sender, SessionEventArgs e)
        {
            this._logger.Information("Completed SMTP connection from {EndpointAddress}", e.Context.EndpointDefinition.Endpoint.Address.ToString());
        }

        private void OnSessionCreated(object? sender, SessionEventArgs e)
        {
            e.Context.CommandExecuting += (o, args) =>
            {
                this._logger.Verbose("SMTP Command {@SmtpCommand}", args.Command);
            };

            this._logger.Information("New SMTP connection from {EndpointAddress}", e.Context.EndpointDefinition.Endpoint.Address.ToString());
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