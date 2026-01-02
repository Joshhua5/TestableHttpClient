using Newtonsoft.Json;

namespace Codenizer.HttpClient.Testable.SentryServer.Models
{
    public class SentryOrganization
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "";

        [JsonProperty("slug")]
        public string Slug { get; set; } = "";

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("dateCreated")]
        public DateTime DateCreated { get; set; }
    }
}
