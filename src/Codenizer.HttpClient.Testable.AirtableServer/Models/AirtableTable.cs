namespace Codenizer.HttpClient.Testable.AirtableServer.Models
{
    /// <summary>
    /// Represents a table within an Airtable base.
    /// </summary>
    public class AirtableTable
    {
        /// <summary>
        /// The unique identifier for this table (e.g., "tblXXXXXXXXXXXXXX").
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The name of the table.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional description for the table.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The ID of the primary field for this table.
        /// </summary>
        public string PrimaryFieldId { get; set; } = string.Empty;

        /// <summary>
        /// The fields (columns) in this table.
        /// </summary>
        public List<AirtableField> Fields { get; set; } = new();

        /// <summary>
        /// The views defined on this table.
        /// </summary>
        public List<AirtableView> Views { get; set; } = new();
    }
}
