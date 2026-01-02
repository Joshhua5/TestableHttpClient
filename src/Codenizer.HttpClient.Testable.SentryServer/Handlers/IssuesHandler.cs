using Codenizer.HttpClient.Testable.SentryServer.Models;

namespace Codenizer.HttpClient.Testable.SentryServer.Handlers
{
    public class IssuesHandler
    {
        private readonly SentryState _state;

        public IssuesHandler(SentryState state)
        {
            _state = state;
        }

        public object? Get(string issueId)
        {
            if (_state.Issues.TryGetValue(issueId, out var issue))
            {
                return issue;
            }
            return null;
        }

        public object? Update(string issueId, string? status = null, string? title = null, object? assignedTo = null) // assignedTo can be email string or user object
        {
            if (!_state.Issues.TryGetValue(issueId, out var issue)) return null;

            if (status != null) issue.Status = status;
            if (title != null) issue.Title = title;
            
            if (assignedTo != null)
            {
                 // Simplistic assignment logic
                 if (assignedTo is string email)
                 {
                     issue.AssignedTo = new SentryUserStub { Email = email };
                 }
                 else if(assignedTo is Newtonsoft.Json.Linq.JObject userObj)
                 {
                     issue.AssignedTo = userObj.ToObject<SentryUserStub>();
                 }
            }

            return issue;
        }
    }
}
