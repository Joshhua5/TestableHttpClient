namespace Codenizer.HttpClient.Testable.LinearServer.Models
{
    /// <summary>
    /// Represents a Linear cycle (sprint).
    /// </summary>
    public class LinearCycle
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
        public int Number { get; set; }
        public string TeamId { get; set; } = string.Empty;
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public int Progress { get; set; } // 0-100
        public int IssueCountScope { get; set; }
        public int CompletedIssueCountScope { get; set; }
    }
}
