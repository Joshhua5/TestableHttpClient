using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Codenizer.HttpClient.Testable.ZepServer;
using Codenizer.HttpClient.Testable.ZepServer.Models;
using Codenizer.HttpClient.Testable;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Codenizer.HttpClient.Testable.ZepServer.Tests
{
    public class ZepServerTests
    {
        private readonly ZepSimulatedServer _server;
        private readonly TestableMessageHandler _handler;
        private readonly System.Net.Http.HttpClient _client;

        public ZepServerTests()
        {
            _server = new ZepSimulatedServer();
            _handler = new TestableMessageHandler();
            _client = new System.Net.Http.HttpClient(_handler);
        }

        [Fact]
        public async Task GetSessions_ReturnsEmptyList_WhenNoSessionsExist()
        {
            _handler.RespondTo().Get().ForUrl("https://api.getzep.com/api/v1/sessions").HandledBy(_server);

            var response = await _client.GetAsync("https://api.getzep.com/api/v1/sessions");
            var content = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var sessions = JsonConvert.DeserializeObject<System.Collections.Generic.List<Session>>(content);
            sessions.Should().BeEmpty();
        }

        [Fact]
        public async Task CreateSession_ReturnsCreatedSession()
        {
            _handler.RespondTo().Post().ForUrl("https://api.getzep.com/api/v1/sessions").HandledBy(_server);

            var session = new Session { SessionId = "test-session" };

            var response = await _client.PostAsync("https://api.getzep.com/api/v1/sessions", new StringContent(JsonConvert.SerializeObject(session)));
            var content = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var createdSession = JsonConvert.DeserializeObject<Session>(content);
            createdSession!.SessionId.Should().Be("test-session");
        }

        [Fact]
        public async Task AddMemory_ReturnsSuccess()
        {
            _handler.RespondTo().Post().ForUrl("https://api.getzep.com/api/v1/sessions").HandledBy(_server);
            _handler.RespondTo().Post().ForUrl("https://api.getzep.com/api/v1/sessions/test-session/memory").HandledBy(_server);
            
            // Create session first
            await _client.PostAsync("https://api.getzep.com/api/v1/sessions", new StringContent(JsonConvert.SerializeObject(new Session { SessionId = "test-session" })));

            var memory = new Memory 
            {
                Messages = new System.Collections.Generic.List<Message> 
                {
                    new Message { Role = "user", Content = "Hello" }
                }
            };

            var response = await _client.PostAsync("https://api.getzep.com/api/v1/sessions/test-session/memory", new StringContent(JsonConvert.SerializeObject(memory)));
            
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

         [Fact]
        public async Task GetMemory_ReturnsAddedMemory()
        {
            _handler.RespondTo().Post().ForUrl("https://api.getzep.com/api/v1/sessions").HandledBy(_server);
            _handler.RespondTo().Post().ForUrl("https://api.getzep.com/api/v1/sessions/test-session/memory").HandledBy(_server);
            _handler.RespondTo().Get().ForUrl("https://api.getzep.com/api/v1/sessions/test-session/memory").HandledBy(_server);

            // Create session and add memory
            await _client.PostAsync("https://api.getzep.com/api/v1/sessions", new StringContent(JsonConvert.SerializeObject(new Session { SessionId = "test-session" })));
            
             var memory = new Memory 
            {
                Messages = new System.Collections.Generic.List<Message> 
                {
                    new Message { Role = "user", Content = "Hello" }
                }
            };
            await _client.PostAsync("https://api.getzep.com/api/v1/sessions/test-session/memory", new StringContent(JsonConvert.SerializeObject(memory)));


            var response = await _client.GetAsync("https://api.getzep.com/api/v1/sessions/test-session/memory");
            var content = await response.Content.ReadAsStringAsync();
            
            response.StatusCode.Should().Be(HttpStatusCode.OK);
             var fetchedMemory = JsonConvert.DeserializeObject<Memory>(content);
             fetchedMemory!.Messages.Should().HaveCount(1);
             fetchedMemory.Messages[0].Content.Should().Be("Hello");
        }

        [Fact]
        public async Task CreateCollection_ReturnsCreatedCollection()
        {
            _handler.RespondTo().Post().ForUrl("https://api.getzep.com/api/v1/collection/test-collection").HandledBy(_server);

            var collection = new DocumentCollection { Description = "Test Collection", Name = "test-collection" }; // Adding Name here as per model requirement

            var response = await _client.PostAsync("https://api.getzep.com/api/v1/collection/test-collection", new StringContent(JsonConvert.SerializeObject(collection)));
            var content = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var createdCollection = JsonConvert.DeserializeObject<DocumentCollection>(content);
            createdCollection!.Name.Should().Be("test-collection");
            createdCollection.Description.Should().Be("Test Collection");
        }
    }
}
