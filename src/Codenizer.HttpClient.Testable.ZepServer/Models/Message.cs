using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codenizer.HttpClient.Testable.ZepServer.Models
{
    public class Message
    {
        [JsonProperty("uuid")]
        public string Uuid { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("role_type")]
        public string RoleType { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("token_count")]
        public int TokenCount { get; set; }

        [JsonProperty("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
