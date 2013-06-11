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
	/// <summary>
	/// The mail headers.
	/// </summary>
	public static class MailHeaders
	{
		#region Constants and Fields

		/// <summary>
		///   The bcc.
		/// </summary>
		public const string Bcc = "bcc";

		/// <summary>
		///   The cc.
		/// </summary>
		public const string Cc = "cc";

		/// <summary>
		///   The date.
		/// </summary>
		public const string Date = "date";

		/// <summary>
		///   The from.
		/// </summary>
		public const string From = "from";

		/// <summary>
		///   The importance.
		/// </summary>
		public const string Importance = "importance";

		/// <summary>
		///   The in reply to.
		/// </summary>
		public const string InReplyTo = "in-reply-to";

		/// <summary>
		///   The message id.
		/// </summary>
		public const string MessageId = "message-id";

		/// <summary>
		///   The received.
		/// </summary>
		public const string Received = "received";

		/// <summary>
		///   The reply to.
		/// </summary>
		public const string ReplyTo = "reply-to";

		/// <summary>
		///   The subject.
		/// </summary>
		public const string Subject = "subject";

		/// <summary>
		///   The to.
		/// </summary>
		public const string To = "to";

		#endregion
	}
}