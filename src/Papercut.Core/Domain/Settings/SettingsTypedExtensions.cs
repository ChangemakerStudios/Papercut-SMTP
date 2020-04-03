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

    using Papercut.Core.Annotations;

    public static class SettingsTypedExtensions
    {
        public static T UseTyped<T>([NotNull] this ISettingStore settingStore)
            where T : ISettingsTyped, new()
        {
            if (settingStore == null) throw new ArgumentNullException(nameof(settingStore));

            return new T { Settings = settingStore };
        }

        public static void Save([NotNull] this ISettingsTyped typedSettings)
        {
            if (typedSettings == null) throw new ArgumentNullException(nameof(typedSettings));

            typedSettings.Settings.Save();
        }

        public static void Load([NotNull] this ISettingsTyped typedSettings)
        {
            if (typedSettings == null) throw new ArgumentNullException(nameof(typedSettings));

            typedSettings.Settings.Load();
        }
    }
}