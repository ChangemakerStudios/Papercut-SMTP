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
    using Papercut.DesktopService.Backend;
    using Papercut.Message;
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Net.Http;
    using Newtonsoft.Json;
    using System.IO;

    public class NativeService
    {
        static HttpClient _httpService;
        static NewMessageRecieviedEvent _newMessageEventHolder;

        static async Task<object> RequestResource(dynamic input)
        {
            if(_httpService == null){
                return UIHttpResponse.NotReady;
            }

            var request = new UIHttpRequest{
                Method = (HttpMethod)Enum.Parse(typeof(HttpMethod), (string)input.method, true),
                Path = (string)input.path,
                ContentType = (string)input.contentType,
                Content = (byte[])input.content
            };
            var httpRequestMessage = new HttpRequestMessage(request.Method, request.Path);
            if(request.Content != null && request.Content.Length > 0){
                httpRequestMessage.Content = new ByteArrayContent(request.Content);
            }

            var response = await _httpService.SendAsync(httpRequestMessage);
            var uiResponse = new UIHttpResponse{
                Status = (int)response.StatusCode,
                ContentType = response.Content?.Headers.ContentType.MediaType
            };

            if(response.Content != null && response.Content.Headers.ContentLength > 0){
                uiResponse.Content = await response.Content.ReadAsByteArrayAsync();
            }
            return uiResponse;
        }

        static async Task<object> OnNewMessageArrives(object input)
        {
            var callback = input as Func<object, Task<object>>;
            if (callback != null && _newMessageEventHolder != null)
            {
                _newMessageEventHolder.NewMessageReceived += (s, e) => {
                    var msg = new NewMailMessageNotification
                    {
                        Subject = e.NewMessage.DisplayText,
                        Id = e.NewMessage.Name
                    };

                    callback(msg);
                };

                return await Task.FromResult((object)0);
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
                        NativeService._newMessageEventHolder = container.Resolve<NewMessageRecieviedEvent>();
                        container.Resolve<WebServerReadyEvent>().ServiceReady += (sender, e) =>
                        {
                            NativeService._httpService = e.HttpClient;
                        };
                        
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
                RequestResource = (Func<object, Task<object>>) RequestResource,
                OnNewMessageArrives = (Func<object, Task<object>>) OnNewMessageArrives,
                StopService = (Func<object, Task<object>>) StopPapercut
            };
        }
    }
}
