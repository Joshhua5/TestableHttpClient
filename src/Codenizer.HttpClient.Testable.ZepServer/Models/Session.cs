using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codenizer.HttpClient.Testable.ZepServer.Models
{
    public class Session
    {
        [JsonProperty("session_id")]
        public string SessionId { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
