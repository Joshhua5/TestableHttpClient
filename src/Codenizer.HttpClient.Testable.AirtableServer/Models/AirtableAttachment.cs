namespace Codenizer.HttpClient.Testable.AirtableServer.Models
{
    /// <summary>
    /// Represents an attachment in Airtable.
    /// </summary>
    public class AirtableAttachment
    {
        /// <summary>
        /// The unique identifier for this attachment.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The URL to access the attachment.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// The filename of the attachment.
        /// </summary>
        public string Filename { get; set; } = string.Empty;

        /// <summary>
        /// The size of the attachment in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// The MIME type of the attachment.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// The width of the attachment (for images/videos).
        /// </summary>
        public int? Width { get; set; }

        /// <summary>
        /// The height of the attachment (for images/videos).
        /// </summary>
        public int? Height { get; set; }

        /// <summary>
        /// Thumbnail information for images.
        /// </summary>
        public AirtableThumbnails? Thumbnails { get; set; }
    }

    /// <summary>
    /// Represents thumbnails for an image attachment.
    /// </summary>
    public class AirtableThumbnails
    {
        /// <summary>
        /// Small thumbnail.
        /// </summary>
        public AirtableThumbnail? Small { get; set; }

        /// <summary>
        /// Large thumbnail.
        /// </summary>
        public AirtableThumbnail? Large { get; set; }

        /// <summary>
        /// Full-size image.
        /// </summary>
        public AirtableThumbnail? Full { get; set; }
    }

    /// <summary>
    /// Represents a single thumbnail.
    /// </summary>
    public class AirtableThumbnail
    {
        /// <summary>
        /// The URL of the thumbnail.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// The width of the thumbnail.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// The height of the thumbnail.
        /// </summary>
        public int Height { get; set; }
    }
}
