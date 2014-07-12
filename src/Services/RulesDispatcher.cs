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

namespace Papercut.Services
{
    using System;
    using System.IO;
    using System.Linq;

    using Papercut.Core.Events;
    using Papercut.Core.Rules;

    using Serilog;
    
    public class RulesDispatcher : IHandleEvent<AppReadyEvent>, IHandleEvent<AppExitEvent>
    {
        readonly PapercutServiceBackendCoordinator _coordinator;

        readonly RuleService _ruleService;

        readonly ILogger _logger;

        public RulesDispatcher(ILogger logger, PapercutServiceBackendCoordinator coordinator, RuleService ruleService)
        {
            _logger = logger;
            _coordinator = coordinator;
            _ruleService = ruleService;
        }

        public void Handle(AppExitEvent @event)
        {
            if (!_ruleService.Rules.Any()) return;

            try
            {
                _ruleService.Save();
                _logger.Information(
                    "Saved {RuleCount} to {RuleFileName}",
                    _ruleService.Rules.Count,
                    _ruleService.RuleFileName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error saving rules to file {RuleFileName}", _ruleService.RuleFileName);
            }
        }

        public void Handle(AppReadyEvent @event)
        {
            _logger.Debug("Attempting to Load Rules from {RuleFileName} on AppReady", _ruleService.RuleFileName);
            try
            {
                if (_ruleService.Rules.Any())
                {
                    _logger.Information(
                        "Loaded {RuleCount} from {RuleFileName}",
                        _ruleService.Rules.Count,
                        _ruleService.RuleFileName);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error loading rules from file {RuleFileName}", _ruleService.RuleFileName);
            }

            //RuleRespository.Add(new ForwardRule("127.0.0.1", "testing@papercut.com", "blah@papercut.com"));
        }
    }
}