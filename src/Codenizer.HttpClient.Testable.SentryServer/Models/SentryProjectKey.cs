using Newtonsoft.Json;

namespace Codenizer.HttpClient.Testable.SentryServer.Models
{
    public class SentryProjectKey
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "";
        
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("public")]
        public string PublicKey { get; set; } = "";

        [JsonProperty("secret")]
        public string SecretKey { get; set; } = "";

        [JsonProperty("isActive")]
        public bool IsActive { get; set; } = true;

        [JsonProperty("dateCreated")]
        public DateTime DateCreated { get; set; }

        [JsonProperty("dsn")]
        public SentryDsn Dsn { get; set; } = new();
    }

    public class SentryDsn
    {
        [JsonProperty("secret")]
        public string Secret { get; set; } = "";

        [JsonProperty("public")]
        public string Public { get; set; } = "";
        
        [JsonProperty("csp")]
        public string Csp { get; set; } = "";
        
        [JsonProperty("security")]
        public string Security { get; set; } = "";
        
        [JsonProperty("minidump")]
        public string Minidump { get; set; } = "";
        
         [JsonProperty("cdn")]
        public string Cdn { get; set; } = "";
    }
}
