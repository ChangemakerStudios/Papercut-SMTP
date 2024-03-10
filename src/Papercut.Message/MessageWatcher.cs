// Papercut
// 
// Copyright � 2008 - 2012 Ken Robertson
// Copyright � 2013 - 2024 Jaben Cargman
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


using Papercut.Core.Domain.Paths;

namespace Papercut.Message;

public class MessageWatcher : IDisposable
{
    readonly ILogger _logger;

    readonly IMessagePathConfigurator _messagePathConfigurator;

    readonly List<FileSystemWatcher> _watchers;

    public MessageWatcher(IMessagePathConfigurator messagePathConfigurator, ILogger logger)
    {
        this._logger = logger;
        this._messagePathConfigurator = messagePathConfigurator;
        this._messagePathConfigurator.RefreshLoadPath += this.OnRefreshLoadPaths;
        this._watchers = this.CreateMessageWatchers().ToList();
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;

        foreach (var watch in this._watchers)
        {
            DisposeWatch(watch);
        }
    }

    void OnRefreshLoadPaths(object? sender, EventArgs eventArgs)
    {
        var existingPaths = this._watchers.Select(s => s.Path).ToList();
        var removePaths = existingPaths.Except(this._messagePathConfigurator.LoadPaths).ToList();
        var addPaths = this._messagePathConfigurator.LoadPaths.Except(existingPaths).ToList();

        foreach (var watch in this._watchers.Where(s => removePaths.Contains(s.Path)).ToList())
        {
            DisposeWatch(watch);
            this._watchers.Remove(watch);
        }

        // setup new ones...
        foreach (var newPath in addPaths)
        {
            this._watchers.Add(this.CreateWatcher(newPath));
        }
    }

    static void DisposeWatch(FileSystemWatcher watch)
    {
        try
        {
            watch.EnableRaisingEvents = false;
            watch.Dispose();
        }
        catch (Exception)
        {
            // ignored
        }
    }

    IEnumerable<FileSystemWatcher> CreateMessageWatchers()
    {
        // setup watcher for each path...
        foreach (var path in this._messagePathConfigurator.LoadPaths)
        {
            yield return this.CreateWatcher(path);
        }
    }

    FileSystemWatcher CreateWatcher(string path)
    {
        this._logger.Debug("Creating FileSystemWatcher for {Path}", path);

        var watcher = new FileSystemWatcher(path, MessageRepository.MessageFileSearchPattern)
                      {
                          NotifyFilter =
                              NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName
                      };

        // Add event handlers.
        watcher.Created += this.OnChanged;
        watcher.Deleted += this.OnDeleted;
        watcher.Renamed += this.OnRenamed;

        // Begin watching.
        watcher.EnableRaisingEvents = true;

        return watcher;
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
        Task.Factory.StartNew(
            async () =>
            {
                var info = new FileInfo(e.FullPath);
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
                        break;
                    }
                }
                while (!await info.CanReadFile());

                this.OnNewMessage(new NewMessageEventArgs(new MessageEntry(info)));

                return info;
            });
    }

    public event EventHandler<NewMessageEventArgs> NewMessage;

    public event EventHandler RefreshNeeded;

    protected virtual void OnRefreshNeeded()
    {
        var handler = this.RefreshNeeded;
        handler?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnNewMessage(NewMessageEventArgs e)
    {
        var handler = this.NewMessage;
        handler?.Invoke(this, e);
    }
}