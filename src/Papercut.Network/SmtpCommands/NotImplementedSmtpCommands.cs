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

namespace Papercut.Network.SmtpCommands
{
    using System.Collections.Generic;

    using Papercut.Network.Protocols;

    public class NotImplementedSmtpCommands : BaseSmtpCommand
    {
        protected override IEnumerable<string> GetMatchCommands()
        {
            return new[] { "VRFY", "EXPN", "HELP", "TURN" };
        }

        protected override void Run(string command, string[] args)
        {
            switch (command)
            {
                case "VRFY":
                    Connection.SendLine(
                        "252 Cannot VRFY user, but will accept message and attempt delivery");
                    break;

                case "EXPN":
                    Connection.SendLine("252 Cannot expand upon list");
                    break;
                case "HELP":
                case "TURN":
                    Connection.SendLine("502 Command not implemented");
                    break;
            }
        }
    }
}