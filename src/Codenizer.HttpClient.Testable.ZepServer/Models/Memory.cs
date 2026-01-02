using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codenizer.HttpClient.Testable.ZepServer.Models
{
    public class Memory
    {
        [JsonProperty("messages")]
        public List<Message> Messages { get; set; } = new List<Message>();

        [JsonProperty("summary")]
        public Summary Summary { get; set; }

        [JsonProperty("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class Summary
    {
        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
        
        [JsonProperty("recent_message_uuid")]
        public string RecentMessageUuid { get; set; }
        
        [JsonProperty("token_count")]
        public int TokenCount { get; set; }
    }
}
