// Papercut
// 
// Copyright � 2008 - 2012 Ken Robertson
// Copyright � 2013 - 2025 Jaben Cargman
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


using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

using Papercut.Common.Extensions;
using Papercut.Core.Domain.Message;
using Papercut.Core.Domain.Paths;

namespace Papercut.Message;

public class MessageWatcher : IDisposable
{
    readonly ILogger _logger;

    readonly MessagePathConfigurator _messagePathConfigurator;

    readonly object _watchersLock = new();

    List<FileSystemWatcher> _watchers = [];

    public MessageWatcher(ILogger logger, MessagePathConfigurator messagePathConfigurator)
    {
        this._logger = logger;
        this._messagePathConfigurator = messagePathConfigurator;
        this._messagePathConfigurator.RefreshLoadPath += this.OnRefreshLoadPaths;
        this.SetupMessageWatchers();
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    public IObservable<EventPattern<NewMessageEventArgs>> GetNewMessageObservable(IScheduler? scheduler = null)
    {
        return Observable.FromEventPattern<NewMessageEventArgs>(
            e => this.NewMessage += e,
            e => this.NewMessage -= e,
            scheduler?? Scheduler.Default);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;

        lock (this._watchersLock)
        {
            foreach (var watch in this._watchers)
            {
                DisposeWatch(watch);
            }

            this._watchers.Clear();
        }
    }

    void OnRefreshLoadPaths(object? sender, EventArgs eventArgs)
    {
        lock (this._watchersLock)
        {
            List<string> existingPaths = this._watchers.Select(s => s.Path).ToList();
            List<string> removePaths = existingPaths.Except(this._messagePathConfigurator.LoadPaths).ToList();
            List<string> addPaths = this._messagePathConfigurator.LoadPaths.Except(existingPaths).ToList();

            foreach (var watch in this._watchers.Where(s => removePaths.Contains(s.Path)).ToList())
            {
                DisposeWatch(watch);
                this._watchers.Remove(watch);
            }

            // setup new ones...
            foreach (string newPath in addPaths)
            {
                this.AddWatcher(newPath);
            }
        }
    }

    void OnWatcherError(object sender, ErrorEventArgs e)
    {
        var ex = e.GetException();
        this._logger.Warning(ex, "FileSystemWatcher error, recreating watcher");

        if (sender is FileSystemWatcher faulted)
        {
            var path = faulted.Path;

            lock (this._watchersLock)
            {
                DisposeWatch(faulted);
                this._watchers.Remove(faulted);

                try
                {
                    this.AddWatcher(path);
                }
                catch (Exception addEx)
                {
                    this._logger.Error(addEx, "Failed to recreate FileSystemWatcher for {Path}", path);
                }
            }

            // trigger a full refresh to pick up anything missed during the buffer overflow
            this.OnRefreshNeeded();
        }
    }

    static void DisposeWatch(FileSystemWatcher watch)
    {
        try
        {
            watch.EnableRaisingEvents = false;
            watch.Dispose();
        }
        catch
        {
            // ignored
        }
    }

    void SetupMessageWatchers()
    {
        lock (this._watchersLock)
        {
            this._watchers = new List<FileSystemWatcher>();

            // setup watcher for each path...
            foreach (string path in this._messagePathConfigurator.LoadPaths)
            {
                this.AddWatcher(path);
            }
        }
    }

    void AddWatcher(string path)
    {
        this._logger.Debug("Adding FileSystemWatcher for {Path}", path);

        var watcher = new FileSystemWatcher(path, IMessageRepository.MessageFileSearchPattern)
        {
            NotifyFilter = NotifyFilters.FileName,
            InternalBufferSize = 65536
        };

        // Add event handlers.
        watcher.Created += this.OnChanged;
        watcher.Deleted += this.OnDeleted;
        watcher.Renamed += this.OnRenamed;
        watcher.Error += this.OnWatcherError;

        // Begin watching.
        watcher.EnableRaisingEvents = true;

        this._watchers.Add(watcher);
    }

    void OnDeleted(object sender, FileSystemEventArgs e)
    {
        this.OnRefreshNeeded();
    }

    void OnRenamed(object sender, RenamedEventArgs e)
    {
        this.OnRefreshNeeded();
    }

    void OnChanged(object sender, FileSystemEventArgs e)
    {
        _ = this.WaitForFileAndNotifyAsync(e.FullPath);
    }

    async Task WaitForFileAndNotifyAsync(string fullPath)
    {
        try
        {
            var info = new FileInfo(fullPath);
            var retryCount = 0;

            do
            {
                var timeout = 500 + retryCount * 100;
                await Task.Delay(timeout);
                if (++retryCount > 30)
                {
                    this._logger.Error(
                        "Failed after {RetryCount} retries to Open File {FileInfo}",
                        retryCount,
                        info);
                    return;
                }
            }
            while (!await info.CanReadFile());

            this.OnNewMessage(new NewMessageEventArgs(new MessageEntry(info)));
        }
        catch (Exception ex)
        {
            this._logger.Error(ex, "Error processing new message file {FullPath}", fullPath);
        }
    }

    public event EventHandler<NewMessageEventArgs>? NewMessage;

    public event EventHandler? RefreshNeeded;

    protected virtual void OnRefreshNeeded()
    {
        var eventHandler = this.RefreshNeeded;
        if (eventHandler != null)
        {
            eventHandler.Invoke(this, EventArgs.Empty);
        }
    }

    protected virtual void OnNewMessage(NewMessageEventArgs e)
    {
        var eventHandler = this.NewMessage;
        if (eventHandler != null)
        {
            eventHandler.Invoke(this, e);
        }
    }
}