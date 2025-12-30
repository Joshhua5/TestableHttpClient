namespace Codenizer.HttpClient.Testable.SlackServer.Handlers
{
    public class UsersHandler
    {
        private readonly SlackState _state;

        public UsersHandler(SlackState state)
        {
            _state = state;
        }

        public object List(Dictionary<string, string> data)
        {
            var includeDeleted = data.TryGetValue("include_deleted", out var id) && id == "true";
            var users = _state.Users.Values
                .Where(u => includeDeleted || !u.IsDeleted)
                .ToList();

            return new { ok = true, members = users };
        }

        public object Info(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("user", out var userId))
                return new { ok = false, error = "user_not_found" };

            if (!_state.Users.TryGetValue(userId, out var user))
                return new { ok = false, error = "user_not_found" };

            return new { ok = true, user };
        }

        public object ProfileGet(Dictionary<string, string> data)
        {
            var userId = data.TryGetValue("user", out var uid) ? uid : _state.CurrentUserId;

            if (!_state.Users.TryGetValue(userId, out var user))
                return new { ok = false, error = "user_not_found" };

            return new { ok = true, profile = user.Profile };
        }
    }
}
