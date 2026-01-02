using System.Text.Json.Serialization;

namespace Codenizer.HttpClient.Testable.AssemblyAiServer.Models
{
    public class Transcript
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = "queued"; // queued, processing, completed, error

        [JsonPropertyName("audio_url")]
        public string AudioUrl { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("confidence")]
        public double? Confidence { get; set; }

        [JsonPropertyName("audio_duration")]
        public double? AudioDuration { get; set; }
        
        [JsonPropertyName("words")]
        public List<Word>? Words { get; set; }
        
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class Word
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("start")]
        public int Start { get; set; }

        [JsonPropertyName("end")]
        public int End { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }
        
        [JsonPropertyName("speaker")]
        public string? Speaker { get; set; }
    }
}
