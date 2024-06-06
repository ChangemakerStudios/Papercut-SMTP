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


using System.ComponentModel;

using Autofac;

using Papercut.Common.Extensions;
using Papercut.Common.Helper;
using Papercut.Core.Domain.Rules;
using Papercut.Rules.Domain.Relaying;

namespace Papercut.Rules.Domain.Conditional.Relaying
{
    [Serializable]
    public class ConditionalRelayRule : RelayRule, IConditionalRule
    {
        string? _regexBodyMatch;

        string? _regexHeaderMatch;

        [Category("Information")]
        public override string Type => "Conditional Relay";

        [DisplayName("Regex Header Match")]
        public string RegexHeaderMatch
        {
            get => this._regexHeaderMatch;
            set
            {
                if (value == this._regexHeaderMatch)
                    return;
                this._regexHeaderMatch = value.IsSet() && value.IsValidRegex() ? value : null; ;
                this.OnPropertyChanged(nameof(this.RegexHeaderMatch));
            }
        }

        [DisplayName("Regex Body Match")]
        public string RegexBodyMatch
        {
            get => this._regexBodyMatch;
            set
            {
                if (value == this._regexBodyMatch)
                    return;

                this._regexBodyMatch = value.IsSet() && value.IsValidRegex() ? value : null;
                this.OnPropertyChanged(nameof(this.RegexBodyMatch));
            }
        }

        protected override IEnumerable<KeyValuePair<string, Lazy<object>>> GetPropertiesForDescription()
        {
            return base.GetPropertiesForDescription().Concat(this.GetProperties());
        }

        #region Begin Static Container Registrations

        /// <summary>
        /// Called dynamically from the RegisterStaticMethods() call in the container module.
        /// </summary>
        /// <param name="builder"></param>
        [UsedImplicitly]
        static void Register(ContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.RegisterType<ConditionalRelayRule>().AsSelf().As<IRule>().InstancePerDependency();
        }

        #endregion
    }
}