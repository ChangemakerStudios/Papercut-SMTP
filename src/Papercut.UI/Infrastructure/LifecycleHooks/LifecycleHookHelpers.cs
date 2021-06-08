// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2021 Jaben Cargman
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


namespace Papercut.Infrastructure.LifecycleHooks
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Autofac;

    using Papercut.Common.Extensions;
    using Papercut.Core.Annotations;
    using Papercut.Domain.LifecycleHooks;

    using Serilog;

    public static class LifecycleHookHelpers
    {
        public static AppLifecycleActionResultType RunLifecycleHooks<TLifecycle>([NotNull] this ILifetimeScope scope, Func<TLifecycle, AppLifecycleActionResultType> runHook)
            where TLifecycle : IAppLifecycleHook
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));

            var logger = scope.Resolve<ILogger>();

            foreach (var appLifecycleHook in scope.Resolve<IEnumerable<TLifecycle>>().MaybeByOrderable())
            {
                logger.Debug("Running {LifecycleHookType}...", appLifecycleHook.GetType().FullName);

                var result = runHook(appLifecycleHook);

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

        public static AppLifecycleActionResultType RunPreStart([NotNull] this ILifetimeScope scope)
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));

            return scope.RunLifecycleHooks<IAppLifecyclePreStart>(hook => hook.OnPreStart());
        }

        public static async Task RunStartedAsync([NotNull] this ILifetimeScope scope)
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));

            var logger = scope.Resolve<ILogger>();

            foreach (var appLifecycleHook in scope.Resolve<IEnumerable<IAppLifecycleStarted>>().MaybeByOrderable())
            {
                logger.Debug("Running {LifecycleHookType}...", appLifecycleHook.GetType().FullName);

                await appLifecycleHook.OnStartedAsync();
            }
        }

        public static AppLifecycleActionResultType RunPreExit([NotNull] this ILifetimeScope scope)
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));

            return scope.RunLifecycleHooks<IAppLifecyclePreExit>(hook => hook.OnPreExit());
        }
    }
}