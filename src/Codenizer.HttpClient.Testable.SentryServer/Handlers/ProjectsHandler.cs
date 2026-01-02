using Codenizer.HttpClient.Testable.SentryServer.Models;

namespace Codenizer.HttpClient.Testable.SentryServer.Handlers
{
    public class ProjectsHandler
    {
        private readonly SentryState _state;

        public ProjectsHandler(SentryState state)
        {
            _state = state;
        }

        public object List()
        {
            return _state.Projects.Values.ToList();
        }

        public object? Get(string orgSlug, string projectSlug)
        {
            if (_state.Projects.TryGetValue(projectSlug, out var project))
            {
                if(project.Organization.Slug == orgSlug)
                {
                    return project;
                }
            }
            return null;
        }

        public object? Create(string orgSlug, string teamSlug, string name, string? slug = null)
        {
            if (!_state.Organizations.TryGetValue(orgSlug, out var org)) return null;
            // Ideally we check team existence too, but keeping it simple for now or assuming checks done before
            
            var generatedSlug = slug ?? name.ToLower().Replace(" ", "-");
            var project = new SentryProject
            {
                Id = _state.GenerateId(),
                Name = name,
                Slug = generatedSlug,
                Organization = org,
                DateCreated = DateTime.UtcNow
            };
            
            _state.Projects[generatedSlug] = project;
            return project;
        }
    }
}
