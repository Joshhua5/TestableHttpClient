namespace Codenizer.HttpClient.Testable.LinearServer.Models
{
    /// <summary>
    /// Represents a workflow state (status) for issues.
    /// </summary>
    public class LinearWorkflowState
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "unstarted"; // "triage", "backlog", "unstarted", "started", "completed", "canceled"
        public string Color { get; set; } = "#6B7280";
        public string? Description { get; set; }
        public string TeamId { get; set; } = string.Empty;
        public double Position { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ArchivedAt { get; set; }
    }
}
