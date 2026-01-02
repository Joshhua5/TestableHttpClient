namespace Codenizer.HttpClient.Testable.AirtableServer.Models
{
    /// <summary>
    /// Represents an Airtable user.
    /// </summary>
    public class AirtableUser
    {
        /// <summary>
        /// The unique identifier for this user.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The user's email address.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// The scopes granted to the current token.
        /// </summary>
        public AirtableTokenScopes? Scopes { get; set; }
    }

    /// <summary>
    /// Represents the scopes granted to an Airtable token.
    /// </summary>
    public class AirtableTokenScopes
    {
        /// <summary>
        /// Account-level scopes.
        /// </summary>
        public List<string>? AccountScopes { get; set; }

        /// <summary>
        /// Base-level scopes with the bases they apply to.
        /// </summary>
        public AirtableBaseScopesInfo? BaseScopesInfo { get; set; }

        /// <summary>
        /// User token scopes.
        /// </summary>
        public List<string>? UserTokenScopes { get; set; }
    }

    /// <summary>
    /// Represents base-level scope information.
    /// </summary>
    public class AirtableBaseScopesInfo
    {
        /// <summary>
        /// The permissions granted.
        /// </summary>
        public List<string>? Permissions { get; set; }

        /// <summary>
        /// The base IDs these permissions apply to.
        /// </summary>
        public List<string>? BaseIds { get; set; }

        /// <summary>
        /// The workspace IDs these permissions apply to.
        /// </summary>
        public List<string>? WorkspaceIds { get; set; }
    }
}
