// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2026 Jaben Cargman
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

namespace Papercut.Service.Infrastructure.Configuration;

/// <summary>
/// The core JSON setting store, saved under DataDirectory when it is configured.
/// </summary>
public class ServiceSettingStore : JsonSettingStore
{
    public ServiceSettingStore(IAppMeta appMeta, IConfiguration configuration)
        : base(appMeta)
    {
        var filePath = ServiceDataDirectory.GetFilePath(configuration, $"{appMeta.AppName}.Settings.json");

        if (filePath != null) SettingsFilePath = filePath;
    }

    #region Begin Static Container Registrations

    /// <summary>
    /// Called dynamically from the RegisterStaticMethods() call in the container module.
    /// Registered after PapercutCoreModule, so it replaces the core JsonSettingStore.
    /// </summary>
    [UsedImplicitly]
    private static void Register(ContainerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RegisterType<ServiceSettingStore>()
            .As<ISettingStore>()
            .OnActivated(
                j =>
                {
                    try
                    {
                        j.Instance.Load();

                        // immediately save all settings
                        j.Instance.Save();
                    }
                    catch
                    {
                    }
                })
            .AsSelf()
            .SingleInstance();
    }

    #endregion
}
