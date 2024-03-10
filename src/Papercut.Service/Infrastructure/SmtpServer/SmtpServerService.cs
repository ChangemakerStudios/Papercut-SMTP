// Papercut
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


using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using Papercut.Common.Helper;
using Papercut.Core.Domain.Application;
using Papercut.Service.Domain.SmtpServer;

using Serilog;

using SmtpServer;

namespace Papercut.Service.Infrastructure.SmtpServer;

public class SmtpServerService : IHostedService
{
    private readonly IAppMeta _applicationMetaData;

    private readonly ILogger _logger;

    private readonly IServiceProvider _serviceProvider;

    private readonly SmtpServerOptions _smtpServerOptions;

    private global::SmtpServer.SmtpServer _server;

    public SmtpServerService(
        IServiceProvider serviceProvider,
        SmtpServerOptions smtpServerOptions,
        IAppMeta applicationMetaData,
        ILogger logger)
    {
        this._serviceProvider = serviceProvider;
        this._smtpServerOptions = smtpServerOptions;
        this._applicationMetaData = applicationMetaData;
        this._logger = logger.ForContext<SmtpServerService>();
    }

    private string ListenIpAddress
    {
        get
        {
            if (this._smtpServerOptions.IP.IsNullOrWhiteSpace()
                || this._smtpServerOptions.IP.Trim().Equals("Any", StringComparison.OrdinalIgnoreCase))
            {
                return "0.0.0.0";
            }

            return this._smtpServerOptions.IP;
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
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

        this._server = new global::SmtpServer.SmtpServer(options.Build(), this._serviceProvider);

        this._server.SessionCreated += this.OnSessionCreated;
        this._server.SessionCompleted += this.OnSessionCompleted;

        this._logger.Information(
            "Starting Smtp Server on {IP}:{Port}...",
            this.ListenIpAddress,
            this._smtpServerOptions.Port);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        Task.Run(async () =>
        {
            try
            {
                await this._server.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                this._logger.Error(ex, "Failure Starting SMTP Server");
            }
        }, cancellationToken);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this._server.Shutdown();

        return Task.CompletedTask;
    }

    private IEnumerable<IEndpointDefinition> GetEndpoints()
    {
        yield return new EndpointDefinitionBuilder()
            .Endpoint(
                new IPEndPoint(
                    IPAddress.Parse(this.ListenIpAddress),
                    this._smtpServerOptions.Port))
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
        this._logger.Verbose(
            "Completed SMTP connection from {Endpoint}",
            e.Context.EndpointDefinition.Endpoint);
    }

    private void OnSessionCreated(object sender, SessionEventArgs e)
    {
        e.Context.CommandExecuting += (o, args) =>
        {
            this._logger.Verbose("SMTP Command {@SmtpCommand}", args.Command);
        };

        this._logger.Verbose(
            "New SMTP connection from {Endpoint}",
            e.Context.EndpointDefinition.Endpoint);
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