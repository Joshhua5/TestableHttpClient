namespace Codenizer.HttpClient.Testable.SlackServer.Models
{
    public class SlackFile
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Mimetype { get; set; } = string.Empty;
        public string Filetype { get; set; } = string.Empty;
        public long Size { get; set; }
        public string User { get; set; } = string.Empty;
        public long Created { get; set; }
        public long Timestamp { get; set; }
        public bool IsPublic { get; set; }
        public bool IsExternal { get; set; }
        public string? UrlPrivate { get; set; }
        public string? UrlPrivateDownload { get; set; }
        public string? Permalink { get; set; }
        public string? PermalinkPublic { get; set; }
        public List<string> Channels { get; set; } = new();
    }

    public class SlackPin
    {
        public string Type { get; set; } = "message";
        public string Channel { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string? File { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public long Created { get; set; }
    }

    public class SlackBookmark
    {
        public string Id { get; set; } = string.Empty;
        public string ChannelId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string Type { get; set; } = "link";
        public long DateCreated { get; set; }
        public long DateUpdated { get; set; }
    }
}
