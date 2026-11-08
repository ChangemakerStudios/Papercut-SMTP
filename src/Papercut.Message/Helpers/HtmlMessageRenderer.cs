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


using System.Text.RegularExpressions;

using MimeKit;
using MimeKit.Text;
using MimeKit.Tnef;

namespace Papercut.Message.Helpers;

/// <summary>
///     Renders a <see cref="MimeMessage" /> to display-ready HTML.
///
///     This walks the actual MIME tree the way the desktop preview does
///     (most-faithful alternative wins, inline images resolve against their
///     multipart/related parent) and rewrites inline references through a
///     caller-supplied URL resolver, so a host can point them at files, an
///     HTTP endpoint, or anything else.
///
///     Unlike the raw <see cref="MimeMessage.HtmlBody" />, the output is
///     sanitized: script/frame/object/embed elements and their content are
///     dropped, event-handler attributes are removed, and javascript:,
///     vbscript: and non-image data: URLs are neutralized. Sanitizing happens
///     through MimeKit's HTML tokenizer rather than string matching, so
///     malformed markup cannot smuggle a tag past it.
/// </summary>
public static class HtmlMessageRenderer
{
    /// <summary>
    ///     Marks output converted from a plain-text part. A host can use it to
    ///     decide whether the content has a design of its own to preserve, or
    ///     whether it should be styled to match the surrounding application.
    /// </summary>
    public const string PlainTextMarkerClass = "papercut-plain-text";

