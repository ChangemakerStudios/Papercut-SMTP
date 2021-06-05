// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2021 Jaben Cargman
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


namespace Papercut.AppLayer.Settings
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Autofac;

    using Papercut.Common.Domain;
    using Papercut.Common.Extensions;
    using Papercut.Core.Annotations;
    using Papercut.Core.Domain.Paths;
    using Papercut.Core.Infrastructure.Network;
    using Papercut.Domain.Events;
    using Papercut.Properties;

    public class MergeServerBackendSettings : IEventHandler<AppProcessExchangeEvent>
    {
        readonly IMessagePathConfigurator _configurator;

        readonly IMessageBus _messageBus;

        public MergeServerBackendSettings(IMessagePathConfigurator configurator, IMessageBus messageBus)
        {
            this._configurator = configurator;
            this._messageBus = messageBus;
        }

        public async Task HandleAsync([NotNull] AppProcessExchangeEvent @event)
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));

            if (string.IsNullOrWhiteSpace(@event.MessageWritePath))
                return;

            if (!this._configurator.LoadPaths.Any(s => s.StartsWith(@event.MessageWritePath, StringComparison.OrdinalIgnoreCase)))
            {
                // add it for watching...
                Settings.Default.MessagePaths = $"{Settings.Default.MessagePaths};{@event.MessageWritePath}";
            }

            var previousSettings = new Settings();

            Settings.Default.CopyTo(previousSettings);

            // save ip:port bindings as our own to keep in sync...
            Settings.Default.IP = @event.IP;
            Settings.Default.Port = @event.Port;
            Settings.Default.Save();

            await this._messageBus.PublishAsync(new SettingsUpdatedEvent(previousSettings));
        }

        #region Begin Static Container Registrations

        /// <summary>
        /// Called dynamically from the RegisterStaticMethods() call in the container module.
        /// </summary>
        /// <param name="builder"></param>
        [UsedImplicitly]
        static void Register([NotNull] ContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.RegisterType<MergeServerBackendSettings>().AsImplementedInterfaces()
                .InstancePerLifetimeScope();
        }

        #endregion
    }
}