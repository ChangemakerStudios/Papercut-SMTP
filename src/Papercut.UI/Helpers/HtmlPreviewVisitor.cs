// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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
            image = null;

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
                return false;
            }

            foreach (var item in this._stack.ToArray().Reverse())
            {
                int index = item.IndexOf(uri);
                
                if (index == IndexNotFound)
                    continue;

                image = item[index] as MimePart;

                return image != null;
            }

            return false;
        }

        const int IndexNotFound = -1;

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

            return $"file://{path.Replace('\\', '/')}";
        }

        void HtmlTagCallback(HtmlTagContext ctx, HtmlWriter htmlWriter)
        {
            void WriteTagAsIs()
            {
                // pass the tag through to the output
                ctx.WriteTag(htmlWriter, true);
            }

            switch (ctx.TagId)
            {
                case HtmlTagId.Head when !ctx.IsEndTag:
                    ctx.WriteTag(htmlWriter, false);
                    AddMetaCompatibleIEEdge(htmlWriter);

                    break;
                    
                case HtmlTagId.Image when !ctx.IsEndTag && this._stack.Count > 0:
                    LinkImageTag(ctx, htmlWriter);

                    break;
                case HtmlTagId.Body when !ctx.IsEndTag:
                    RemoveContextMenuFromBodyTag(ctx, htmlWriter);
                    break;
                default:
                    
                    WriteTagAsIs();

                    break;
            }
        }

        private void AddMetaCompatibleIEEdge(HtmlWriter htmlWriter)
        {
            htmlWriter.WriteStartTag(HtmlTagId.Meta);
            htmlWriter.WriteAttribute("http-equiv", "X-UA-Compatible");
            htmlWriter.WriteAttribute("content", "IE=Edge");
            htmlWriter.WriteEndTag(HtmlTagId.Meta);
        }

        private static void RemoveContextMenuFromBodyTag(HtmlTagContext ctx, HtmlWriter htmlWriter)
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

        private void LinkImageTag(HtmlTagContext ctx, HtmlWriter htmlWriter)
        {
            ctx.WriteTag(htmlWriter, false);

            // replace the src attribute with a file:// URL
            foreach (var attribute in ctx.Attributes)
            {
                if (attribute.Id == HtmlAttributeId.Src)
                {
                    if (!this.TryGetImage(attribute.Value, out MimePart image))
                    {
                        htmlWriter.WriteAttribute(attribute);
                        continue;
                    }

                    var url = this.SaveImage(image, attribute.Value);

                    htmlWriter.WriteAttributeName(attribute.Name);
                    htmlWriter.WriteAttributeValue(url);
                }
                else
                    htmlWriter.WriteAttribute(attribute);
            }
        }

        static (string Before,string After) GetBeforeAfterFormatWrapper(string formatWrapper)
        {
            //var format = UIStrings.TextToHtmlFormatWrapper;
            int index = formatWrapper.IndexOf("{0}");

            return (formatWrapper.Substring(0, index), formatWrapper.Substring(index + 3));
        }

        protected override void VisitTextPart(TextPart entity)
        {
            if (_body != null)
            {
                // since we've already found the body, treat this as an attachment
                _attachments.Add(entity);
                return;
            }

            if (entity.IsHtml)
            {
                this.SetHtmlToBody(entity);
            }
            else if (entity.IsFlowed)
            {
                this.SetFlowedHtmlToBody(entity);
            }
            else
            {
                this.SetTextToHtml(entity);
            }
        }

        private void SetTextToHtml(TextPart entity)
        {
            var beforeAfter = GetBeforeAfterFormatWrapper(UIStrings.TextToHtmlFormatWrapper);

            var converter = new TextToHtml
                        {
                            Header = $"{UIStrings.MarkOfTheWeb}{Environment.NewLine}{beforeAfter.Before}",
                            HeaderFormat = HeaderFooterFormat.Html,
                            Footer = beforeAfter.After,
                            FooterFormat = HeaderFooterFormat.Html
                        };

            _body = converter.Convert(entity.Text);
        }

        private void SetFlowedHtmlToBody(TextPart entity)
        {
            var beforeAfter = GetBeforeAfterFormatWrapper(UIStrings.HtmlToHtmlFormatWrapper);

            var convertor = new FlowedToHtml
                            {
                                Header = $"{UIStrings.MarkOfTheWeb}{Environment.NewLine}{beforeAfter.Before}",
                                HeaderFormat = HeaderFooterFormat.Html,
                                Footer = beforeAfter.After,
                                FooterFormat = HeaderFooterFormat.Html
                            };

            if (entity.ContentType.Parameters.TryGetValue("delsp", out string delsp))
            {
                convertor.DeleteSpace = delsp.ToLowerInvariant() == "yes";
            }

            _body = convertor.Convert(entity.Text);
        }

        private void SetHtmlToBody(TextPart entity)
        {
            var converter = new HtmlToHtml
                            {
                                Header = $"{UIStrings.MarkOfTheWeb}{Environment.NewLine}",
                                HeaderFormat = HeaderFooterFormat.Html,
                                HtmlTagCallback = this.HtmlTagCallback
                            };

            var html = entity.Text;

            if (!html.Contains("<head>") || !html.Contains("<body>"))
            {
                var beforeAfter = GetBeforeAfterFormatWrapper(UIStrings.HtmlToHtmlFormatWrapper);

                _body = converter.Convert(beforeAfter.Before + html + beforeAfter.After);

            }
            else _body = converter.Convert(html);
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