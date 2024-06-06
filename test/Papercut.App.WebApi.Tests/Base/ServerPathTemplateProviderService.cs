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


using System.Collections.ObjectModel;

using Papercut.Common.Helper;
using Papercut.Core.Domain.Paths;

namespace Papercut.App.WebApi.Tests.Base
{
    public class ServerPathTemplateProviderService : IPathTemplatesProvider
    {
        public ServerPathTemplateProviderService()
        {
            var basePath = Path.GetDirectoryName(typeof(ServerPathTemplateProviderService).Assembly.Location);
            var messageStoragePath = Path.Combine(basePath, $"Incoming-{StringHelpers.SmallRandomString()}");

            if (!Directory.Exists(messageStoragePath))
            {
                Directory.CreateDirectory(messageStoragePath);
            }

            this.MessagePathTemplates = new ObservableCollection<string>(new[] {messageStoragePath});

            var loggingStoragePath = Path.Combine(basePath, $"Papercut.Service-{StringHelpers.SmallRandomString()}");

            if (!Directory.Exists(loggingStoragePath))
            {
                Directory.CreateDirectory(loggingStoragePath);
            }

            this.LoggingPathTemplates = new ObservableCollection<string>(new[] { loggingStoragePath });
        }

        public ObservableCollection<string> MessagePathTemplates { get; }

        public ObservableCollection<string> LoggingPathTemplates { get; }
    }
}