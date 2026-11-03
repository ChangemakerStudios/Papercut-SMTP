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


using Microsoft.AspNetCore.SignalR.Client;

namespace Papercut.Desktop.Services;

/// <summary>
/// New-mail events from the service's /hubs/messages, for notifications. Ported from the
/// WinForms tray (Papercut.Service.TrayNotification), but pointed at whichever source the
/// supervisor picked, and re-pointed when that changes.
/// </summary>
internal sealed class MessagesHubClient : IAsyncDisposable
{
    // matches the web UI's retry schedule (signalr.service.ts)
    private static readonly TimeSpan[] ReconnectDelays =
    [
        TimeSpan.Zero,
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30)
    ];

    private static readonly TimeSpan RetryAfterClosed = TimeSpan.FromSeconds(10);

    private readonly SemaphoreSlim _gate = new(1, 1);

    private CancellationTokenSource? _loop;

    private Task? _loopTask;

    private HubConnection? _connection;

    private Uri? _baseUri;

    public event Action<NewMessageDto>? NewMessageReceived;

    public async Task ConnectAsync(Uri? baseUri)
    {
        await _gate.WaitAsync();

        try
        {
            if (baseUri == _baseUri && _loopTask is { IsCompleted: false }) return;

            await StopLoopAsync();

            _baseUri = baseUri;

            if (baseUri == null) return;

            _loop = new CancellationTokenSource();
            var token = _loop.Token;
            _loopTask = Task.Run(() => RunAsync(new Uri(baseUri, "hubs/messages"), token), token);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await ConnectAsync(null);
    }

    private async Task StopLoopAsync()
    {
        if (_loop == null) return;

        await _loop.CancelAsync();

        try
        {
            if (_loopTask != null) await _loopTask.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex) when (ex is OperationCanceledException or TimeoutException)
        {
        }

        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        _loop.Dispose();
        _loop = null;
        _loopTask = null;
    }

    private async Task RunAsync(Uri hubUrl, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (_connection != null)
                {
                    await _connection.DisposeAsync();
                    _connection = null;
                }

                var connection = new HubConnectionBuilder()
                    .WithUrl(hubUrl)
                    .WithAutomaticReconnect(ReconnectDelays)
                    .Build();

                connection.On<NewMessageDto>("NewMessageReceived", message => NewMessageReceived?.Invoke(message));

                connection.Reconnected += async _ =>
                {
                    Log.Information("Reconnected to the messages hub");
                    await JoinAsync(connection, CancellationToken.None);
                };

                var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                connection.Closed += _ =>
                {
                    closed.TrySetResult();
                    return Task.CompletedTask;
                };

                _connection = connection;

                await connection.StartAsync(token);
                await JoinAsync(connection, token);

                Log.Information("Connected to the messages hub at {HubUrl}", hubUrl);

                await using (token.Register(() => closed.TrySetResult()))
                {
                    await closed.Task;
                }

                if (!token.IsCancellationRequested) Log.Information("Messages hub connection closed");
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Log.Information("Could not reach the messages hub at {HubUrl} ({Reason})", hubUrl, ex.Message);
            }

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

    private static async Task JoinAsync(HubConnection connection, CancellationToken token)
    {
        try
        {
            await connection.InvokeAsync("JoinMessagesGroup", token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Warning(ex, "Failed to join the Messages hub group -- notifications may not arrive");
        }
    }
}

/// <summary>
/// The part of the service's RefDto that notifications need.
/// </summary>
internal sealed class NewMessageDto
{
    public string? Id { get; set; }

    public string? Subject { get; set; }

    public List<EmailAddressDto> From { get; set; } = [];

    public string FromDisplay
    {
        get
        {
            var first = From.FirstOrDefault();

            if (first == null) return "New message";

            return !string.IsNullOrWhiteSpace(first.Name) ? first.Name : first.Address ?? "New message";
        }
    }
}

internal sealed class EmailAddressDto
{
    public string? Name { get; set; }

    public string? Address { get; set; }
}
