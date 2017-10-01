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

namespace Papercut.DesktopService
{
    using System;
    using System.Threading.Tasks;
    using Papercut.Service;
    using Autofac;
    using System.Threading;
    using Papercut.Core.Infrastructure.Container;
    using System.Reflection;

    // entry point for Desktop Electron Edge
    public class Startup
    {
        public Task<object> Invoke(object input)
        {
            //Console.WriteLine("Waiting 30s for debugger...");
            //Thread.Sleep(30 * 1000);

            var task = new TaskCompletionSource<object>();

            var _ = Task.Factory.StartNew(() => {
                try
                {
                    PapercutCoreModule.SpecifiedEntryAssembly = (typeof(Startup).GetTypeInfo()).Assembly;
                    Program.StartPapercutService((container) =>
                    {
                        PapercutNativeService.MailMessageRepo = new PapercutNativeMessageRepository
                        {
                            PapercutFacade = container.Resolve<PublicServiceFacade>()
                        };

                        task.SetResult(new
                        {
                            MessageRepository = PapercutNativeService.ExportAll(),
                            StopService = (Func<object, Task<object>>)Stop
                        });
                    });
                }
                catch(Exception ex)
                {
                    task.SetException(ex);
                }
            });

            return task.Task;
        }

        static Task<object> Stop(object input){
            Program.StopPapercutService();
            return Task.FromResult((object)0);
        }
    }
}