using Newtonsoft.Json;

namespace Codenizer.HttpClient.Testable.SentryServer.Models
{
    public class SentryEvent
    {
        [JsonProperty("event_id")]
        public string EventId { get; set; } = "";

        [JsonProperty("message")]
        public string Message { get; set; } = "";

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("level")]
        public string Level { get; set; } = "error";

        [JsonProperty("platform")]
        public string Platform { get; set; } = "csharp";

        [JsonProperty("tags")]
        public Dictionary<string, string> Tags { get; set; } = new();

        [JsonProperty("extra")]
        public Dictionary<string, object> Extra { get; set; } = new();

        [JsonProperty("user")]
        public SentryUserStub? User { get; set; }
    }
}
