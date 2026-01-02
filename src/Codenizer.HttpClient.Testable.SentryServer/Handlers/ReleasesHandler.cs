using Codenizer.HttpClient.Testable.SentryServer.Models;

namespace Codenizer.HttpClient.Testable.SentryServer.Handlers
{
    public class ReleasesHandler
    {
        private readonly SentryState _state;

        public ReleasesHandler(SentryState state)
        {
            _state = state;
        }

        public object List(string orgSlug)
        {
             // Ideally filter by org... but projects are linked to releases. 
             // We can assume for now releases are global or per org in our simplified state.
             return _state.Releases.Values.ToList();
        }

        public object? Create(string orgSlug, string version, List<string>? projects = null)
        {
            if (!_state.Organizations.ContainsKey(orgSlug)) return null;

            if (_state.Releases.ContainsKey(version))
            {
                // Already exists, return it
                return _state.Releases[version];
            }

            var release = new SentryRelease
            {
                Version = version,
                ShortVersion = version.Length > 7 ? version.Substring(0, 7) : version,
                DateCreated = DateTime.UtcNow
            };

            if (projects != null)
            {
                foreach (var projSlug in projects)
                {
                    if (_state.Projects.TryGetValue(projSlug, out var proj))
                    {
                        release.Projects.Add(proj);
                    }
                }
            }
            
            _state.Releases[version] = release;
            return release;
        }
    }
}
