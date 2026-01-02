using System.Text.Json.Serialization;

namespace Codenizer.HttpClient.Testable.AssemblyAiServer.Models
{
    public class TranscriptListResponse
    {
        [JsonPropertyName("transcripts")]
        public List<Transcript> Transcripts { get; set; } = new();

        [JsonPropertyName("page_details")]
        public PageDetails PageDetails { get; set; } = new();
    }

    public class PageDetails
    {
        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("result_count")]
        public int ResultCount { get; set; }

        [JsonPropertyName("current_url")]
        public string CurrentUrl { get; set; } = string.Empty;
        
        [JsonPropertyName("prev_url")]
        public string? PrevUrl { get; set; }
        
        [JsonPropertyName("next_url")]
        public string? NextUrl { get; set; }
    }
}
