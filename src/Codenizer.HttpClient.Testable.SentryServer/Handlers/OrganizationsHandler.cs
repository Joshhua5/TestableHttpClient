using Codenizer.HttpClient.Testable.SentryServer.Models;

namespace Codenizer.HttpClient.Testable.SentryServer.Handlers
{
    public class OrganizationsHandler
    {
        private readonly SentryState _state;

        public OrganizationsHandler(SentryState state)
        {
            _state = state;
        }

        public object List()
        {
            return _state.Organizations.Values.ToList();
        }

        public object? Get(string slug)
        {
            if (_state.Organizations.TryGetValue(slug, out var org))
            {
                return org;
            }
            return null;
        }

        public object Create(string name, string? slug = null)
        {
            var generatedSlug = slug ?? name.ToLower().Replace(" ", "-");
            var org = new SentryOrganization
            {
                Id = _state.GenerateId(),
                Name = name,
                Slug = generatedSlug,
                DateCreated = DateTime.UtcNow
            };
            _state.Organizations[generatedSlug] = org;
            return org;
        }
    }
}
