using System.Text.Json.Serialization;

namespace Codenizer.HttpClient.Testable.AssemblyAiServer.Models
{
    public class LemurTaskResponse
    {
        [JsonPropertyName("request_id")]
        public string RequestId { get; set; } = string.Empty;

        [JsonPropertyName("response")]
        public string Response { get; set; } = string.Empty;
    }

    public class LemurChatResponse
    {
         [JsonPropertyName("request_id")]
        public string RequestId { get; set; } = string.Empty;

        [JsonPropertyName("response")]
        public List<LemurChatMessage> Response { get; set; } = new();
    }

    public class LemurChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }
}
