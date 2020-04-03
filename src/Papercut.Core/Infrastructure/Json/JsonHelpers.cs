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

namespace Papercut.Core.Infrastructure.Json
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters;
    using System.Text;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public static class JsonHelpers
    {
        private static readonly JsonSerializerSettings _serializationSettings =
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Include,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
            };

        public static string ToJson(this object obj, JsonSerializerSettings setting = null)
        {
            return JsonConvert.SerializeObject(obj, setting ?? _serializationSettings);
        }

        public static void SaveJson<T>(
            T obj,
            string path,
            Encoding textEncoding = null,
            JsonSerializerSettings setting = null)
            where T : class
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (path == null) throw new ArgumentNullException(nameof(path));

            string json = JsonConvert.SerializeObject(
                obj,
                Formatting.Indented,
                setting ?? _serializationSettings);

            File.WriteAllText(path, json, textEncoding ?? Encoding.UTF8);
        }

        public static T LoadJson<T>(
            string path,
            Func<T> defaultValueFunc = null,
            Encoding textEncoding = null,
            JsonSerializerSettings setting = null)
            where T : class
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            if (!File.Exists(path)) return defaultValueFunc?.Invoke();

            string json = File.ReadAllText(path, textEncoding ?? Encoding.UTF8);

            return JsonConvert.DeserializeObject<T>(json, setting ?? _serializationSettings);
        }

        public static object FromJson(
            this string json,
            Type type,
            JsonSerializerSettings setting = null)
        {
            return JsonConvert.DeserializeObject(json, type, setting ?? _serializationSettings);
        }

        public static T FromJson<T>(this string json, JsonSerializerSettings setting = null)
        {
            return JsonConvert.DeserializeObject<T>(json, setting ?? _serializationSettings);
        }
    }
}