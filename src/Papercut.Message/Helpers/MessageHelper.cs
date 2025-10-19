// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
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


using Microsoft.Win32;

using MimeKit;

using Papercut.Common.Extensions;

namespace Papercut.Message.Helpers;

public static class MessageHelper
{
    public static async Task<MimeMessage> CloneMessageAsync(this MimeMessage? mimeMessage, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(mimeMessage);

        using var ms = new MemoryStream();

        await mimeMessage.WriteToAsync(FormatOptions.Default, ms, token);
        ms.Seek(0, SeekOrigin.Begin);
        var clonedMessage = await MimeMessage.LoadAsync(ParserOptions.Default, ms, token);

        return clonedMessage;
    }

    public static string? GetStringDump(this MimeMessage? mimeMessage)
    {
        ArgumentNullException.ThrowIfNull(mimeMessage);

        using var ms = new MemoryStream();
        mimeMessage.WriteTo(FormatOptions.Default, ms);
        ms.Seek(0, SeekOrigin.Begin);
        var mail = ms.ToArray();

        return Encoding.UTF8.GetString(mail, 0, mail.Length);
    }

    public static bool IsContentHtml(this TextPart textPart)
    {
        return textPart.ContentType.IsMimeType("text", "html");
    }

    public static string GetExtension(this ContentType contentType)
    {
        ArgumentNullException.ThrowIfNull(contentType);

        return
            Registry.ClassesRoot.OpenSubKey(
                    $@"MIME\Database\Content Type\{contentType.MediaType}/{contentType.MediaSubtype}",
                    false)
                .ToEnumerable()
                .Select(k => k.GetValue("Extension", null))
                .Where(v => v != null)
                .Select(v => v.ToString())
                .FirstOrDefault();
    }

    public static IEnumerable<MimePart> GetImages(this IEnumerable<MimePart> prefilteredMimeParts)
    {
        ArgumentNullException.ThrowIfNull(prefilteredMimeParts);

        return prefilteredMimeParts.Where(e => e.ContentType.IsMimeType("image", "*"));
    }

    public static IEnumerable<MimePart> GetAttachments(this IEnumerable<MimePart> prefilteredMimeParts)
    {
        ArgumentNullException.ThrowIfNull(prefilteredMimeParts);

        return prefilteredMimeParts.Where(p => p.IsAttachment);
    }

    public static TextPart? GetMainBodyTextPart(this IEnumerable<MimePart> prefilteredMimeParts)
    {
        var mimeParts = prefilteredMimeParts.OfType<TextPart>().Where(s => !s.IsAttachment).ToList();

        // return html if available first
        var html = mimeParts.FirstOrDefault(s => s.IsContentHtml());

        if (!html.IsDefault())
            return html;

        // anything else available
        return mimeParts.FirstOrDefault();
    }
}