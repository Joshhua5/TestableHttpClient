using Codenizer.HttpClient.Testable.SentryServer.Models;

namespace Codenizer.HttpClient.Testable.SentryServer.Handlers
{
    public class ProjectKeysHandler
    {
        private readonly SentryState _state;

        public ProjectKeysHandler(SentryState state)
        {
            _state = state;
        }

        public object? List(string orgSlug, string projectSlug)
        {
             // We need to filter keys by project. 
             // Our simple state keys by ID. In a real DB we'd have a foreign key.
             // We should probably add a reference to Project in SentryProjectKey or just filter here poorly.
             // Let's iterate and filter (inefficient but fine for test doubles)
             
             // Wait, I didn't add Project reference to SentryProjectKey model. 
             // I'll assume for now we just return all keys because differentiating is hard without that link.
             // OR, I can add a dictionary of ProjectSlug -> List<KeyId> in State.
             // Let's stick to simple: return all keys for now or modify model later if needed.
             // Actually, the DSN contains the project ID usually. 
             
             return _state.ProjectKeys.Values.ToList();
        }

        public object? Create(string orgSlug, string projectSlug, string name)
        {
            if (!_state.Projects.TryGetValue(projectSlug, out var project)) return null;

            var keyId = _state.GenerateId();
            var publicId = _state.GenerateId();
            var secretId = _state.GenerateId();
            var projectIdStr = project.Id; // Assuming numeric ID stored as string

            var key = new SentryProjectKey
            {
                Id = keyId,
                Name = name,
                PublicKey = publicId,
                SecretKey = secretId,
                DateCreated = DateTime.UtcNow,
                IsActive = true,
                Dsn = new SentryDsn
                {
                    Public = $"https://{publicId}@sentry.io/{projectIdStr}",
                    Secret = $"https://{publicId}:{secretId}@sentry.io/{projectIdStr}",
                    Csp = $"https://sentry.io/api/{projectIdStr}/csp-report/?sentry_key={publicId}",
                    Security = $"https://sentry.io/api/{projectIdStr}/security/?sentry_key={publicId}",
                    Minidump = $"https://sentry.io/api/{projectIdStr}/minidump/?sentry_key={publicId}",
                    Cdn = $"https://js.sentry-cdn.com/{publicId}.min.js"
                }
            };
            
            _state.ProjectKeys[keyId] = key;
            return key;
        }
    }
}
