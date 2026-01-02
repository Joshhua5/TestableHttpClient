using System.Collections.Generic;
using System.Linq;
using Codenizer.HttpClient.Testable.GitLabServer.Models;

namespace Codenizer.HttpClient.Testable.GitLabServer.Handlers
{
    public class UsersHandler
    {
        private readonly GitLabState _state;

        public UsersHandler(GitLabState state)
        {
            _state = state;
        }

        public IEnumerable<GitLabUser> List()
        {
            return _state.Users.ToList();
        }

        public GitLabUser? Get(int id)
        {
            return _state.Users.FirstOrDefault(u => u.Id == id);
        }
        
        public GitLabUser Create(string username, string email, string name)
        {
             var id = _state.Users.Count + 1;
             var user = new GitLabUser
             {
                 Id = id,
                 Username = username,
                 Name = name,
                 WebUrl = $"http://gitlab.example.com/{username}"
             };
             _state.Users.Add(user);
             return user;
        }
    }
}
