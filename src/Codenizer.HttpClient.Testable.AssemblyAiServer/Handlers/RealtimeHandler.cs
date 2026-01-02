using System.Net;
using System.Text.Json;
using Codenizer.HttpClient.Testable.AssemblyAiServer.Models;

namespace Codenizer.HttpClient.Testable.AssemblyAiServer.Handlers
{
    public static class RealtimeHandler
    {
        public static HttpResponseMessage HandleTokenRequest()
        {
             var response = new RealtimeTokenResponse
            {
                Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(response), System.Text.Encoding.UTF8, "application/json")
            };
        }
    }
}
