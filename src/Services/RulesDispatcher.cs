// /*  
//  * Papercut
//  *
//  *  Copyright © 2008 - 2012 Ken Robertson
//  *  Copyright © 2013 - 2014 Jaben Cargman
//  *  
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *  
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *  
//  *  Unless required by applicable law or agreed to in writing, software
//  *  distributed under the License is distributed on an "AS IS" BASIS,
//  *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *  See the License for the specific language governing permissions and
//  *  limitations under the License.
//  *  
//  */

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
        readonly ILogger _logger;

        readonly PapercutServiceBackendCoordinator _coordinator;

        public RulesDispatcher(ILogger logger, PapercutServiceBackendCoordinator coordinator)
        {
            _logger = logger;
            _coordinator = coordinator;
            RuleCollection = new RuleCollection();
            RuleFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rules.json");
        }

        public string RuleFileName { get; set; }

        public RuleCollection RuleCollection { get; set; }

        public void Handle(AppExitEvent @event)
        {
            if (!RuleCollection.Any()) return;

            try
            {
                RuleCollection.SaveTo(RuleFileName);
                _logger.Information(
                    "Saved {RuleCount} to {RuleFileName}",
                    RuleCollection.Count,
                    RuleFileName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error saving rules to file {RuleFileName}", RuleFileName);
            }
        }

        public void Handle(AppReadyEvent @event)
        {
            _logger.Debug("Attempting to Load Rules from {RuleFileName} on AppReady", RuleFileName);
            try
            {
                if (RuleCollection.LoadFrom(RuleFileName))
                {
                    _logger.Information(
                        "Loaded {RuleCount} from {RuleFileName}",
                        RuleCollection.Count,
                        RuleFileName);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error loading rules from file {RuleFileName}", RuleFileName);
            }
        }
    }
}