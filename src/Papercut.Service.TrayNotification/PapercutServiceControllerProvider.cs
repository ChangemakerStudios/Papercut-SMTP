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


using System.ServiceProcess;

using Autofac;

namespace Papercut.Service.TrayNotification;

/// <summary>
/// Provides access to the Papercut SMTP Service via ServiceController
/// </summary>
public class PapercutServiceControllerProvider
{
    private const string ServiceName = "Papercut.SMTP.Service";

    private static readonly TimeSpan ServiceTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets the current service status
    /// </summary>
    public ServiceControllerStatus GetStatus()
    {
        using var controller = GetController();
        return controller.Status;
    }

    protected ServiceController GetController() => new(ServiceName);

    /// <summary>
    /// Checks if the service exists/is installed
    /// </summary>
    public bool IsInstalled()
    {
        try
        {
            GetStatus();
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Starts the service and waits for it to reach Running status
    /// </summary>
    public void Start()
    {
        using var controller = GetController();
        controller.Start();
        controller.WaitForStatus(ServiceControllerStatus.Running, ServiceTimeout);
    }

    /// <summary>
    /// Stops the service and waits for it to reach Stopped status
    /// </summary>
    public void Stop()
    {
        using var controller = GetController();
        controller.Stop();
        controller.WaitForStatus(ServiceControllerStatus.Stopped, ServiceTimeout);
    }

    /// <summary>
    /// Restarts the service (stop then start)
    /// </summary>
    public void Restart()
    {
        using var controller = GetController();
        controller.Stop();
        controller.WaitForStatus(ServiceControllerStatus.Stopped, ServiceTimeout);
        controller.Start();
        controller.WaitForStatus(ServiceControllerStatus.Running, ServiceTimeout);
    }

    #region Begin Static Container Registrations

    [UsedImplicitly]
    private static void Register(ContainerBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.RegisterType<PapercutServiceControllerProvider>().AsSelf().InstancePerDependency();
    }

    #endregion
}
