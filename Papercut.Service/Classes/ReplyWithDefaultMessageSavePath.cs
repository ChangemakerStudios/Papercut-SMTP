// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2014 Jaben Cargman
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

namespace Papercut.Service.Classes
{
    using System.IO;

    using Papercut.Core.Configuration;
    using Papercut.Core.Events;

    public class ReplyWithDefaultMessageSavePath : IHandleEvent<AppProcessExchangeEvent>
    {
        readonly IMessagePathConfigurator _messagePathConfigurator;

        readonly PapercutServiceSettings _serviceSettings;

        public ReplyWithDefaultMessageSavePath(IMessagePathConfigurator messagePathConfigurator, PapercutServiceSettings serviceSettings)
        {
            _messagePathConfigurator = messagePathConfigurator;
            _serviceSettings = serviceSettings;
        }

        public void Handle(AppProcessExchangeEvent @event)
        {
            // respond with the current save path...
            @event.MessageWritePath = Path.GetDirectoryName(
                _messagePathConfigurator.DefaultSavePath);

            // share our current ip and port binding for the SMTP server.
            @event.IP = _serviceSettings.IP;
            @event.Port = _serviceSettings.Port;
        }
    }
}