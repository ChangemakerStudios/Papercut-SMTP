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

namespace Papercut.Core.Network.SmtpCommands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Papercut.Core.Helper;

    public abstract class BaseSmtpCommand : ISmtpCommand
    {
        protected SmtpSession Session
        {
            get { return CommandContext.Session; }
        }

        protected Connection Connection
        {
            get { return CommandContext.Connection; }
        }

        public ISmtpContext CommandContext { protected get; set; }

        public virtual SmtpCommandResult Execute(string command, string[] args)
        {
            if (GetMatchCommands().IfNullEmpty().Any(c => c.Equals(command, StringComparison.OrdinalIgnoreCase)))
            {
                Run(command, args);
                return SmtpCommandResult.Done;
            }

            return SmtpCommandResult.Continue;
        }

        protected abstract IEnumerable<string> GetMatchCommands();

        protected abstract void Run(string command, string[] args);
    }
}