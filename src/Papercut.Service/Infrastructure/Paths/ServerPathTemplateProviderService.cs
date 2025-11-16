// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2025 Jaben Cargman
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

namespace Papercut.Service.Infrastructure.Paths;

public class ServerPathTemplateProviderService : IPathTemplatesProvider
{
    public ServerPathTemplateProviderService(PathTemplateType type, SmtpServerOptions smtpServerOptions)
    {
        Type = type;

        var paths = type == PathTemplateType.Message ? smtpServerOptions.MessagePath : smtpServerOptions.LoggingPath;

        var messagePaths = paths.Split(';')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s));

        PathTemplates = new ObservableCollection<string>(messagePaths);
    }

    public PathTemplateType Type { get; }

    public ObservableCollection<string> PathTemplates { get; }

    #region Begin Static Container Registrations

    private static void Register(ContainerBuilder builder)
    {
        builder.Register(p => new ServerPathTemplateProviderService(PathTemplateType.Message, p.Resolve<SmtpServerOptions>()))
            .Keyed<IPathTemplatesProvider>(PathTemplateType.Message).SingleInstance();

        builder.Register(p => new ServerPathTemplateProviderService(PathTemplateType.Logging, p.Resolve<SmtpServerOptions>()))
            .Keyed<IPathTemplatesProvider>(PathTemplateType.Logging).SingleInstance();
    }

    #endregion
}