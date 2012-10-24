/*  
 * Papercut
 *
 *  Copyright © 2008 - 2012 Ken Robertson
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

namespace Net.Mime
{
	#region Using

	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Net.Mime;

	#endregion

	/// <summary>
	/// This class is responsible for parsing a string array of lines containing a MIME message.
	/// </summary>
	public class MimeReader
	{
		#region Constants and Fields

		/// <summary>
		///   The header whitespace chars.
		/// </summary>
		private static readonly char[] HeaderWhitespaceChars = new[] { ' ', '\t' };

		/// <summary>
		///   The _entity.
		/// </summary>
		private readonly MimeEntity _entity;

		/// <summary>
		///   The _lines.
		/// </summary>
		private readonly Queue<string> _lines;

		#endregion

		#region Constructors and Destructors

		/// <summary>
		/// Initializes a new instance of the <see cref="MimeReader"/> class. 
		/// </summary>
		/// <param name="lines">
		/// The lines. 
		/// </param>
		public MimeReader(IEnumerable<string> lines)
			: this()
		{
			if (lines == null)
			{
				throw new ArgumentNullException("lines");
			}

			this._lines = new Queue<string>(lines);
		}

		/// <summary>
		/// Prevents a default instance of the <see cref="MimeReader"/> class from being created. 
		/// </summary>
		private MimeReader()
		{
			this._entity = new MimeEntity();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MimeReader"/> class. 
		/// </summary>
		/// <param name="entity">
		/// The entity. 
		/// </param>
		/// <param name="lines">
		/// The lines. 
		/// </param>
		private MimeReader(MimeEntity entity, Queue<string> lines)
			: this()
		{
			if (entity == null)
			{
				throw new ArgumentNullException("entity");
			}

			if (lines == null)
			{
				throw new ArgumentNullException("lines");
			}

			this._lines = lines;
			this._entity = new MimeEntity(entity);
		}

		#endregion

		#region Public Properties

		/// <summary>
		///   Gets the lines.
		/// </summary>
		/// <value> The lines. </value>
		public Queue<string> Lines
		{
			get
			{
				return this._lines;
			}
		}

		#endregion

		#region Public Methods and Operators

		/// <summary>
		/// Gets the type of the content.
		/// </summary>
		/// <param name="contentType">
		/// Type of the content. 
		/// </param>
		/// <returns>
		/// </returns>
		public static ContentType GetContentType(string contentType)
		{
			if (string.IsNullOrEmpty(contentType))
			{
				contentType = "text/plain; charset=us-ascii";
			}

			try
			{
				return new ContentType(contentType);
			}
			catch (FormatException f)
			{
				return new ContentType(contentType.Replace("\"", string.Empty).Replace(" = ", "="));
			}
		}

		/// <summary>
		/// Gets the type of the media main.
		/// </summary>
		/// <param name="mediaType">
		/// Type of the media. 
		/// </param>
		/// <returns>
		/// The get media main type. 
		/// </returns>
		public static string GetMediaMainType(string mediaType)
		{
			int separatorIndex = mediaType.IndexOf('/');
			return separatorIndex < 0 ? mediaType : mediaType.Substring(0, separatorIndex);
		}

		/// <summary>
		/// Gets the type of the media sub.
		/// </summary>
		/// <param name="mediaType">
		/// Type of the media. 
		/// </param>
		/// <returns>
		/// The get media sub type. 
		/// </returns>
		public static string GetMediaSubType(string mediaType)
		{
			int separatorIndex = mediaType.IndexOf('/');
			if (separatorIndex < 0)
			{
				return mediaType.Equals("text") ? "plain" : string.Empty;
			}
			if (mediaType.Length > separatorIndex)
			{
				return mediaType.Substring(separatorIndex + 1);
			}
			else
			{
				return GetMediaMainType(mediaType).Equals("text") ? "plain" : string.Empty;
			}
		}

		/// <summary>
		/// Gets the type of the media.
		/// </summary>
		/// <param name="mediaType">
		/// Type of the media. 
		/// </param>
		/// <returns>
		/// The get media type. 
		/// </returns>
		public static string GetMediaType(string mediaType)
		{
			return string.IsNullOrEmpty(mediaType) ? "text/plain" : mediaType.Trim();
		}

		/// <summary>
		/// Gets the transfer encoding.
		/// </summary>
		/// <param name="transferEncoding">
		/// The transfer encoding. 
		/// </param>
		/// <returns>
		/// </returns>
		/// <remarks>
		/// The transfer encoding determination follows the same rules as Peter Huber's article w/ the exception of not throwing exceptions when binary is provided as a transferEncoding. Instead it is left to the calling code to check for binary.
		/// </remarks>
		public static TransferEncoding GetTransferEncoding(string transferEncoding)
		{
			switch (transferEncoding.Trim().ToLowerInvariant())
			{
				case "7bit":
				case "8bit":
					return TransferEncoding.SevenBit;
				case "quoted-printable":
					return TransferEncoding.QuotedPrintable;
				case "base64":
					return TransferEncoding.Base64;
				case "binary":
				default:
					return TransferEncoding.Unknown;
			}
		}

		/// <summary>
		/// Creates the MIME entity.
		/// </summary>
		/// <returns>
		/// A mime entity containing 0 or more children representing the mime message. 
		/// </returns>
		public MimeEntity CreateMimeEntity()
		{
			this.ParseHeaders();

			this.ProcessHeaders();

			this.ParseBody();

			this.SetDecodedContentStream();

			return this._entity;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Adds the child entity.
		/// </summary>
		/// <param name="entity">
		/// The entity. 
		/// </param>
		/// <param name="lines">
		/// The lines. 
		/// </param>
		private void AddChildEntity(MimeEntity entity, Queue<string> lines)
		{
			/*if (entity == null)
			{
					return;
			}

			if (lines == null)
			{
					return;
			}*/
			var reader = new MimeReader(entity, lines);
			entity.Children.Add(reader.CreateMimeEntity());
		}

		/// <summary>
		/// Gets a byte[] of content for the provided string.
		/// </summary>
		/// <param name="content">
		/// The content. 
		/// </param>
		/// <returns>
		/// A byte[] containing content. 
		/// </returns>
		private byte[] GetBytes(string content)
		{
			using (var stream = new MemoryStream())
			{
				using (var writer = new StreamWriter(stream))
				{
					writer.Write(content);
				}

				return stream.ToArray();
			}
		}

		/// <summary>
		/// Parses the body.
		/// </summary>
		private void ParseBody()
		{
			if (this._entity.HasBoundary)
			{
				while (this._lines.Count > 0 && !string.Equals(this._lines.Peek(), this._entity.EndBoundary))
				{
					/*Check to verify the current line is not the same as the parent starting boundary.  
						 If it is the same as the parent starting boundary this indicates existence of a 
						 new child entity. Return and process the next child.*/
					if (this._entity.Parent != null && string.Equals(this._entity.Parent.StartBoundary, this._lines.Peek()))
					{
						return;
					}

					if (string.Equals(this._lines.Peek(), this._entity.StartBoundary))
					{
						this.AddChildEntity(this._entity, this._lines);
					}
					// Parse a new child mime part.
					else if (string.Equals(this._entity.ContentType.MediaType, MediaTypes.MessageRfc822, StringComparison.InvariantCultureIgnoreCase)
					         &&
					         string.Equals(
					         	this._entity.ContentDisposition.DispositionType, 
					         	DispositionTypeNames.Attachment, 
					         	StringComparison.InvariantCultureIgnoreCase))
					{
						/*If the content type is message/rfc822 the stop condition to parse headers has already been encountered.
						 But, a content type of message/rfc822 would have the message headers immediately following the mime
						 headers so we need to parse the headers for the attached message now.  This is done by creating
						 a new child entity.*/
						this.AddChildEntity(this._entity, this._lines);

						break;
					}
					else
					{
						this._entity.EncodedMessage.Append(string.Concat(this._lines.Dequeue(), "\r\n"));
					}

					// Append the message content.
				}
			}
			else
			{
				// Parse a multipart message.
				while (this._lines.Count > 0)
				{
					this._entity.EncodedMessage.Append(string.Concat(this._lines.Dequeue(), "\r\n"));
				}
			}

			// Parse a single part message.
		}

		/// <summary>
		/// Parse headers into _entity.Headers NameValueCollection.
		/// </summary>
		/// <returns>
		/// The parse headers. 
		/// </returns>
		private int ParseHeaders()
		{
			string lastHeader = string.Empty;
			string line = string.Empty;

			// the first empty line is the end of the headers.
			while (this._lines.Count > 0 && !string.IsNullOrEmpty(this._lines.Peek()))
			{
				line = this._lines.Dequeue();

				// if a header line starts with a space or tab then it is a continuation of the
				// previous line.
				if (line.StartsWith(" ") || line.StartsWith(Convert.ToString('\t')))
				{
					this._entity.Headers[lastHeader] = string.Concat(this._entity.Headers[lastHeader], line);
					continue;
				}

				int separatorIndex = line.IndexOf(':');

				if (separatorIndex < 0)
				{
					Debug.WriteLine("Invalid header: " + line);
					continue;
				}

				// This is an invalid header field.  Ignore this line.
				string headerName = line.Substring(0, separatorIndex);
				string headerValue = line.Substring(separatorIndex + 1).Trim(HeaderWhitespaceChars);

				this._entity.Headers.Add(headerName.ToLower(), headerValue);
				lastHeader = headerName;
			}

			if (this._lines.Count > 0)
			{
				this._lines.Dequeue();
			}

			// remove closing header CRLF.
			return this._entity.Headers.Count;
		}

		/// <summary>
		/// Processes mime specific headers.
		/// </summary>
		private void ProcessHeaders()
		{
			foreach (string key in this._entity.Headers.AllKeys)
			{
				switch (key)
				{
					case "content-description":
						this._entity.ContentDescription = this._entity.Headers[key];
						break;
					case "content-disposition":
						this._entity.ContentDisposition = new ContentDisposition(this._entity.Headers[key]);
						break;
					case "content-id":
						this._entity.ContentId = this._entity.Headers[key];
						break;
					case "content-transfer-encoding":
						this._entity.TransferEncoding = this._entity.Headers[key];
						this._entity.ContentTransferEncoding = GetTransferEncoding(this._entity.Headers[key]);
						break;
					case "content-type":
						this._entity.SetContentType(GetContentType(this._entity.Headers[key]));
						break;
					case "mime-version":
						this._entity.MimeVersion = this._entity.Headers[key];
						break;
				}
			}
		}

		/// <summary>
		/// Sets the decoded content stream by decoding the EncodedMessage and writing it to the entity content stream.
		/// </summary>
		/// <param name="entity">
		/// The entity containing the encoded message. 
		/// </param>
		private void SetDecodedContentStream()
		{
			switch (this._entity.ContentTransferEncoding)
			{
				case TransferEncoding.Base64:
					this._entity.Content = new MemoryStream(Convert.FromBase64String(this._entity.EncodedMessage.ToString()), false);
					break;

				case TransferEncoding.QuotedPrintable:
					this._entity.Content =
						new MemoryStream(this.GetBytes(QuotedPrintableEncoding.Decode(this._entity.EncodedMessage.ToString())), false);
					break;

				case TransferEncoding.SevenBit:
				default:
					this._entity.Content = new MemoryStream(this.GetBytes(this._entity.EncodedMessage.ToString()), false);
					break;
			}
		}

		#endregion
	}
}