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

namespace Papercut.Service.Helpers
{
    using Papercut.Core.Configuration;
    using Papercut.Core.Settings;

    using Serilog;

    public sealed class ApplicationJsonSettingStore : JsonSettingStore
    {
        readonly ILogger _logger;

        public ApplicationJsonSettingStore(IAppMeta appMeta, ILogger logger)
            : base(appMeta.AppName)
        {
            _logger = logger;

            // immediately load the settings
            Load();
        }

        public override void Load()
        {
            try
            {
                base.Load();
                _logger.Information("Loaded Settings {@CurrentSettings} from {SettingsFile}", CurrentSettings, SettingsFilePath);
            }
            catch (System.Exception ex)
            {
                _logger.Warning(ex, "Unable to load settings from {SettingsFile}", SettingsFilePath);
            }
        }

        public override void Save()
        {
            _logger.Information(
                "Saving Settings {@CurrentSettings} to {SettingsFile}",
                CurrentSettings,
                SettingsFilePath);
            base.Save();
        }
    }
}