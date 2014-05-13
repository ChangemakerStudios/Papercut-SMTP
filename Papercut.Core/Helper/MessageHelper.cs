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

namespace Papercut.Core.Helper
{
    using System.Collections.Generic;
    using System.Linq;

    using MimeKit;

    public static class MessageHelper
    {
        public static bool IsContentHtml([NotNull] this TextPart textPart)
        {
            return textPart.ContentType.Matches("text", "html");
        }

        public static IEnumerable<MimePart> GetImages([NotNull] this MimeMessage mimeMessage)
        {
            return mimeMessage.BodyParts.Where(e => e.ContentType.Matches("image", "*"));
        }

        public static TextPart GetMainBodyTextPart([NotNull] this IEnumerable<MimePart> parts)
        {
            var mimeParts = parts.OfType<TextPart>().Where(s => !s.IsAttachment).ToArray();

            // return html if available first
            var html = mimeParts.FirstOrDefault(s => s.IsContentHtml());

            if (!html.IsDefault()) return html;

            // anything else available
            return mimeParts.FirstOrDefault();
        }
    }
}