using System.Collections.Generic;
using System.Linq;
using Codenizer.HttpClient.Testable.GitLabServer.Models;

namespace Codenizer.HttpClient.Testable.GitLabServer.Handlers
{
    public class GroupsHandler
    {
        private readonly GitLabState _state;

        public GroupsHandler(GitLabState state)
        {
            _state = state;
        }

        public IEnumerable<GitLabGroup> List()
        {
            return _state.Groups.ToList();
        }
        
        public GitLabGroup Create(string name, string path)
        {
            var id = _state.Groups.Count + 1;
            var group = new GitLabGroup
            {
                Id = id,
                Name = name,
                Path = path,
                FullName = name,
                FullPath = path,
                WebUrl = $"http://gitlab.example.com/groups/{path}"
            };
            _state.Groups.Add(group);
            return group;
        }
    }
}
