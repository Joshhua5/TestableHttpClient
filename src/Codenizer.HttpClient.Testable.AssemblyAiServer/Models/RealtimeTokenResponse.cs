using System.Text.Json.Serialization;

namespace Codenizer.HttpClient.Testable.AssemblyAiServer.Models
{
    public class RealtimeTokenResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
    }
}
