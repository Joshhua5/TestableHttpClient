using Codenizer.HttpClient.Testable.SentryServer.Models;
using System.Collections.Generic;

namespace Codenizer.HttpClient.Testable.SentryServer
{
    public class SentryState
    {
        public Dictionary<string, SentryOrganization> Organizations { get; } = new();
        public Dictionary<string, SentryProject> Projects { get; } = new();
        public Dictionary<string, SentryTeam> Teams { get; } = new();
        public Dictionary<string, SentryIssue> Issues { get; } = new();
        public List<SentryEvent> Events { get; } = new();
        public Dictionary<string, SentryRelease> Releases { get; } = new(); // Keyed by version
        public Dictionary<string, SentryProjectKey> ProjectKeys { get; } = new(); // Keyed by ID
        public Dictionary<string, List<SentryComment>> IssueComments { get; } = new(); // Keyed by IssueId
        public List<SentryUserReport> UserReports { get; } = new(); 


        public SentryState()
        {
            SeedDefaultData();
        }

        private void SeedDefaultData()
        {
            var org = new SentryOrganization
            {
                Id = "1",
                Slug = "sentry-sc",
                Name = "Sentry Simulated Corp",
                DateCreated = DateTime.UtcNow
            };
            Organizations[org.Slug] = org;

            var team = new SentryTeam
            {
                Id = "1",
                Slug = "backend",
                Name = "Backend Team",
                DateCreated = DateTime.UtcNow
            };
            Teams[team.Slug] = team;

            var project = new SentryProject
            {
                Id = "1",
                Slug = "api-server",
                Name = "API Server",
                Organization = org,
                DateCreated = DateTime.UtcNow
            };
            Projects[project.Slug] = project;
        }
        
        public string GenerateId()
        {
             return Guid.NewGuid().ToString("N");
        }
    }
}
