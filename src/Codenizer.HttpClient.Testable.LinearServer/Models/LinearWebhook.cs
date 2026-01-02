namespace Codenizer.HttpClient.Testable.LinearServer.Models
{
    /// <summary>
    /// Represents a webhook configuration.
    /// </summary>
    public class LinearWebhook
    {
        public string Id { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public string? Label { get; set; }
        public string? TeamId { get; set; }
        public bool AllPublicTeams { get; set; }
        public List<string> ResourceTypes { get; set; } = new(); // "Issue", "Comment", "Project", "Cycle", etc.
        public string? Secret { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
