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


using System.Threading.Channels;

using Autofac;
using Autofac.Util;

using Papercut.Core.Domain.BackgroundTasks;
using Papercut.Core.Infrastructure.Logging;

namespace Papercut.Core.Infrastructure.BackgroundTasks;

public sealed class BackgroundTaskRunner : Disposable, IBackgroundTaskRunner
{
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ILogger _logger;
    private readonly Task _processingTask;
    private readonly Channel<Func<CancellationToken, Task>> _taskChannel;

    public BackgroundTaskRunner(ILogger logger)
    {
        _logger = logger;
        _taskChannel = Channel.CreateUnbounded<Func<CancellationToken, Task>>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

        _cancellationTokenSource = new CancellationTokenSource();
        _processingTask = Task.Run(() => ProcessTasksAsync(_cancellationTokenSource.Token));
    }

    /// <summary>
    /// Queues a background task to be executed.
    /// </summary>
    /// <param name="taskFunc">An asynchronous function representing the task.</param>
    public void QueueBackgroundTask(Func<CancellationToken, Task> taskFunc)
    {
        if (taskFunc == null) throw new ArgumentNullException(nameof(taskFunc));

        if (!_taskChannel.Writer.TryWrite(taskFunc))
        {
            throw new InvalidOperationException("Unable to queue the background task.");
        }
    }

    private async Task ProcessTasksAsync(CancellationToken cancellationToken)
    {
        await foreach (var taskFunc in _taskChannel.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await taskFunc(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // ignore
            }
            catch (Exception ex) when (_logger.ErrorWithContext(ex, "Failure running background task"))
            {
            }
        }
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            _taskChannel.Writer.Complete();
            await _cancellationTokenSource.CancelAsync();

            try
            {
                await _processingTask.WaitAsync(TimeSpan.FromSeconds(15));
            }
            catch (TimeoutException)
            {
                // Ignore timeout if background task doesn't complete within the grace period.
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation exceptions that occur during shutdown.
            }
            catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is TaskCanceledException or OperationCanceledException))
            {
                // Ignore TaskCanceledExceptions and OperationCanceledExceptions that occur during shutdown.
            }
            finally
            {
                _cancellationTokenSource.Dispose();
            }
        }

        await base.DisposeAsync(disposing);
    }
}
