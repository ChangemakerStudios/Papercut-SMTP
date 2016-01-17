// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2016 Jaben Cargman
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

namespace Papercut.Network.SmtpCommands
{
    using System.Collections.Generic;
    using System.Linq;

    using Papercut.Core.Helper;
    using Papercut.Core.Network;

    public class SendSmtpCommand : BaseSmtpCommand
    {
        protected override IEnumerable<string> GetMatchCommands()
        {
            return new[] { "SEND", "SOML", "SAML", "MAIL" };
        }

        protected override void Run(string command, string[] args)
        {
            string line = string.Join(" ", args);

            // Check for the right number of parameters
            if (args.Length < 1)
            {
                Connection.SendLine("504 Command parameter not implemented");
                return;
            }

            // Check for the ":"
            if (!args[0].ToUpper().StartsWith("FROM") || !line.Contains(":"))
            {
                Connection.SendLine("504 Command parameter not implemented");
                return;
            }

            // Check request order
            if (Session.Sender == null)
            {
                Connection.SendLine("503 Bad sequence of commands");
                return;
            }

            // Set the from settings
            Session.Reset();

            string address =
                line.Substring(line.IndexOf(":") + 1)
                    .Replace("<", string.Empty)
                    .Replace(">", string.Empty)
                    .Trim();

            Session.MailFrom = address;

            // Check for encoding
            var anyEncoding = args.Where(part => part.ToUpper().StartsWith("BODY="))
                .Select(p => p.ToUpper().Replace("BODY=", string.Empty).Trim())
                .Any(p => p == "8BITMIME");

            Session.UseUtf8 = anyEncoding;

            Connection.SendLine("250 <{0}> OK", address);
        }
    }
}