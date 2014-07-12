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

namespace Papercut.Core.Rules
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Formatters;
    using System.Text;

    using Newtonsoft.Json;

    using Papercut.Core.Annotations;

    public class RuleRespository
    {
        readonly Lazy<JsonSerializerSettings> _serializationSettings =
            new Lazy<JsonSerializerSettings>(
                () => new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple
                });

        JsonSerializerSettings SerializationSettings
        {
            get { return _serializationSettings.Value; }
        }

        public void SaveRules([NotNull] IList<IRule> rules, string path)
        {
            if (rules == null) throw new ArgumentNullException("rules");
            if (path == null) throw new ArgumentNullException("path");

            string rulesJson = JsonConvert.SerializeObject(
                rules,
                Formatting.Indented,
                SerializationSettings);

            File.WriteAllText(path, rulesJson, Encoding.UTF8);
        }

        public IList<IRule> LoadRules([NotNull] string path)
        {
            if (path == null) throw new ArgumentNullException("path");

            if (!File.Exists(path)) return new List<IRule>(0);

            string json = File.ReadAllText(path, Encoding.UTF8);

            return JsonConvert.DeserializeObject<List<IRule>>(json, SerializationSettings);
        }
    }
}