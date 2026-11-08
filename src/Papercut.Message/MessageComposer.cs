// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2026 Jaben Cargman
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


using Markdig;

using MimeKit;
using MimeKit.Utils;

using Papercut.Core.Domain.Message;

namespace Papercut.Message;

/// <summary>
/// A message created through the API instead of received over SMTP.
/// </summary>
[PublicAPI]
public class MessageDraft
{
    public string? Subject { get; set; }

    /// <summary>A single address. Defaults to Papercut itself.</summary>
    public string? From { get; set; }

    /// <summary>Comma-separated addresses.</summary>
    public string? To { get; set; }

    /// <summary>Comma-separated addresses.</summary>
    public string? Cc { get; set; }

    /// <summary>Plain text body. With markdown, replaces the source as the text part.</summary>
    public string? Text { get; set; }

    public string? Html { get; set; }

    /// <summary>Rendered to the HTML part; the source becomes the text part. Not with Html.</summary>
    public string? Markdown { get; set; }

    /// <summary>False: it still appears in the message list, without a notification.</summary>
    public bool Notify { get; set; } = true;
}

public static class MessageComposer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    /// <exception cref="ArgumentException">The draft is empty, or an address does not parse.</exception>
    public static MimeMessage Compose(MessageDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var hasText = !string.IsNullOrWhiteSpace(draft.Text);
        var hasHtml = !string.IsNullOrWhiteSpace(draft.Html);
        var hasMarkdown = !string.IsNullOrWhiteSpace(draft.Markdown);

        if (hasHtml && hasMarkdown)
            throw new ArgumentException("Provide 'html' or 'markdown', not both");

        if (!hasHtml && !hasMarkdown && !hasText && string.IsNullOrWhiteSpace(draft.Subject))
            throw new ArgumentException("Provide a subject or a body ('text', 'html' or 'markdown')");

        var message = new MimeMessage
        {
            Subject = draft.Subject ?? string.Empty,
            Date = DateTimeOffset.Now,
            MessageId = MimeUtils.GenerateMessageId()
        };

        // same leniency as received mail: developers use domains like @__ (Issue #284)
        var parserOptions = ParserOptions.Default.Clone();
        parserOptions.AddressParserComplianceMode = RfcComplianceMode.Loose;

        message.From.Add(ParseFrom(draft.From, parserOptions));
        message.To.AddRange(ParseList(draft.To, "to", parserOptions));
        message.Cc.AddRange(ParseList(draft.Cc, "cc", parserOptions));

        var body = new BodyBuilder();

        if (hasText) body.TextBody = draft.Text;
        else if (hasMarkdown) body.TextBody = draft.Markdown;
        else if (!hasHtml) body.TextBody = string.Empty;

        if (hasMarkdown) body.HtmlBody = Markdown.ToHtml(draft.Markdown!, Pipeline);
        else if (hasHtml) body.HtmlBody = draft.Html;

        message.Body = body.ToMessageBody();

        return message;
    }

    /// <summary>
    /// Goes through the received-mail path, so rules, the message list and notifications
    /// all treat it like any other message. Returns the saved file.
    /// </summary>
    public static async Task<string> SaveDraftAsync(this IReceivedDataHandler handler, MessageDraft draft)
    {
        var message = Compose(draft);

        using var stream = new MemoryStream();
        await message.WriteToAsync(stream);

        var recipients = message.To.Mailboxes.Concat(message.Cc.Mailboxes).Select(m => m.Address).ToArray();

        return await handler.HandleReceivedAsync(stream.ToArray(), recipients, draft.Notify);
    }

    private static MailboxAddress ParseFrom(string? value, ParserOptions options)
    {
        if (string.IsNullOrWhiteSpace(value)) return new MailboxAddress("Papercut", "papercut@localhost");

        return MailboxAddress.TryParse(options, value, out var mailbox)
            ? mailbox
            : throw new ArgumentException($"'from' is not a valid address: {value}");
    }

    private static IEnumerable<InternetAddress> ParseList(string? value, string field, ParserOptions options)
    {
        if (string.IsNullOrWhiteSpace(value)) return [];

        return InternetAddressList.TryParse(options, value, out var addresses)
            ? addresses
            : throw new ArgumentException($"'{field}' is not a valid address list: {value}");
    }
}
