using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using MimeKit;
using MimeKit.Text;
using MimeKit.Tnef;

using Papercut.Properties;

namespace Papercut.Helpers
{
	class HtmlPreviewVisitor : MimeVisitor
	{
		List<MultipartRelated> stack = new List<MultipartRelated> ();
		List<MimeEntity> attachments = new List<MimeEntity> ();
		readonly string tempDir;
		string body;

		public HtmlPreviewVisitor (string tempDirectory)
		{
			tempDir = tempDirectory;
		}

		public IList<MimeEntity> Attachments {
			get { return attachments; }
		}

		public string HtmlBody {
			get { return body ?? string.Empty; }
		}

		protected override void VisitMultipartAlternative (MultipartAlternative alternative)
		{
			// walk the multipart/alternative children backwards from greatest level of faithfulness to the least faithful
			for (int i = alternative.Count - 1; i >= 0 && body == null; i--)
				alternative[i].Accept (this);
		}

		protected override void VisitMultipartRelated (MultipartRelated related)
		{
			var root = related.Root;

			stack.Add (related);
			root.Accept (this);
			stack.RemoveAt (stack.Count - 1);
		}

		bool TryGetImage (string url, out MimePart image)
		{
			UriKind kind;
			int index;
			Uri uri;

			if (Uri.IsWellFormedUriString (url, UriKind.Absolute))
				kind = UriKind.Absolute;
			else if (Uri.IsWellFormedUriString (url, UriKind.Relative))
				kind = UriKind.Relative;
			else
				kind = UriKind.RelativeOrAbsolute;

			try {
				uri = new Uri (url, kind);
			} catch {
				image = null;
				return false;
			}

			for (int i = stack.Count - 1; i >= 0; i--) {
				if ((index = stack[i].IndexOf (uri)) == -1)
					continue;

				image = stack[i][index] as MimePart;
				return image != null;
			}

			image = null;

			return false;
		}

		string SaveImage (MimePart image, string url)
		{
			string fileName = url.Replace (':', '_').Replace ('\\', '_').Replace ('/', '_');

			// try to add a file extension for niceness
			switch (image.ContentType.MimeType.ToLowerInvariant ()) {
			case "image/jpeg": fileName += ".jpg"; break;
			case "image/png": fileName += ".png"; break;
			case "image/gif": fileName += ".gif"; break;
			}

			string path = Path.Combine (tempDir, fileName);

			if (!File.Exists (path)) {
				using (var output = File.Create (path))
					image.ContentObject.DecodeTo (output);
			}

			return "file://" + path.Replace ('\\', '/');
		}

		void HtmlTagCallback (HtmlTagContext ctx, HtmlWriter htmlWriter)
		{
			if (ctx.TagId == HtmlTagId.Image && !ctx.IsEndTag && stack.Count > 0) {
				if (ctx.IsEmptyElementTag)
					htmlWriter.WriteEmptyElementTag (ctx.TagName);
				else
					htmlWriter.WriteStartTag (ctx.TagName);

				// replace the src attribute with a file:// URL
				foreach (var attribute in ctx.Attributes) {
					if (attribute.Id == HtmlAttributeId.Src) {
						MimePart image;
						string url;

						if (!TryGetImage (attribute.Value, out image)) {
							htmlWriter.WriteAttribute (attribute);
							continue;
						}

						url = SaveImage (image, attribute.Value);

						htmlWriter.WriteAttributeName (attribute.Name);
						htmlWriter.WriteAttributeValue (url);
					} else {
						htmlWriter.WriteAttribute (attribute);
					}
				}
			} else {
				// pass the tag through to the output
				ctx.WriteTag (htmlWriter, true);

				if (ctx.TagId == HtmlTagId.Body && !ctx.IsEndTag) {
					// add oncontextmenu="return false;"
					htmlWriter.WriteAttributeName ("oncontextmenu");
					htmlWriter.WriteAttributeValue ("return false;");
				}
			}
		}

		static void GetHeaderFooter (out string header, out string footer)
		{
			var format = UIStrings.HtmlFormatWrapper;
			int index = format.IndexOf ("{0}");

			header = format.Substring (0, index);
			footer = format.Substring (index + 3);
		}

		protected override void VisitTextPart (TextPart entity)
		{
			TextConverter converter;
			string header, footer;

			if (body != null) {
				// since we've already found the body, treat this as an attachment
				attachments.Add (entity);
				return;
			}

			GetHeaderFooter (out header, out footer);

			if (entity.IsHtml) {
				converter = new HtmlToHtml {
					Header = UIStrings.MarkOfTheWeb,
					HeaderFormat = HeaderFooterFormat.Html,
					HtmlTagCallback = HtmlTagCallback
				};
			} else if (entity.IsFlowed) {
				var flowed = new FlowedToHtml {
					Header = UIStrings.MarkOfTheWeb + header,
					HeaderFormat = HeaderFooterFormat.Html,
					Footer = footer,
					FooterFormat = HeaderFooterFormat.Html
				};
				string delsp;

				if (entity.ContentType.Parameters.TryGetValue ("delsp", out delsp))
					flowed.DeleteSpace = delsp.ToLowerInvariant () == "yes";

				converter = flowed;
			} else {
				converter = new TextToHtml {
					Header = UIStrings.MarkOfTheWeb + header,
					HeaderFormat = HeaderFooterFormat.Html,
					Footer = footer,
					FooterFormat = HeaderFooterFormat.Html
				};
			}

			string text = entity.Text;

			body = converter.Convert (entity.Text);
		}

		protected override void VisitTnefPart (TnefPart entity)
		{
			// extract any attachments in the MS-TNEF part
			attachments.AddRange (entity.ExtractAttachments ());
		}

		protected override void VisitMessagePart (MessagePart entity)
		{
			// treat message/rfc822 parts as attachments
			attachments.Add (entity);
		}

		protected override void VisitMimePart (MimePart entity)
		{
			// realistically, if we've gotten this far, then we can treat this as an attachment
			// even if the IsAttachment property is false.
			attachments.Add (entity);
		}
	}
}
