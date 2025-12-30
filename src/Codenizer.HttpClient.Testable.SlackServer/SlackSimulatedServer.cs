using System.Net;
using System.Net.Http;
using System.Web;
using Codenizer.HttpClient.Testable.SlackServer.Handlers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Codenizer.HttpClient.Testable.SlackServer
{
    /// <summary>
    /// A simulated Slack API server implementing ISimulatedServer.
    /// Provides stateful mock responses for common Slack API endpoints.
    /// </summary>
    public class SlackSimulatedServer : ISimulatedServer
    {
        private readonly SlackState _state;
        private readonly ConversationsHandler _conversationsHandler;
        private readonly UsersHandler _usersHandler;
        private readonly ChatHandler _chatHandler;
        private readonly AuthHandler _authHandler;
        private readonly TeamHandler _teamHandler;
        private readonly ReactionsHandler _reactionsHandler;
        private readonly FilesHandler _filesHandler;
        private readonly PinsHandler _pinsHandler;
        private readonly EmojiHandler _emojiHandler;
        private readonly BookmarksHandler _bookmarksHandler;

        private readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            },
            NullValueHandling = NullValueHandling.Ignore
        };

        public SlackSimulatedServer() : this(new SlackState())
        {
        }

        public SlackSimulatedServer(SlackState state)
        {
            _state = state;
            _conversationsHandler = new ConversationsHandler(_state);
            _usersHandler = new UsersHandler(_state);
            _chatHandler = new ChatHandler(_state);
            _authHandler = new AuthHandler(_state);
            _teamHandler = new TeamHandler(_state);
            _reactionsHandler = new ReactionsHandler(_state);
            _filesHandler = new FilesHandler(_state);
            _pinsHandler = new PinsHandler(_state);
            _emojiHandler = new EmojiHandler(_state);
            _bookmarksHandler = new BookmarksHandler(_state);
        }

        /// <summary>
        /// Gets the internal state for testing purposes.
        /// </summary>
        public SlackState State => _state;

        /// <summary>
        /// Gets or sets the required token for authentication.
        /// If null, authentication is not enforced.
        /// </summary>
        public string? RequiredToken { get; set; }

        public async Task<HttpResponseMessage> HandleRequestAsync(HttpRequestMessage request)
        {
            // Validate authentication if required
            if (RequiredToken != null)
            {
                var authHeader = request.Headers.Authorization;
                if (authHeader == null || authHeader.Scheme != "Bearer" || authHeader.Parameter != RequiredToken)
                {
                    return CreateErrorResponse("invalid_auth", "Invalid authentication token");
                }
            }

            var path = request.RequestUri?.AbsolutePath ?? "";
            var method = path.TrimStart('/').Replace("api/", "");

            // Parse request body if POST
            Dictionary<string, string> formData = new();
            if (request.Method == HttpMethod.Post && request.Content != null)
            {
                var content = await request.Content.ReadAsStringAsync();
                if (request.Content.Headers.ContentType?.MediaType == "application/x-www-form-urlencoded")
                {
                    formData = ParseFormData(content);
                }
                else if (request.Content.Headers.ContentType?.MediaType == "application/json")
                {
                    formData = JsonConvert.DeserializeObject<Dictionary<string, string>>(content) ?? new();
                }
            }

            // Parse query string for GET requests
            if (request.Method == HttpMethod.Get && request.RequestUri?.Query != null)
            {
                formData = ParseFormData(request.RequestUri.Query.TrimStart('?'));
            }

            object? result = method switch
            {
                // Auth
                "auth.test" => _authHandler.Test(),

                // Team
                "team.info" => _teamHandler.Info(),

                // Conversations
                "conversations.list" => _conversationsHandler.List(formData),
                "conversations.info" => _conversationsHandler.Info(formData),
                "conversations.create" => _conversationsHandler.Create(formData),
                "conversations.archive" => _conversationsHandler.Archive(formData),
                "conversations.unarchive" => _conversationsHandler.Unarchive(formData),
                "conversations.history" => _conversationsHandler.History(formData),
                "conversations.members" => _conversationsHandler.Members(formData),
                "conversations.join" => _conversationsHandler.Join(formData),
                "conversations.leave" => _conversationsHandler.Leave(formData),
                "conversations.invite" => _conversationsHandler.Invite(formData),
                "conversations.kick" => _conversationsHandler.Kick(formData),
                "conversations.rename" => _conversationsHandler.Rename(formData),
                "conversations.setTopic" => _conversationsHandler.SetTopic(formData),
                "conversations.setPurpose" => _conversationsHandler.SetPurpose(formData),

                // Users
                "users.list" => _usersHandler.List(formData),
                "users.info" => _usersHandler.Info(formData),
                "users.profile.get" => _usersHandler.ProfileGet(formData),

                // Chat
                "chat.postMessage" => _chatHandler.PostMessage(formData),
                "chat.update" => _chatHandler.Update(formData),
                "chat.delete" => _chatHandler.Delete(formData),
                "chat.postEphemeral" => _chatHandler.PostEphemeral(formData),

                // Reactions
                "reactions.add" => _reactionsHandler.Add(formData),
                "reactions.remove" => _reactionsHandler.Remove(formData),
                "reactions.get" => _reactionsHandler.Get(formData),
                "reactions.list" => _reactionsHandler.List(formData),

                // Files
                "files.list" => _filesHandler.List(formData),
                "files.info" => _filesHandler.Info(formData),
                "files.delete" => _filesHandler.Delete(formData),
                "files.upload" => _filesHandler.Upload(formData),
                "files.sharedPublicURL" => _filesHandler.SharedPublicURL(formData),
                "files.revokePublicURL" => _filesHandler.RevokePublicURL(formData),

                // Pins
                "pins.add" => _pinsHandler.Add(formData),
                "pins.remove" => _pinsHandler.Remove(formData),
                "pins.list" => _pinsHandler.List(formData),

                // Emoji
                "emoji.list" => _emojiHandler.List(),

                // Bookmarks
                "bookmarks.add" => _bookmarksHandler.Add(formData),
                "bookmarks.edit" => _bookmarksHandler.Edit(formData),
                "bookmarks.remove" => _bookmarksHandler.Remove(formData),
                "bookmarks.list" => _bookmarksHandler.List(formData),

                _ => new { ok = false, error = "unknown_method" }
            };

            var json = JsonConvert.SerializeObject(result, _jsonSettings);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        }

        private HttpResponseMessage CreateErrorResponse(string error, string? description = null)
        {
            var result = new { ok = false, error, description };
            var json = JsonConvert.SerializeObject(result, _jsonSettings);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        }

        private static Dictionary<string, string> ParseFormData(string data)
        {
            var result = new Dictionary<string, string>();
            foreach (var pair in data.Split('&'))
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2)
                {
                    result[HttpUtility.UrlDecode(parts[0])] = HttpUtility.UrlDecode(parts[1]);
                }
            }
            return result;
        }
    }
}
