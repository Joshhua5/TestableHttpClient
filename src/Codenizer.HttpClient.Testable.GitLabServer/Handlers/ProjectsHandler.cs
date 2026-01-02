using System;
using System.Collections.Generic;
using System.Linq;
using Codenizer.HttpClient.Testable.GitLabServer.Models;

namespace Codenizer.HttpClient.Testable.GitLabServer.Handlers
{
    public class ProjectsHandler
    {
        private readonly GitLabState _state;

        public ProjectsHandler(GitLabState state)
        {
            _state = state;
        }

        public IEnumerable<GitLabProject> List()
        {
            return _state.Projects.ToList();
        }

        public GitLabProject? Get(string idOrPath)
        {
            if (int.TryParse(idOrPath, out var id))
            {
                return _state.Projects.FirstOrDefault(p => p.Id == id);
            }
            
            // GitLab allows URL encoded paths, e.g. namespace%2Fproject
            var decoded = System.Net.WebUtility.UrlDecode(idOrPath);
            return _state.Projects.FirstOrDefault(p => p.PathWithNamespace == decoded);
        }

        public GitLabProject Create(string name, string? path = null, string? namespacePath = null, string visibility = "private")
        {
            var id = _state.Projects.Count + 1;
            path ??= name.ToLowerInvariant().Replace(" ", "-");
            namespacePath ??= "root"; // Default namespace
            
            var project = new GitLabProject
            {
                Id = id,
                Name = name,
                Path = path,
                PathWithNamespace = $"{namespacePath}/{path}",
                Visibility = visibility,
                WebUrl = $"http://gitlab.example.com/{namespacePath}/{path}"
            };

            _state.Projects.Add(project);
            return project;
        }
    }
}
