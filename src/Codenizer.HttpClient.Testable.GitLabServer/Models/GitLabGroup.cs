namespace Codenizer.HttpClient.Testable.GitLabServer.Models
{
    public class GitLabGroup
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Visibility { get; set; } = "private";
        public string WebUrl { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
    }
}
