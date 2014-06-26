/*  
 * Papercut
 *
 *  Copyright © 2008 - 2012 Ken Robertson
 *  Copyright © 2013 - 2014 Jaben Cargman
 *  
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *  
 *  http://www.apache.org/licenses/LICENSE-2.0
 *  
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *  
 */

namespace Papercut.Core.Rules
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.Serialization.Formatters;
    using System.Text;

    using Newtonsoft.Json;

    using Papercut.Core.Annotations;

    [Serializable]
    public class RuleCollection : ICollection<Rule>
    {
        readonly Lazy<JsonSerializerSettings> _serializationSettings =
            new Lazy<JsonSerializerSettings>(
                () => new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple
                });

        List<Rule> _rules = new List<Rule>();

        JsonSerializerSettings SerializationSettings
        {
            get
            {
                return _serializationSettings.Value;
            }
        }

        public IEnumerator<Rule> GetEnumerator()
        {
            return _rules.GetEnumerator();
        }

        public void Add(Rule item)
        {
            _rules.Add(item);
        }

        public void Clear()
        {
            _rules.Clear();
        }

        public bool Contains(Rule item)
        {
            return _rules.Contains(item);
        }

        public void CopyTo(Rule[] array, int arrayIndex)
        {
            _rules.CopyTo(array, arrayIndex);
        }

        public bool Remove(Rule item)
        {
            return _rules.Remove(item);
        }

        public int Count
        {
            get
            {
                return _rules.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void SaveTo(string path)
        {
            if (path == null) throw new ArgumentNullException("path");

            string rulesJson = JsonConvert.SerializeObject(
                _rules,
                Formatting.Indented,
                SerializationSettings);

            File.WriteAllText(path, rulesJson, Encoding.UTF8);
        }

        public bool LoadFrom([NotNull] string path)
        {
            if (path == null) throw new ArgumentNullException("path");

            Clear();

            if (!File.Exists(path)) return false;

            string json = File.ReadAllText(path, Encoding.UTF8);

            _rules = JsonConvert.DeserializeObject<List<Rule>>(json, SerializationSettings);

            return true;
        }
    }
}