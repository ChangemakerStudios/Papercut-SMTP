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


using Papercut.Core.Domain.BackgroundTasks;
using Papercut.Core.Domain.Rules;
using Papercut.Core.Infrastructure.Logging;
using Papercut.Rules.App;
using Papercut.Rules.Domain.Rules;

namespace Papercut.Service.Infrastructure.Rules
{
    public class RuleService : RuleServiceBase,
            IEventHandler<RulesUpdatedEvent>,
            IEventHandler<PapercutClientReadyEvent>,
            IEventHandler<NewMessageEvent>
    {
        private readonly IBackgroundTaskRunner _backgroundTaskRunner;
        private readonly IRulesRunner _rulesRunner;

        public RuleService(IRuleRepository ruleRepository,
            IBackgroundTaskRunner backgroundTaskRunner,
            ILogger logger,
            IRulesRunner rulesRunner) : base(ruleRepository, logger)
        {
            _backgroundTaskRunner = backgroundTaskRunner;
            _rulesRunner = rulesRunner;
        }

        public Task HandleAsync(NewMessageEvent @event, CancellationToken token = default)
        {
            _logger.Information(
                "New Message {MessageFile} Arrived -- Running Rules",
                @event.NewMessage);

            _backgroundTaskRunner.QueueBackgroundTask(
                async (t) =>
                {
                    try
                    {
                        await Task.Delay(2000, t);
                        await _rulesRunner.RunNewMessageRules(
                            Rules.OfType<INewMessageRule>().ToArray(),
                            @event.NewMessage,
                            t);
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex) when (_logger.ErrorWithContext(ex, "Failure Running New Message Rules"))
                    {
                    }
                });

            return Task.CompletedTask;
        }

        public Task HandleAsync(PapercutClientReadyEvent @event, CancellationToken token = default)
        {
            _logger.Debug("Attempting to Load Rules from {RuleFileName} on AppReady", RuleFileName);

            try
            {
                // accessing "Rules" forces the collection to be loaded
                if (Rules.Any())
                {
                    _logger.Information(
                        "Loaded {RuleCount} from {RuleFileName}",
                        Rules.Count,
                        RuleFileName);
                }
            }
            catch (Exception ex) when (_logger.ErrorWithContext(ex, "Error loading rules from file {RuleFileName}", RuleFileName))
            {
            }

            return Task.CompletedTask;
        }

        public Task HandleAsync(RulesUpdatedEvent @event, CancellationToken token = default)
        {
            Rules.Clear();
            Rules.AddRange(@event.Rules);
            Save();

            return Task.CompletedTask;
        }

        #region Begin Static Container Registrations

        static void Register(ContainerBuilder builder)
        {
            builder.RegisterType<RuleService>().AsImplementedInterfaces().AsSelf()
                .InstancePerLifetimeScope();
        }

        #endregion
    }
}