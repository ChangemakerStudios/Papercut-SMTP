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

	using System.Net.Mime;

	#endregion

	/// <summary>
	/// The media types.
	/// </summary>
	public static class MediaTypes
	{
		#region Constants and Fields

		/// <summary>
		///   The alternative.
		/// </summary>
		public static readonly string Alternative;

		/// <summary>
		///   The application.
		/// </summary>
		public static readonly string Application;

		/// <summary>
		///   The message.
		/// </summary>
		public static readonly string Message;

		/// <summary>
		///   The message rfc 822.
		/// </summary>
		public static readonly string MessageRfc822;

		/// <summary>
		///   The mixed.
		/// </summary>
		public static readonly string Mixed;

		/// <summary>
		///   The multipart.
		/// </summary>
		public static readonly string Multipart;

		/// <summary>
		///   The multipart alternative.
		/// </summary>
		public static readonly string MultipartAlternative;

		/// <summary>
		///   The multipart mixed.
		/// </summary>
		public static readonly string MultipartMixed;

		/// <summary>
		///   The multipart related.
		/// </summary>
		public static readonly string MultipartRelated;

		/// <summary>
		///   The rfc 822.
		/// </summary>
		public static readonly string Rfc822;

		/// <summary>
		///   The text html.
		/// </summary>
		public static readonly string TextHtml;

		/// <summary>
		///   The text plain.
		/// </summary>
		public static readonly string TextPlain;

		/// <summary>
		///   The text rich.
		/// </summary>
		public static readonly string TextRich;

		/// <summary>
		///   The text xml.
		/// </summary>
		public static readonly string TextXml;

		#endregion

		#region Constructors and Destructors

		/// <summary>
		/// Initializes static members of the <see cref="MediaTypes"/> class. 
		/// </summary>
		static MediaTypes()
		{
			Multipart = "multipart";
			Mixed = "mixed";
			Alternative = "alternative";
			Message = "message";
			Rfc822 = "rfc822";

			MultipartMixed = string.Concat(Multipart, "/", Mixed);
			MultipartAlternative = string.Concat(Multipart, "/", Alternative);
			MultipartRelated = string.Concat(Multipart, "/related");

			MessageRfc822 = string.Concat(Message, "/", Rfc822);

			TextPlain = MediaTypeNames.Text.Plain;
			TextHtml = MediaTypeNames.Text.Html;
			TextRich = MediaTypeNames.Text.RichText;
			TextXml = MediaTypeNames.Text.Xml;
		}

		#endregion
	}
}