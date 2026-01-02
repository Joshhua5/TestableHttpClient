using Codenizer.HttpClient.Testable.SentryServer.Models;

namespace Codenizer.HttpClient.Testable.SentryServer.Handlers
{
    public class TeamsHandler
    {
        private readonly SentryState _state;

        public TeamsHandler(SentryState state)
        {
            _state = state;
        }

        public object List(string orgSlug)
        {
            // In a real implementation we would filter by organization
            return _state.Teams.Values.ToList();
        }

        public object? Create(string orgSlug, string name, string? slug = null)
        {
            if (!_state.Organizations.TryGetValue(orgSlug, out var org)) return null;

            var generatedSlug = slug ?? name.ToLower().Replace(" ", "-");
            if (_state.Teams.ContainsKey(generatedSlug))
            {
                // Conflict
                // For simplicity, we might just overwrite or throw, but let's overwrite to simulate successful "get or create" or just basic behavior
            }

            var team = new SentryTeam
            {
                Id = _state.GenerateId(),
                Name = name,
                Slug = generatedSlug,
                DateCreated = DateTime.UtcNow
            };
            _state.Teams[generatedSlug] = team;
            return team;
        }

        public object? Update(string orgSlug, string teamSlug, string? name = null, string? newSlug = null)
        {
             if (!_state.Teams.TryGetValue(teamSlug, out var team)) return null;
             
             if(name != null) team.Name = name;
             if(newSlug != null)
             {
                 _state.Teams.Remove(teamSlug);
                 team.Slug = newSlug;
                 _state.Teams[newSlug] = team;
             }
             
             return team;
        }

        public bool Delete(string orgSlug, string teamSlug)
        {
            return _state.Teams.Remove(teamSlug);
        }
    }
}
