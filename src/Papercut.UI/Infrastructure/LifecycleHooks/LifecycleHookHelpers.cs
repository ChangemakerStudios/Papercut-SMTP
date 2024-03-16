// Papercut
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

using Papercut.Common.Extensions;
using Papercut.Domain.LifecycleHooks;

namespace Papercut.Infrastructure.LifecycleHooks
{
    public static class LifecycleHookHelpers
    {
        public static async Task<AppLifecycleActionResultType> RunLifecycleHooks<TLifecycle>(this IComponentContext container, Func<TLifecycle, Task<AppLifecycleActionResultType>> runHook)
            where TLifecycle : IAppLifecycleHook
        {
            if (container == null) throw new ArgumentNullException(nameof(container));

            var logger = container.Resolve<ILogger>();

            foreach (var appLifecycleHook in container.Resolve<IEnumerable<TLifecycle>>().MaybeByOrderable())
            {
                logger.Debug("Running {LifecycleHookType}...", appLifecycleHook.GetType().FullName);

                var result = await runHook(appLifecycleHook);

                if (result == AppLifecycleActionResultType.Cancel)
                {
                    logger.Debug(
                        "{LifecycleHookType} has cancelled action {TLifecycle}",
                        appLifecycleHook.GetType().FullName,
                        typeof(TLifecycle));

                    return AppLifecycleActionResultType.Cancel;
                }
            }

            return AppLifecycleActionResultType.Continue;
        }

        public static async Task<AppLifecycleActionResultType> RunPreExit(this IComponentContext container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));

            return await container.RunLifecycleHooks<IAppLifecyclePreExit>(hook => hook.OnPreExit());
        }

        public static async Task<AppLifecycleActionResultType> RunPreStart(this IComponentContext container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));

            return await container.RunLifecycleHooks<IAppLifecyclePreStart>(hook => hook.OnPreStart());
        }

        public static async Task RunStarted(this IComponentContext container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));

            var logger = container.Resolve<ILogger>();

            foreach (var appLifecycleHook in container.Resolve<IEnumerable<IAppLifecycleStarted>>().MaybeByOrderable())
            {
                logger.Debug("Running {LifecycleHookType}...", appLifecycleHook.GetType().FullName);

                await appLifecycleHook.OnStartedAsync();
            }
        }
    }
}