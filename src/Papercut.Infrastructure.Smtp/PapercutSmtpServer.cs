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
using System.Net.Sockets;
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
    Func<ISmtpServerOptions, SmtpServer.SmtpServer> smtpServerFactory)
    : Disposable, IServer
{
    private static readonly TimeSpan BindRetryDelay = TimeSpan.FromSeconds(2);

    private EndpointDefinition _currentEndpoint = null!;

    private Task _listening = Task.CompletedTask;

    private SmtpServer.SmtpServer? _server;

    private CancellationTokenSource? _tokenSource;

    public bool IsActive => _server != null;

    public IPAddress? ListenIpAddress => _currentEndpoint?.Address;

    public int ListenPort => _currentEndpoint?.Port ?? 0;

    public async Task StopAsync(CancellationToken token = default)
    {
        try
        {
            if (_tokenSource != null)
                await _tokenSource.CancelAsync();

            if (_server != null)
            {
                logger.Information("Stopping Smtp Server");

                // ShutdownTask never completes on a server that has not bound yet
                await Task.WhenAny(_server.ShutdownTask, _listening);
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

    public Task StartAsync(EndpointDefinition smtpEndpoint, CancellationToken token = default)
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

        var server = smtpServerFactory(options.Build());

        server.SessionCreated += OnSessionCreated;
        server.SessionCompleted += OnSessionCompleted;

        _server = server;

        logger.Information("Starting Smtp Server on {IP}:{Port}...", ListenIpAddress, ListenPort);
        _tokenSource = new CancellationTokenSource();

        // server will block -- just let it run
        var stopToken = _tokenSource.Token;

        _listening = Task.Factory.StartNew(
            () => ListenAsync(server, stopToken),
            stopToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default).Unwrap();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Keeps trying while the port is in use: another Papercut may be about to release it.
    /// </summary>
    private async Task ListenAsync(SmtpServer.SmtpServer server, CancellationToken stopToken)
    {
        var warned = false;

        while (!stopToken.IsCancellationRequested)
        {
            try
            {
                await server.StartAsync(stopToken);
                return;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                if (!warned)
                    logger.Warning("SMTP port {Port} is in use -- retrying until it is free", ListenPort);

                warned = true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Smtp Server Error");
                return;
            }

            try
            {
                await Task.Delay(BindRetryDelay, stopToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
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
        logger.Information("Completed SMTP connection from {RemoteIp}", e.Context.GetRemoteIpAddress());
    }

    private void OnSessionCreated(object? sender, SessionEventArgs e)
    {
        e.Context.CommandExecuting += (_, args) =>
        {
            logger.Verbose("SMTP Command {@SmtpCommand}", args.Command);
        };

        var remoteIp = e.Context.GetRemoteIpAddress();

        logger.Information("New SMTP connection from {RemoteIp}", remoteIp);

        // IP validation is now handled by IpAllowlistMailboxFilter via IMailboxFilter interface
        // This provides proper rejection semantics and prevents socket resource leaks
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