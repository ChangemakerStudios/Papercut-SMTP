/* Yet Another Forum.NET
 * Copyright (C) 2006-2013 Jaben Cargman
 * http://www.yetanotherforum.net/
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 */

namespace Papercut.SMTP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public static class MessageFileService
    {
        #region Constants

        public const string MessageFileSearchPattern = "*.eml";

        #endregion

        #region Static Fields

        public static readonly IEnumerable<string> LoadPaths;

        public static readonly string DefaultSavePath;

        public static readonly IEnumerable<string> ExcludeFilesFromMigration =
            new string[]
            {
                "readme.eml"
            };

        #endregion

        #region Constructors and Destructors

        static MessageFileService()
        {
            var papercutBasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Papercut");

            try
            {
                if (!Directory.Exists(papercutBasePath))
                {
                    Directory.CreateDirectory(papercutBasePath);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError(string.Format("Failure accessing or creating directory: {0}", papercutBasePath), ex);
            }

            DefaultSavePath = papercutBasePath;
            LoadPaths = new List<string>
                        {
                            AppDomain.CurrentDomain.BaseDirectory,
                            DefaultSavePath
                        };

            // attempt migration for previous versions...
            TryMigrateMessages();
        }

        #endregion

        #region Public Methods and Operators

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

        #endregion

        #region Methods

        private static void TryMigrateMessages()
        {
            try
            {
                var current = AppDomain.CurrentDomain.BaseDirectory;

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