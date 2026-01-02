namespace Codenizer.HttpClient.Testable.LinearServer.Models
{
    /// <summary>
    /// Represents a Linear project.
    /// </summary>
    public class LinearProject
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string State { get; set; } = "planned"; // "backlog", "planned", "started", "paused", "completed", "canceled"
        public string? Icon { get; set; }
        public string? Color { get; set; }
        public int Progress { get; set; } // 0-100
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ArchivedAt { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? TargetDate { get; set; }
        public string? LeadId { get; set; }
        public List<string> MemberIds { get; set; } = new();
        public List<string> TeamIds { get; set; } = new();
        public string Url { get; set; } = string.Empty;
    }
}
