using System;

namespace Codenizer.HttpClient.Testable.GitLabServer.Models
{
    public class GitLabPipeline
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Status { get; set; } = "pending";
        public string Ref { get; set; } = string.Empty;
        public string Sha { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string WebUrl { get; set; } = string.Empty;
    }
}
