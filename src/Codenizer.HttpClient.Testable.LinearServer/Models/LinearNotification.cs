namespace Codenizer.HttpClient.Testable.LinearServer.Models
{
    /// <summary>
    /// Represents a user notification.
    /// </summary>
    public class LinearNotification
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "issueAssigned"; // issueAssigned, issueMention, issueComment, etc.
        public string UserId { get; set; } = "";
        public string? IssueId { get; set; }
        public string? CommentId { get; set; }
        public string? ActorId { get; set; }
        public bool ReadAt { get; set; }
        public bool SnoozedUntilAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ArchivedAt { get; set; }
    }
}
