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

namespace Papercut.SMTP.Mime
{
	#region Using

    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net.Mail;
    using System.Net.Mime;
    using System.Text;

    #endregion

	/// <summary>
	/// This class represents a Mime entity.
	/// </summary>
	public class MimeEntity
	{
		#region Constants and Fields

		/// <summary>
		///   The _children.
		/// </summary>
		private readonly List<MimeEntity> _children;

		/// <summary>
		///   The _encoded message.
		/// </summary>
		private readonly StringBuilder _encodedMessage;

		/// <summary>
		///   The _headers.
		/// </summary>
		private readonly NameValueCollection _headers;

		/// <summary>
		///   The _start boundary.
		/// </summary>
		private readonly string _startBoundary;

		/// <summary>
		///   The _content type.
		/// </summary>
		private ContentType _contentType;

		/// <summary>
		///   The _media main type.
		/// </summary>
		private string _mediaMainType;

		/// <summary>
		///   The _media sub type.
		/// </summary>
		private string _mediaSubType;

		#endregion

		#region Constructors and Destructors

		/// <summary>
		/// Initializes a new instance of the <see cref="MimeEntity"/> class. 
		/// </summary>
		public MimeEntity()
		{
			this._children = new List<MimeEntity>();
			this._headers = new NameValueCollection();
			this._contentType = MimeReader.GetContentType(string.Empty);
			this.Parent = null;
			this._encodedMessage = new StringBuilder();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MimeEntity"/> class. 
		/// </summary>
		/// <param name="parent">
		/// The parent. 
		/// </param>
		public MimeEntity(MimeEntity parent)
			: this()
		{
			if (parent == null)
			{
				throw new ArgumentNullException("parent");
			}

			this.Parent = parent;
			this._startBoundary = parent.StartBoundary;
		}

		#endregion

		#region Public Properties

		/// <summary>
		///   Gets the children.
		/// </summary>
		/// <value> The children. </value>
		public List<MimeEntity> Children
		{
			get
			{
				return this._children;
			}
		}

		/// <summary>
		///   Gets or sets the content.
		/// </summary>
		/// <value> The content. </value>
		public MemoryStream Content { get; internal set; }

		/// <summary>
		///   Gets or sets the content description.
		/// </summary>
		/// <value> The content description. </value>
		public string ContentDescription { get; set; }

		/// <summary>
		///   Gets or sets the content disposition.
		/// </summary>
		/// <value> The content disposition. </value>
		public ContentDisposition ContentDisposition { get; set; }

		/// <summary>
		///   Gets or sets the content id.
		/// </summary>
		/// <value> The content id. </value>
		public string ContentId { get; set; }

		/// <summary>
		///   Gets or sets the content transfer encoding.
		/// </summary>
		/// <value> The content transfer encoding. </value>
		public TransferEncoding ContentTransferEncoding { get; set; }

		/// <summary>
		///   Gets the type of the content.
		/// </summary>
		/// <value> The type of the content. </value>
		public ContentType ContentType
		{
			get
			{
				return this._contentType;
			}
		}

		/// <summary>
		///   Gets the encoded message.
		/// </summary>
		/// <value> The encoded message. </value>
		public StringBuilder EncodedMessage
		{
			get
			{
				return this._encodedMessage;
			}
		}

		/// <summary>
		///   Gets the end boundary.
		/// </summary>
		/// <value> The end boundary. </value>
		public string EndBoundary
		{
			get
			{
				return string.Concat(this.StartBoundary, "--");
			}
		}

		/// <summary>
		///   Gets the headers.
		/// </summary>
		/// <value> The headers. </value>
		public NameValueCollection Headers
		{
			get
			{
				return this._headers;
			}
		}

		/// <summary>
		///   Gets the type of the media main.
		/// </summary>
		/// <value> The type of the media main. </value>
		public string MediaMainType
		{
			get
			{
				return this._mediaMainType;
			}
		}

		/// <summary>
		///   Gets the type of the media sub.
		/// </summary>
		/// <value> The type of the media sub. </value>
		public string MediaSubType
		{
			get
			{
				return this._mediaSubType;
			}
		}

		/// <summary>
		///   Gets or sets the MIME version.
		/// </summary>
		/// <value> The MIME version. </value>
		public string MimeVersion { get; set; }

		/// <summary>
		///   Gets or sets the parent.
		/// </summary>
		/// <value> The parent. </value>
		public MimeEntity Parent { get; set; }

		/// <summary>
		///   Gets the start boundary.
		/// </summary>
		/// <value> The start boundary. </value>
		public string StartBoundary
		{
			get
			{
				if (string.IsNullOrEmpty(this._startBoundary) || !string.IsNullOrEmpty(this._contentType.Boundary))
				{
					return string.Concat("--", this._contentType.Boundary);
				}

				return this._startBoundary;
			}
		}

		/// <summary>
		///   Gets or sets the transfer encoding.
		/// </summary>
		/// <value> The transfer encoding. </value>
		public string TransferEncoding { get; set; }

		#endregion

		#region Properties

		/// <summary>
		///   Gets a value indicating whether this instance has boundary.
		/// </summary>
		/// <value> <c>true</c> if this instance has boundary; otherwise, <c>false</c> . </value>
		internal bool HasBoundary
		{
			get
			{
				return (!string.IsNullOrEmpty(this._contentType.Boundary)) || (!string.IsNullOrEmpty(this._startBoundary));
			}
		}

		#endregion

		#region Public Methods and Operators

		/// <summary>
		/// Trims the brackets.
		/// </summary>
		/// <param name="value">
		/// The value. 
		/// </param>
		/// <returns>
		/// The trim brackets. 
		/// </returns>
		public static string TrimBrackets(string value)
		{
			if (value == null)
			{
				return value;
			}

			if (value.StartsWith("<") && value.EndsWith(">"))
			{
				return value.Trim('<', '>');
			}

			return value;
		}

		/// <summary>
		/// Gets the body encoding.
		/// </summary>
		/// <param name="contentType">
		/// Type of the content. 
		/// </param>
		public Encoding GetEncoding()
		{
			if (string.IsNullOrEmpty(this.ContentType.CharSet))
			{
				return Encoding.ASCII;
			}
			else
			{
				try
				{
					return Encoding.GetEncoding(this.ContentType.CharSet);
				}
				catch (ArgumentException)
				{
					return Encoding.ASCII;
				}
			}
		}

		/// <summary>
		/// Toes the mail message ex.
		/// </summary>
		/// <returns>
		/// </returns>
		public MailMessageEx ToMailMessageEx()
		{
			return this.ToMailMessageEx(this);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Sets the type of the content.
		/// </summary>
		/// <param name="contentType">
		/// Type of the content. 
		/// </param>
		internal void SetContentType(ContentType contentType)
		{
			this._contentType = contentType;
			this._contentType.MediaType = MimeReader.GetMediaType(contentType.MediaType);
			this._mediaMainType = MimeReader.GetMediaMainType(contentType.MediaType);
			this._mediaSubType = MimeReader.GetMediaSubType(contentType.MediaType);
		}

		/// <summary>
		/// The is attachment.
		/// </summary>
		/// <param name="child">
		/// The child. 
		/// </param>
		/// <returns>
		/// The is attachment. 
		/// </returns>
		private static bool IsAttachment(MimeEntity child)
		{
			return (child.ContentDisposition != null)
			       &&
			       string.Equals(
			       	child.ContentDisposition.DispositionType, 
			       	DispositionTypeNames.Attachment, 
			       	StringComparison.InvariantCultureIgnoreCase);
		}

		/// <summary>
		/// Builds the multi part message.
		/// </summary>
		/// <param name="entity">
		/// The entity. 
		/// </param>
		/// <param name="message">
		/// The message. 
		/// </param>
		private void BuildMultiPartMessage(MimeEntity entity, MailMessageEx message)
		{
			foreach (MimeEntity child in entity.Children)
			{
				if (string.Equals(
					child.ContentType.MediaType, MediaTypes.MultipartAlternative, StringComparison.InvariantCultureIgnoreCase)
				    ||
				    string.Equals(
				    	child.ContentType.MediaType, MediaTypes.MultipartMixed, StringComparison.InvariantCultureIgnoreCase)
				    ||
				    string.Equals(
				    	child.ContentType.MediaType, MediaTypes.MultipartRelated, StringComparison.InvariantCultureIgnoreCase))
				{
					this.BuildMultiPartMessage(child, message);
				}
					
					// if the message is mulitpart/alternative or multipart/mixed then the entity will have children needing parsed.
				else if (!IsAttachment(child)
				         &&
				         (string.Equals(child.ContentType.MediaType, MediaTypes.TextPlain)
				          || string.Equals(child.ContentType.MediaType, MediaTypes.TextHtml)))
				{
					message.AlternateViews.Add(this.CreateAlternateView(child));
					this.SetMessageBody(message, child);
				}
					
					// add the alternative views.
				else if (string.Equals(
					child.ContentType.MediaType, MediaTypes.MessageRfc822, StringComparison.InvariantCultureIgnoreCase)
				         &&
				         string.Equals(
				         	child.ContentDisposition.DispositionType, 
				         	DispositionTypeNames.Attachment, 
				         	StringComparison.InvariantCultureIgnoreCase))
				{
					message.Children.Add(this.ToMailMessageEx(child));
				}
					
					// create a child message and 
				else if (IsAttachment(child))
				{
					message.Attachments.Add(this.CreateAttachment(child));
				}
				else if (string.Equals(
					entity.ContentType.MediaType, MediaTypes.MultipartRelated, StringComparison.InvariantCultureIgnoreCase)
				         ||
				         string.Equals(
				         	entity.ContentType.MediaType, MediaTypes.MultipartMixed, StringComparison.InvariantCultureIgnoreCase))
				{
					message.Attachments.Add(this.CreateAttachment(child));
				}
			}
		}

		/// <summary>
		/// Builds the single part message.
		/// </summary>
		/// <param name="entity">
		/// The entity. 
		/// </param>
		/// <param name="message">
		/// The message. 
		/// </param>
		private void BuildSinglePartMessage(MimeEntity entity, MailMessageEx message)
		{
			this.SetMessageBody(message, entity);
		}

		/// <summary>
		/// Creates the alternate view.
		/// </summary>
		/// <param name="view">
		/// The view. 
		/// </param>
		/// <returns>
		/// </returns>
		private AlternateView CreateAlternateView(MimeEntity view)
		{
			var alternateView = new AlternateView(view.Content, view.ContentType);
			alternateView.TransferEncoding = view.ContentTransferEncoding;
			alternateView.ContentId = TrimBrackets(view.ContentId);
			return alternateView;
		}

		/// <summary>
		/// Creates the attachment.
		/// </summary>
		/// <param name="entity">
		/// The entity. 
		/// </param>
		/// <returns>
		/// </returns>
		private Attachment CreateAttachment(MimeEntity entity)
		{
			var attachment = new Attachment(entity.Content, entity.ContentType);

			if (entity.ContentDisposition != null)
			{
				attachment.ContentDisposition.Parameters.Clear();
				foreach (string key in entity.ContentDisposition.Parameters.Keys)
				{
					attachment.ContentDisposition.Parameters.Add(key, entity.ContentDisposition.Parameters[key]);
				}

				attachment.ContentDisposition.CreationDate = entity.ContentDisposition.CreationDate;
				attachment.ContentDisposition.DispositionType = entity.ContentDisposition.DispositionType;
				attachment.ContentDisposition.FileName = entity.ContentDisposition.FileName;
				attachment.ContentDisposition.Inline = entity.ContentDisposition.Inline;
				attachment.ContentDisposition.ModificationDate = entity.ContentDisposition.ModificationDate;
				attachment.ContentDisposition.ReadDate = entity.ContentDisposition.ReadDate;
				attachment.ContentDisposition.Size = entity.ContentDisposition.Size;
			}

			if (!string.IsNullOrEmpty(entity.ContentId))
			{
				attachment.ContentId = TrimBrackets(entity.ContentId);
			}

			attachment.TransferEncoding = entity.ContentTransferEncoding;

			return attachment;
		}

		/// <summary>
		/// Decodes the bytes.
		/// </summary>
		/// <param name="buffer">
		/// The buffer. 
		/// </param>
		/// <param name="encoding">
		/// The encoding. 
		/// </param>
		/// <returns>
		/// The decode bytes. 
		/// </returns>
		private string DecodeBytes(byte[] buffer, Encoding encoding)
		{
			if (buffer == null)
			{
				return null;
			}

			if (encoding == null)
			{
				encoding = Encoding.UTF7;
			}

			// email defaults to 7bit.  
			return encoding.GetString(buffer);
		}

		/// <summary>
		/// Sets the message body.
		/// </summary>
		/// <param name="message">
		/// The message. 
		/// </param>
		/// <param name="child">
		/// The child. 
		/// </param>
		private void SetMessageBody(MailMessageEx message, MimeEntity child)
		{
			Encoding encoding = child.GetEncoding();
			message.Body = this.DecodeBytes(child.Content.ToArray(), encoding);
			message.BodyEncoding = encoding;
			message.IsBodyHtml = string.Equals(
				MediaTypes.TextHtml, child.ContentType.MediaType, StringComparison.InvariantCultureIgnoreCase);
		}

		/// <summary>
		/// Toes the mail message ex.
		/// </summary>
		/// <param name="entity">
		/// The entity. 
		/// </param>
		/// <returns>
		/// </returns>
		private MailMessageEx ToMailMessageEx(MimeEntity entity)
		{
			if (entity == null)
			{
				throw new ArgumentNullException("entity");
			}

			// parse standard headers and create base email.
			MailMessageEx message = MailMessageEx.CreateMailMessageFromEntity(entity);

			if (!string.IsNullOrEmpty(entity.ContentType.Boundary))
			{
				message = MailMessageEx.CreateMailMessageFromEntity(entity);
				this.BuildMultiPartMessage(entity, message);
			}
				
				// parse multipart message into sub parts.
			else if (string.Equals(
				entity.ContentType.MediaType, MediaTypes.MessageRfc822, StringComparison.InvariantCultureIgnoreCase))
			{
				// use the first child to create the multipart message.
				if (entity.Children.Count < 0)
				{
					throw new Exception("Invalid child count on message/rfc822 entity.");
				}

				// create the mail message from the first child because it will
				// contain all of the mail headers.  The entity in this state
				// only contains simple content type headers indicating, disposition, type and description.
				// This means we can't create the mail message from this type as there is no 
				// internet mail headers attached to this entity.
				message = MailMessageEx.CreateMailMessageFromEntity(entity.Children[0]);
				this.BuildMultiPartMessage(entity, message);
			}
				
				// parse nested message.
			else
			{
				message = MailMessageEx.CreateMailMessageFromEntity(entity);
				this.BuildSinglePartMessage(entity, message);
			}

			// Create single part message.
			return message;
		}

		#endregion
	}
}