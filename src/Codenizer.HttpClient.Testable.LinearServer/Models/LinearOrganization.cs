namespace Codenizer.HttpClient.Testable.LinearServer.Models
{
    /// <summary>
    /// Represents the Linear organization.
    /// </summary>
    public class LinearOrganization
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string UrlKey { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool SamlEnabled { get; set; }
        public bool ScimEnabled { get; set; }
        public int UserCount { get; set; }
        public string GitBranchFormat { get; set; } = "{issueIdentifier}-{issueTitle}";
    }
}
