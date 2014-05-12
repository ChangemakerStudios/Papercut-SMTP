/*  
 * Papercut
 *
 *  Copyright © 2008 - 2012 Ken Robertson
 *  Copyright © 2013 - 2014 Jaben Cargman
 *  
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *  
 *  http://www.apache.org/licenses/LICENSE-2.0
 *  
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *  
 */

namespace Papercut.Core.Message
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Papercut.Core.Configuration;

    using Serilog;

    public class MessageRepository : IDisposable
    {
        public const string MessageFileSearchPattern = "*.eml";

        readonly IMessagePathConfigurator _messagePathConfigurator;

        List<FileSystemWatcher> _watchers;

        public MessageRepository(ILogger logger, IMessagePathConfigurator messagePathConfigurator)
        {
            Logger = logger;
            _messagePathConfigurator = messagePathConfigurator;
            SetupMessageWatchers();
        }

        public ILogger Logger { get; private set; }

        public void Dispose()
        {
            foreach (FileSystemWatcher watch in _watchers)
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
        }

        void SetupMessageWatchers()
        {
            _watchers = new List<FileSystemWatcher>();

            // setup watcher for each path...
            foreach (string path in _messagePathConfigurator.LoadPaths)
            {
                var watcher = new FileSystemWatcher(path, MessageFileSearchPattern)
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
        }

        void OnDeleted(object sender, FileSystemEventArgs e)
        {
            RefreshNeeded(this, new EventArgs());
        }

        void OnRenamed(object sender, RenamedEventArgs e)
        {
            RefreshNeeded(this, new EventArgs());
        }

        void OnChanged(object sender, FileSystemEventArgs e)
        {
            Task.Factory.StartNew(
                () =>
                {
                    var info = new FileInfo(e.FullPath);
                    int retryCount = 0;

                    while (!CanOpenFile(info))
                    {
                        Thread.Sleep(500);
                        if (++retryCount > 30)
                        {
                            Logger.Error(
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

        bool CanOpenFile(FileInfo file)
        {
            if (file == null) throw new ArgumentNullException("file");

            try
            {
                using (
                    FileStream fileStream = file.Open(
                        FileMode.Open,
                        FileAccess.ReadWrite,
                        FileShare.None))
                {
                    fileStream.Close();
                }
            }
            catch (IOException)
            {
                return false;
            }

            return true;
        }

        bool TryReadFile(FileInfo file, out byte[] fileBytes)
        {
            if (file == null) throw new ArgumentNullException("file");

            fileBytes = null;

            try
            {
                using (FileStream fileStream = file.OpenRead())
                {
                    using (var ms = new MemoryStream())
                    {
                        fileStream.CopyTo(ms);
                        fileBytes = ms.ToArray();
                    }

                    fileStream.Close();
                }
            }
            catch (IOException)
            {
                // the file is unavailable because it is still being written by another thread or process
                return false;
            }

            return true;
        }

        public bool DeleteMessage(MessageEntry entry)
        {
            // Delete the file and remove the entry
            if (!File.Exists(entry.File)) return false;

            File.Delete(entry.File);
            return true;
        }

        public byte[] GetMessage(string file)
        {
            if (!File.Exists(file)) throw new IOException(string.Format("File {0} Does Not Exist", file));

            var info = new FileInfo(file);
            byte[] data;
            int retryCount = 0;

            while (!TryReadFile(info, out data))
            {
                Thread.Sleep(500);

                if (++retryCount > 10)
                {
                    throw new IOException(
                        string.Format("Cannot Load File {0} After 5 Seconds", file));
                }
            }

            return data;
        }

        public IList<MessageEntry> LoadMessages()
        {
            IEnumerable<string> files =
                _messagePathConfigurator.LoadPaths.SelectMany(
                    p => Directory.GetFiles(p, MessageFileSearchPattern));

            return
                files.Select(file => new MessageEntry(file))
                    .OrderByDescending(m => m.ModifiedDate)
                    .ThenBy(m => m.Name)
                    .ToList();
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

        public string SaveMessage(IList<string> output)
        {
            string file = null;

            try
            {
                do
                {
                    // the file must not exists.  the resolution of DataTime.Now may be slow w.r.t. the speed of the received files
                    string fileNameUnique = string.Format(
                        "{0}-{1}.eml",
                        DateTime.Now.ToString("yyyyMMddHHmmssFF"),
                        Guid.NewGuid().ToString().Substring(0, 2));

                    file = Path.Combine(_messagePathConfigurator.DefaultSavePath, fileNameUnique);
                }
                while (File.Exists(file));

                File.WriteAllLines(file, output);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failure saving email message: {EmailMessageFile}", file);
            }

            return file;
        }
    }
}