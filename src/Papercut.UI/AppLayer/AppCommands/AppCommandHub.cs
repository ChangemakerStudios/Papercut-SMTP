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


using System.Reactive.Subjects;

using Papercut.Domain.AppCommands;

namespace Papercut.AppLayer.AppCommands;

public class AppCommandHub : Disposable, IAppCommandHub
{
    private readonly Subject<ShutdownCommand> _onShutdown = new Subject<ShutdownCommand>();

    public IObservable<ShutdownCommand> OnShutdown => this._onShutdown;

    public void Shutdown(int exitCode = 0)
    {
        var command = new ShutdownCommand(exitCode);
        this._onShutdown.OnNext(command);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._onShutdown.Dispose();
        }
    }

    #region Begin Static Container Registrations

    /// <summary>
    /// Called dynamically from the RegisterStaticMethods() call in the container module.
    /// </summary>
    /// <param name="builder"></param>
    [UsedImplicitly]
    static void Register(ContainerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RegisterType<AppCommandHub>().As<IAppCommandHub>().SingleInstance();
    }

    #endregion
}