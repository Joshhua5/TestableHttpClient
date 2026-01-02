using Newtonsoft.Json;

namespace Codenizer.HttpClient.Testable.SentryServer.Models
{
    public class SentryProject
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "";

        [JsonProperty("slug")]
        public string Slug { get; set; } = "";

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("organization")]
        public SentryOrganization Organization { get; set; } = new();

        [JsonProperty("dateCreated")]
        public DateTime DateCreated { get; set; }
    }
}
