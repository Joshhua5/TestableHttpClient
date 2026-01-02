using Codenizer.HttpClient.Testable.SentryServer.Models;

namespace Codenizer.HttpClient.Testable.SentryServer.Handlers
{
    public class CommentsHandler
    {
        private readonly SentryState _state;

        public CommentsHandler(SentryState state)
        {
            _state = state;
        }

        public object List(string issueId)
        {
            if (_state.IssueComments.TryGetValue(issueId, out var comments))
            {
                return comments;
            }
            return new List<SentryComment>();
        }

        public object? Create(string issueId, Dictionary<string, object> data)
        {
            if (!_state.Issues.ContainsKey(issueId)) return null;

            var comment = new SentryComment
            {
                Id = _state.GenerateId(),
                Data = data,
                DateCreated = DateTime.UtcNow
            };

            if (!_state.IssueComments.ContainsKey(issueId))
            {
                _state.IssueComments[issueId] = new List<SentryComment>();
            }
            _state.IssueComments[issueId].Add(comment);

            return comment;
        }
    }
}
