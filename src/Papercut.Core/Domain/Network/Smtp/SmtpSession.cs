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


namespace Papercut.Core.Domain.Network.Smtp
{
    using System.Collections.Generic;

    /// <summary>
    ///     The smtp session.
    /// </summary>
    public class SmtpSession
    {
        #region Public Methods and Operators

        /// <summary>
        ///     The reset.
        /// </summary>
        public void Reset()
        {
            this.MailFrom = null;
            this.Recipients.Clear();
            this.UseUtf8 = false;
        }

        #endregion

        #region Constants and Fields

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets MailFrom.
        /// </summary>
        public string MailFrom { get; set; }

        /// <summary>
        ///     Gets or sets Message.
        /// </summary>
        public byte[] Message { get; set; }

        /// <summary>
        ///     Gets or sets Recipients.
        /// </summary>
        public List<string> Recipients { get; set; } = new List<string>();

        /// <summary>
        ///     Gets or sets Sender.
        /// </summary>
        public string Sender { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether UseUtf8.
        /// </summary>
        public bool UseUtf8 { get; set; }

        #endregion
    }
}