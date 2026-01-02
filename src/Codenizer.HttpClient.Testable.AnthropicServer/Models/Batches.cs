using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Codenizer.HttpClient.Testable.AnthropicServer.Models
{
    public class CreateMessageBatchRequest
    {
        [JsonProperty("requests")]
        public List<MessageBatchRequestItem> Requests { get; set; } = new();
    }

    public class MessageBatchRequestItem
    {
        [JsonProperty("custom_id")]
        public string CustomId { get; set; }

        [JsonProperty("params")]
        public MessageRequest Params { get; set; }
    }

    public class MessageBatch
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = "message_batch";

        [JsonProperty("processing_status")]
        public string ProcessingStatus { get; set; } // in_progress, canceled, ended

        [JsonProperty("request_counts")]
        public BatchRequestCounts RequestCounts { get; set; } = new();

        [JsonProperty("ended_at")]
        public DateTime? EndedAt { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [JsonProperty("archived_at")]
        public DateTime? ArchivedAt { get; set; }

        [JsonProperty("cancel_initiated_at")]
        public DateTime? CancelInitiatedAt { get; set; }

        [JsonProperty("results_url")]
        public string? ResultsUrl { get; set; }
    }

    public class BatchRequestCounts
    {
        [JsonProperty("processing")]
        public int Processing { get; set; }

        [JsonProperty("succeeded")]
        public int Succeeded { get; set; }

        [JsonProperty("errored")]
        public int Errored { get; set; }

        [JsonProperty("canceled")]
        public int Canceled { get; set; }

        [JsonProperty("expired")]
        public int Expired { get; set; }
    }
}
