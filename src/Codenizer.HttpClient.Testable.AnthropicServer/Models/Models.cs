using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Codenizer.HttpClient.Testable.AnthropicServer.Models
{
    public class ModelListResponse
    {
        [JsonProperty("data")]
        public List<ModelData> Data { get; set; } = new();

        [JsonProperty("has_more")]
        public bool HasMore { get; set; }

        [JsonProperty("first_id")]
        public string? FirstId { get; set; }

        [JsonProperty("last_id")]
        public string? LastId { get; set; }
    }

    public class ModelData
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "model";

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
