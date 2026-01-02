namespace Codenizer.HttpClient.Testable.LinearServer.Models
{
    /// <summary>
    /// Represents a custom view/filter.
    /// </summary>
    public class LinearCustomView
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public string? Color { get; set; }
        public string CreatorId { get; set; } = "";
        public string? TeamId { get; set; }
        public string? FilterData { get; set; } // JSON filter definition
        public bool Shared { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
