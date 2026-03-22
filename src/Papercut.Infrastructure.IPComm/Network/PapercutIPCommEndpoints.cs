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


using Papercut.Core.Domain.Network;
using Papercut.Core.Domain.Settings;

namespace Papercut.Infrastructure.IPComm.Network;

public class PapercutIPCommEndpoints
{
    public PapercutIPCommEndpoints(ISettingStore settingStore)
    {
        if (settingStore == null) throw new ArgumentNullException(nameof(settingStore));

        var uiAddress = settingStore.GetOrSet("IPCommUIAddress", PapercutIPCommConstants.Localhost,
            $"The IP Comm UI IP address (Defaults to {PapercutIPCommConstants.Localhost}).");
        var uiPort = settingStore.GetOrSet("IPCommUIPort", PapercutIPCommConstants.UiListeningPort,
            $"The IP Comm UI listening port (Defaults to {PapercutIPCommConstants.UiListeningPort}).");
        UI = new EndpointDefinition(uiAddress, uiPort);

        var serviceAddress = settingStore.GetOrSet("IPCommServiceAddress", PapercutIPCommConstants.Localhost,
            $"The IP Comm Service IP address (Defaults to {PapercutIPCommConstants.Localhost}).");
        var servicePort = settingStore.GetOrSet("IPCommServicePort", PapercutIPCommConstants.ServiceListeningPort,
            $"The IP Comm Service UI listening port (Defaults to {PapercutIPCommConstants.ServiceListeningPort}).");
        Service = new EndpointDefinition(serviceAddress, servicePort);

        var trayServiceAddress = settingStore.GetOrSet("IPCommTrayServiceAddress", PapercutIPCommConstants.Localhost,
            $"The IP Comm Tray Service IP address (Defaults to {PapercutIPCommConstants.Localhost}).");
        var trayServicePort = settingStore.GetOrSet("IPCommTrayServicePort", PapercutIPCommConstants.TrayServiceListeningPort,
            $"The IP Comm Tray Service UI listening port (Defaults to {PapercutIPCommConstants.TrayServiceListeningPort}).");
        TrayService = new EndpointDefinition(trayServiceAddress, trayServicePort);
    }

    public EndpointDefinition UI { get; }

    public EndpointDefinition Service { get; }

    public EndpointDefinition TrayService { get; }
}