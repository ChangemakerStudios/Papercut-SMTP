// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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


namespace Papercut.Service.Infrastructure.Paths;

using Core.Infrastructure.Network;

public class ReplyWithDefaultMessageSavePathService(MessagePathConfigurator messagePathConfigurator, SmtpServerOptions smtpServerOptions)
    : IEventHandler<AppProcessExchangeEvent>
{
    public Task HandleAsync(AppProcessExchangeEvent @event, CancellationToken token = default)
    {
        // respond with the current save path...
        @event.MessageWritePath = messagePathConfigurator.DefaultSavePath;

        // share our current ip and port binding for the SMTP server.
        @event.IP = smtpServerOptions.IP;
        @event.Port = smtpServerOptions.Port;

        return Task.CompletedTask;
    }

    #region Begin Static Container Registrations

    static void Register(ContainerBuilder builder)
    {
        builder.RegisterType<ReplyWithDefaultMessageSavePathService>().AsImplementedInterfaces().AsSelf()
            .InstancePerLifetimeScope();
    }

    #endregion
}