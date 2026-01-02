namespace Codenizer.HttpClient.Testable.LinearServer.Models
{
    /// <summary>
    /// Represents a user favorite.
    /// </summary>
    public class LinearFavorite
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "issue"; // issue, project, cycle, customView, label
        public string UserId { get; set; } = "";
        public string? IssueId { get; set; }
        public string? ProjectId { get; set; }
        public string? CycleId { get; set; }
        public string? LabelId { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
