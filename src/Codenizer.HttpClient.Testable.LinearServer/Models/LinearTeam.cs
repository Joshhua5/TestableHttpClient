namespace Codenizer.HttpClient.Testable.LinearServer.Models
{
    /// <summary>
    /// Represents a Linear team.
    /// </summary>
    public class LinearTeam
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public List<string> MemberIds { get; set; } = new();
        public int IssueCount { get; set; }
        public bool Private { get; set; }
    }
}
