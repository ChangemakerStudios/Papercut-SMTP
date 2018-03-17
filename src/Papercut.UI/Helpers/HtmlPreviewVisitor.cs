// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2017 Jaben Cargman
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

    using MimeKit;
    using MimeKit.Text;
    using MimeKit.Tnef;

    using Papercut.Properties;

    class HtmlPreviewVisitor : MimeVisitor
    {
        readonly List<MimeEntity> _attachments = new List<MimeEntity>();

        readonly List<MultipartRelated> _stack = new List<MultipartRelated>();

        readonly string _tempDir;

        string _body;

        public HtmlPreviewVisitor(string tempDirectory)
        {
            _tempDir = tempDirectory;
        }

        public IList<MimeEntity> Attachments => _attachments;

        public string HtmlBody => _body ?? string.Empty;

        protected override void VisitMultipartAlternative(MultipartAlternative alternative)
        {
            // walk the multipart/alternative children backwards from greatest level of faithfulness to the least faithful
            for (int i = alternative.Count - 1; i >= 0 && _body == null; i--)
            {
                alternative[i].Accept(this);
            }
        }

        protected override void VisitMultipartRelated(MultipartRelated related)
        {
            var root = related.Root;
            _stack.Add(related);
            root.Accept(this);
            _stack.RemoveAt(_stack.Count - 1);
        }

        bool TryGetImage(string url, out MimePart image)
        {
            UriKind kind;
            Uri uri;

            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
                kind = UriKind.Absolute;
            else if (Uri.IsWellFormedUriString(url, UriKind.Relative))
                kind = UriKind.Relative;
            else
                kind = UriKind.RelativeOrAbsolute;

            try
            {
                uri = new Uri(url, kind);
            }
            catch (UriFormatException)
            {
                image = null;
                return false;
            }

            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                int index;
                if ((index = _stack[i].IndexOf(uri)) == -1)
                    continue;

                image = _stack[i][index] as MimePart;
                return image != null;
            }

            image = null;

            return false;
        }

        string SaveImage(MimePart image, string url)
        {
            string fileName = url.Replace(':', '_').Replace('\\', '_').Replace('/', '_');

            // try to add a file extension for niceness
            switch (image.ContentType.MimeType.ToLowerInvariant())
            {
                case "image/jpeg":
                    fileName += ".jpg";
                    break;
                case "image/png":
                    fileName += ".png";
                    break;
                case "image/gif":
                    fileName += ".gif";
                    break;
            }

            string path = Path.Combine(_tempDir, fileName);

            if (!File.Exists(path))
            {
                using (var output = File.Create(path))
                    image.Content.DecodeTo(output);
            }

            return "file://" + path.Replace('\\', '/');
        }

        void HtmlTagCallback(HtmlTagContext ctx, HtmlWriter htmlWriter)
        {
            if (ctx.TagId == HtmlTagId.Image && !ctx.IsEndTag && _stack.Count > 0)
            {
                ctx.WriteTag(htmlWriter, false);

                // replace the src attribute with a file:// URL
                foreach (var attribute in ctx.Attributes)
                {
                    if (attribute.Id == HtmlAttributeId.Src)
                    {

                        if (!TryGetImage(attribute.Value, out MimePart image))
                        {
                            htmlWriter.WriteAttribute(attribute);
                            continue;
                        }

                        var url = SaveImage(image, attribute.Value);

                        htmlWriter.WriteAttributeName(attribute.Name);
                        htmlWriter.WriteAttributeValue(url);
                    }
                    else
                        htmlWriter.WriteAttribute(attribute);
                }
            }
            else if (ctx.TagId == HtmlTagId.Body && !ctx.IsEndTag)
            {
                ctx.WriteTag(htmlWriter, false);

                // add and/or replace oncontextmenu="return false;"
                foreach (var attribute in ctx.Attributes)
                {
                    if (attribute.Name.ToLowerInvariant() == "oncontextmenu")
                        continue;

                    htmlWriter.WriteAttribute(attribute);
                }

                htmlWriter.WriteAttribute("oncontextmenu", "return false;");
            }
            else
            {
                // pass the tag through to the output
                ctx.WriteTag(htmlWriter, true);
            }
        }

        static void GetHeaderFooter(out string header, out string footer)
        {
            var format = UIStrings.HtmlFormatWrapper;
            int index = format.IndexOf("{0}");

            header = format.Substring(0, index);
            footer = format.Substring(index + 3);
        }

        protected override void VisitTextPart(TextPart entity)
        {
            TextConverter converter;
            if (_body != null)
            {
                // since we've already found the body, treat this as an attachment
                _attachments.Add(entity);
                return;
            }

            GetHeaderFooter(out string header, out string footer);

            if (entity.IsHtml)
            {
                converter = new HtmlToHtml
                {
                    Header = UIStrings.MarkOfTheWeb + Environment.NewLine,
                    HeaderFormat = HeaderFooterFormat.Html,
                    HtmlTagCallback = HtmlTagCallback
                };
            }
            else if (entity.IsFlowed)
            {
                var flowed = new FlowedToHtml
                {
                    Header = UIStrings.MarkOfTheWeb + Environment.NewLine + header,
                    HeaderFormat = HeaderFooterFormat.Html,
                    Footer = footer,
                    FooterFormat = HeaderFooterFormat.Html
                };

                if (entity.ContentType.Parameters.TryGetValue("delsp", out string delsp))
                    flowed.DeleteSpace = delsp.ToLowerInvariant() == "yes";

                converter = flowed;
            }
            else
            {
                converter = new TextToHtml
                {
                    Header = UIStrings.MarkOfTheWeb + Environment.NewLine + header,
                    HeaderFormat = HeaderFooterFormat.Html,
                    Footer = footer,
                    FooterFormat = HeaderFooterFormat.Html
                };
            }

            string text = entity.Text;

            _body = converter.Convert(entity.Text);
        }

        protected override void VisitTnefPart(TnefPart entity)
        {
            // extract any attachments in the MS-TNEF part
            _attachments.AddRange(entity.ExtractAttachments());
        }

        protected override void VisitMessagePart(MessagePart entity)
        {
            // treat message/rfc822 parts as attachments
            _attachments.Add(entity);
        }

        protected override void VisitMimePart(MimePart entity)
        {
            // realistically, if we've gotten this far, then we can treat this as an attachment
            // even if the IsAttachment property is false.
            _attachments.Add(entity);
        }
    }
}