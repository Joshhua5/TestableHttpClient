using Newtonsoft.Json.Linq;

namespace Codenizer.HttpClient.Testable.AirtableServer.Models
{
    /// <summary>
    /// Represents a webhook subscription in Airtable.
    /// </summary>
    public class AirtableWebhook
    {
        /// <summary>
        /// The unique identifier for this webhook.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The base64-encoded HMAC secret used to verify webhook payloads.
        /// </summary>
        public string? MacSecretBase64 { get; set; }

        /// <summary>
        /// The URL where webhook notifications will be sent.
        /// </summary>
        public string? NotificationUrl { get; set; }

        /// <summary>
        /// The cursor for retrieving the next payload.
        /// </summary>
        public int CursorForNextPayload { get; set; } = 1;

        /// <summary>
        /// Whether the webhook is currently enabled.
        /// </summary>
        public bool IsHookEnabled { get; set; } = true;

        /// <summary>
        /// The expiration time for this webhook (ISO 8601 format).
        /// </summary>
        public string? ExpirationTime { get; set; }

        /// <summary>
        /// The webhook specification defining what events to listen for.
        /// </summary>
        public AirtableWebhookSpecification? Specification { get; set; }
    }

    /// <summary>
    /// Represents the specification for a webhook.
    /// </summary>
    public class AirtableWebhookSpecification
    {
        /// <summary>
        /// Options for the webhook specification.
        /// </summary>
        public AirtableWebhookOptions? Options { get; set; }
    }

    /// <summary>
    /// Represents options for a webhook specification.
    /// </summary>
    public class AirtableWebhookOptions
    {
        /// <summary>
        /// Filters for the webhook.
        /// </summary>
        public AirtableWebhookFilters? Filters { get; set; }

        /// <summary>
        /// Includes configuration (what data to include in payloads).
        /// </summary>
        public JObject? Includes { get; set; }
    }

    /// <summary>
    /// Represents filters for a webhook.
    /// </summary>
    public class AirtableWebhookFilters
    {
        /// <summary>
        /// Data types to filter on.
        /// </summary>
        public List<string>? DataTypes { get; set; }

        /// <summary>
        /// Record change scope (e.g., "tblXXX" or specific record IDs).
        /// </summary>
        public string? RecordChangeScope { get; set; }

        /// <summary>
        /// Change types to watch (e.g., "add", "update", "remove").
        /// </summary>
        public List<string>? ChangeTypes { get; set; }

        /// <summary>
        /// Source options (e.g., "client", "publicApi", "formSubmission").
        /// </summary>
        public JObject? SourceOptions { get; set; }
    }

    /// <summary>
    /// Represents a webhook payload.
    /// </summary>
    public class AirtableWebhookPayload
    {
        /// <summary>
        /// The timestamp of the payload.
        /// </summary>
        public string Timestamp { get; set; } = string.Empty;

        /// <summary>
        /// The base ID that triggered this payload.
        /// </summary>
        public string BaseTransactionNumber { get; set; } = string.Empty;

        /// <summary>
        /// The payload number for cursor tracking.
        /// </summary>
        public int PayloadNumber { get; set; }

        /// <summary>
        /// Action metadata (what changed).
        /// </summary>
        public JObject? ActionMetadata { get; set; }

        /// <summary>
        /// The changed tables and records.
        /// </summary>
        public JObject? ChangedTablesById { get; set; }
    }
}
