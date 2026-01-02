using System.Net;
using System.Net.Http;
using Codenizer.HttpClient.Testable.AnthropicServer.Models;
using Newtonsoft.Json;

namespace Codenizer.HttpClient.Testable.AnthropicServer.Handlers
{
    public class TokenCountingHandler
    {
        public static HttpResponseMessage Handle(HttpRequestMessage request)
        {
            // For simulation, we just return a fixed token count or length of content
            // We don't actually tokenize.
            var response = new CountTokensResponse
            {
                InputTokens = 1337 // Leet tokens
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(response), System.Text.Encoding.UTF8, "application/json")
            };
        }
    }
}
