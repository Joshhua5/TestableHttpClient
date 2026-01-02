using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Codenizer.HttpClient.Testable.SentryServer;
using Codenizer.HttpClient.Testable.SentryServer.Models;
using Codenizer.HttpClient.Testable.SentryServer.Handlers;
using System.Collections.Generic;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Codenizer.HttpClient.Testable.Tests.Unit
{
    public class SentryServerTests
    {
        [Fact]
        public async Task CanListOrganizations()
        {
            var handler = new TestableMessageHandler();
            var server = new SentrySimulatedServer();
            handler.RespondTo().Get().ForUrl("https://sentry.io/api/0/organizations").HandledBy(server);

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("https://sentry.io/api/0/organizations"); // No trailing slash logic handled? Code handles both

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var orgs = JsonConvert.DeserializeObject<SentryOrganization[]>(content);
            orgs.Should().NotBeNull();
            orgs.Should().HaveCount(1);
            orgs[0].Slug.Should().Be("sentry-sc");
        }

        [Fact]
        public async Task CanCreateProject()
        {
            var handler = new TestableMessageHandler();
            var server = new SentrySimulatedServer();
            // Register both the create endpoint and the list endpoint
            handler.RespondTo().Post().ForUrl("https://sentry.io/api/0/teams/sentry-sc/backend/projects/").HandledBy(server);
            handler.RespondTo().Get().ForUrl("https://sentry.io/api/0/projects").HandledBy(server);
            
            var client = new System.Net.Http.HttpClient(handler);
            
            // Create a new project
            // URL: /api/0/teams/{orgSlug}/{teamSlug}/projects/
            var request = new HttpRequestMessage(HttpMethod.Post, "https://sentry.io/api/0/teams/sentry-sc/backend/projects/");
            request.Content = new StringContent("{\"name\": \"My New Project\"}", System.Text.Encoding.UTF8, "application/json");
            
            var response = await client.SendAsync(request);
            
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var content = await response.Content.ReadAsStringAsync();
            var project = JsonConvert.DeserializeObject<SentryProject>(content);
            project.Should().NotBeNull();
            project.Name.Should().Be("My New Project");
            project.Slug.Should().Be("my-new-project");
            
            // Verify it exists in list
            var listResponse = await client.GetAsync("https://sentry.io/api/0/projects");
            var listContent = await listResponse.Content.ReadAsStringAsync();
            var projects = JsonConvert.DeserializeObject<SentryProject[]>(listContent);
            projects.Should().NotBeNull();
            projects.Should().Contain(p => p.Slug == "my-new-project");
        }

        [Fact]
        public async Task CanStoreEvent()
        {
            var handler = new TestableMessageHandler();
            var server = new SentrySimulatedServer();
            handler.RespondTo().Post().ForUrl("https://sentry.io/api/123/store").HandledBy(server);

            var client = new System.Net.Http.HttpClient(handler);
            var evt = new SentryEvent { Message = "Test Error", Level = "error" };
            
            var request = new HttpRequestMessage(HttpMethod.Post, "https://sentry.io/api/123/store");
            request.Content = new StringContent(JsonConvert.SerializeObject(evt), System.Text.Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            // Verify event is stored in state
            server.State.Events.Should().HaveCount(1);
            server.State.Events[0].Message.Should().Be("Test Error");
            
            // Verify issue was created
            server.State.Issues.Should().HaveCount(1);
            server.State.Issues.Values.First().Title.Should().Be("Test Error");
        }
        
        [Fact]
        public async Task RequiresAuthWhenConfigured()
        {
            var handler = new TestableMessageHandler();
            var server = new SentrySimulatedServer { RequiredToken = "secret-token" };
            handler.RespondTo().Get().ForUrl("https://sentry.io/api/0/organizations").HandledBy(server);

            var client = new System.Net.Http.HttpClient(handler);
            
            // Without token
            var response = await client.GetAsync("https://sentry.io/api/0/organizations");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            
            // With token
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "secret-token");
            var responseWithAuth = await client.GetAsync("https://sentry.io/api/0/organizations");
            responseWithAuth.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CanManageTeams()
        {
            var handler = new TestableMessageHandler();
            var server = new SentrySimulatedServer();
            handler.RespondTo().Get().ForUrl("https://sentry.io/api/0/organizations/sentry-sc/teams").HandledBy(server);
            handler.RespondTo().Post().ForUrl("https://sentry.io/api/0/organizations/sentry-sc/teams").HandledBy(server);
            handler.RespondTo().Delete().ForUrl("https://sentry.io/api/0/teams/sentry-sc/my-new-team/").HandledBy(server);

            var client = new System.Net.Http.HttpClient(handler);

            // Create Team
            var createRequest = new HttpRequestMessage(HttpMethod.Post, "https://sentry.io/api/0/organizations/sentry-sc/teams");
            createRequest.Content = new StringContent("{\"name\": \"My New Team\", \"slug\": \"my-new-team\"}", System.Text.Encoding.UTF8, "application/json");
            var createResponse = await client.SendAsync(createRequest);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            // List Teams
            var listResponse = await client.GetAsync("https://sentry.io/api/0/organizations/sentry-sc/teams");
            listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var listContent = await listResponse.Content.ReadAsStringAsync();
            var teams = JsonConvert.DeserializeObject<SentryTeam[]>(listContent);
            teams.Should().NotBeNull();
            teams.Should().Contain(t => t.Slug == "my-new-team");

            // Delete Team
            var deleteResponse = await client.DeleteAsync("https://sentry.io/api/0/teams/sentry-sc/my-new-team/");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // List again to verify deletion
            var listResponse2 = await client.GetAsync("https://sentry.io/api/0/organizations/sentry-sc/teams");
            var listContent2 = await listResponse2.Content.ReadAsStringAsync();
            var teams2 = JsonConvert.DeserializeObject<SentryTeam[]>(listContent2);
            teams2.Should().NotBeNull();
            teams2.Should().NotContain(t => t.Slug == "my-new-team");
        }

        [Fact]
        public async Task CanManageReleases()
        {
            var handler = new TestableMessageHandler();
            var server = new SentrySimulatedServer();
            handler.RespondTo().Get().ForUrl("https://sentry.io/api/0/organizations/sentry-sc/releases").HandledBy(server);
            handler.RespondTo().Post().ForUrl("https://sentry.io/api/0/organizations/sentry-sc/releases").HandledBy(server);

            var client = new System.Net.Http.HttpClient(handler);

            // Create Release
            var createRequest = new HttpRequestMessage(HttpMethod.Post, "https://sentry.io/api/0/organizations/sentry-sc/releases");
            createRequest.Content = new StringContent("{\"version\": \"v1.0.0\"}", System.Text.Encoding.UTF8, "application/json");
            var createResponse = await client.SendAsync(createRequest);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            // List Releases
            var listResponse = await client.GetAsync("https://sentry.io/api/0/organizations/sentry-sc/releases");
            listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var listContent = await listResponse.Content.ReadAsStringAsync();
            var releases = JsonConvert.DeserializeObject<SentryRelease[]>(listContent);
            releases.Should().NotBeNull();
            releases.Should().HaveCount(1);
            releases[0].Version.Should().Be("v1.0.0");
        }

        [Fact]
        public async Task CanGetProjectKeys()
        {
            var handler = new TestableMessageHandler();
            var server = new SentrySimulatedServer();
            
            // Setup: Create project first (or assume default data if seeded properly, but explicit is better)
            handler.RespondTo().Post().ForUrl("https://sentry.io/api/0/projects/sentry-sc/api-server/keys/").HandledBy(server);
            handler.RespondTo().Get().ForUrl("https://sentry.io/api/0/projects/sentry-sc/api-server/keys/").HandledBy(server);

            var client = new System.Net.Http.HttpClient(handler);
            
            // Create Key
            var createRequest = new HttpRequestMessage(HttpMethod.Post, "https://sentry.io/api/0/projects/sentry-sc/api-server/keys/");
            createRequest.Content = new StringContent("{\"name\": \"New Key\"}", System.Text.Encoding.UTF8, "application/json");
            var createResponse = await client.SendAsync(createRequest);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            
            var content = await createResponse.Content.ReadAsStringAsync();
            var createdKey = JsonConvert.DeserializeObject<SentryProjectKey>(content);
            createdKey.Should().NotBeNull();
            createdKey.Dsn.Public.Should().NotBeNullOrEmpty();
            createdKey.Name.Should().Be("New Key");

            // List Keys
            var listResponse = await client.GetAsync("https://sentry.io/api/0/projects/sentry-sc/api-server/keys/");
            listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var listContent = await listResponse.Content.ReadAsStringAsync();
            var keys = JsonConvert.DeserializeObject<SentryProjectKey[]>(listContent);
            keys.Should().NotBeNull();
            keys.Should().Contain(k => k.Name == "New Key");
        }

        [Fact]
        public async Task CanStoreEventWithTagsAndUser()
        {
            var handler = new TestableMessageHandler();
            var server = new SentrySimulatedServer();
            handler.RespondTo().Post().ForUrl("https://sentry.io/api/42/store").HandledBy(server);

            var client = new System.Net.Http.HttpClient(handler);
            var evt = new SentryEvent 
            { 
                Message = "Rich Error", 
                Level = "error",
                Tags = new Dictionary<string, string>{ { "browser", "chrome" } },
                User = new SentryUserStub { Email = "user@example.com", Id = "u123" }
            };
            
            var request = new HttpRequestMessage(HttpMethod.Post, "https://sentry.io/api/42/store");
            request.Content = new StringContent(JsonConvert.SerializeObject(evt), System.Text.Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify
            server.State.Events.Should().HaveCount(1);
            var storedEvent = server.State.Events[0];
            storedEvent.Tags.Should().ContainKey("browser");
            storedEvent.User.Should().NotBeNull();
            storedEvent.User!.Email.Should().Be("user@example.com");
        }

        [Fact]
        public async Task CanAssignIssue()
        {
            var handler = new TestableMessageHandler();
            var server = new SentrySimulatedServer();
            // Seed an issue
            var issue = new SentryIssue { Id = "i99", Title = "Some specific error" };
            server.State.Issues.Add(issue.Id, issue);
            
            handler.RespondTo().Put().ForUrl("https://sentry.io/api/0/issues/i99/").HandledBy(server);

            var client = new System.Net.Http.HttpClient(handler);
            
            var request = new HttpRequestMessage(HttpMethod.Put, "https://sentry.io/api/0/issues/i99/");
            // Simulate payload: { "assignedTo": "assignee@example.com" }
            request.Content = new StringContent("{\"assignedTo\": \"assignee@example.com\"}", System.Text.Encoding.UTF8, "application/json"); 
            
            var response = await client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            server.State.Issues["i99"].AssignedTo.Should().NotBeNull();
            server.State.Issues["i99"].AssignedTo!.Email.Should().Be("assignee@example.com");
        }

        [Fact]
        public async Task CanAddCommentToIssue()
        {
            var handler = new TestableMessageHandler();
            var server = new SentrySimulatedServer();
            var issue = new SentryIssue { Id = "i88", Title = "Another error" };
            server.State.Issues.Add(issue.Id, issue);

            handler.RespondTo().Post().ForUrl("https://sentry.io/api/0/issues/i88/comments/").HandledBy(server);
            handler.RespondTo().Get().ForUrl("https://sentry.io/api/0/issues/i88/comments/").HandledBy(server);

            var client = new System.Net.Http.HttpClient(handler);

            // Create Comment
            var createRequest = new HttpRequestMessage(HttpMethod.Post, "https://sentry.io/api/0/issues/i88/comments/");
            createRequest.Content = new StringContent("{\"data\": { \"text\": \"Fixing this now\" }}", System.Text.Encoding.UTF8, "application/json");
            
            var createResponse = await client.SendAsync(createRequest);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            // List Comments
            var listResponse = await client.GetAsync("https://sentry.io/api/0/issues/i88/comments/");
            listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await listResponse.Content.ReadAsStringAsync();
            var comments = JsonConvert.DeserializeObject<SentryComment[]>(content);
            comments.Should().NotBeNull();
            
            comments.Should().HaveCount(1);
            // SentryComment.Data is Dict<string,object>, check contents
            comments[0].Data.Should().NotBeNull();
            comments[0].Data!.Should().ContainKey("text");
            comments[0].Data!["text"].Should().Be("Fixing this now");
        }
        [Fact]
        public async Task CanAcceptEnvelope()
        {
            var handler = new TestableMessageHandler();
            var server = new SentrySimulatedServer();
            handler.RespondTo().Post().ForUrl("https://sentry.io/api/123/envelope/").HandledBy(server);

            var client = new System.Net.Http.HttpClient(handler);
            
            // Construct Envelope (NDJSON)
            // Header
            var envelope = "{\"event_id\":\"9ec79c33ec9942ab8353589fcb2e04dc\",\"dsn\":\"https://public@sentry.io/123\"}\n";
            // Item Header
            envelope += "{\"type\":\"event\",\"length\":41}\n";
            // Payload
            envelope += "{\"message\":\"Envelope Error\",\"level\":\"error\"}\n";

            var request = new HttpRequestMessage(HttpMethod.Post, "https://sentry.io/api/123/envelope/");
            request.Content = new StringContent(envelope, System.Text.Encoding.UTF8, "application/x-sentry-envelope");

            var response = await client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            // Verify
            server.State.Events.Should().HaveCount(1);
            server.State.Events[0].Message.Should().Be("Envelope Error");
        }

        [Fact]
        public async Task CanCollectUserFeedback()
        {
            var handler = new TestableMessageHandler();
            var server = new SentrySimulatedServer();
            
            handler.RespondTo().Post().ForUrl("https://sentry.io/api/0/projects/sentry-sc/backend/user-reports/").HandledBy(server);
            handler.RespondTo().Get().ForUrl("https://sentry.io/api/0/projects/sentry-sc/backend/user-reports/").HandledBy(server);

            var client = new System.Net.Http.HttpClient(handler);
            var report = new SentryUserReport 
            {
                Name = "John Doe",
                Email = "john@example.com",
                Comments = "It crashed when I clicked the button",
                EventId = "some-event-id"
            };
            
            var request = new HttpRequestMessage(HttpMethod.Post, "https://sentry.io/api/0/projects/sentry-sc/backend/user-reports/");
            request.Content = new StringContent(JsonConvert.SerializeObject(report), System.Text.Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            
            // List
            var listResponse = await client.GetAsync("https://sentry.io/api/0/projects/sentry-sc/backend/user-reports/");
            var content = await listResponse.Content.ReadAsStringAsync();
            var reports = JsonConvert.DeserializeObject<SentryUserReport[]>(content);
            reports.Should().NotBeNull();
            
            reports.Should().HaveCount(1);
            reports[0].Email.Should().Be("john@example.com");
        }
        [Fact]
        public async Task CanAcceptEnvelopeWithUserFeedback()
        {
            var handler = new TestableMessageHandler();
            var server = new SentrySimulatedServer();
            handler.RespondTo().Post().ForUrl("https://sentry.io/api/123/envelope/").HandledBy(server);
            handler.RespondTo().Get().ForUrl("https://sentry.io/api/0/projects/sentry-sc/backend/user-reports/").HandledBy(server);

            var client = new System.Net.Http.HttpClient(handler);
            
            // Construct Envelope (NDJSON) with user report
            // Header
            var envelope = "{\"event_id\":\"9ec79c33ec9942ab8353589fcb2e04dc\",\"dsn\":\"https://public@sentry.io/123\"}\n";
            // Item Header
            envelope += "{\"type\":\"user_report\",\"length\":118}\n";
            // Payload
            envelope += "{\"event_id\":\"9ec79c33ec9942ab8353589fcb2e04dc\",\"name\":\"Jane Doe\",\"email\":\"jane@example.com\",\"comments\":\"It broke again\"}\n";

            var request = new HttpRequestMessage(HttpMethod.Post, "https://sentry.io/api/123/envelope/");
            request.Content = new StringContent(envelope, System.Text.Encoding.UTF8, "application/x-sentry-envelope");

            var response = await client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            // Verify
            server.State.UserReports.Should().HaveCount(1);
            server.State.UserReports[0].Email.Should().Be("jane@example.com");
            server.State.UserReports[0].Comments.Should().Be("It broke again");
        }
    }
}