    /// <summary>
    ///     Elements dropped entirely, along with their content. style is
    ///     deliberately kept -- email layout (MJML and friends) depends on it,
    ///     and it is inert inside a script-less frame.
    /// </summary>
    private static readonly HashSet<string> _droppedElements =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "script", "iframe", "frame", "frameset", "applet", "object", "embed",
            // an email's own <base> would break the inline-content URLs we emit
            "base"
        };

    /// <summary>
    ///     Void elements have no closing tag, so suppressing "inner content"
    ///     on one would swallow the rest of the document.
    /// </summary>
    private static readonly HashSet<string> _voidElements =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "area", "base", "br", "col", "embed", "frame", "hr", "img",
            "input", "link", "meta", "param", "source", "track", "wbr"
        };

    /// <summary>Attributes that carry a URL and therefore need scheme checks.</summary>
    private static readonly HashSet<string> _urlAttributes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "href", "src", "action", "formaction", "background", "poster", "srcset"
        };

    private static readonly Regex _unsafeScheme =
        new(@"^\s*(javascript|vbscript):", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex _dataUrl =
        new(@"^\s*data:", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex _dataImageUrl =
        new(@"^\s*data:image/", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex _cidReference =
        new(@"cid:([^""'\s;,<>\)]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <param name="message">The message to render.</param>
    /// <param name="resolveInlineUrl">
    ///     Given an inline part and the original URL it was referenced by,
    ///     returns the URL to use instead (or null to leave it untouched).
    /// </param>
    public static HtmlRenderResult Render(
        MimeMessage message,
        Func<MimePart, string, string?> resolveInlineUrl)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(resolveInlineUrl);

        var visitor = new RenderVisitor(resolveInlineUrl);
        visitor.Visit(message);

        return new HtmlRenderResult(
            ResolveRemainingCidReferences(visitor.Body, message, resolveInlineUrl),
            visitor.Attachments);
    }

    /// <summary>
    ///     Catches cid: references the tag walker cannot reach -- CSS
    ///     url(cid:...) inside style blocks, for instance.
    /// </summary>
    private static string ResolveRemainingCidReferences(
        string html,
        MimeMessage message,
        Func<MimePart, string, string?> resolveInlineUrl)
    {
        if (string.IsNullOrEmpty(html) || !html.Contains("cid:", StringComparison.OrdinalIgnoreCase))
        {
            return html;
        }

        var partsByContentId = new Dictionary<string, MimePart>(StringComparer.OrdinalIgnoreCase);

        foreach (var part in message.BodyParts.OfType<MimePart>())
        {
            if (!string.IsNullOrEmpty(part.ContentId))
            {
                partsByContentId[part.ContentId.Trim('<', '>')] = part;
            }
        }

        if (partsByContentId.Count == 0) return html;

        return _cidReference.Replace(
            html,
            match =>
            {
                var contentId = match.Groups[1].Value.Trim('<', '>');

                if (!partsByContentId.TryGetValue(contentId, out var part)) return match.Value;

                return resolveInlineUrl(part, match.Value) ?? match.Value;
            });
    }

    private static bool IsUnsafeUrl(string value)
    {
        if (_unsafeScheme.IsMatch(value)) return true;

        // data: URLs can carry markup; images are the common legitimate case
        return _dataUrl.IsMatch(value) && !_dataImageUrl.IsMatch(value);
    }

    /// <summary>The rendered body plus everything treated as an attachment.</summary>
    public record HtmlRenderResult(string Html, IList<MimeEntity> Attachments);

    private sealed class RenderVisitor(Func<MimePart, string, string?> resolveInlineUrl) : MimeVisitor
    {
        private const int IndexNotFound = -1;

        private readonly List<MimeEntity> _attachments = [];

        private readonly List<MultipartRelated> _stack = [];

        private string? _body;

        public string Body => _body ?? string.Empty;

        public IList<MimeEntity> Attachments => _attachments;

        protected override void VisitMultipartAlternative(MultipartAlternative alternative)
        {
            // walk backwards: the most faithful representation comes last
            for (var i = alternative.Count - 1; i >= 0 && _body == null; i--)
            {
                alternative[i].Accept(this);
            }
        }

        protected override void VisitMultipartRelated(MultipartRelated related)
        {
            _stack.Add(related);
            related.Root.Accept(this);
            _stack.RemoveAt(_stack.Count - 1);
        }

        protected override void VisitTextPart(TextPart entity)
        {
            if (_body != null)
            {
                // the body is already set, so this is an alternative or attachment
                _attachments.Add(entity);
                return;
            }

            if (entity.IsHtml)
            {
                _body = new HtmlToHtml { HtmlTagCallback = HtmlTagCallback }.Convert(entity.Text);
            }
            else if (entity.IsFlowed)
            {
                var converter = new FlowedToHtml();

                var delsp = entity.ContentType.Parameters["delsp"];

                if (delsp != null)
                {
                    converter.DeleteSpace = delsp.Equals("yes", StringComparison.OrdinalIgnoreCase);
                }

                _body = converter.Convert(entity.Text);
            }
            else
            {
                // <pre> matches the desktop's TextToHtmlFormatWrapper so plain
                // text keeps its spacing and monospace rendering. The wrapper's
                // IE-only bits (X-UA-Compatible) are deliberately dropped, and
                // the host supplies the font/margins.
                _body = new TextToHtml
                {
                    Header = $"<pre class=\"{PlainTextMarkerClass}\">",
                    HeaderFormat = HeaderFooterFormat.Html,
                    Footer = "</pre>",
                    FooterFormat = HeaderFooterFormat.Html
                }.Convert(entity.Text);
            }
        }

        protected override void VisitTnefPart(TnefPart entity)
        {
            // a TNEF part is a container of attachments
            foreach (var attachment in entity.ExtractAttachments())
            {
                _attachments.Add(attachment);
            }
        }

        protected override void VisitMessagePart(MessagePart entity)
        {
            _attachments.Add(entity);
        }

        protected override void VisitMimePart(MimePart entity)
        {
            _attachments.Add(entity);
        }

        private void HtmlTagCallback(HtmlTagContext ctx, HtmlWriter htmlWriter)
        {
            if (_droppedElements.Contains(ctx.TagName))
            {
                // writing nothing removes the element itself; content only needs
                // suppressing for elements that actually have a closing tag
                if (!ctx.IsEndTag
                    && !ctx.IsEmptyElementTag
                    && !_voidElements.Contains(ctx.TagName))
                {
                    ctx.SuppressInnerContent = true;
                    ctx.DeleteEndTag = true;
                }

                return;
            }

            if (ctx.IsEndTag)
            {
                ctx.WriteTag(htmlWriter, true);
                return;
            }

            ctx.WriteTag(htmlWriter, false);

            foreach (var attribute in ctx.Attributes)
            {
                WriteSanitizedAttribute(ctx, htmlWriter, attribute);
            }
        }

        private void WriteSanitizedAttribute(HtmlTagContext ctx, HtmlWriter htmlWriter, HtmlAttribute attribute)
        {
            // on* handlers cannot run in a script-less frame, but they should
            // not survive into anything that later renders this html
            if (attribute.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase)
                || attribute.Name.Equals("srcdoc", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!_urlAttributes.Contains(attribute.Name))
            {
                htmlWriter.WriteAttribute(attribute);
                return;
            }

            var value = attribute.Value ?? string.Empty;

            if (IsUnsafeUrl(value))
            {
                return;
            }

            // point inline references (cid:, or a related part's own location)
            // at wherever the host serves them from
            if (attribute.Id != HtmlAttributeId.Href && TryResolveInline(value, out var resolved))
            {
                htmlWriter.WriteAttributeName(attribute.Name);
                htmlWriter.WriteAttributeValue(resolved!);
                return;
            }

            htmlWriter.WriteAttribute(attribute);
        }

        private bool TryResolveInline(string url, out string? resolved)
        {
            resolved = null;

            if (string.IsNullOrWhiteSpace(url) || _stack.Count == 0) return false;

            var kind = Uri.IsWellFormedUriString(url, UriKind.Absolute)
                ? UriKind.Absolute
                : Uri.IsWellFormedUriString(url, UriKind.Relative)
                    ? UriKind.Relative
                    : UriKind.RelativeOrAbsolute;

            Uri uri;

            try
            {
                uri = new Uri(url, kind);
            }
            catch (UriFormatException)
            {
                return false;
            }

            foreach (var related in Enumerable.Reverse(_stack.ToArray()))
            {
                var index = related.IndexOf(uri);

                if (index == IndexNotFound) continue;

                if (related[index] is not MimePart part) continue;

                resolved = resolveInlineUrl(part, url);

                return resolved != null;
            }

            return false;
        }
    }
}
