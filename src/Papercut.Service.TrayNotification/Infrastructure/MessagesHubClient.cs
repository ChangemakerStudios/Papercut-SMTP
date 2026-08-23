// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2026 Jaben Cargman
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


using Autofac;

using Microsoft.AspNetCore.SignalR.Client;

namespace Papercut.Service.TrayNotification.Infrastructure;

/// <summary>
/// Subscribes to the service's messages hub and surfaces new-message events to
/// the tray.
///
/// Replaces the previous IPComm listener. The service already broadcasts a fully
/// populated message over SignalR to drive the web UI, so the tray now receives
/// the real subject and sender instead of parsing them back out of the .eml
/// filename.
/// </summary>
public class MessagesHubClient : IStartable, IAsyncDisposable
{
    /// <summary>
    /// Matches the web UI's retry schedule (signalr.service.ts). After these are
    /// exhausted the connection is closed and we restart it ourselves, so a
    /// service that is down for a long stretch is still picked up when it returns.
    /// </summary>
    private static readonly TimeSpan[] ReconnectDelays =
    [
        TimeSpan.Zero,
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30)
    ];

    private static readonly TimeSpan RetryAfterClosed = TimeSpan.FromSeconds(30);

    private readonly ServiceEndpointProvider _endpointProvider;

    private readonly ILogger _logger;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private HubConnection? _connection;

    public MessagesHubClient(ServiceEndpointProvider endpointProvider, ILogger logger)
    {
        _endpointProvider = endpointProvider;
        _logger = logger;
    }

    public event EventHandler<NewMessageDto>? NewMessageReceived;

    public void Start()
    {
        _ = Task.Run(() => RunAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
    }

    public async ValueTask DisposeAsync()
    {
        await _cancellationTokenSource.CancelAsync();

        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        _cancellationTokenSource.Dispose();

        GC.SuppressFinalize(this);
    }

    private async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await ConnectAsync(token);

                // ConnectAsync returns once the connection is established; wait here
                // until it closes for good (automatic reconnect exhausted).
                await WaitForCloseAsync(token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Messages hub connection attempt failed, will retry");
            }

            if (token.IsCancellationRequested) return;

            // the service may simply not be running yet -- back off and try again
            try
            {
                await Task.Delay(RetryAfterClosed, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task ConnectAsync(CancellationToken token)
    {
        // re-read configuration each attempt so a service reconfigured between
        // attempts is picked up without restarting the tray
        _endpointProvider.InvalidateCache();

        var hubUrl = _endpointProvider.MessagesHubUrl;

        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect(ReconnectDelays)
            .Build();

        connection.On<NewMessageDto>(
            "NewMessageReceived",
            message =>
            {
                _logger.Debug("Hub reported new message {Subject}", message.Subject);
                NewMessageReceived?.Invoke(this, message);
            });

        connection.Reconnected += async _ =>
        {
            _logger.Information("Reconnected to the Papercut messages hub");
            await JoinMessagesGroupAsync(connection, CancellationToken.None);
        };

        _connection = connection;

        _logger.Debug("Connecting to the Papercut messages hub at {HubUrl}", hubUrl);

        await connection.StartAsync(token);
        await JoinMessagesGroupAsync(connection, token);

        _logger.Information("Connected to the Papercut messages hub at {HubUrl}", hubUrl);
    }

    private async Task JoinMessagesGroupAsync(HubConnection connection, CancellationToken token)
    {
        try
        {
            await connection.InvokeAsync("JoinMessagesGroup", token);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to join the Messages hub group -- notifications may not arrive");
        }
    }

    /// <summary>
    /// Completes when the connection closes (after automatic reconnect gives up)
    /// or the tray is shutting down.
    /// </summary>
    private async Task WaitForCloseAsync(CancellationToken token)
    {
        var connection = _connection;

        if (connection == null) return;

        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task OnClosed(Exception? error)
        {
            if (error != null)
                _logger.Warning(error, "Messages hub connection closed");
            else
                _logger.Debug("Messages hub connection closed");

            closed.TrySetResult();

            return Task.CompletedTask;
        }

        connection.Closed += OnClosed;

        try
        {
            await using (token.Register(() => closed.TrySetResult()))
            {
                await closed.Task;
            }
        }
        finally
        {
            connection.Closed -= OnClosed;
        }
    }

    #region Begin Static Container Registrations

    /// <summary>
    /// Called dynamically from the RegisterStaticMethods() call in the container module.
    /// </summary>
    /// <param name="builder"></param>
    [UsedImplicitly]
    private static void Register(ContainerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RegisterType<MessagesHubClient>().AsSelf().AsImplementedInterfaces().SingleInstance();
    }

    #endregion
}
