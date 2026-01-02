using System;

namespace Codenizer.HttpClient.Testable.GitLabServer.Models
{
    public class GitLabIssue
    {
        public int Id { get; set; }
        public int Iid { get; set; }
        public int ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string State { get; set; } = "opened";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public GitLabUser? Author { get; set; }
        public GitLabUser? Assignee { get; set; }
        public string WebUrl { get; set; } = string.Empty;
    }
}
