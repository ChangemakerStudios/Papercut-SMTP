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

namespace Papercut.SMTP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Principal;

    public static class MessageFileService
    {
        #region Constants

        public const string MessageFileSearchPattern = "*.eml";

        #endregion

        #region Static Fields

        public static readonly string DefaultSavePath;

        public static readonly IEnumerable<string> ExcludeFilesFromMigration =
            new string[]
            {
                "readme.eml"
            };

        public static readonly IEnumerable<string> LoadPaths;

        #endregion

        #region Constructors and Destructors

        static MessageFileService()
        {
            DefaultSavePath = AppDomain.CurrentDomain.BaseDirectory;

            var loadPaths = new List<string>
                            {
                                AppDomain.CurrentDomain.BaseDirectory
                            };

            bool isSystem;
            using (var identity = WindowsIdentity.GetCurrent()) isSystem = identity.IsSystem;

            if (!isSystem)
            {
                var papercutBasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Papercut");

                if (ValidatePathExists(papercutBasePath))
                {
                    loadPaths.Add(papercutBasePath);
                    DefaultSavePath = papercutBasePath;
                }
            }

            LoadPaths = loadPaths;

            // attempt migration for previous versions...
            TryMigrateMessages();
        }

        #endregion

        #region Public Methods and Operators

        public static bool DeleteMessage(MessageEntry entry)
        {
            // Delete the file and remove the entry
            if (!File.Exists(entry.File))
            {
                return false;
            }

            File.Delete(entry.File);
            return true;
        }

        public static IEnumerable<MessageEntry> LoadMessages()
        {
            var files = LoadPaths.SelectMany(p => Directory.GetFiles(p, MessageFileSearchPattern));

            return files.Select(file => new MessageEntry(file));
        }

        public static string SaveMessage(IList<string> output)
        {
            string file = null;

            try
            {
                do
                {
                    // the file must not exists.  the resolution of DataTime.Now may be slow w.r.t. the speed of the received files
                    var fileNameUnique = string.Format(
                        "{0}-{1}.eml",
                        DateTime.Now.ToString("yyyyMMddHHmmssFF"),
                        Guid.NewGuid().ToString().Substring(0, 2));

                    file = Path.Combine(DefaultSavePath, fileNameUnique);
                }
                while (File.Exists(file));

                File.WriteAllLines(file, output);
            }
            catch (Exception ex)
            {
                Logger.WriteError(string.Format("Failure saving email message: {0}", file), ex);
            }

            return file;
        }

        public static bool ValidatePathExists(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteError(string.Format("Failure accessing or creating directory: {0}", path), ex);
            }

            return false;
        }

        #endregion

        #region Methods

        private static void TryMigrateMessages()
        {
            try
            {
                var current = AppDomain.CurrentDomain.BaseDirectory;

                if (current == DefaultSavePath)
                {
                    // no migration required
                    return;
                }

                string[] files = Directory
                    .GetFiles(current, MessageFileSearchPattern)
                    .Where(s => !ExcludeFilesFromMigration.Any(e => e.EndsWith(s, StringComparison.OrdinalIgnoreCase)))
                    .ToArray();

                if (!files.Any())
                {
                    return;
                }

                foreach (var f in files)
                {
                    var destFileName = Path.Combine(DefaultSavePath, Path.GetFileName(f));
                    Logger.WriteWarning(string.Format("Migrating message from {0} to new path {1}.", f, destFileName));
                    File.Move(f, destFileName);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError("Failure attempting to migrate old messages to new location", ex);
            }
        }

        #endregion
    }
}