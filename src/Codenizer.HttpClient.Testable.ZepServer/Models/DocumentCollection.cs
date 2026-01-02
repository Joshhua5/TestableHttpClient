using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codenizer.HttpClient.Testable.ZepServer.Models
{
    public class DocumentCollection
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("uuid")]
        public string Uuid { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        [JsonIgnore]
        public List<Document> Documents { get; set; } = new List<Document>();
    }

    public class Document
    {
        [JsonProperty("uuid")]
        public string Uuid { get; set; } = Guid.NewGuid().ToString();
        
        [JsonProperty("content")]
        public string Content { get; set; }
        
        [JsonProperty("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
