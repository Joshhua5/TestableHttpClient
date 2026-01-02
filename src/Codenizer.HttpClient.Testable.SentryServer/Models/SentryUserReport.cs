using Newtonsoft.Json;

namespace Codenizer.HttpClient.Testable.SentryServer.Models
{
    public class SentryUserReport
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "";
        
        [JsonProperty("event_id")]
        public string EventId { get; set; } = "";

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("email")]
        public string Email { get; set; } = "";

        [JsonProperty("comments")]
        public string Comments { get; set; } = "";

        [JsonProperty("dateCreated")]
        public DateTime DateCreated { get; set; }
    }
}
