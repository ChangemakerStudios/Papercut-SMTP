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


namespace Papercut.Services
{
    using System.Diagnostics;
    using System.Reflection;

    using Papercut.Common.Domain;
    using Papercut.Core.Infrastructure.Lifecycle;

    using Serilog;

    public class ReportVersionService : IEventHandler<PapercutClientReadyEvent>
    {
        private readonly ILogger _logger;

        public ReportVersionService(ILogger logger)
        {
            this._logger = logger;
        }

        public void Handle(PapercutClientReadyEvent @event)
        {
            var productVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

            this._logger.Information("Papercut Version {PapercutVersion:l}", productVersion);
        }
    }
}