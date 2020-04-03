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

namespace Papercut.Helpers
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;

    using Papercut.Core.Annotations;

    using Veil;

    public static class VeilHelpers
    {
        static readonly Lazy<VeilEngine> _lazyVailEngine;

        static readonly ConcurrentDictionary<string, Tuple<Action<TextWriter, object>>> _parsedCache
            = new ConcurrentDictionary<string, Tuple<Action<TextWriter, object>>>();

        static VeilHelpers()
        {
            _lazyVailEngine = new Lazy<VeilEngine>(() => new VeilEngine());
        }

        static VeilEngine VailEngine => _lazyVailEngine.Value;

        static Action<TextWriter, object> GetCompiledTemplate([NotNull] string templateString, Type modelType)
        {
            if (templateString == null) throw new ArgumentNullException(nameof(templateString));

            // requires this reference so it pulls this assembly
            var parser = new Veil.Handlebars.HandlebarsParser();
            
            var parsedTemplate = _parsedCache.GetOrAdd(templateString,
                t =>
                {
                    using (var sr = new StringReader(templateString))
                        return Tuple.Create(VailEngine.CompileNonGeneric("handlebars", sr, modelType));
                });

            return parsedTemplate.Item1;
        }

        public static string RenderTemplate([NotNull] this string template, [NotNull] object model)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            if (model == null) throw new ArgumentNullException(nameof(model));

            using (var writer = new StringWriter())
            {
                GetCompiledTemplate(template, model.GetType())(writer, model);
                return writer.ToString();
            }
        }
    }
}