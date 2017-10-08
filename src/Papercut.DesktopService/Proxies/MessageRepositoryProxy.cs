// Papercut
// 
// Copyright � 2008 - 2012 Ken Robertson
// Copyright � 2013 - 2017 Jaben Cargman
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
    using Newtonsoft.Json;
    using System.Threading.Tasks;
    using System;
    using Papercut.Service.Web.Controllers;
    using Newtonsoft.Json.Serialization;
    using Papercut.DesktopService.Events;
    using Microsoft.AspNetCore.Mvc;
    using System.IO;
    using System.Collections.Generic;

    class MessageRepositoryProxy
    {
        private readonly MessagesController WebMsgCtrl;
        private readonly NewMessageRecieviedEvent NewMessageEventHolder;

        public MessageRepositoryProxy(MessagesController webMessageController, NewMessageRecieviedEvent messageReceivedEvent)
        {
            this.NewMessageEventHolder = messageReceivedEvent;
            this.WebMsgCtrl = webMessageController;
        }

        public async Task<TransformedHttpResponse> ListAll(string parameters)
        {
            if (WebMsgCtrl == null)
            {
                return TransformedHttpResponse.NotReady;
            }

            var req = JsonConvert.DeserializeObject<ListMessageRequest>(parameters);
            if(req.limit <= 0)
            {
                req.limit = 10;
            }

            var response = WebMsgCtrl.GetAll(req.limit, req.start);
            return await RespondOK(response);
        }

        public async Task<TransformedHttpResponse> GetDetail(string msgId)
        {
            if (WebMsgCtrl == null)
            {
                return TransformedHttpResponse.NotReady;
            }
            
            var response = WebMsgCtrl.Get(msgId);
            return await RespondOK(response);
        }

        public async Task<byte[]> DownloadRawMessage(string msgId)
        {
            if (WebMsgCtrl == null)
            {
                return new byte[0];
            }
            
            var result = WebMsgCtrl.DownloadRaw(msgId);
            var fileResult = result as FileStreamResult;
            return await ToByteArray(fileResult.FileStream);
        }

        public async Task<byte[]> DownloadSection(string inputStr)
        {
            if (WebMsgCtrl == null)
            {
                return new byte[0];
            }

            var parameters = JsonConvert.DeserializeObject<List<string>>(inputStr);            
            var result = WebMsgCtrl.DownloadSection(parameters[0], int.Parse(parameters[1]));
            var fileResult = result as FileStreamResult;
            return await ToByteArray(fileResult.FileStream);
        }

        public async Task<byte[]> DownloadSectionContent(string inputStr)
        {
            if (WebMsgCtrl == null)
            {
                return new byte[0];
            }

            var parameters = JsonConvert.DeserializeObject<List<string>>(inputStr);            
            var result = WebMsgCtrl.DownloadSectionContent(parameters[0], parameters[1]);
            var fileResult = result as FileStreamResult;
            return await ToByteArray(fileResult.FileStream);
        }

        public async Task<TransformedHttpResponse> DeleteAll()
        {
            if (WebMsgCtrl == null)
            {
                return TransformedHttpResponse.NotReady;
            }

            WebMsgCtrl.DeleteAll();
            return await RespondOK();
        }

        public Task<object> OnNewMessageArrives(Action<MailMessageNotification> input)
        {
            if (NewMessageEventHolder != null)
            {
                NewMessageEventHolder.NewMessageReceived += (s, e) => {
                    var msg = new MailMessageNotification
                    {
                        Subject = e.NewMessage.DisplayText,
                        Id = e.NewMessage.Name
                    };
                    input(msg);
                };
            }

            return Task.FromResult((object)0);
        }


        static JsonSerializerSettings camelCaseJsonSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
        Task<TransformedHttpResponse> RespondOK(object val = null){
            return Task.FromResult(new TransformedHttpResponse
            {
                Status = 200,
                Content = JsonConvert.SerializeObject(val, camelCaseJsonSettings)
            });
        }

        async Task<byte[]> ToByteArray(Stream stream){
            if(!stream.CanRead){
                return new byte[0];
            }

            using(var ms = new MemoryStream()){
                await stream.CopyToAsync(ms);
                return ms.ToArray();
            }
        }
    }

    class MailMessageNotification
    {
        public string Subject { get; set; }
        public string From { get; set; }
        public string Id { get; set; }
    }

    class TransformedHttpResponse{
        public static TransformedHttpResponse NotReady = new TransformedHttpResponse
        {
            Status = 503
        };

        public int Status {get;set;}
        public string Content {get;set;}
    }

    class ListMessageRequest {
        public int start {get;set;}
        public int limit {get;set;}
    }
}