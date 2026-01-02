namespace Codenizer.HttpClient.Testable.GitLabServer.Models
{
    public class GitLabUser
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string State { get; set; } = "active";
        public string AvatarUrl { get; set; } = string.Empty;
        public string WebUrl { get; set; } = string.Empty;
    }
}
