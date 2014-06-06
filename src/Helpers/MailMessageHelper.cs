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

namespace Papercut.Helpers
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;

    using MimeKit;

    using Papercut.Core.Helper;

    public static class MailMessageHelper
    {
        const string PreviewFileNme = "papercut.htm";

        internal static string CreateHtmlPreviewFile(this MimeMessage mailMessageEx)
        {
            if (mailMessageEx == null) throw new ArgumentNullException("mailMessageEx");

            var replaceEmbeddedImageFormats = new[] { @"cid:{0}", @"cid:'{0}'", @"cid:""{0}""" };

            string tempPath = Path.GetTempPath();
            string htmlFile = Path.Combine(tempPath, PreviewFileNme);

            var mimeParts = mailMessageEx.BodyParts.ToList();

            string htmlText = mimeParts.GetMainBodyTextPart().Text;

            foreach (MimePart image in mimeParts.GetImages().Where(i => !string.IsNullOrWhiteSpace(i.ContentId)))
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