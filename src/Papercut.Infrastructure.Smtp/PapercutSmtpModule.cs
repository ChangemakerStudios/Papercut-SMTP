// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
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


using Autofac;

using SmtpServer;
using SmtpServer.Authentication;
using SmtpServer.Net;
using SmtpServer.Protocol;
using SmtpServer.Storage;

namespace Papercut.Infrastructure.Smtp;

[PublicAPI]
public class PapercutSmtpModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<PapercutSmtpServer>().AsSelf();

        // smtp
        builder.RegisterType<SmtpMessageStore>().As<IMessageStore>();

        // factories
        builder.RegisterType<SimpleAuthentication>().As<IUserAuthenticatorFactory>();
        builder.RegisterType<EndpointListenerFactory>().As<IEndpointListenerFactory>();
        builder.RegisterType<SmtpCommandFactory>().As<ISmtpCommandFactory>();

        builder.Register(
                ctx =>
                {
                    var c = ctx.Resolve<IComponentContext>();
                    return new DelegatingMessageStoreFactory(context => c.Resolve<IMessageStore>());
                })
            .As<IMessageStoreFactory>();

        builder.Register(
            ctx =>
            {
                return new DelegatingMailboxFilterFactory(
                    context => new DelegatingMailboxFilter(mailbox => true));
            }).As<IMailboxFilterFactory>();

        builder.Register(
            (ctx, p) =>
            {
                var c = ctx.Resolve<IComponentContext>();

                try
                {
                    return new SmtpServer.SmtpServer(
                        p.TypedAs<ISmtpServerOptions>(),
                        (IServiceProvider)c);
                }
                catch (Exception ex)
                {
                    ctx.Resolve<ILogger>().ForContext<PapercutSmtpModule>()
                        .Error(ex, "Failure Loading Smtp Server");
                }

                return null;
            }).As<SmtpServer.SmtpServer>();
    }
}