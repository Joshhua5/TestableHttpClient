namespace Codenizer.HttpClient.Testable.LinearServer.Models
{
    /// <summary>
    /// Represents an issue label.
    /// </summary>
    public class LinearLabel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#6B7280";
        public string? Description { get; set; }
        public string? TeamId { get; set; } // null for workspace-level labels
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ArchivedAt { get; set; }
        public bool IsGroup { get; set; }
        public string? ParentId { get; set; }
    }
}
