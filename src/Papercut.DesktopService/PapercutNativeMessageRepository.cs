

namespace Papercut.DesktopService
{
    using System.Net.Http;
    using Newtonsoft.Json;
    using System.Threading.Tasks;
    using Papercut.Service;
    using System;

    class PapercutNativeMessageRepository
    {
        public PublicServiceFacade PapercutFacade { get; set; }        

        public async Task<TransformedHttpResponse> ListAll(string parameters)
        {
            if(PapercutWebClient == null)
            {
                return TransformedHttpResponse.NotReady;
            }

            var req = JsonConvert.DeserializeObject<ListMessageRequest>(parameters);
            var uri = $"/api/messages?start={req.start}";
            if(req.limit > 0)
            {
                uri += $"&limit={req.limit}";
            }

            var response = await PapercutWebClient.GetAsync(uri);
            return await TransformResponse(response);
        }

        public async Task<TransformedHttpResponse> GetDetail(string msgId)
        {
            if (PapercutWebClient == null)
            {
                return TransformedHttpResponse.NotReady;
            }

            var response = await PapercutWebClient.GetAsync($"/api/messages/{msgId}");
            return await TransformResponse(response);
        }

        public async Task<TransformedHttpResponse> DeleteAll()
        {
            if (PapercutWebClient == null)
            {
                return TransformedHttpResponse.NotReady;
            }

            var response = await PapercutWebClient.DeleteAsync("/api/messages");
            return await TransformResponse(response);
        }

        public Task<object> OnNewMessageArrives(Action<MailMessageNotification> input)
        {
            if (PapercutFacade != null)
            {
                PapercutFacade.NewMessageReceived += (s, e) => {
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
        

        HttpClient PapercutWebClient {
            get
            {
                return PapercutFacade?.PapercutWebClient;
            }
        }

        async Task<TransformedHttpResponse> TransformResponse(HttpResponseMessage response){
            return new TransformedHttpResponse {
                Status = (int)response.StatusCode,
                Content = await response.Content?.ReadAsStringAsync()
            };
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