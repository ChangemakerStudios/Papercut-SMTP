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


using Autofac;

using Papercut.Common.Domain;
using Papercut.Common.Extensions;
using Papercut.Core.Domain.Paths;
using Papercut.Core.Infrastructure.Network;
using Papercut.Domain.Events;

namespace Papercut.AppLayer.Settings
{
    public class MergeServerBackendSettings : IEventHandler<AppProcessExchangeEvent>
    {
        readonly MessagePathConfigurator _configurator;

        readonly IMessageBus _messageBus;

        public MergeServerBackendSettings(MessagePathConfigurator configurator, IMessageBus messageBus)
        {
            this._configurator = configurator;
            this._messageBus = messageBus;
        }

        public async Task HandleAsync(AppProcessExchangeEvent @event, CancellationToken token)
        {
            ArgumentNullException.ThrowIfNull(@event);

            if (string.IsNullOrWhiteSpace(@event.MessageWritePath))
                return;

            if (!this._configurator.LoadPaths.Any(s => s.StartsWith(@event.MessageWritePath, StringComparison.OrdinalIgnoreCase)))
            {
                // add it for watching...
                Properties.Settings.Default.MessagePaths = $"{Properties.Settings.Default.MessagePaths};{@event.MessageWritePath}";
            }

            var previousSettings = new Properties.Settings();

            Properties.Settings.Default.CopyTo(previousSettings);

            // save ip:port bindings as our own to keep in sync...
            Properties.Settings.Default.IP = @event.IP;
            Properties.Settings.Default.Port = @event.Port;
            Properties.Settings.Default.Save();

            await this._messageBus.PublishAsync(new SettingsUpdatedEvent(previousSettings), token);
        }

        #region Begin Static Container Registrations

        /// <summary>
        /// Called dynamically from the RegisterStaticMethods() call in the container module.
        /// </summary>
        /// <param name="builder"></param>
        [UsedImplicitly]
        static void Register(ContainerBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.RegisterType<MergeServerBackendSettings>().AsImplementedInterfaces()
                .InstancePerLifetimeScope();
        }

        #endregion
    }
}