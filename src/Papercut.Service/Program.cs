// Papercut
// 
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2017 Jaben Cargman
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

namespace Papercut.Service
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Autofac;
    
    using Papercut.Core.Infrastructure.Container;

    using System.Threading;

    using Serilog;
    using Papercut.Core.Domain.Application;
    using System.Threading.Tasks;
    using System.Reflection;
    using System.Runtime.Loader;

    using Papercut.Core.Infrastructure.Lifecycle;

    public class Program
    {
        public static int Main(string[] args)
        {
            Log.Logger = PapercutContainer.RootLogger;

            return RunAsync().GetAwaiter().GetResult();
        }

        private static void HookHandlers()
        {
            TaskScheduler.UnobservedTaskException += (sender, e) => Log.Logger.Error(e.Exception, "Unobserved Task Exception");
            Console.CancelKeyPress += (s, e) => Shutdown(true);
            AssemblyLoadContext.Default.Unloading += context => Shutdown();
        }

        public static async Task<int> RunAsync()
        {
            HookHandlers();

            return await RunContainer(
                       (scope, token) =>
                       {
                           Console.Title = scope.Resolve<IAppMeta>().AppName;
                           return Task.CompletedTask;
                       });
        }

        #region Service Control

        private static readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public static bool HandleExceptions { get; set; } = true;

        public static async Task<int> RunContainer(Func<ILifetimeScope, CancellationToken, Task> runAction, Func<ILifetimeScope, Task> shutdownAction = null)
        {
            try
            {
                if (PapercutCoreModule.SpecifiedEntryAssembly == null)
                {
                    PapercutCoreModule.SpecifiedEntryAssembly = Assembly.GetEntryAssembly();
                }

                using (var appContainer = PapercutContainer.Instance.BeginLifetimeScope())
                {
                    await runAction(appContainer, _cancellationTokenSource.Token);

                    var tasks = new List<Task>();

                    // run all
                    foreach (var service in appContainer.Resolve<IEnumerable<IStartupService>>().ToList())
                    {
                        tasks.Add(service.Start(_cancellationTokenSource.Token));
                    }

                    _cancellationTokenSource.Token.WaitHandle.WaitOne();

                    // wait for the processes to finish
                    await Task.WhenAll(tasks);

                    if (shutdownAction != null)
                    {
                        await shutdownAction.Invoke(appContainer);
                    }
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                // all good
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, "Unhandled Exception");

                if (!HandleExceptions)
                {
                    throw;
                }
            }

            return 1;
        }

        public static void Shutdown(bool cancelled = false)
        {
            try
            {
                PapercutContainer.RootLogger.Information("Shutting Down (Cancelled: {Cancelled})...", cancelled);

                _cancellationTokenSource.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // not logged
            }
        }

        #endregion
    }
}