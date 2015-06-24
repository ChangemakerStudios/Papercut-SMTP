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

namespace Papercut.Helpers
{
    using System;
    using System.IO;
    using System.Net.Mail;
    using System.Text;

    using MimeKit;

    using Serilog;

    public static class MailMessageHelper
    {
        const string PreviewFilePrefix = "Papercut-";

        public static MailMessage CreateFailureMailMessage(string error)
        {
            var errorMessage = new MailMessage
            {
                From = new MailAddress("fail@papercut.com", "Papercut Failure"),
                Subject = "Failure loading message: " + error,
                Body = "Unable to load",
                IsBodyHtml = false
            };

            return errorMessage;
        }

        internal static int TryCleanUpTempFiles(ILogger logger = null)
        {
            int deleteCount = 0;
            string tempPath = Path.GetTempPath();

            logger = logger ?? Log.Logger;

            // try cleanup...
            try
            {
                string[] tmpDirs = Directory.GetDirectories(tempPath, PreviewFilePrefix + "*");

                foreach (string tmpDir in tmpDirs)
                {
                    try
                    {
                        Directory.Delete(tmpDir, true);
                        deleteCount++;
                    }
                    catch (Exception ex)
                    {
                        logger.Warning(ex, @"Unable to delete {TempFile}", tmpDir);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warning(
                    ex,
                    @"Failure running temp file cleanup task on path delete {TempPath}",
                    tempPath);
            }

            if (deleteCount > 0)
                logger.Information("Deleted {DeleteCount} temp files", deleteCount);

            return deleteCount;
        }

        internal static string CreateHtmlPreviewFile(this MimeMessage mailMessageEx)
        {
            if (mailMessageEx == null) throw new ArgumentNullException("mailMessageEx");

            string tempDir = Path.Combine(Path.GetTempPath(), string.Format("{0}{1}", PreviewFilePrefix, Guid.NewGuid()));

            Directory.CreateDirectory(tempDir);

            HtmlPreviewVisitor visitor = new HtmlPreviewVisitor(tempDir);

            string htmlFile = Path.Combine(tempDir, "index.html");

            mailMessageEx.Accept(visitor);

            File.WriteAllText(htmlFile, visitor.HtmlBody, Encoding.Unicode);

            return htmlFile;
        }
    }
}