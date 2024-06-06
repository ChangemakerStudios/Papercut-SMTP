// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2024 Jaben Cargman
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


using Autofac;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Papercut.Common.Extensions;
using Papercut.Core.Domain.Rules;
using Papercut.Core.Infrastructure.Json;
using Papercut.Rules.Domain.Conditional.Forwarding;
using Papercut.Rules.Domain.Forwarding;
using Papercut.Rules.Domain.Relaying;
using Papercut.Rules.Domain.Rules;

namespace Papercut.Rules.Infrastructure
{
    public class RuleRepository : IRuleRepository
    {
        private readonly JsonSerializerSettings _serializationSettings;

        public RuleRepository()
        {
            this._serializationSettings =
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Include,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                    SerializationBinder = new NamespaceMigrationSerializationBinder(this.NamespaceMigrations)
                };
        }

        IEnumerable<INamespaceMigration> NamespaceMigrations
        {
            get
            {
                var assembly = "Papercut.Rules";

                yield return new NamespaceMigrationImpl(
                    assembly,
                    "Papercut.Rules.Implementations.ConditionalForwardWithRelayRule",
                    typeof(ConditionalForwardWithRetryRule));

                yield return new NamespaceMigrationImpl(
                    assembly,
                    "Papercut.Rules.Implementations.ConditionalForwardRule",
                    typeof(ConditionalForwardRule));

                yield return new NamespaceMigrationImpl(
                    assembly,
                    "Papercut.Rules.Implementations.RelayRule",
                    typeof(RelayRule));

                yield return new NamespaceMigrationImpl(
                    assembly,
                    "Papercut.Rules.Implementations.ForwardRule",
                    typeof(ForwardRule));
            }
        }

        public void SaveRules([NotNull] IList<IRule> rules, string path)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (path == null) throw new ArgumentNullException(nameof(path));

            JsonHelpers.SaveJson(rules, path, setting: this._serializationSettings);
        }

        public IList<IRule> LoadRules([NotNull] string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            return JsonHelpers.LoadJson<IList<IRule>>(
                path,
                () => new List<IRule>(0),
                setting: this._serializationSettings);
        }

        #region Begin Static Container Registrations

        /// <summary>
        /// Called dynamically from the RegisterStaticMethods() call in the container module.
        /// </summary>
        /// <param name="builder"></param>
        [UsedImplicitly]
        static void Register([NotNull] ContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.RegisterType<RuleRepository>().As<IRuleRepository>().InstancePerDependency();
        }

        #endregion

        public interface INamespaceMigration
        {
            string FromAssembly { get; }

            string FromType { get; }

            Type ToType { get; }
        }

        internal class NamespaceMigrationImpl : INamespaceMigration
        {
            public NamespaceMigrationImpl(string fromAssembly, string fromType, Type toType)
            {
                this.FromAssembly = fromAssembly;
                this.FromType = fromType;
                this.ToType = toType;
            }

            public string FromAssembly { get; }

            public string FromType { get; }

            public Type ToType { get; }
        }

        public class NamespaceMigrationSerializationBinder : DefaultSerializationBinder
        {
            private readonly INamespaceMigration[] _migrations;

            public NamespaceMigrationSerializationBinder(IEnumerable<INamespaceMigration> migrations)
            {
                this._migrations = migrations.IfNullEmpty().ToArray();
            }

            public override Type BindToType(string assemblyName, string typeName)
            {
                var migration = this._migrations.SingleOrDefault(p => p.FromAssembly == assemblyName && p.FromType == typeName);
                if(migration != null)
                {
                    return migration.ToType;
                }
                return base.BindToType(assemblyName, typeName);
            }
        }
    }
}