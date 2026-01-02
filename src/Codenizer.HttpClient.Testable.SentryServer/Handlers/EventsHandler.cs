using Codenizer.HttpClient.Testable.SentryServer.Models;

namespace Codenizer.HttpClient.Testable.SentryServer.Handlers
{
    public class EventsHandler
    {
        private readonly SentryState _state;

        public EventsHandler(SentryState state)
        {
            _state = state;
        }

        public object? Store(string projectId, SentryEvent evt)
        {
            // Ideally verify project exists first. 
            // Sentry API uses project ID in URL.
            // We can match by numeric ID or slug if we had a mapping, but let's assume numeric ID matches.
            
            // For simplicity, we just store it
            
            var eventId = _state.GenerateId();
            evt.EventId = eventId;
            evt.Timestamp = DateTime.UtcNow;
            
            _state.Events.Add(evt);
            
            // Side effect: Create an issue if one doesn't exist for this "type" of error
            // For simulation, we group by message
            var existingIssue = _state.Issues.Values.FirstOrDefault(i => i.Title == evt.Message);
            if (existingIssue == null)
            {
                var issue = new SentryIssue
                {
                    Id = _state.GenerateId(),
                    Title = evt.Message,
                    Status = "unresolved",
                    ShortId = "LIST-" + _state.Issues.Count,
                    Culprit = "unknown",
                    Metadata = evt.Extra,
                    // Project... ideally we find it by ID
                };
                
                 if (_state.Projects.Values.Any(p => p.Id == projectId))
                 {
                     issue.Project = _state.Projects.Values.First(p => p.Id == projectId);
                 }

                _state.Issues.Add(issue.Id, issue);
                return new { id = eventId };
            }
            
            return new { id = eventId };
        }
    }
}
