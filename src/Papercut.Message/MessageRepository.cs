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


using Papercut.Common.Extensions;
using Papercut.Common.Helper;
using Papercut.Core.Domain.Message;
using Papercut.Core.Domain.Paths;

using Serilog;

namespace Papercut.Message
{
    public class MessageRepository(ILogger logger, MessagePathConfigurator messagePathConfigurator)
    {
        public const string MessageFileSearchPattern = "*.eml";

        private const string EmptyStringReplacement = "_";

        static char[] _invalidFileNameChars;

        public bool DeleteMessage(MessageEntry entry)
        {
            // Delete the file and remove the entry
            if (!File.Exists(entry.File))
                return false;

            var attributes = File.GetAttributes(entry.File);

            try
            {
                if (attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    // remove read only attribute
                    File.SetAttributes(entry.File, attributes ^ FileAttributes.ReadOnly);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedAccessException(
                    $"Unable to remove read-only attribute on file '{entry.File}'",
                    ex);
            }

            File.Delete(entry.File);
            return true;
        }

        public async Task<byte[]> GetMessage(string? file)
        {
            if (!File.Exists(file))
                throw new IOException($"File {file} Does Not Exist");

            var info = new FileInfo(file);
            int retryCount = 0;

            var result = await info.TryReadFile();

            while (result.IsFailed)
            {
                await Task.Delay(500);

                if (++retryCount > 10)
                {
                    throw new IOException(
                        $"Cannot Load File {file} After 5 Seconds");
                }

                result = await info.TryReadFile();
            }

            return result.Value;
        }

        /// <summary>
        /// Loads all messages
        /// </summary>
        public IEnumerable<MessageEntry> LoadMessages()
        {
            return messagePathConfigurator.LoadPaths.SelectMany(
                p => Directory.GetFiles(p, MessageFileSearchPattern)).Select(file => new MessageEntry(file));
        }

        public string GetFullMailFilename(string mailSubject)
        {
            var validPart = MakeValidFileName(mailSubject.Truncate(40)!, "subject unknown");

            var dateTimeFormatted = DateTime.Now.ToString(MessageEntry.DateTimeFormat);

            // the file must not exist:  the resolution of DataTime.Now may be slow w.r.t. the speed of the received files
            return Path.Combine(
                messagePathConfigurator.DefaultSavePath,
                $"{dateTimeFormatted} {validPart} {StringHelpers.SmallRandomString()}.eml");
        }

        public async Task<string> SaveMessage(string mailSubject, Func<FileStream, Task> writeTo)
        {
            var fileName = this.GetFullMailFilename(mailSubject);

            try
            {
                await using (var fileStream = File.Create(fileName))
                {
                    await writeTo(fileStream);
                }

                logger.Information("Successfully Saved email message: {EmailMessageFile}", fileName);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failure saving email message: {EmailMessageFile}", fileName);
            }

            return fileName;
        }

        /// <summary>Replaces characters in <c>text</c> that are not allowed in 
        /// file names with the specified replacement character.<para/>
        /// https://stackoverflow.com/questions/620605/how-to-make-a-valid-windows-filename-from-an-arbitrary-string/25223884#25223884
        /// </summary>
        /// <param name="inputText">Text to make into a valid filename. The same string is returned if it is valid already.</param>
        /// <param name="replacement">Replacement character, or null to simply remove bad characters.</param>
        /// <param name="emptyText">A replacement for the empty result.</param>
        /// <param name="fancy">Whether to replace quotes and slashes with the non-ASCII characters ” and ⁄.</param>
        /// <returns>A string that can be used as a filename. If the output string would otherwise be empty,
        /// returns <see cref="EmptyStringReplacement"/>.</returns>
        public static string MakeValidFileName(
            string inputText,
            string emptyText = EmptyStringReplacement,
            char? replacement = '_',
            bool fancy = true)
        {
            var text = inputText ?? string.Empty;

            var invalids = _invalidFileNameChars ?? (_invalidFileNameChars = Path.GetInvalidFileNameChars());

            emptyText = emptyText ?? string.Empty;
            if (!string.IsNullOrEmpty(emptyText) && emptyText != EmptyStringReplacement)
            {
                emptyText = MakeValidFileName(emptyText);
            }

            var sb = new StringBuilder(text.Length);
            var changed = false;
            foreach (var ch in text)
            {
                if (!invalids.Contains(ch))
                {
                    sb.Append(ch);

                    continue;
                }

                changed = true;
                var repl = replacement ?? '\0';
                if (fancy)
                {
                    switch (ch)
                    {
                        case '"':
                            // U+201D right double quotation mark
                            repl = '”';
                            break;
                        case '\'':
                            // U+2019 right single quotation mark
                            repl = '’';
                            break;
                        case '/':
                            // U+2044 fraction slash
                            repl = '⁄';
                            break;
                    }
                }

                if (repl != '\0')
                {
                    sb.Append(repl);
                }
            }

            if (sb.Length == 0)
            {
                return emptyText;
            }

            return changed ? sb.ToString() : text;
        }
    }
}