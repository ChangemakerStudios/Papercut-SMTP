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

    using Newtonsoft.Json;

    public static class PapercutIPCommSerializer
    {
        public static string ToJson<TObject>(TObject @object)
        {
            return
                JsonConvert.SerializeObject(@object, Formatting.None, _ipCommJsonSerializerSettings);
        }

        public static object FromJson(Type type, string json)
        {
            return
                JsonConvert.DeserializeObject(json, type, _ipCommJsonSerializerSettings);
        }

        public static string ToJson(Type type, object @object)
        {
            return
                JsonConvert.SerializeObject(@object, type, Formatting.None, _ipCommJsonSerializerSettings);
        }

        public static TObject FromJson<TObject>(string json)
        {
            return
                JsonConvert.DeserializeObject<TObject>(json, _ipCommJsonSerializerSettings);
        }

        private static readonly JsonSerializerSettings _ipCommJsonSerializerSettings
            = new JsonSerializerSettings
              {
                  TypeNameHandling = TypeNameHandling.Auto,
                  Formatting = Formatting.None,
                  NullValueHandling = NullValueHandling.Include,
                  TypeNameAssemblyFormatHandling =
                      TypeNameAssemblyFormatHandling.Simple
              };
    }
}