namespace Codenizer.HttpClient.Testable.LinearServer.Models
{
    /// <summary>
    /// Represents a document attached to a project.
    /// </summary>
    public class LinearDocument
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string? Content { get; set; }
        public string? Icon { get; set; }
        public string? Color { get; set; }
        public string? ProjectId { get; set; }
        public string CreatorId { get; set; } = "";
        public string? Url { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ArchivedAt { get; set; }
    }
}
