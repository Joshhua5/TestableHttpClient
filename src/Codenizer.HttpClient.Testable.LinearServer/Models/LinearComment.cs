namespace Codenizer.HttpClient.Testable.LinearServer.Models
{
    /// <summary>
    /// Represents a comment on a Linear issue.
    /// </summary>
    public class LinearComment
    {
        public string Id { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string IssueId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public bool Edited { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ArchivedAt { get; set; }
        public string? ParentId { get; set; }
        public string Url { get; set; } = string.Empty;
    }
}
