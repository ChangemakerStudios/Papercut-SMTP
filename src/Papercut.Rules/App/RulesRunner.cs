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


using System.Reflection;

using Autofac;

using Papercut.Core.Domain.Message;
using Papercut.Core.Domain.Rules;

namespace Papercut.Rules.App
{
    public class RulesRunner : IRulesRunner
    {
        readonly MethodInfo _dispatchRuleMethod;

        readonly ILifetimeScope _lifetimeScope;

        readonly ILogger _logger;

        public RulesRunner(ILifetimeScope lifetimeScope, ILogger logger)
        {
            this._lifetimeScope = lifetimeScope;
            this._logger = logger;
            this._dispatchRuleMethod = this.GetType()
                .GetMethod(
                    nameof(this.DispatchRuleAsync),
                    BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public async Task RunAsync([NotNull] IRule[] rules, [NotNull] MessageEntry messageEntry, CancellationToken token)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (messageEntry == null) throw new ArgumentNullException(nameof(messageEntry));

            var ruleTasks = new List<Task>();

            foreach (IRule rule in rules.Where(_ => _.IsEnabled))
            {
                token.ThrowIfCancellationRequested();

                ruleTasks.Add(
                    (Task)this._dispatchRuleMethod.MakeGenericMethod(rule.GetType()).Invoke(
                        this,
                        new object[] { rule, messageEntry, token }));
            }

            await Task.WhenAll(ruleTasks);
        }

        [UsedImplicitly]
        async Task DispatchRuleAsync<TRule>(TRule rule, MessageEntry messageEntry, CancellationToken token)
            where TRule : IRule
        {
            this._logger.Information(
                "Running Rule Dispatch for Rule {Rule} on Message {@MessageEntry}",
                rule,
                messageEntry);

            try
            {
                var ruleDispatcher = this._lifetimeScope.Resolve<IRuleDispatcher<TRule>>();
                await ruleDispatcher.DispatchAsync(rule, messageEntry, token);
            }
            catch (Exception ex)
            {
                this._logger.Warning(
                    ex,
                    "Failure Dispatching Rule {Rule} for Message {@MessageEntry}",
                    rule,
                    messageEntry);
            }
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

            builder.RegisterType<RulesRunner>().As<IRulesRunner>().SingleInstance();
        }

        #endregion
    }
}