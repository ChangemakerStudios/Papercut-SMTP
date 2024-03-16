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

using Papercut.Core.Domain.Rules;
using Papercut.Message;

namespace Papercut.Rules.Domain.Relaying
{
    [UsedImplicitly]
    public class RelayRuleDispatch : BaseRelayRuleDispatch<RelayRule>
    {
        public RelayRuleDispatch(Lazy<MimeMessageLoader> mimeMessageLoader, ILogger logger)
            : base(mimeMessageLoader, logger)
        {
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

            builder.RegisterType<RelayRuleDispatch>()
                .As<IRuleDispatcher<RelayRule>>().AsSelf().InstancePerDependency();
        }

        #endregion
    }
}