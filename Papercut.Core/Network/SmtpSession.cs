// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2015 Jaben Cargman
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

namespace Papercut.Core.Network
{
    #region Using

    using System.Collections.Generic;

    #endregion

    /// <summary>
    ///     The smtp session.
    /// </summary>
    public class SmtpSession
    {
        #region Constants and Fields

        /// <summary>
        ///     The _mail from.
        /// </summary>
        string _mailFrom;

        /// <summary>
        ///     The _recipients.
        /// </summary>
        IList<string> _recipients = new List<string>();

        /// <summary>
        ///     The use utf 8.
        /// </summary>
        bool _useUtf8;

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets MailFrom.
        /// </summary>
        public string MailFrom
        {
            get { return _mailFrom; }

            set { _mailFrom = value; }
        }

        /// <summary>
        ///     Gets or sets Message.
        /// </summary>
        public byte[] Message { get; set; }

        /// <summary>
        ///     Gets or sets Recipients.
        /// </summary>
        public IList<string> Recipients
        {
            get { return _recipients; }

            set { _recipients = value; }
        }

        /// <summary>
        ///     Gets or sets Sender.
        /// </summary>
        public string Sender { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether UseUtf8.
        /// </summary>
        public bool UseUtf8
        {
            get { return _useUtf8; }

            set { _useUtf8 = value; }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The reset.
        /// </summary>
        public void Reset()
        {
            _mailFrom = null;
            _recipients.Clear();
            _useUtf8 = false;
        }

        #endregion
    }
}