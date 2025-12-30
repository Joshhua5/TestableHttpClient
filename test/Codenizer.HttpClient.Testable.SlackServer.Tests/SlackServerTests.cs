using System.Net;
using System.Net.Http;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Codenizer.HttpClient.Testable.SlackServer.Tests
{
    public class SlackServerTests
    {
        private readonly SlackSimulatedServer _server;
        private readonly TestableMessageHandler _handler;
        private readonly System.Net.Http.HttpClient _client;

        public SlackServerTests()
        {
            _server = new SlackSimulatedServer();
            _handler = new TestableMessageHandler();
            _client = new System.Net.Http.HttpClient(_handler);

            // Register all Slack endpoints to be handled by the simulated server
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/auth.test").HandledBy(_server);
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/team.info").HandledBy(_server);
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/conversations.list").HandledBy(_server);
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/conversations.info").HandledBy(_server);
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/conversations.create").HandledBy(_server);
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/conversations.archive").HandledBy(_server);
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/conversations.history").HandledBy(_server);
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/conversations.join").HandledBy(_server);
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/users.list").HandledBy(_server);
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/users.info").HandledBy(_server);
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/chat.postMessage").HandledBy(_server);
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/chat.update").HandledBy(_server);
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/chat.delete").HandledBy(_server);
            
            // Reactions
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/reactions.add").HandledBy(_server);
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/reactions.remove").HandledBy(_server);
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/reactions.get").HandledBy(_server);
            
            // Files
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/files.upload").HandledBy(_server);
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/files.list").HandledBy(_server);
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/files.delete").HandledBy(_server);
            
            // Pins
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/pins.add").HandledBy(_server);
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/pins.list").HandledBy(_server);
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/pins.remove").HandledBy(_server);
            
            // Emoji
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/emoji.list").HandledBy(_server);
            
            // Bookmarks
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/bookmarks.add").HandledBy(_server);
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/bookmarks.list").HandledBy(_server);
            _handler.RespondTo().Post().ForUrl("https://slack.com/api/bookmarks.remove").HandledBy(_server);
        }

        [Fact]
        public async Task AuthTest_ReturnsValidResponse()
        {
            var response = await _client.PostAsync("https://slack.com/api/auth.test", null);
            var json = JObject.Parse(await response.Content.ReadAsStringAsync());

            json["ok"]!.Value<bool>().Should().BeTrue();
            json["team"]!.Value<string>().Should().Be("Test Workspace");
            json["team_id"]!.Value<string>().Should().Be("T00000001");
        }

        [Fact]
        public async Task TeamInfo_ReturnsTeamDetails()
        {
            var response = await _client.PostAsync("https://slack.com/api/team.info", null);
            var json = JObject.Parse(await response.Content.ReadAsStringAsync());

            json["ok"]!.Value<bool>().Should().BeTrue();
            json["team"]!["name"]!.Value<string>().Should().Be("Test Workspace");
            json["team"]!["domain"]!.Value<string>().Should().Be("test-workspace");
        }

        [Fact]
        public async Task ConversationsList_ReturnsDefaultChannels()
        {
            var response = await _client.PostAsync("https://slack.com/api/conversations.list", null);
            var json = JObject.Parse(await response.Content.ReadAsStringAsync());

            json["ok"]!.Value<bool>().Should().BeTrue();
            json["channels"]!.Should().HaveCountGreaterOrEqualTo(2);
        }

        [Fact]
        public async Task ConversationsCreate_CreatesNewChannel_ThenAppearInList()
        {
            // Create a new channel
            var createContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("name", "my-new-channel")
            });
            var createResponse = await _client.PostAsync("https://slack.com/api/conversations.create", createContent);
            var createJson = JObject.Parse(await createResponse.Content.ReadAsStringAsync());

            createJson["ok"]!.Value<bool>().Should().BeTrue();
            var channelId = createJson["channel"]!["id"]!.Value<string>();
            channelId.Should().NotBeNullOrEmpty();

            // Verify it appears in the list
            var listResponse = await _client.PostAsync("https://slack.com/api/conversations.list", null);
            var listJson = JObject.Parse(await listResponse.Content.ReadAsStringAsync());

            var channels = listJson["channels"]!.ToObject<List<JObject>>()!;
            channels.Should().Contain(c => c["name"]!.Value<string>() == "my-new-channel");
        }

        [Fact]
        public async Task ConversationsArchive_ArchivesChannel()
        {
            var archiveContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("channel", "C00000001")
            });
            var archiveResponse = await _client.PostAsync("https://slack.com/api/conversations.archive", archiveContent);
            var archiveJson = JObject.Parse(await archiveResponse.Content.ReadAsStringAsync());

            archiveJson["ok"]!.Value<bool>().Should().BeTrue();
            _server.State.Channels["C00000001"].IsArchived.Should().BeTrue();
        }

        [Fact]
        public async Task UsersList_ReturnsDefaultUsers()
        {
            var response = await _client.PostAsync("https://slack.com/api/users.list", null);
            var json = JObject.Parse(await response.Content.ReadAsStringAsync());

            json["ok"]!.Value<bool>().Should().BeTrue();
            json["members"]!.Should().HaveCountGreaterOrEqualTo(2);
        }

        [Fact]
        public async Task UsersInfo_ReturnsUserDetails()
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("user", "U00000001")
            });
            var response = await _client.PostAsync("https://slack.com/api/users.info", content);
            var json = JObject.Parse(await response.Content.ReadAsStringAsync());

            json["ok"]!.Value<bool>().Should().BeTrue();
            json["user"]!["name"]!.Value<string>().Should().Be("testuser");
            json["user"]!["real_name"]!.Value<string>().Should().Be("Test User");
        }

        [Fact]
        public async Task ChatPostMessage_PostsMessage_ThenAppearsInHistory()
        {
            // Post a message
            var postContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("channel", "C00000001"),
                new KeyValuePair<string, string>("text", "Hello, World!")
            });
            var postResponse = await _client.PostAsync("https://slack.com/api/chat.postMessage", postContent);
            var postJson = JObject.Parse(await postResponse.Content.ReadAsStringAsync());

            postJson["ok"]!.Value<bool>().Should().BeTrue();
            var ts = postJson["ts"]!.Value<string>();
            ts.Should().NotBeNullOrEmpty();

            // Get history
            var historyContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("channel", "C00000001")
            });
            var historyResponse = await _client.PostAsync("https://slack.com/api/conversations.history", historyContent);
            var historyJson = JObject.Parse(await historyResponse.Content.ReadAsStringAsync());

            historyJson["ok"]!.Value<bool>().Should().BeTrue();
            var messages = historyJson["messages"]!.ToObject<List<JObject>>()!;
            messages.Should().Contain(m => m["text"]!.Value<string>() == "Hello, World!");
        }

        [Fact]
        public async Task ChatUpdate_UpdatesExistingMessage()
        {
            // Post a message first
            var postContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("channel", "C00000001"),
                new KeyValuePair<string, string>("text", "Original text")
            });
            var postResponse = await _client.PostAsync("https://slack.com/api/chat.postMessage", postContent);
            var postJson = JObject.Parse(await postResponse.Content.ReadAsStringAsync());
            var ts = postJson["ts"]!.Value<string>();

            // Update the message
            var updateContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("channel", "C00000001"),
                new KeyValuePair<string, string>("ts", ts!),
                new KeyValuePair<string, string>("text", "Updated text")
            });
            var updateResponse = await _client.PostAsync("https://slack.com/api/chat.update", updateContent);
            var updateJson = JObject.Parse(await updateResponse.Content.ReadAsStringAsync());

            updateJson["ok"]!.Value<bool>().Should().BeTrue();
            updateJson["text"]!.Value<string>().Should().Be("Updated text");
        }

        [Fact]
        public async Task ChatDelete_DeletesMessage()
        {
            // Post a message first
            var postContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("channel", "C00000001"),
                new KeyValuePair<string, string>("text", "To be deleted")
            });
            var postResponse = await _client.PostAsync("https://slack.com/api/chat.postMessage", postContent);
            var postJson = JObject.Parse(await postResponse.Content.ReadAsStringAsync());
            var ts = postJson["ts"]!.Value<string>();

            // Delete the message
            var deleteContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("channel", "C00000001"),
                new KeyValuePair<string, string>("ts", ts!)
            });
            var deleteResponse = await _client.PostAsync("https://slack.com/api/chat.delete", deleteContent);
            var deleteJson = JObject.Parse(await deleteResponse.Content.ReadAsStringAsync());

            deleteJson["ok"]!.Value<bool>().Should().BeTrue();

            // Verify it's gone from history
            _server.State.Messages["C00000001"].Should().NotContain(m => m.Ts == ts);
        }

        [Fact]
        public async Task Authentication_WhenTokenRequired_RejectsInvalidToken()
        {
            _server.RequiredToken = "xoxb-valid-token";

            var response = await _client.PostAsync("https://slack.com/api/auth.test", null);
            var json = JObject.Parse(await response.Content.ReadAsStringAsync());

            json["ok"]!.Value<bool>().Should().BeFalse();
            json["error"]!.Value<string>().Should().Be("invalid_auth");
        }

        [Fact]
        public async Task Authentication_WhenTokenRequired_AcceptsValidToken()
        {
            _server.RequiredToken = "xoxb-valid-token";

            var request = new HttpRequestMessage(HttpMethod.Post, "https://slack.com/api/auth.test");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "xoxb-valid-token");

            var response = await _client.SendAsync(request);
            var json = JObject.Parse(await response.Content.ReadAsStringAsync());

            json["ok"]!.Value<bool>().Should().BeTrue();
        }

        // Reactions Tests
        [Fact]
        public async Task ReactionsAdd_AddsReactionToMessage()
        {
            // Post a message first
            var postContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("channel", "C00000001"),
                new KeyValuePair<string, string>("text", "React to this!")
            });
            var postResponse = await _client.PostAsync("https://slack.com/api/chat.postMessage", postContent);
            var postJson = JObject.Parse(await postResponse.Content.ReadAsStringAsync());
            var ts = postJson["ts"]!.Value<string>();

            // Add reaction
            var reactionContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("channel", "C00000001"),
                new KeyValuePair<string, string>("timestamp", ts!),
                new KeyValuePair<string, string>("name", "thumbsup")
            });
            var reactionResponse = await _client.PostAsync("https://slack.com/api/reactions.add", reactionContent);
            var reactionJson = JObject.Parse(await reactionResponse.Content.ReadAsStringAsync());

            reactionJson["ok"]!.Value<bool>().Should().BeTrue();
            _server.State.Messages["C00000001"].First(m => m.Ts == ts).Reactions.Should().Contain(r => r.Name == "thumbsup");
        }

        // Files Tests
        [Fact]
        public async Task FilesUpload_UploadsFile_ThenAppearsInList()
        {
            var uploadContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("filename", "test.txt"),
                new KeyValuePair<string, string>("title", "Test File"),
                new KeyValuePair<string, string>("content", "Hello World")
            });
            var uploadResponse = await _client.PostAsync("https://slack.com/api/files.upload", uploadContent);
            var uploadJson = JObject.Parse(await uploadResponse.Content.ReadAsStringAsync());

            uploadJson["ok"]!.Value<bool>().Should().BeTrue();
            var fileId = uploadJson["file"]!["id"]!.Value<string>();

            // Verify in list
            var listResponse = await _client.PostAsync("https://slack.com/api/files.list", null);
            var listJson = JObject.Parse(await listResponse.Content.ReadAsStringAsync());
            var files = listJson["files"]!.ToObject<List<JObject>>()!;
            files.Should().Contain(f => f["id"]!.Value<string>() == fileId);
        }

        // Pins Tests
        [Fact]
        public async Task PinsAdd_PinsMessage_ThenAppearsInList()
        {
            // Post a message first
            var postContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("channel", "C00000001"),
                new KeyValuePair<string, string>("text", "Pin this!")
            });
            var postResponse = await _client.PostAsync("https://slack.com/api/chat.postMessage", postContent);
            var postJson = JObject.Parse(await postResponse.Content.ReadAsStringAsync());
            var ts = postJson["ts"]!.Value<string>();

            // Pin the message
            var pinContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("channel", "C00000001"),
                new KeyValuePair<string, string>("timestamp", ts!)
            });
            var pinResponse = await _client.PostAsync("https://slack.com/api/pins.add", pinContent);
            var pinJson = JObject.Parse(await pinResponse.Content.ReadAsStringAsync());

            pinJson["ok"]!.Value<bool>().Should().BeTrue();

            // Verify in list
            var listContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("channel", "C00000001")
            });
            var listResponse = await _client.PostAsync("https://slack.com/api/pins.list", listContent);
            var listJson = JObject.Parse(await listResponse.Content.ReadAsStringAsync());
            listJson["items"]!.Should().HaveCountGreaterOrEqualTo(1);
        }

        // Emoji Tests
        [Fact]
        public async Task EmojiList_ReturnsCustomEmoji()
        {
            var response = await _client.PostAsync("https://slack.com/api/emoji.list", null);
            var json = JObject.Parse(await response.Content.ReadAsStringAsync());

            json["ok"]!.Value<bool>().Should().BeTrue();
            json["emoji"]!.Should().NotBeNull();
        }

        // Bookmarks Tests
        [Fact]
        public async Task BookmarksAdd_AddsBookmark_ThenAppearsInList()
        {
            var addContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("channel_id", "C00000001"),
                new KeyValuePair<string, string>("title", "Important Link"),
                new KeyValuePair<string, string>("link", "https://example.com")
            });
            var addResponse = await _client.PostAsync("https://slack.com/api/bookmarks.add", addContent);
            var addJson = JObject.Parse(await addResponse.Content.ReadAsStringAsync());

            addJson["ok"]!.Value<bool>().Should().BeTrue();
            var bookmarkId = addJson["bookmark"]!["id"]!.Value<string>();

            // Verify in list
            var listContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("channel_id", "C00000001")
            });
            var listResponse = await _client.PostAsync("https://slack.com/api/bookmarks.list", listContent);
            var listJson = JObject.Parse(await listResponse.Content.ReadAsStringAsync());
            var bookmarks = listJson["bookmarks"]!.ToObject<List<JObject>>()!;
            bookmarks.Should().Contain(b => b["id"]!.Value<string>() == bookmarkId);
        }
    }
}
