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

using Autofac;

using Papercut.Core.Domain.Paths;

namespace Papercut.Service.TrayNotification;

public class BasicPathTemplateProvider : IPathTemplatesProvider
{
    public ObservableCollection<string> MessagePathTemplates { get; } = new(["%BaseDirectory%\\Incoming"]);

    public ObservableCollection<string> LoggingPathTemplates { get; } = new(["%BaseDirectory%\\Logs"]);

    #region Begin Static Container Registrations

    private static void Register(ContainerBuilder builder)
    {
        builder.RegisterType<BasicPathTemplateProvider>().AsImplementedInterfaces().AsSelf()
            .InstancePerLifetimeScope();
    }

    #endregion
}