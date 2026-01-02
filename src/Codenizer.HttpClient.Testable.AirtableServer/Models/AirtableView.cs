using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Codenizer.HttpClient.Testable.AirtableServer.Models
{
    /// <summary>
    /// Represents a view on an Airtable table.
    /// </summary>
    public class AirtableView
    {
        /// <summary>
        /// The unique identifier for this view (e.g., "viwXXXXXXXXXXXXXX").
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The name of the view.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The type of the view (e.g., "grid", "form", "calendar", "gallery", "kanban").
        /// </summary>
        public string Type { get; set; } = "grid";

        /// <summary>
        /// The sorting configuration for this view.
        /// </summary>
        public List<AirtableSortConfig>? Sort { get; set; }

        /// <summary>
        /// The fields visible in this view. If null, all fields are visible.
        /// </summary>
        public List<string>? VisibleFields { get; set; }

        /// <summary>
        /// A formula used to filter records in this view.
        /// </summary>
        public string? FilterFormula { get; set; }
    }

    public class AirtableSortConfig
    {
        public string Field { get; set; } = string.Empty;
        public string Direction { get; set; } = "asc";
    }

    /// <summary>
    /// Common Airtable view types.
    /// </summary>
    public static class AirtableViewTypes
    {
        public const string Grid = "grid";
        public const string Form = "form";
        public const string Calendar = "calendar";
        public const string Gallery = "gallery";
        public const string Kanban = "kanban";
        public const string Timeline = "timeline";
        public const string List = "list";
    }
}
