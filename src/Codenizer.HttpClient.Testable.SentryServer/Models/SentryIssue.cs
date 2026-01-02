using Newtonsoft.Json;

namespace Codenizer.HttpClient.Testable.SentryServer.Models
{
    public class SentryIssue
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "";

        [JsonProperty("shortId")]
        public string ShortId { get; set; } = "";

        [JsonProperty("title")]
        public string Title { get; set; } = "";

        [JsonProperty("culprit")]
        public string Culprit { get; set; } = "";

        [JsonProperty("status")]
        public string Status { get; set; } = "unresolved";

        [JsonProperty("project")]
        public SentryProject Project { get; set; } = new();

        [JsonProperty("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();

        [JsonProperty("assignedTo")]
        public SentryUserStub? AssignedTo { get; set; }
    }
}
