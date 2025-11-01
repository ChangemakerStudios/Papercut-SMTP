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


using Papercut.Core.Infrastructure.Container;
using Papercut.Infrastructure.LifecycleHooks;

namespace Papercut;

public partial class App : Application
{
    internal ILifetimeScope Container { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        Log.Debug("App.OnStartup");

        this.Container = Program.Container.BeginLifetimeScope(ContainerScope.UIScopeTag);

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Debug("App.OnExit");

        // run pre-exit
        AppLifecycleActionResultType runPreExit = AppLifecycleActionResultType.Continue;
            
        Task.Run(async () =>
        {
            runPreExit = await this.Container.RunPreExit();
        }).Wait();

        if (runPreExit == AppLifecycleActionResultType.Cancel)
        {
            // cancel exit
            return;
        }

        try
        {
            this.Container.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // no bother
        }

        base.OnExit(e);
    }
}