namespace Codenizer.HttpClient.Testable.LinearServer.Models
{
    /// <summary>
    /// Represents an attachment on an issue.
    /// </summary>
    public class LinearAttachment
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string Url { get; set; } = string.Empty;
        public string IssueId { get; set; } = string.Empty;
        public string CreatorId { get; set; } = string.Empty;
        public string? Metadata { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ArchivedAt { get; set; }
    }
}
