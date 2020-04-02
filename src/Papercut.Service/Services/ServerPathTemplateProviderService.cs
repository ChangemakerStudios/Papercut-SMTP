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
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    using Papercut.Core.Domain.Paths;
    using Papercut.Service.Helpers;

    public class ServerPathTemplateProviderService : IPathTemplatesProvider
    {
        public ServerPathTemplateProviderService(PapercutServiceSettings serviceSettings)
        {
            var paths = serviceSettings.MessagePath.Split(';')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s));

            PathTemplates = new ObservableCollection<string>(paths);
        }

        public ObservableCollection<string> PathTemplates { get; private set; }
    }
}