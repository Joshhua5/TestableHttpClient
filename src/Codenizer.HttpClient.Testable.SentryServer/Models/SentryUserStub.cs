using Newtonsoft.Json;

namespace Codenizer.HttpClient.Testable.SentryServer.Models
{
    public class SentryUserStub
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "";

        [JsonProperty("email")]
        public string Email { get; set; } = "";

        [JsonProperty("username")]
        public string Username { get; set; } = "";
        
        [JsonProperty("ip_address")]
        public string IpAddress { get; set; } = "";
    }
}
