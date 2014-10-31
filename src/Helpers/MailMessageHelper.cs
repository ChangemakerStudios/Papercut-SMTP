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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using MimeKit;

    using Papercut.Core.Helper;
    using Papercut.Properties;

    using Serilog;

    public static class MailMessageHelper
    {
        const string PreviewFilePrefix = "Papercut-";

        const string BodyContentsDisableContextMenu =
            @"<body${contents} oncontextmenu=""return false;"">";

        const string HtmlBodyPattern = @"\<body(?<contents>[^\<]*?)\>";

        static readonly Regex _htmlBodyReplaceRegex =
            new Regex(HtmlBodyPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        internal static int TryCleanUpTempFiles(ILogger logger = null)
        {
            int deleteCount = 0;
            string tempPath = Path.GetTempPath();

            logger = logger ?? Log.Logger;

            // try cleanup...
            try
            {
                string[] tmpFiles = Directory.GetFiles(
                    tempPath,
                    string.Format(
                        "{0}*.html",
                        PreviewFilePrefix));

                foreach (string tmpFile in tmpFiles)
                {
                    try
                    {
                        File.Delete(tmpFile);
                        deleteCount++;
                    }
                    catch (Exception ex)
                    {
                        logger.Warning(ex, @"Unable to delete {TempFile}", tmpFile);
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
            {
                logger.Information(
                    "Deleted {DeleteCount} temp files",
                    deleteCount);
            }

            return deleteCount;
        }

        internal static string CreateHtmlPreviewFile(this MimeMessage mailMessageEx)
        {
            if (mailMessageEx == null) throw new ArgumentNullException("mailMessageEx");

            var replaceEmbeddedImageFormats = new[] { @"cid:{0}", @"cid:'{0}'", @"cid:""{0}""" };

            string tempPath = Path.GetTempPath();
            string tempFileName = string.Format(
                "{0}{1}.html",
                PreviewFilePrefix,
                mailMessageEx.GetHashCode());

            string htmlFile = Path.Combine(tempPath, tempFileName);

            List<MimePart> mimeParts = mailMessageEx.BodyParts.ToList();

            TextPart mainBodyTextPart = mimeParts.GetMainBodyTextPart();
            string htmlText = mainBodyTextPart.Text;

            if (mainBodyTextPart.IsContentHtml())
            {
                // add the mark of the web plus the html text
                htmlText = UIStrings.MarkOfTheWeb
                           + _htmlBodyReplaceRegex.Replace(htmlText, BodyContentsDisableContextMenu);
            }
            else
            {
                // add some html formatting to the display html
                htmlText = UIStrings.MarkOfTheWeb + string.Format(UIStrings.HtmlFormatWrapper, htmlText);
            }

            foreach (
                MimePart image in
                    mimeParts.GetImages().Where(i => !string.IsNullOrWhiteSpace(i.ContentId)))
            {
                string fileName = Path.Combine(tempPath, image.ContentId);
                using (FileStream fs = File.OpenWrite(fileName))
                {
                    using (Stream content = image.ContentObject.Open()) content.CopyBufferedTo(fs);
                    fs.Close();
                }

                htmlText = replaceEmbeddedImageFormats.Aggregate(
                    htmlText,
                    (current, format) =>
                    current.Replace(string.Format(format, image.ContentId), image.ContentId));
            }

            File.WriteAllText(htmlFile, htmlText, Encoding.Unicode);

            return htmlFile;
        }
    }
}