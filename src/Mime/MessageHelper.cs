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

namespace Papercut.Mime
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Automation;

    using MimeKit;

    using Papercut.Annotations;

    public static class MessageHelper
    {
        public static async Task<MimeMessage> LoadMessage([NotNull] string mailFile, CancellationToken token)
        {
            if (mailFile == null)
            {
                throw new ArgumentNullException("mailFile");
            }

            return await Task.Run(() => MimeMessage.Load(ParserOptions.Default, mailFile, token), token);
        }

        public static bool IsContentHtml([NotNull] this TextPart textPart)
        {
            if (textPart == null)
            {
                throw new ArgumentNullException("textPart");
            }

            return textPart.ContentType.Matches("text", "html");
        }

        public static IEnumerable<MimePart> GetImages([NotNull] this MimeMessage mimeMessage)
        {
            return mimeMessage.BodyParts.Where(e => e.ContentType.Matches("image", "*"));
        }

        public static TextPart GetMainBodyTextPart([NotNull] this IEnumerable<MimePart> parts)
        {
            if (parts == null)
            {
                throw new ArgumentNullException("parts");
            }

            var mimeParts = parts.OfType<TextPart>().Where(s => !s.IsAttachment).ToArray();

            // return html if available first
            var html = mimeParts.FirstOrDefault(s => s.IsContentHtml());
            if (html != null)
            {
                return html;
            }

            // anything else available
            return mimeParts.FirstOrDefault();
        }
    }
}