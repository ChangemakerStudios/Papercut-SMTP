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
    using Autofac;
    
    using Papercut.Core.Infrastructure.Container;
    using Papercut.Service.Services;
    using System.Threading;

    using Serilog;
    using Papercut.Core.Domain.Application;
    using System.Threading.Tasks;
    using System.Reflection;

    public class Program
    {
        static int Main(string[] args)
        {
            var appTask = Startup(args);
            appTask.Wait();
            return appTask.Result;
        }

        public static Task<int> Startup(string[] args, Action<ILifetimeScope> bootstrap = null, Action shutdown = null)
        {
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            return Task.Factory.StartNew(() =>
                StartPapercutService((container) =>
                    {
                        Console.CancelKeyPress += Console_CancelKeyPress;
                        Console.Title = container.Resolve<IAppMeta>().AppName;
                        
                        bootstrap?.Invoke(container);
                    },
                    shutdown,
                    throwErrors: false));
        }

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Shutdown();
        }

        static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            WriteFatal(e.Exception);
        }

        static void WriteFatal(Exception ex)
        {
            Console.Error.WriteLine(ex);
            if (Log.Logger != null)
            {
                Log.Logger.Fatal(ex, "Unhandled Exception");
            }
        }

        static void WriteInfo(string info)
        {
            Console.WriteLine(info);
            if (Log.Logger != null)
            {
                Log.Logger.Information(info);
            }
        }

        #region Service Control

        static ManualResetEvent appWaitHandle = new ManualResetEvent(false);

        static int StartPapercutService(Action<ILifetimeScope> initialization, Action shutdown, bool throwErrors = true)
        {
            try
            {
                if (PapercutCoreModule.SpecifiedEntryAssembly == null)
                {
                    PapercutCoreModule.SpecifiedEntryAssembly = Assembly.GetEntryAssembly();
                }

                using (var appContainer = PapercutContainer.Instance.BeginLifetimeScope())
                {
                    initialization(appContainer);

                    var papercutService = appContainer.Resolve<PapercutServerService>();
                    papercutService.Start();

                    appWaitHandle.WaitOne();
                    try
                    {
                        papercutService.Stop();

                        appWaitHandle.Dispose();
                        appWaitHandle = null;

                        shutdown?.Invoke();
                    }
                    catch { }
                }

                return 0;
            }
            catch (Exception ex)
            {
                WriteFatal(ex);
                appWaitHandle.Set();

                if (throwErrors)
                {
                    throw;
                }
                return 1;
            }
        }

        public static void Shutdown()
        {
            WriteInfo("Exiting...");
            if (appWaitHandle != null)
            {
                appWaitHandle.Set();
            }
        }

        #endregion

    }
}