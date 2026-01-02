using System;

namespace Codenizer.HttpClient.Testable.GitLabServer.Models
{
    public class GitLabProject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string PathWithNamespace { get; set; } = string.Empty;
        public string Visibility { get; set; } = "private";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string WebUrl { get; set; } = string.Empty;
    }
}
