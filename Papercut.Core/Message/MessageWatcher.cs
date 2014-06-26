namespace Papercut.Core.Message
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Papercut.Core.Configuration;
    using Papercut.Core.Helper;

    using Serilog;

    public class MessageWatcher : IDisposable
    {
        readonly ILogger _logger;

        readonly IMessagePathConfigurator _messagePathConfigurator;

        List<FileSystemWatcher> _watchers;

        public MessageWatcher(ILogger logger, IMessagePathConfigurator messagePathConfigurator)
        {
            _logger = logger;
            _messagePathConfigurator = messagePathConfigurator;
            _messagePathConfigurator.RefreshLoadPath += OnRefreshLoadPaths;
            SetupMessageWatchers();
        }

        public void Dispose()
        {
            foreach (FileSystemWatcher watch in _watchers)
            {
                DisposeWatch(watch);
            }
        }

        void OnRefreshLoadPaths(object sender, EventArgs eventArgs)
        {
            List<string> existingPaths = _watchers.Select(s => s.Path).ToList();
            List<string> removePaths =
                existingPaths.Except(_messagePathConfigurator.LoadPaths).ToList();
            List<string> addPaths =
                _messagePathConfigurator.LoadPaths.Except(existingPaths).ToList();

            foreach (FileSystemWatcher watch in
                _watchers.Where(s => removePaths.Contains(s.Path)).ToList())
            {
                DisposeWatch(watch);
                _watchers.Remove(watch);
            }

            // setup new ones...
            foreach (string newPath in addPaths)
            {
                AddWatcher(newPath);
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
            }
        }

        void SetupMessageWatchers()
        {
            _watchers = new List<FileSystemWatcher>();

            // setup watcher for each path...
            foreach (string path in _messagePathConfigurator.LoadPaths)
            {
                AddWatcher(path);
            }
        }

        void AddWatcher(string path)
        {
            _logger.Information("Adding FileSystemWatcher for {Path}", path);

            var watcher = new FileSystemWatcher(path, MessageRepository.MessageFileSearchPattern)
            {
                NotifyFilter =
                    NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName
            };

            // Add event handlers.
            watcher.Created += OnChanged;
            watcher.Deleted += OnDeleted;
            watcher.Renamed += OnRenamed;

            // Begin watching.
            watcher.EnableRaisingEvents = true;

            _watchers.Add(watcher);
        }

        void OnDeleted(object sender, FileSystemEventArgs e)
        {
            OnRefreshNeeded();
        }

        void OnRenamed(object sender, RenamedEventArgs e)
        {
            OnRefreshNeeded();
        }

        void OnChanged(object sender, FileSystemEventArgs e)
        {
            Task.Factory.StartNew(
                () =>
                {
                    var info = new FileInfo(e.FullPath);
                    int retryCount = 0;

                    while (!info.CanOpenFile())
                    {
                        Thread.Sleep(500);
                        if (++retryCount > 30)
                        {
                            _logger.Error(
                                "Failed after {RetryCount} retries to Open File {FileInfo}",
                                retryCount,
                                info);
                            break;
                        }
                    }

                    return info;
                })
                .ContinueWith(
                    r => NewMessage(this, new NewMessageEventArgs(new MessageEntry(r.Result))));
        }

        public event EventHandler<NewMessageEventArgs> NewMessage;

        public event EventHandler RefreshNeeded;

        protected virtual void OnRefreshNeeded()
        {
            EventHandler handler = RefreshNeeded;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected virtual void OnNewMessage(NewMessageEventArgs e)
        {
            EventHandler<NewMessageEventArgs> handler = NewMessage;
            if (handler != null) handler(this, e);
        }
    }
}