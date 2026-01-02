using System.Collections.Generic;
using System.Linq;
using Codenizer.HttpClient.Testable.GitLabServer.Models;

namespace Codenizer.HttpClient.Testable.GitLabServer.Handlers
{
    public class IssuesHandler
    {
        private readonly GitLabState _state;

        public IssuesHandler(GitLabState state)
        {
            _state = state;
        }

        public IEnumerable<GitLabIssue> List(int? projectId = null)
        {
            if (projectId.HasValue)
            {
                return _state.Issues.Where(i => i.ProjectId == projectId.Value).ToList();
            }
            return _state.Issues.ToList();
        }

        public GitLabIssue? Get(int projectId, int issueIid)
        {
             return _state.Issues.FirstOrDefault(i => i.ProjectId == projectId && i.Iid == issueIid);
        }

        public GitLabIssue Create(int projectId, string title, string description = "")
        {
            var project = _state.Projects.FirstOrDefault(p => p.Id == projectId);
            if (project == null) return null; // Should probably throw or handle gracefully at caller

            var id = _state.Issues.Count + 1;
            // Calculate sequential IID per project
            var iid = _state.Issues.Count(i => i.ProjectId == projectId) + 1;

            var issue = new GitLabIssue
            {
                Id = id,
                Iid = iid,
                ProjectId = projectId,
                Title = title,
                Description = description,
                WebUrl = $"{project.WebUrl}/-/issues/{iid}"
            };

            _state.Issues.Add(issue);
            return issue;
        }
    }
}
