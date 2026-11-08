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


using System.IO.Pipes;

namespace Papercut.Desktop;

/// <summary>
/// One shell per user: a second one would crash on WebView2's locked data folder (0x800700AA)
/// and fight over the SMTP port. A second launch asks the first to show its window, then exits.
/// </summary>
internal sealed class SingleInstance : IDisposable
{
    private static readonly string Name = $"PapercutSMTP.Shell.{Environment.UserName}";

    private readonly Mutex _mutex;

    private readonly CancellationTokenSource _stop = new();

    private SingleInstance(Mutex mutex)
    {
        _mutex = mutex;
    }

    /// <summary>
    /// Null when another instance owns the lock; that instance has been asked to show itself.
    /// </summary>
    public static SingleInstance? TryAcquire()
    {
        var mutex = new Mutex(true, Name, out var createdNew);

        if (createdNew) return new SingleInstance(mutex);

        mutex.Dispose();
        SignalExisting();

        return null;
    }

    /// <summary>
    /// Calls <paramref name="onActivate" /> (on a pool thread) whenever a second launch signals.
    /// </summary>
    public void Listen(Action onActivate)
    {
        _ = Task.Run(
            async () =>
            {
                while (!_stop.IsCancellationRequested)
                {
                    try
                    {
                        await using var server = new NamedPipeServerStream(Name, PipeDirection.In, 1,
                            PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

                        await server.WaitForConnectionAsync(_stop.Token);

                        onActivate();
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex, "Single-instance pipe failed; listening again");
                        await Task.Delay(500);
                    }
                }
            });
    }

    public void Dispose()
    {
        _stop.Cancel();
        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }

    private static void SignalExisting()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", Name, PipeDirection.Out, PipeOptions.CurrentUserOnly);
            client.Connect(2000);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Papercut is already running but did not answer");
        }
    }
}
