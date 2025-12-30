namespace Codenizer.HttpClient.Testable.SlackServer.Handlers
{
    public class TeamHandler
    {
        private readonly SlackState _state;

        public TeamHandler(SlackState state)
        {
            _state = state;
        }

        public object Info()
        {
            return new { ok = true, team = _state.Team };
        }
    }
}
