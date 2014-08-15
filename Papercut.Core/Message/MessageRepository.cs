// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2014 Jaben Cargman
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

namespace Papercut.Core.Message
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using Papercut.Core.Configuration;
    using Papercut.Core.Helper;

    using Serilog;

    public class MessageRepository
    {
        public const string MessageFileSearchPattern = "*.eml";

        readonly ILogger _logger;

        readonly IMessagePathConfigurator _messagePathConfigurator;

        public MessageRepository(ILogger logger, IMessagePathConfigurator messagePathConfigurator)
        {
            _logger = logger;
            _messagePathConfigurator = messagePathConfigurator;
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

            while (!info.TryReadFile(out data))
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

                _logger.Debug("Successfully Saved email message: {EmailMessageFile}", file);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failure saving email message: {EmailMessageFile}", file);
            }

            return file;
        }
    }
}