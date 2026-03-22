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


using Autofac;

using Papercut.Infrastructure.IPComm.Network;

namespace Papercut.Service.TrayNotification.Infrastructure;

public class PapercutServiceTrayServer(
    PapercutIPCommServer ipCommServer,
    PapercutIPCommEndpoints papercutIpCommEndpoints,
    ILogger logger) : IStartable, IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private Task _serverTask;

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _serverTask?.Dispose();
        ipCommServer?.Dispose();
    }

    public void Start()
    {
        logger.Information("Starting IPComm Server for the Tray Service");

        _serverTask = Task.Run(async () =>
            {
                try
                {
                    await ipCommServer.StopAsync();
                    await ipCommServer.StartAsync(papercutIpCommEndpoints.TrayService);
                }
                catch (Exception ex)
                {
                    logger.Warning(
                        ex,
                        "Unable to Create Papercut Tray IPComm Server Listener on {IP}:{Port}. After 5 Retries. Failing",
                        ipCommServer.ListenIpAddress,
                        ipCommServer.ListenPort);
                }
            },
            _cancellationTokenSource.Token);
    }

    #region Begin Static Container Registrations

    /// <summary>
    /// Called dynamically from the RegisterStaticMethods() call in the container module.
    /// </summary>
    /// <param name="builder"></param>
    [UsedImplicitly]
    private static void Register(ContainerBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.RegisterType<PapercutServiceTrayServer>().AsImplementedInterfaces().SingleInstance();
    }

    #endregion
}