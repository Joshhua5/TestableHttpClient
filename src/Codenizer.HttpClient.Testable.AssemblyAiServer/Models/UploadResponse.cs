using System.Text.Json.Serialization;

namespace Codenizer.HttpClient.Testable.AssemblyAiServer.Models
{
    public class UploadResponse
    {
        [JsonPropertyName("upload_url")]
        public string UploadUrl { get; set; } = string.Empty;
    }
}
