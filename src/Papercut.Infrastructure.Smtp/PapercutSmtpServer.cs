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


using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

using Autofac.Util;

using Papercut.Core.Domain.Application;
using Papercut.Core.Domain.Network;

using SmtpServer;

namespace Papercut.Infrastructure.Smtp;

using ILogger = Serilog.ILogger;

public class PapercutSmtpServer(
    IAppMeta applicationMetaData,
    ILogger logger,
    Func<ISmtpServerOptions, SmtpServer.SmtpServer> smtpServerFactory,
    IPAllowedList ipAllowedList)
    : Disposable, IServer
{
    private EndpointDefinition _currentEndpoint = null!;

    private SmtpServer.SmtpServer? _server;

    private CancellationTokenSource? _tokenSource;

    public bool IsActive => _server != null;

    public IPAddress? ListenIpAddress => _currentEndpoint?.Address;

    public int ListenPort => _currentEndpoint?.Port ?? 0;

    public async Task StopAsync()
    {
        try
        {
            if (_tokenSource != null)
                await _tokenSource.CancelAsync();

            if (_server != null)
            {
                logger.Information("Stopping Smtp Server");

                await _server.ShutdownTask;
            }

            _tokenSource?.Dispose();
        }
        catch (Exception ex) when (ex is AggregateException or TaskCanceledException or OperationCanceledException)
        {
        }
        finally
        {
            _tokenSource = null;
            _server = null;
        }
    }

    public Task StartAsync(EndpointDefinition smtpEndpoint)
    {
        if (IsActive)
        {
            return Task.CompletedTask;
        }

        ServicePointManager.ServerCertificateValidationCallback = IgnoreCertificateValidationFailureForTestingOnly;

        _currentEndpoint = smtpEndpoint;

        var options = new SmtpServerOptionsBuilder()
            .ServerName(applicationMetaData.AppName)
            .Endpoint(
                new EndpointDefinitionBuilder()
                    .WithEndpoint(smtpEndpoint)
                    .IsSecure(false)
                    .AllowUnsecureAuthentication(false)
                    .Build());

        _server = smtpServerFactory(options.Build());

        _server.SessionCreated += OnSessionCreated;
        _server.SessionCompleted += OnSessionCompleted;

        logger.Information("Starting Smtp Server on {IP}:{Port}...", ListenIpAddress, ListenPort);
        _tokenSource = new CancellationTokenSource();

#pragma warning disable 4014
        // server will block -- just let it run
        Task.Factory.StartNew(
            async () =>
            {
                try
                {
                    await _server.StartAsync(_tokenSource.Token);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Smtp Server Error");
                }
            },
            _tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
#pragma warning restore 4014

        return Task.CompletedTask;
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            await StopAsync();
        }
    }

    private void OnSessionCompleted(object? sender, SessionEventArgs e)
    {
        logger.Information("Completed SMTP connection from {RemoteIp}", GetRemoteEndPoint(e.Context));
    }

    private void OnSessionCreated(object? sender, SessionEventArgs e)
    {
        e.Context.CommandExecuting += (_, args) =>
        {
            logger.Verbose("SMTP Command {@SmtpCommand}", args.Command);
        };

        var remoteIp = GetRemoteEndPoint(e.Context);

        logger.Information("New SMTP connection from {RemoteIp}", remoteIp);

        // Validate IP address against allowlist
        if (remoteIp.ToString() != IPAddress.None.ToString() && !ipAllowedList.IsAllowed(remoteIp))
        {
            logger.Warning(
                "Rejected SMTP connection from {RemoteIp} - IP not in allowlist",
                remoteIp);

            // Abort the session by throwing an exception
            throw new InvalidOperationException($"Connection from {remoteIp} is not allowed");
        }
    }

    private IPAddress GetRemoteEndPoint(ISessionContext context)
    {
        const string RemoteEndPointKey = "EndpointListener:RemoteEndPoint";

        if (context.Properties.TryGetValue(RemoteEndPointKey, out var endpointObj)
            && endpointObj is IPEndPoint remoteEndPoint)
        {
            return remoteEndPoint.Address;
        }

        return IPAddress.None;
    }

    private bool IgnoreCertificateValidationFailureForTestingOnly(
        object sender,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors sslPolicyErrors)
    {
        return true;
    }
}