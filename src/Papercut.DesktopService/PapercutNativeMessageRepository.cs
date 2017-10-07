

namespace Papercut.DesktopService
{
    using Newtonsoft.Json;
    using System.Threading.Tasks;
    using System;
    using Papercut.Core.Domain.Message;
    using Papercut.Service.Web.Controllers;
    using Newtonsoft.Json.Serialization;

    class PapercutNativeMessageRepository
    {
        public NewMessageEventHolder NewMessageEventHolder { get; set; }
        public MessagesController WebMsgCtrl { get; set; }

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