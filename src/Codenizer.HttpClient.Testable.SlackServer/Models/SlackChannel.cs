namespace Codenizer.HttpClient.Testable.SlackServer.Models
{
    public class SlackChannel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsChannel { get; set; } = true;
        public bool IsPrivate { get; set; }
        public bool IsArchived { get; set; }
        public bool IsMember { get; set; }
        public string Creator { get; set; } = string.Empty;
        public long Created { get; set; }
        public SlackChannelTopic Topic { get; set; } = new();
        public SlackChannelPurpose Purpose { get; set; } = new();
        public List<string> Members { get; set; } = new();
        public int NumMembers { get; set; }
    }

    public class SlackChannelTopic
    {
        public string Value { get; set; } = string.Empty;
        public string Creator { get; set; } = string.Empty;
        public long LastSet { get; set; }
    }

    public class SlackChannelPurpose
    {
        public string Value { get; set; } = string.Empty;
        public string Creator { get; set; } = string.Empty;
        public long LastSet { get; set; }
    }
}
