namespace Codenizer.HttpClient.Testable.SlackServer.Models
{
    public class SlackUser
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string RealName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool IsBot { get; set; }
        public bool IsDeleted { get; set; }
        public SlackUserProfile Profile { get; set; } = new();
        public long Updated { get; set; }
    }

    public class SlackUserProfile
    {
        public string DisplayName { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public string StatusEmoji { get; set; } = string.Empty;
        public string Image48 { get; set; } = string.Empty;
        public string Image72 { get; set; } = string.Empty;
    }
}
