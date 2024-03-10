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


using Papercut.Core.Domain.Paths;

namespace Papercut.Message;

public class MessageRepository(IMessagePathConfigurator messagePathConfigurator, ILogger logger)
{
    public const string MessageFileSearchPattern = "*.eml";

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

    public async Task<byte[]?> GetMessage(string? file)
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

    public IList<MessageEntry> LoadMessages()
    {
        IEnumerable<string?> files = messagePathConfigurator.LoadPaths.SelectMany(
            p => Directory.GetFiles(p, MessageFileSearchPattern));

        return
            files.Select(file => new MessageEntry(file))
                .OrderByDescending(m => m.ModifiedDate)
                .ThenBy(m => m.Name)
                .ToList();
    }

    public async Task<string?> SaveMessageAsync(Func<FileStream, Task> writeTo)
    {
        string? fileName = null;

        try
        {
            // the file must not exists.  the resolution of DataTime.Now may be slow w.r.t. the speed of the received files
            fileName = Path.Combine(
                messagePathConfigurator.DefaultSavePath,
                $"{DateTime.Now:yyyyMMddHHmmssfff}-{StringHelpers.SmallRandomString()}.eml");

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
}