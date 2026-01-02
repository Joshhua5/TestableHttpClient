using Newtonsoft.Json.Linq;

namespace Codenizer.HttpClient.Testable.AirtableServer.Models
{
    /// <summary>
    /// Represents a record (row) in an Airtable table.
    /// </summary>
    public class AirtableRecord
    {
        /// <summary>
        /// The unique identifier for this record (e.g., "recXXXXXXXXXXXXXX").
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The time when the record was created (ISO 8601 format).
        /// </summary>
        public string CreatedTime { get; set; } = string.Empty;

        /// <summary>
        /// The field values for this record, keyed by field name or ID.
        /// </summary>
        public Dictionary<string, object?> Fields { get; set; } = new();
    }

    /// <summary>
    /// Represents a record input for create/update operations.
    /// </summary>
    public class AirtableRecordInput
    {
        /// <summary>
        /// The record ID (used for updates).
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// The field values to set.
        /// </summary>
        public Dictionary<string, object?>? Fields { get; set; }
    }

    /// <summary>
    /// Represents the request body for creating/updating multiple records.
    /// </summary>
    public class AirtableRecordsRequest
    {
        /// <summary>
        /// Array of records to create or update.
        /// </summary>
        public List<AirtableRecordInput>? Records { get; set; }

        /// <summary>
        /// Whether to perform an upsert (update or insert).
        /// </summary>
        public bool? PerformUpsert { get; set; }

        /// <summary>
        /// Fields to match on for upsert operations.
        /// </summary>
        public List<string>? FieldsToMergeOn { get; set; }

        /// <summary>
        /// Whether typecast should be enabled (auto-convert field values).
        /// </summary>
        public bool? Typecast { get; set; }

        /// <summary>
        /// Whether to return field IDs instead of names.
        /// </summary>
        public bool? ReturnFieldsByFieldId { get; set; }
    }
}
