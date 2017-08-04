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
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Papercut.Common.Extensions;
    using Papercut.Core.Domain.Network;
    using Papercut.Core.Domain.Network.Smtp;

    public abstract class BaseSmtpCommand : ISmtpCommand
    {
        protected SmtpSession Session => this.Context.Session;

        protected IConnection Connection => this.Context.Connection;

        public ISmtpContext Context { protected get; set; }

        public virtual SmtpCommandResult Execute(ISmtpContext context, string request)
        {
            this.Context = context;

            var parts = request.Split(' ');
            var command = parts[0].ToUpper().Trim();

            if (GetMatchCommands().IfNullEmpty().Any(c => c.Equals(command, StringComparison.OrdinalIgnoreCase)))
            {
                Run(command, parts.Skip(1).ToArray());
                return SmtpCommandResult.Done;
            }

            return SmtpCommandResult.Continue;
        }

        protected abstract IEnumerable<string> GetMatchCommands();

        protected abstract void Run(string command, string[] requestParts);
    }
}