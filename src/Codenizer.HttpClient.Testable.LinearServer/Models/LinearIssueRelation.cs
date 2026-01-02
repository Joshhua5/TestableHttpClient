namespace Codenizer.HttpClient.Testable.LinearServer.Models
{
    /// <summary>
    /// Represents a relation between two issues.
    /// </summary>
    public class LinearIssueRelation
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "related"; // related, blocks, duplicate
        public string IssueId { get; set; } = "";
        public string RelatedIssueId { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ArchivedAt { get; set; }
    }
}
