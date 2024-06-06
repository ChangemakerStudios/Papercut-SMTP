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


using Autofac;

using Papercut.Common.Domain;
using Papercut.Core.Domain.Application;
using Papercut.Core.Infrastructure.Lifecycle;
using Papercut.Domain.LifecycleHooks;

namespace Papercut.Smtp.Desktop.Infrastructure.Startup;

public class ReadyEventTrigger : IAppLifecycleStarted, IOrderable
{
    private readonly IAppMeta _appMeta;

    private readonly IMessageBus _messageBus;

    public ReadyEventTrigger(IMessageBus messageBus, IAppMeta appMeta)
    {
        this._messageBus = messageBus;
        this._appMeta = appMeta;
    }

    public async Task OnStartedAsync()
    {
        await this._messageBus.PublishAsync(
            new PapercutClientReadyEvent() { AppMeta = this._appMeta });
    }

    /// <summary>
    /// Start this last
    /// </summary>
    public int Order => 999999;

    #region Begin Static Container Registrations

    /// <summary>
    /// Called dynamically from the RegisterStaticMethods() call in the container module.
    /// </summary>
    /// <param name="builder"></param>
    [UsedImplicitly]
    static void Register(ContainerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RegisterType<ReadyEventTrigger>().AsImplementedInterfaces().SingleInstance();
    }

    #endregion
}