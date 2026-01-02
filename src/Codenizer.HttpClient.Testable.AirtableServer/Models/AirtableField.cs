using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Codenizer.HttpClient.Testable.AirtableServer.Models
{
    /// <summary>
    /// Represents a field (column) in an Airtable table.
    /// </summary>
    public class AirtableField
    {
        /// <summary>
        /// The unique identifier for this field (e.g., "fldXXXXXXXXXXXXXX").
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The name of the field.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The type of the field (e.g., "singleLineText", "multilineText", "number", "checkbox", etc.).
        /// </summary>
        public string Type { get; set; } = "singleLineText";

        /// <summary>
        /// Optional description for the field.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Type-specific options for the field.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public JObject? Options { get; set; }
    }

    /// <summary>
    /// Common Airtable field types.
    /// </summary>
    public static class AirtableFieldTypes
    {
        public const string SingleLineText = "singleLineText";
        public const string MultilineText = "multilineText";
        public const string Email = "email";
        public const string Url = "url";
        public const string PhoneNumber = "phoneNumber";
        public const string Number = "number";
        public const string Currency = "currency";
        public const string Percent = "percent";
        public const string Duration = "duration";
        public const string Checkbox = "checkbox";
        public const string SingleSelect = "singleSelect";
        public const string MultipleSelects = "multipleSelects";
        public const string Date = "date";
        public const string DateTime = "dateTime";
        public const string Attachment = "multipleAttachments";
        public const string LinkedRecord = "multipleRecordLinks";
        public const string Lookup = "lookup";
        public const string Rollup = "rollup";
        public const string Count = "count";
        public const string Formula = "formula";
        public const string CreatedTime = "createdTime";
        public const string LastModifiedTime = "lastModifiedTime";
        public const string CreatedBy = "createdBy";
        public const string LastModifiedBy = "lastModifiedBy";
        public const string AutoNumber = "autoNumber";
        public const string Barcode = "barcode";
        public const string Rating = "rating";
        public const string RichText = "richText";
        public const string Button = "button";
    }
}
