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

namespace Papercut.Core.Domain.Settings
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Papercut.Core.Domain.Application;
    using Papercut.Core.Infrastructure.Json;

    public class JsonSettingStore : BaseSettingsStore
    {
        public JsonSettingStore(IAppMeta appMeta)
        {
            this.SettingsFilePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                $"{appMeta.AppName}.Settings.json");
        }

        protected string SettingsFilePath { get; set; }
        
        public override void Load()
        {
            if (this.SettingsFilePath == null) return;

            this.LoadSettings(JsonHelpers.LoadJson<Dictionary<string, string>>(this.SettingsFilePath));
        }

        public override void Save()
        {
            if (this.SettingsFilePath == null) return;

            JsonHelpers.SaveJson(new SortedDictionary<string, string>(this.GetSettingSnapshot()), this.SettingsFilePath);
        }
    }
}