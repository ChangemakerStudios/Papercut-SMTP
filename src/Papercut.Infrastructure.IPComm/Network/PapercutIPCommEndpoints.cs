// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2020 Jaben Cargman
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


namespace Papercut.Infrastructure.IPComm.Network
{
    using System;

    using Core.Annotations;
    using Core.Domain.Network;
    using Core.Domain.Settings;

    public class PapercutIPCommEndpoints
    {
        public PapercutIPCommEndpoints([NotNull] ISettingStore settingStore)
        {
            if (settingStore == null) throw new ArgumentNullException(nameof(settingStore));

            var uiAddress = settingStore.GetOrSet("IPCommUIAddress", PapercutIPCommConstants.Localhost,
                $"The IP Comm UI IP address (Defaults to {PapercutIPCommConstants.Localhost}).");

            var uiPort = settingStore.GetOrSet("IPCommUIPort", PapercutIPCommConstants.UiListeningPort,
                $"The IP Comm UI listening port (Defaults to {PapercutIPCommConstants.UiListeningPort}).");

            this.UI = new EndpointDefinition(uiAddress, uiPort);

            var serviceAddress = settingStore.GetOrSet("IPCommServiceAddress", PapercutIPCommConstants.Localhost,
                $"The IP Comm Service IP address (Defaults to {PapercutIPCommConstants.Localhost}).");

            var servicePort = settingStore.GetOrSet("IPCommServicePort", PapercutIPCommConstants.ServiceListeningPort,
                $"The IP Comm Service UI listening port (Defaults to {PapercutIPCommConstants.ServiceListeningPort}).");

            this.Service = new EndpointDefinition(serviceAddress, servicePort);
        }

        public EndpointDefinition UI { get; }
        public EndpointDefinition Service { get; }
    }
}