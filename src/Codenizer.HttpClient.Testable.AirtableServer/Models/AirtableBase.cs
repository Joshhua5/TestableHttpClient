namespace Codenizer.HttpClient.Testable.AirtableServer.Models
{
    /// <summary>
    /// Represents an Airtable base (workspace).
    /// </summary>
    public class AirtableBase
    {
        /// <summary>
        /// The unique identifier for this base (e.g., "appXXXXXXXXXXXXXX").
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The name of the base.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The user's permission level for this base (e.g., "create", "edit", "comment", "read").
        /// </summary>
        public string PermissionLevel { get; set; } = "create";
    }
}
