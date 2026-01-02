using Newtonsoft.Json;

namespace Codenizer.HttpClient.Testable.SentryServer.Models
{
    public class SentryRelease
    {
        [JsonProperty("version")]
        public string Version { get; set; } = "";

        [JsonProperty("shortVersion")]
        public string ShortVersion { get; set; } = "";

        [JsonProperty("dateCreated")]
        public DateTime DateCreated { get; set; }

        [JsonProperty("projects")]
        public List<SentryProject> Projects { get; set; } = new();
    }
}
