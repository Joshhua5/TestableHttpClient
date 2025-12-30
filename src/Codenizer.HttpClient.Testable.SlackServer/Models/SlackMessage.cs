namespace Codenizer.HttpClient.Testable.SlackServer.Models
{
    public class SlackMessage
    {
        public string Type { get; set; } = "message";
        public string Ts { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string? ThreadTs { get; set; }
        public List<SlackReaction> Reactions { get; set; } = new();
        public SlackMessageEdited? Edited { get; set; }
    }

    public class SlackReaction
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<string> Users { get; set; } = new();
    }

    public class SlackMessageEdited
    {
        public string User { get; set; } = string.Empty;
        public string Ts { get; set; } = string.Empty;
    }
}
