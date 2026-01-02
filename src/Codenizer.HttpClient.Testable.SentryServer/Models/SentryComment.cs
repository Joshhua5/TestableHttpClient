using Newtonsoft.Json;

namespace Codenizer.HttpClient.Testable.SentryServer.Models
{
    public class SentryComment
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "";

        [JsonProperty("data")]
        public Dictionary<string, object> Data { get; set; } = new();

        [JsonProperty("dateCreated")]
        public DateTime DateCreated { get; set; }
    }
}
