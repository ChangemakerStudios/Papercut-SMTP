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


using Microsoft.Extensions.Options;

using Papercut.Service.Domain;

namespace Papercut.Service.Infrastructure.Configuration;

public class SmtpServerOptionsProvider(
    Lazy<IOptions<SmtpServerOptions>> smtpServerOptions,
    Lazy<ISettingStore> settingStore,
    Lazy<ILogger> logger)
    : ISmtpServerOptionsProvider
{
    protected ISettingStore SettingStore => settingStore.Value;

    protected ILogger Logger => logger.Value;

    protected SmtpServerOptions SmtpServerOptions => smtpServerOptions.Value.Value;

    public SmtpServerSettings Settings =>
        SmtpServerOptions.GetSettings(SettingStore.Get("IP", SmtpServerOptions.IP), SettingStore.Get("Port", SmtpServerOptions.Port));

    #region Begin Static Container Registrations

    /// <summary>
    /// Called dynamically from the RegisterStaticMethods() call in the container module.
    /// </summary>
    /// <param name="builder"></param>
    [UsedImplicitly]
    private static void Register(ContainerBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.RegisterType<SmtpServerOptionsProvider>().As<ISmtpServerOptionsProvider>().AsSelf().InstancePerLifetimeScope();
        builder.Register(p => p.Resolve<ISmtpServerOptionsProvider>().Settings).AsSelf().InstancePerDependency();
    }

    #endregion
}
