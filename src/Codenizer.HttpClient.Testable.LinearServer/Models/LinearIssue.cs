namespace Codenizer.HttpClient.Testable.LinearServer.Models
{
    /// <summary>
    /// Represents a Linear issue.
    /// </summary>
    public class LinearIssue
    {
        public string Id { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty; // e.g., "PROJ-123"
        public int Number { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Priority { get; set; } // 0-4: No priority, Urgent, High, Normal, Low
        public double? Estimate { get; set; }
        public string TeamId { get; set; } = string.Empty;
        public string? AssigneeId { get; set; }
        public string StateId { get; set; } = string.Empty;
        public string? ProjectId { get; set; }
        public string? CycleId { get; set; }
        public string? ParentId { get; set; }
        public List<string> LabelIds { get; set; } = new();
        public List<string> SubscriberIds { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ArchivedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CanceledAt { get; set; }
        public DateTime? DueDate { get; set; }
        public string Url { get; set; } = string.Empty;
        public string CreatorId { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public int SubIssueSortOrder { get; set; }
    }
}
