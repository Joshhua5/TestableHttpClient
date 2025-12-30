namespace Codenizer.HttpClient.Testable.SlackServer.Handlers
{
    public class AuthHandler
    {
        private readonly SlackState _state;

        public AuthHandler(SlackState state)
        {
            _state = state;
        }

        public object Test()
        {
            var user = _state.Users.TryGetValue(_state.CurrentUserId, out var u) ? u : null;

            return new
            {
                ok = true,
                url = $"https://{_state.Team.Domain}.slack.com/",
                team = _state.Team.Name,
                user = user?.Name ?? "unknown",
                team_id = _state.Team.Id,
                user_id = _state.CurrentUserId,
                bot_id = _state.CurrentBotId
            };
        }
    }
}
