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
    using System.Linq;
    using System.Net.Mail;
    using System.Net.Mime;
    using System.Text;
    using System.Text.RegularExpressions;

    using Papercut.SMTP;

    #endregion

    /// <summary>
    /// This class adds a few internet mail headers not already exposed by the System.Net.MailMessage. It also provides support to encapsulate the nested mail attachments in the Children collection.
    /// </summary>
    public class MailMessageEx : MailMessage
    {
        #region Constants and Fields

        /// <summary>
        ///   The address delimiters.
        /// </summary>
        private static readonly char[] AddressDelimiters = new[] { ',', ';' };

        /// <summary>
        ///   The _children.
        /// </summary>
        private readonly List<MailMessageEx> _children;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MailMessageEx"/> class. 
        /// </summary>
        public MailMessageEx()
        {
            this._children = new List<MailMessageEx>();
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets the children MailMessage attachments.
        /// </summary>
        /// <value> The children MailMessage attachments. </value>
        public List<MailMessageEx> Children
        {
            get
            {
                return this._children;
            }
        }

        /// <summary>
        ///   Gets the content description.
        /// </summary>
        /// <value> The content description. </value>
        public string ContentDescription
        {
            get
            {
                return this.GetHeader(MimeHeaders.ContentDescription);
            }
        }

        /// <summary>
        ///   Gets the content disposition.
        /// </summary>
        /// <value> The content disposition. </value>
        public ContentDisposition ContentDisposition
        {
            get
            {
                string contentDisposition = this.GetHeader(MimeHeaders.ContentDisposition);
                return string.IsNullOrEmpty(contentDisposition) ? null : new ContentDisposition(contentDisposition);
            }
        }

        /// <summary>
        ///   Gets the content id.
        /// </summary>
        /// <value> The content id. </value>
        public string ContentId
        {
            get
            {
                return this.GetHeader(MimeHeaders.ContentId);
            }
        }

        /// <summary>
        ///   Gets the type of the content.
        /// </summary>
        /// <value> The type of the content. </value>
        public ContentType ContentType
        {
            get
            {
                string contentType = this.GetHeader(MimeHeaders.ContentType);
                return string.IsNullOrEmpty(contentType) ? null : MimeReader.GetContentType(contentType);
            }
        }

        /// <summary>
        ///   Gets the delivery date.
        /// </summary>
        /// <value> The delivery date. </value>
        public DateTime DeliveryDate
        {
            get
            {
                string date = this.GetHeader(MailHeaders.Date);

                return Util.TryParseSTMPDateTime(date) ?? DateTime.MinValue;
            }
        }

        /// <summary>
        ///   Gets the message id.
        /// </summary>
        /// <value> The message id. </value>
        public string MessageId
        {
            get
            {
                return this.GetHeader(MailHeaders.MessageId);
            }
        }

        /// <summary>
        ///   Gets or sets the message number of the MailMessage on the POP3 server.
        /// </summary>
        /// <value> The message number. </value>
        public int MessageNumber { get; internal set; }

        /// <summary>
        ///   Gets the MIME version.
        /// </summary>
        /// <value> The MIME version. </value>
        public string MimeVersion
        {
            get
            {
                return this.GetHeader(MimeHeaders.MimeVersion);
            }
        }

        /// <summary>
        ///   Gets or sets Octets.
        /// </summary>
        public long Octets { get; set; }

        /// <summary>
        ///   Gets ReplyToMessageId.
        /// </summary>
        public string ReplyToMessageId
        {
            get
            {
                return this.GetHeader(MailHeaders.InReplyTo, true);
            }
        }

        /// <summary>
        ///   Gets the return address.
        /// </summary>
        /// <value> The return address. </value>
        public MailAddress ReturnAddress
        {
            get
            {
                MailAddress address;

                TryParseMailAddress(this.GetHeader(MailHeaders.ReplyTo), out address);

                return address;
            }
        }

        /// <summary>
        ///   Gets the routing.
        /// </summary>
        /// <value> The routing. </value>
        public string Routing
        {
            get
            {
                return this.GetHeader(MailHeaders.Received);
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Attempts to parse an email address as a mail address.
        /// </summary>
        /// <param name="email">
        /// The address. 
        /// </param>
        /// <param name="parsedMailAddress">Successfully parsed MailAddress or null if failure.</param>
        /// <returns>
        /// </returns>
        public static bool TryParseMailAddress(string email, out MailAddress parsedMailAddress)
        {
            parsedMailAddress = null;

            if (string.IsNullOrEmpty(email))
            {
                return false;
            }

            try
            {
                parsedMailAddress = new MailAddress(email.Trim('\t', ' '));
                return true;
            }
            catch (FormatException)
            {
            }

            return false;
        }

        /// <summary>
        /// Creates the mail message from entity.
        /// </summary>
        /// <param name="entity">
        /// The entity. 
        /// </param>
        /// <returns>
        /// </returns>
        public static MailMessageEx CreateMailMessageFromEntity(MimeEntity entity)
        {
            var message = new MailMessageEx();

            foreach (string key in entity.Headers.AllKeys)
            {
                string value = entity.Headers[key];
                if (value.Equals(string.Empty))
                {
                    value = " ";
                }

                message.Headers.Add(key.ToLowerInvariant(), value);

                switch (key.ToLowerInvariant())
                {
                    case MailHeaders.Bcc:
                        PopulateAddressList(value, message.Bcc);
                        break;
                    case MailHeaders.Cc:
                        PopulateAddressList(value, message.CC);
                        break;
                    case MailHeaders.From:
                        MailAddress fromAddress;
                        if (TryParseMailAddress(value, out fromAddress))
                        {
                            message.From = fromAddress;   
                        }
                        break;
                    case MailHeaders.ReplyTo:
                        PopulateAddressList(value, message.ReplyToList);
                        break;
                    case MailHeaders.Subject:
                        message.Subject = DecodeIfEncoded(value);
                        break;
                    case MailHeaders.To:
                        PopulateAddressList(value, message.To);
                        break;
                }
            }

            return message;
        }

        private static readonly Regex encoder = new Regex(
            @"\=\?(?<encoding>[-a-z0-9]+?)\?B\?(?<data>[-A-Za-z0-9+/=]+)\?\=",
            RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        /// <summary>
        /// Gets around a bug in MS stuff:
        /// https://connect.microsoft.com/VisualStudio/feedback/details/785710/mailmessage-subject-incorrectly-encoded-in-utf-8-base64
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string DecodeIfEncoded(string value)
        {
            if (!encoder.IsMatch(value))
            {
                return value;
            }

            // handle decoding...
            var encodedBlocks = encoder.Matches(value).Cast<Match>()
                .Where(m => m.Groups["encoding"].Success && m.Groups["data"].Success)
                .Select(x => new KeyValuePair<Encoding, string>(System.Text.Encoding.GetEncoding(x.Groups["encoding"].Value), x.Groups["data"].Value))
                .ToList();

            // return decoded string...
            return string.Join("", encodedBlocks.Select(kv => kv.Key.GetString(Convert.FromBase64String(kv.Value))));
        }

        /// <summary>
        /// Populates the address list.
        /// </summary>
        /// <param name="addressList">
        /// The address list. 
        /// </param>
        /// <param name="recipients">
        /// The recipients. 
        /// </param>
        public static void PopulateAddressList(string addressList, MailAddressCollection recipients)
        {
            recipients.AddRange(GetMailAddresses(addressList));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the mail addresses.
        /// </summary>
        /// <param name="addressList">
        /// The address list. 
        /// </param>
        /// <returns>
        /// </returns>
        private static IEnumerable<MailAddress> GetMailAddresses(string addressList)
        {
            if (addressList == null)
            {
                throw new ArgumentNullException("addressList");
            }

            string[] addresses = addressList.Split(AddressDelimiters);

            return addresses.Select(e =>
            {
                MailAddress email;
                TryParseMailAddress(e, out email);
                return email;
            }).Where(s => s != null);
        }

        /// <summary>
        /// The get header.
        /// </summary>
        /// <param name="header">
        /// The header. 
        /// </param>
        /// <param name="stripBrackets">
        /// The strip brackets. 
        /// </param>
        /// <returns>
        /// The get header. 
        /// </returns>
        private string GetHeader(string header, bool stripBrackets = false)
        {
            return stripBrackets ? MimeEntity.TrimBrackets(this.Headers[header]) : this.Headers[header];
        }

        #endregion
    }
}