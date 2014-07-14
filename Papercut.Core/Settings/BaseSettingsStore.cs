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

namespace Papercut.Core.Settings
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    using Papercut.Core.Annotations;

    public abstract class BaseSettingsStore : ISettingStore
    {
        protected BaseSettingsStore(IEnumerable<KeyValuePair<string, string>> settings = null)
        {
            CurrentSettings = new ConcurrentDictionary<string, string>(settings);
        }

        protected ConcurrentDictionary<string, string> CurrentSettings { get; set; }

        public string this[[NotNull] string key]
        {
            get
            {
                if (key == null) throw new ArgumentNullException("key");

                string value;
                return CurrentSettings.TryGetValue(key, out value) ? value : null;
            }
            set
            {
                if (key == null) throw new ArgumentNullException("key");

                CurrentSettings.TryUpdate(key, value, null);
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
    }
}