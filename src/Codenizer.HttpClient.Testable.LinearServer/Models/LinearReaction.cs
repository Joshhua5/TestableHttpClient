namespace Codenizer.HttpClient.Testable.LinearServer.Models
{
    /// <summary>
    /// Represents a reaction on a comment.
    /// </summary>
    public class LinearReaction
    {
        public string Id { get; set; } = "";
        public string Emoji { get; set; } = "";
        public string CommentId { get; set; } = "";
        public string UserId { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
