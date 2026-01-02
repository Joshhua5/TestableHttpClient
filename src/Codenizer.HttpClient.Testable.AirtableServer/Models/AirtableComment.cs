namespace Codenizer.HttpClient.Testable.AirtableServer.Models
{
    /// <summary>
    /// Represents a comment on an Airtable record.
    /// </summary>
    public class AirtableComment
    {
        /// <summary>
        /// The unique identifier for this comment.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The author of the comment.
        /// </summary>
        public AirtableCommentAuthor Author { get; set; } = new();

        /// <summary>
        /// The text content of the comment.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// The time when the comment was created (ISO 8601 format).
        /// </summary>
        public string CreatedTime { get; set; } = string.Empty;

        /// <summary>
        /// The time when the comment was last modified (ISO 8601 format).
        /// </summary>
        public string? LastModifiedTime { get; set; }

        /// <summary>
        /// Users mentioned in this comment.
        /// </summary>
        public Dictionary<string, AirtableMentionedUser>? Mentioned { get; set; }
    }

    /// <summary>
    /// Represents the author of a comment.
    /// </summary>
    public class AirtableCommentAuthor
    {
        /// <summary>
        /// The user ID of the author.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The email of the author.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The display name of the author.
        /// </summary>
        public string? Name { get; set; }
    }

    /// <summary>
    /// Represents a user mentioned in a comment.
    /// </summary>
    public class AirtableMentionedUser
    {
        /// <summary>
        /// The display type for the mention.
        /// </summary>
        public string DisplayType { get; set; } = "user";

        /// <summary>
        /// The display text for the mention.
        /// </summary>
        public string DisplayText { get; set; } = string.Empty;

        /// <summary>
        /// The user ID of the mentioned user.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// The email of the mentioned user.
        /// </summary>
        public string? Email { get; set; }
    }
}
