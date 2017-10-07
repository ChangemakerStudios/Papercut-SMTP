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
    using Autofac;
    using Papercut.Core.Infrastructure.Container;
    using Papercut.DesktopService.Events;
    using Papercut.Message;
    using System;
    using System.Reflection;
    using System.Threading.Tasks;


    public class NativeService
    {
        static MessageRepositoryProxy MailMessageRepo { get; set; }

        static async Task<object> ListAll(object input)
        {
            var paramters = input?.ToString();
            return await MailMessageRepo.ListAll(paramters);
        }

        static async Task<object> DeleteAll(object input)
        {
            return await MailMessageRepo.DeleteAll();
        }

        static async Task<object> GetDetail(object input)
        {
            var id = input?.ToString();
            return await MailMessageRepo.GetDetail(id);
        }

        static async Task<object> OnNewMessageArrives(object input)
        {
            var callback = input as Func<object, Task<object>>;
            if (callback != null)
            {
                return await MailMessageRepo.OnNewMessageArrives(async (ev) =>
                {
                    await callback(ev);
                });
            }

            return await Task.FromResult((object)0);
        }


        static Task<object> StopPapercut(object input)
        {
            Papercut.Service.Program.StopPapercutService();
            return Task.FromResult((object)0);
        }

        static TaskCompletionSource<object> _startupProcess;
        public static Task<object> StartPapercut(Assembly entryPointAssembly)
        {
            if(_startupProcess != null)
            {
                throw new InvalidOperationException("Papercut is already started.");
            }

            _startupProcess = new TaskCompletionSource<object>();
            Task.Factory.StartNew(() =>
            {
                try
                {
                    PapercutCoreModule.SpecifiedEntryAssembly = entryPointAssembly;
                    Papercut.Service.Program.StartPapercutService((container) =>
                    {
                        var repo = container.Resolve<MessageRepository>();
                        var loader = container.Resolve<MimeMessageLoader>();

                        NativeService.MailMessageRepo = new MessageRepositoryProxy(new Service.Web.Controllers.MessagesController(repo, loader), container.Resolve<NewMessageRecieviedEvent>());
                        container.Resolve<ServiceReadyEvent>().ServiceReady += (sender, e) =>
                        {
                            _startupProcess.SetResult(NativeService.ExportAll());
                        };
                    });
                }
                catch (Exception ex)
                {
                    _startupProcess.TrySetException(ex);
                }
            });

            return _startupProcess.Task;
        }

        static object ExportAll() {
            return new
            {
                ListAllMessages = (Func<object, Task<object>>) ListAll,
                DeleteAllMessages = (Func<object, Task<object>>) DeleteAll,
                GetMessageDetail = (Func<object, Task<object>>) GetDetail,
                OnNewMessageArrives = (Func<object, Task<object>>) OnNewMessageArrives,
                StopService = (Func<object, Task<object>>) StopPapercut
            };
        }
    }
}
