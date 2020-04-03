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

namespace Papercut.Service.Services
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Papercut.Common.Domain;
    using Papercut.Common.Extensions;
    using Papercut.Core.Domain.Message;
    using Papercut.Core.Domain.Rules;
    using Papercut.Core.Infrastructure.Lifecycle;
    using Papercut.Rules;

    using Serilog;

    public class RuleService : RuleServiceBase,
        IEventHandler<RulesUpdatedEvent>,
        IEventHandler<PapercutClientReadyEvent>,
        IEventHandler<NewMessageEvent>
    {
        readonly IRulesRunner _rulesRunner;

        public RuleService(
            RuleRepository ruleRepository,
            ILogger logger,
            IRulesRunner rulesRunner)
            : base(ruleRepository, logger)
        {
            _rulesRunner = rulesRunner;
        }

        public void Handle(PapercutClientReadyEvent @event)
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
            catch (Exception ex)
            {
                _logger.Error(ex, "Error loading rules from file {RuleFileName}", RuleFileName);
            }
        }

        public void Handle(NewMessageEvent @event)
        {
            _logger.Information(
                "New Message {MessageFile} Arrived -- Running Rules",
                @event.NewMessage);

            Task.Factory.StartNew(
                async () =>
                {
                    await Task.Delay(2000);

                    this._rulesRunner.Run(this.Rules.ToArray(), @event.NewMessage);
                });
        }

        public void Handle(RulesUpdatedEvent @event)
        {
            Rules.Clear();
            Rules.AddRange(@event.Rules);
            Save();
        }
    }
}