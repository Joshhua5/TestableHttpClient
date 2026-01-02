namespace Codenizer.HttpClient.Testable.LinearServer.Models
{
    /// <summary>
    /// Represents a Linear user.
    /// </summary>
    public class LinearUser
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool Active { get; set; } = true;
        public bool Admin { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? AvatarUrl { get; set; }
    }
}
