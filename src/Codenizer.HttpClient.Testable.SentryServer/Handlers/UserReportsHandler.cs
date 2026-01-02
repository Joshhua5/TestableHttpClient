using Codenizer.HttpClient.Testable.SentryServer.Models;

namespace Codenizer.HttpClient.Testable.SentryServer.Handlers
{
    public class UserReportsHandler
    {
        private readonly SentryState _state;

        public UserReportsHandler(SentryState state)
        {
            _state = state;
        }

        public object List(string orgSlug)
        {
            // Ideally filter by org/project
            return _state.UserReports;
        }

        public object? Create(string projectSlug, SentryUserReport report)
        {
            report.Id = _state.GenerateId();
            report.DateCreated = DateTime.UtcNow;
            
            _state.UserReports.Add(report);
            return report;
        }
    }
}
