﻿// Papercut
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


using Papercut.Core.Domain.Rules;
using Papercut.Rules.App;
using Papercut.Rules.Domain.Rules;

namespace Papercut.Service.Infrastructure.Rules
{
    public class RuleService : RuleServiceBase,
        IEventHandler<RulesUpdatedEvent>,
        IEventHandler<PapercutClientReadyEvent>,
        IEventHandler<NewMessageEvent>
    {
        private readonly CancellationTokenSource _cancellationTokenSource =
            new CancellationTokenSource();

        readonly IRulesRunner _rulesRunner;

        public RuleService(
            IRuleRepository ruleRepository,
            ILogger logger,
            IRulesRunner rulesRunner)
            : base(ruleRepository, logger)
        {
            this._rulesRunner = rulesRunner;
        }

        public Task HandleAsync(NewMessageEvent @event, CancellationToken token = default)
        {
            this._logger.Information(
                "New Message {MessageFile} Arrived -- Running Rules",
                @event.NewMessage);

            Task.Run(
                async () =>
                {
                    try
                    {
                        await Task.Delay(2000, this._cancellationTokenSource.Token);
                        await this._rulesRunner.RunAsync(
                            this.Rules.ToArray(),
                            @event.NewMessage,
                            this._cancellationTokenSource.Token);
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failure Running Rules");
                    }
                },
                this._cancellationTokenSource.Token);

            return Task.CompletedTask;
        }

        public Task HandleAsync(PapercutClientReadyEvent @event, CancellationToken token = default)
        {
            this._logger.Debug("Attempting to Load Rules from {RuleFileName} on AppReady", this.RuleFileName);

            try
            {
                // accessing "Rules" forces the collection to be loaded
                if (this.Rules.Any())
                {
                    this._logger.Information(
                        "Loaded {RuleCount} from {RuleFileName}",
                        this.Rules.Count,
                        this.RuleFileName);
                }
            }
            catch (Exception ex)
            {
                this._logger.Error(ex, "Error loading rules from file {RuleFileName}", this.RuleFileName);
            }

            return Task.CompletedTask;
        }

        public Task HandleAsync(RulesUpdatedEvent @event, CancellationToken token = default)
        {
            this.Rules.Clear();
            this.Rules.AddRange(@event.Rules);
            this.Save();

            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._cancellationTokenSource.Cancel();
            }
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