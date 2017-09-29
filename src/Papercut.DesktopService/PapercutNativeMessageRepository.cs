

namespace Papercut.DesktopService
{
    using Papercut.Common.Domain;
    using Papercut.Core.Domain.Message;
    using Papercut.WebUI;
    using System.Net.Http;
    using Newtonsoft.Json;
    using System.Threading.Tasks;

    public class PapercutNativeMessageRepository: IEventHandler<WebUIServerReadyEvent>, IEventHandler<NewMessageEvent>
    {
        internal static PapercutNativeMessageRepository HandlerInstance {get;set;}
        HttpClient theClient;

        public void Handle(WebUIServerReadyEvent readyEvent)
        {
            theClient = readyEvent.TestServer.CreateClient();
        }
        
        public void Handle(NewMessageEvent @event)
        {
            
        }


        public async Task<object> ListAllMessages(object input)
        {
            if(this != HandlerInstance){
                return HandlerInstance?.ListAllMessages(input);
            }

            var req = JsonConvert.DeserializeObject<ListMessageRequest>((string)input);
            var response = await theClient.GetAsync($"/api/messages?start={req.start}&limit={req.limit}");
            return await TransformResponse(response);
        }

        public async Task<object> GetMessageDetail(object input)
        {
            if(this != HandlerInstance){
                return HandlerInstance?.GetMessageDetail(input);
            }

            var response = await theClient.GetAsync($"/api/messages/{input}");
            return await TransformResponse(response);
        }

        public async Task<object> DeleteAll(object input)
        {
            if(this != HandlerInstance){
                return HandlerInstance?.DeleteAll(input);
            }

            var response = await theClient.DeleteAsync("/api/messages");
            return await TransformResponse(response);
        }

        public Task<object> OnNewMessageArrives(object input)
        {
            if(this != HandlerInstance){
                return HandlerInstance?.OnNewMessageArrives(input);
            }

            return Task.FromResult((object)0);
        }

        async Task<object> TransformResponse(HttpResponseMessage response){
            return new TransformedHttpResponse {
                Status = (int)response.StatusCode,
                Content = await response.Content?.ReadAsStringAsync()
            };
        }
    }

    class TransformedHttpResponse{
        public int Status {get;set;}
        public string Content {get;set;}
    }

    class ListMessageRequest {
        public int start {get;set;}
        public int limit {get;set;}
    }
}