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


using System.Collections.Concurrent;

namespace Papercut.Core.Domain.Settings;

public abstract class BaseSettingsStore : ISettingStore
{
    protected BaseSettingsStore()
    {
        this.CurrentSettings = new ConcurrentDictionary<string, string>();
    }

    protected ConcurrentDictionary<string, string> CurrentSettings { get; set; }

    public string this[string key]
    {
        get
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            return this.CurrentSettings.TryGetValue(key, out var value) ? value : null;
        }
        set
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            this.CurrentSettings.AddOrUpdate(key, value, (k, v) => value);
        }
    }

    public string Get(string key)
    {
        return this[key];
    }

    public void Set(string key, string value)
    {
        this[key] = value;
    }

    public abstract void Load();

    public abstract void Save();

    protected Dictionary<string, string> GetSettingSnapshot()
    {
        return new Dictionary<string, string>(this.CurrentSettings);
    }

    protected void LoadSettings(IEnumerable<KeyValuePair<string, string>>? settings = null)
    {
        if (settings != null) this.CurrentSettings = new ConcurrentDictionary<string, string>(settings);
        else this.CurrentSettings.Clear();
    }
}