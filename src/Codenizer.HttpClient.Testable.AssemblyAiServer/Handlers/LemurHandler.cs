using System.Net;
using System.Text.Json;
using Codenizer.HttpClient.Testable.AssemblyAiServer.Models;

namespace Codenizer.HttpClient.Testable.AssemblyAiServer.Handlers
{
    public static class LemurHandler
    {
        public static async Task<HttpResponseMessage> HandleAsync(HttpRequestMessage request)
        {
            var path = request.RequestUri!.AbsolutePath.TrimStart('/');
            
            if (request.Method == HttpMethod.Post)
            {
                if (path.EndsWith("task"))
                {
                    return HandleTask();
                }
                if (path.EndsWith("chat"))
                {
                    return HandleChat();
                }
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static HttpResponseMessage HandleTask()
        {
            var response = new LemurTaskResponse
            {
                RequestId = Guid.NewGuid().ToString(),
                Response = "This is a simulated LeMUR task response."
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(response), System.Text.Encoding.UTF8, "application/json")
            };
        }

        private static HttpResponseMessage HandleChat()
        {
             var response = new LemurChatResponse
            {
                RequestId = Guid.NewGuid().ToString(),
                Response = new List<LemurChatMessage>
                {
                    new LemurChatMessage { Role = "assistant", Content = "This is a simulated LeMUR chat response." }
                }
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(response), System.Text.Encoding.UTF8, "application/json")
            };
        }
    }
}
