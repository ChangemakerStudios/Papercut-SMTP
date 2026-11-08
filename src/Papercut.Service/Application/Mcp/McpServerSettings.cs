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


using Microsoft.Extensions.Configuration;

namespace Papercut.Service.Application.Mcp;

public static class McpServerSettings
{
    public const string EnabledSettingKey = "EnableMcpServer";

    public const string EndpointPath = "/mcp";

    public static bool IsEnabled(ISettingStore settingStore, IConfiguration configuration)
    {
        var setting = settingStore.Get(EnabledSettingKey, configuration[EnabledSettingKey]);

        return bool.TryParse(setting, out var enabled) && enabled;
    }
}
