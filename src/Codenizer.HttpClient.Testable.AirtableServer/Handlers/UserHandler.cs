namespace Codenizer.HttpClient.Testable.AirtableServer.Handlers
{
    /// <summary>
    /// Handles Airtable User API endpoints.
    /// </summary>
    public class UserHandler
    {
        private readonly AirtableState _state;

        public UserHandler(AirtableState state)
        {
            _state = state;
        }

        /// <summary>
        /// Get current user information and token scopes.
        /// GET /v0/meta/whoami
        /// </summary>
        public object WhoAmI()
        {
            var user = _state.CurrentUser;
            return new
            {
                id = user.Id,
                email = user.Email,
                scopes = user.Scopes
            };
        }
    }
}
