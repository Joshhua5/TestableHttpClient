namespace Codenizer.HttpClient.Testable.SlackServer.Models
{
    public class SlackTeam
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string EmailDomain { get; set; } = string.Empty;
        public SlackTeamIcon Icon { get; set; } = new();
    }

    public class SlackTeamIcon
    {
        public string Image34 { get; set; } = string.Empty;
        public string Image44 { get; set; } = string.Empty;
        public string Image68 { get; set; } = string.Empty;
        public bool ImageDefault { get; set; } = true;
    }
}
