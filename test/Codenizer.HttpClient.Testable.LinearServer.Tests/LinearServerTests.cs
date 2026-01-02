using System.Net;
using System.Net.Http;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Codenizer.HttpClient.Testable.LinearServer.Tests
{
    public class LinearServerTests
    {
        private readonly LinearSimulatedServer _server;
        private readonly TestableMessageHandler _handler;
        private readonly System.Net.Http.HttpClient _client;

        public LinearServerTests()
        {
            _server = new LinearSimulatedServer();
            _handler = new TestableMessageHandler();
            _client = new System.Net.Http.HttpClient(_handler);

            // Register Linear API endpoints
            _handler.RespondTo().Post().ForUrl("https://api.linear.app/graphql").HandledBy(_server);
            _handler.RespondTo().Post().ForUrl("https://api.linear.app/oauth/token").HandledBy(_server);
            _handler.RespondTo().Post().ForUrl("https://api.linear.app/oauth/revoke").HandledBy(_server);
        }

        private async Task<JObject> ExecuteGraphQL(string query, object? variables = null)
        {
            var requestBody = new { query, variables };
            var content = new StringContent(
                JsonConvert.SerializeObject(requestBody),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await _client.PostAsync("https://api.linear.app/graphql", content);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            return JObject.Parse(responseContent);
        }

        #region Viewer Query Tests

        [Fact]
        public async Task ViewerQuery_ReturnsCurrentUser()
        {
            var query = "query { viewer { id name email } }";
            var json = await ExecuteGraphQL(query);

            json["data"]!["viewer"]!["id"]!.Value<string>().Should().Be("user_00000001");
            json["data"]!["viewer"]!["name"]!.Value<string>().Should().Be("Test User");
            json["data"]!["viewer"]!["email"]!.Value<string>().Should().Be("testuser@example.com");
        }

        [Fact]
        public async Task ViewerQuery_ReturnsActiveStatus()
        {
            var query = "query { viewer { id active admin } }";
            var json = await ExecuteGraphQL(query);

            json["data"]!["viewer"]!["active"]!.Value<bool>().Should().BeTrue();
            json["data"]!["viewer"]!["admin"]!.Value<bool>().Should().BeTrue();
        }

        #endregion

        #region Organization Query Tests

        [Fact]
        public async Task OrganizationQuery_ReturnsOrganizationDetails()
        {
            var query = "query { organization { id name urlKey } }";
            var json = await ExecuteGraphQL(query);

            json["data"]!["organization"]!["id"]!.Value<string>().Should().Be("org_00000001");
            json["data"]!["organization"]!["name"]!.Value<string>().Should().Be("Test Organization");
            json["data"]!["organization"]!["urlKey"]!.Value<string>().Should().Be("test-org");
        }

        #endregion

        #region Teams Query Tests

        [Fact]
        public async Task TeamsQuery_ReturnsAllTeams()
        {
            var query = "query { teams { nodes { id name key } } }";
            var json = await ExecuteGraphQL(query);

            var teams = json["data"]!["teams"]!["nodes"]!.ToObject<List<JObject>>()!;
            teams.Should().HaveCountGreaterOrEqualTo(1);
            teams.Should().Contain(t => t["name"]!.Value<string>() == "Engineering");
            teams.Should().Contain(t => t["key"]!.Value<string>() == "ENG");
        }

        [Fact]
        public async Task TeamQuery_ReturnsSingleTeam()
        {
            var query = "query { team(id: \"team_00000001\") { id name key } }";
            var json = await ExecuteGraphQL(query);

            json["data"]!["team"]!["id"]!.Value<string>().Should().Be("team_00000001");
            json["data"]!["team"]!["name"]!.Value<string>().Should().Be("Engineering");
        }

        [Fact]
        public async Task TeamQuery_ReturnsWorkflowStates()
        {
            var query = "query { team(id: \"team_00000001\") { id states { nodes { id name type } } } }";
            var json = await ExecuteGraphQL(query);

            var states = json["data"]!["team"]!["states"]!["nodes"]!.ToObject<List<JObject>>()!;
            states.Should().HaveCountGreaterOrEqualTo(5);
            states.Should().Contain(s => s["name"]!.Value<string>() == "Todo");
            states.Should().Contain(s => s["name"]!.Value<string>() == "In Progress");
            states.Should().Contain(s => s["name"]!.Value<string>() == "Done");
        }

        #endregion

        #region Issue CRUD Tests

        [Fact]
        public async Task IssueCreate_CreatesNewIssue()
        {
            var mutation = @"
                mutation {
                    issueCreate(input: {
                        teamId: ""team_00000001""
                        title: ""Test Issue""
                        description: ""Test description""
                    }) {
                        success
                        issue {
                            id
                            identifier
                            title
                            description
                        }
                    }
                }";

            var json = await ExecuteGraphQL(mutation);

            json["data"]!["issueCreate"]!["success"]!.Value<bool>().Should().BeTrue();
            json["data"]!["issueCreate"]!["issue"]!["title"]!.Value<string>().Should().Be("Test Issue");
            json["data"]!["issueCreate"]!["issue"]!["identifier"]!.Value<string>().Should().StartWith("ENG-");
        }

        [Fact]
        public async Task IssueCreate_ThenIssuesList_ShowsNewIssue()
        {
            // Create an issue
            var createMutation = @"
                mutation {
                    issueCreate(input: {
                        teamId: ""team_00000001""
                        title: ""Unique Issue Title""
                    }) {
                        success
                        issue { id }
                    }
                }";

            await ExecuteGraphQL(createMutation);

            // List issues
            var query = "query { issues { nodes { id title } } }";
            var json = await ExecuteGraphQL(query);

            var issues = json["data"]!["issues"]!["nodes"]!.ToObject<List<JObject>>()!;
            issues.Should().Contain(i => i["title"]!.Value<string>() == "Unique Issue Title");
        }

        [Fact]
        public async Task IssueUpdate_UpdatesExistingIssue()
        {
            // Create an issue first
            var createMutation = @"
                mutation {
                    issueCreate(input: {
                        teamId: ""team_00000001""
                        title: ""Original Title""
                    }) {
                        success
                        issue { id identifier }
                    }
                }";

            var createJson = await ExecuteGraphQL(createMutation);
            var issueId = createJson["data"]!["issueCreate"]!["issue"]!["id"]!.Value<string>();

            // Update the issue
            var updateMutation = $@"
                mutation {{
                    issueUpdate(id: ""{issueId}"", input: {{
                        title: ""Updated Title""
                    }}) {{
                        success
                        issue {{
                            id
                            title
                        }}
                    }}
                }}";

            var updateJson = await ExecuteGraphQL(updateMutation);

            updateJson["data"]!["issueUpdate"]!["success"]!.Value<bool>().Should().BeTrue();
            updateJson["data"]!["issueUpdate"]!["issue"]!["title"]!.Value<string>().Should().Be("Updated Title");
        }

        [Fact]
        public async Task IssueArchive_ArchivesIssue()
        {
            // Create an issue first
            var createMutation = @"
                mutation {
                    issueCreate(input: {
                        teamId: ""team_00000001""
                        title: ""Issue To Archive""
                    }) {
                        success
                        issue { id }
                    }
                }";

            var createJson = await ExecuteGraphQL(createMutation);
            var issueId = createJson["data"]!["issueCreate"]!["issue"]!["id"]!.Value<string>();

            // Archive the issue
            var archiveMutation = $@"
                mutation {{
                    issueArchive(id: ""{issueId}"") {{
                        success
                        entity {{
                            id
                            archivedAt
                        }}
                    }}
                }}";

            var archiveJson = await ExecuteGraphQL(archiveMutation);

            archiveJson["data"]!["issueArchive"]!["success"]!.Value<bool>().Should().BeTrue();
            archiveJson["data"]!["issueArchive"]!["entity"]!["archivedAt"]!.Should().NotBeNull();
        }

        [Fact]
        public async Task IssueQuery_ReturnsIssueByIdentifier()
        {
            // Create an issue first
            var createMutation = @"
                mutation {
                    issueCreate(input: {
                        teamId: ""team_00000001""
                        title: ""Issue By Identifier""
                    }) {
                        success
                        issue { id identifier }
                    }
                }";

            var createJson = await ExecuteGraphQL(createMutation);
            var identifier = createJson["data"]!["issueCreate"]!["issue"]!["identifier"]!.Value<string>();

            // Query by identifier
            var query = $"query {{ issue(id: \"{identifier}\") {{ id title }} }}";
            var json = await ExecuteGraphQL(query);

            json["data"]!["issue"]!["title"]!.Value<string>().Should().Be("Issue By Identifier");
        }

        #endregion

        #region Comment Tests

        [Fact]
        public async Task CommentCreate_CreatesCommentOnIssue()
        {
            // Create an issue first
            var createMutation = @"
                mutation {
                    issueCreate(input: {
                        teamId: ""team_00000001""
                        title: ""Issue For Comment""
                    }) {
                        success
                        issue { id }
                    }
                }";

            var createJson = await ExecuteGraphQL(createMutation);
            var issueId = createJson["data"]!["issueCreate"]!["issue"]!["id"]!.Value<string>();

            // Create a comment
            var commentMutation = $@"
                mutation {{
                    commentCreate(input: {{
                        issueId: ""{issueId}""
                        body: ""This is a test comment""
                    }}) {{
                        success
                        comment {{
                            id
                            body
                            user {{ id name }}
                        }}
                    }}
                }}";

            var commentJson = await ExecuteGraphQL(commentMutation);

            commentJson["data"]!["commentCreate"]!["success"]!.Value<bool>().Should().BeTrue();
            commentJson["data"]!["commentCreate"]!["comment"]!["body"]!.Value<string>().Should().Be("This is a test comment");
            commentJson["data"]!["commentCreate"]!["comment"]!["user"]!["id"]!.Value<string>().Should().Be("user_00000001");
        }

        [Fact]
        public async Task IssueQuery_IncludesComments()
        {
            // Create an issue with a comment
            var createMutation = @"
                mutation {
                    issueCreate(input: {
                        teamId: ""team_00000001""
                        title: ""Issue With Comments""
                    }) {
                        success
                        issue { id }
                    }
                }";

            var createJson = await ExecuteGraphQL(createMutation);
            var issueId = createJson["data"]!["issueCreate"]!["issue"]!["id"]!.Value<string>();

            // Add a comment
            var commentMutation = $@"
                mutation {{
                    commentCreate(input: {{
                        issueId: ""{issueId}""
                        body: ""Comment on issue""
                    }}) {{
                        success
                    }}
                }}";

            await ExecuteGraphQL(commentMutation);

            // Query issue with comments
            var query = $"query {{ issue(id: \"{issueId}\") {{ id comments {{ nodes {{ id body }} }} }} }}";
            var json = await ExecuteGraphQL(query);

            var comments = json["data"]!["issue"]!["comments"]!["nodes"]!.ToObject<List<JObject>>()!;
            comments.Should().HaveCount(1);
            comments[0]["body"]!.Value<string>().Should().Be("Comment on issue");
        }

        #endregion

        #region Project Tests

        [Fact]
        public async Task ProjectsQuery_ReturnsAllProjects()
        {
            var query = "query { projects { nodes { id name state } } }";
            var json = await ExecuteGraphQL(query);

            var projects = json["data"]!["projects"]!["nodes"]!.ToObject<List<JObject>>()!;
            projects.Should().HaveCountGreaterOrEqualTo(1);
            projects.Should().Contain(p => p["name"]!.Value<string>() == "Q1 Release");
        }

        [Fact]
        public async Task ProjectCreate_CreatesNewProject()
        {
            var mutation = @"
                mutation {
                    projectCreate(input: {
                        name: ""New Project""
                        description: ""A new project""
                        state: ""planned""
                    }) {
                        success
                        project {
                            id
                            name
                            state
                        }
                    }
                }";

            var json = await ExecuteGraphQL(mutation);

            json["data"]!["projectCreate"]!["success"]!.Value<bool>().Should().BeTrue();
            json["data"]!["projectCreate"]!["project"]!["name"]!.Value<string>().Should().Be("New Project");
            json["data"]!["projectCreate"]!["project"]!["state"]!.Value<string>().Should().Be("planned");
        }

        [Fact]
        public async Task ProjectUpdate_UpdatesExistingProject()
        {
            // Create a project first
            var createMutation = @"
                mutation {
                    projectCreate(input: {
                        name: ""Project To Update""
                    }) {
                        success
                        project { id }
                    }
                }";

            var createJson = await ExecuteGraphQL(createMutation);
            var projectId = createJson["data"]!["projectCreate"]!["project"]!["id"]!.Value<string>();

            // Update the project
            var updateMutation = $@"
                mutation {{
                    projectUpdate(id: ""{projectId}"", input: {{
                        name: ""Updated Project Name""
                        state: ""started""
                    }}) {{
                        success
                        project {{
                            id
                            name
                            state
                        }}
                    }}
                }}";

            var updateJson = await ExecuteGraphQL(updateMutation);

            updateJson["data"]!["projectUpdate"]!["success"]!.Value<bool>().Should().BeTrue();
            updateJson["data"]!["projectUpdate"]!["project"]!["name"]!.Value<string>().Should().Be("Updated Project Name");
            updateJson["data"]!["projectUpdate"]!["project"]!["state"]!.Value<string>().Should().Be("started");
        }

        #endregion

        #region Webhook Tests

        [Fact]
        public async Task WebhookCreate_CreatesNewWebhook()
        {
            var mutation = @"
                mutation {
                    webhookCreate(input: {
                        url: ""https://example.com/webhook""
                        teamId: ""team_00000001""
                        resourceTypes: [""Issue"", ""Comment""]
                    }) {
                        success
                        webhook {
                            id
                            url
                            enabled
                            resourceTypes
                        }
                    }
                }";

            var json = await ExecuteGraphQL(mutation);

            json["data"]!["webhookCreate"]!["success"]!.Value<bool>().Should().BeTrue();
            json["data"]!["webhookCreate"]!["webhook"]!["url"]!.Value<string>().Should().Be("https://example.com/webhook");
            json["data"]!["webhookCreate"]!["webhook"]!["enabled"]!.Value<bool>().Should().BeTrue();
        }

        [Fact]
        public async Task WebhookDelete_DeletesExistingWebhook()
        {
            // Create a webhook first
            var createMutation = @"
                mutation {
                    webhookCreate(input: {
                        url: ""https://example.com/temporary""
                    }) {
                        success
                        webhook { id }
                    }
                }";

            var createJson = await ExecuteGraphQL(createMutation);
            var webhookId = createJson["data"]!["webhookCreate"]!["webhook"]!["id"]!.Value<string>();

            // Delete the webhook
            var deleteMutation = $@"
                mutation {{
                    webhookDelete(id: ""{webhookId}"") {{
                        success
                    }}
                }}";

            var deleteJson = await ExecuteGraphQL(deleteMutation);

            deleteJson["data"]!["webhookDelete"]!["success"]!.Value<bool>().Should().BeTrue();
            _server.State.Webhooks.Should().NotContainKey(webhookId);
        }

        [Fact]
        public async Task WebhooksQuery_ReturnsAllWebhooks()
        {
            // Create a webhook
            var createMutation = @"
                mutation {
                    webhookCreate(input: {
                        url: ""https://example.com/listable""
                    }) {
                        success
                    }
                }";

            await ExecuteGraphQL(createMutation);

            // List webhooks
            var query = "query { webhooks { nodes { id url enabled } } }";
            var json = await ExecuteGraphQL(query);

            var webhooks = json["data"]!["webhooks"]!["nodes"]!.ToObject<List<JObject>>()!;
            webhooks.Should().HaveCountGreaterOrEqualTo(1);
            webhooks.Should().Contain(w => w["url"]!.Value<string>() == "https://example.com/listable");
        }

        #endregion

        #region Workflow State Tests

        [Fact]
        public async Task WorkflowStatesQuery_ReturnsAllStates()
        {
            var query = "query { workflowStates { nodes { id name type color } } }";
            var json = await ExecuteGraphQL(query);

            var states = json["data"]!["workflowStates"]!["nodes"]!.ToObject<List<JObject>>()!;
            states.Should().HaveCountGreaterOrEqualTo(5);
            states.Should().Contain(s => s["type"]!.Value<string>() == "started");
            states.Should().Contain(s => s["type"]!.Value<string>() == "completed");
        }

        #endregion

        #region Users Query Tests

        [Fact]
        public async Task UsersQuery_ReturnsAllUsers()
        {
            var query = "query { users { nodes { id name email } } }";
            var json = await ExecuteGraphQL(query);

            var users = json["data"]!["users"]!["nodes"]!.ToObject<List<JObject>>()!;
            users.Should().HaveCountGreaterOrEqualTo(3);
            users.Should().Contain(u => u["email"]!.Value<string>() == "testuser@example.com");
        }

        [Fact]
        public async Task UserQuery_ReturnsSingleUser()
        {
            var query = "query { user(id: \"user_00000002\") { id name email } }";
            var json = await ExecuteGraphQL(query);

            json["data"]!["user"]!["id"]!.Value<string>().Should().Be("user_00000002");
            json["data"]!["user"]!["name"]!.Value<string>().Should().Be("Other User");
        }

        #endregion

        #region OAuth Tests

        [Fact]
        public async Task OAuthTokenExchange_ReturnsAccessToken()
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", "test_authorization_code"),
                new KeyValuePair<string, string>("client_id", "test_client_id"),
                new KeyValuePair<string, string>("client_secret", "test_client_secret"),
                new KeyValuePair<string, string>("redirect_uri", "https://example.com/callback")
            });

            var response = await _client.PostAsync("https://api.linear.app/oauth/token", content);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            json["access_token"]!.Value<string>().Should().StartWith("lin_oauth_");
            json["token_type"]!.Value<string>().Should().Be("Bearer");
            json["refresh_token"]!.Value<string>().Should().StartWith("lin_ref_");
        }

        [Fact]
        public async Task OAuthTokenRefresh_ReturnsNewAccessToken()
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", "lin_ref_test_token"),
                new KeyValuePair<string, string>("client_id", "test_client_id"),
                new KeyValuePair<string, string>("client_secret", "test_client_secret")
            });

            var response = await _client.PostAsync("https://api.linear.app/oauth/token", content);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            json["access_token"]!.Value<string>().Should().StartWith("lin_oauth_");
        }

        [Fact]
        public async Task OAuthRevoke_RevokesToken()
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("access_token", "lin_oauth_test_token")
            });

            var response = await _client.PostAsync("https://api.linear.app/oauth/revoke", content);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            json["success"]!.Value<bool>().Should().BeTrue();
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task Authentication_WhenRequired_RejectsUnauthorizedRequest()
        {
            _server.EnforceAuthentication = true;

            var requestBody = new { query = "query { viewer { id } }" };
            var content = new StringContent(
                JsonConvert.SerializeObject(requestBody),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await _client.PostAsync("https://api.linear.app/graphql", content);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Authentication_WhenRequired_AcceptsValidToken()
        {
            _server.EnforceAuthentication = true;

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.linear.app/graphql");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "lin_api_test_token");
            request.Content = new StringContent(
                JsonConvert.SerializeObject(new { query = "query { viewer { id } }" }),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await _client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            json["data"]!["viewer"]!["id"]!.Should().NotBeNull();
        }

        [Fact]
        public async Task Authentication_WhenSpecificTokenRequired_RejectsWrongToken()
        {
            _server.RequiredToken = "correct_token";

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.linear.app/graphql");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "wrong_token");
            request.Content = new StringContent(
                JsonConvert.SerializeObject(new { query = "query { viewer { id } }" }),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await _client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Label Tests

        [Fact]
        public async Task IssueLabelsQuery_ReturnsAllLabels()
        {
            var query = "query { issueLabels { nodes { id name color } } }";
            var json = await ExecuteGraphQL(query);

            var labels = json["data"]!["issueLabels"]!["nodes"]!.ToObject<List<JObject>>()!;
            labels.Should().HaveCountGreaterOrEqualTo(3);
            labels.Should().Contain(l => l["name"]!.Value<string>() == "Bug");
            labels.Should().Contain(l => l["name"]!.Value<string>() == "Feature");
        }

        #endregion

        #region Cycle Tests

        [Fact]
        public async Task CyclesQuery_ReturnsAllCycles()
        {
            var query = "query { cycles { nodes { id name number startsAt endsAt } } }";
            var json = await ExecuteGraphQL(query);

            var cycles = json["data"]!["cycles"]!["nodes"]!.ToObject<List<JObject>>()!;
            cycles.Should().HaveCountGreaterOrEqualTo(1);
            cycles.Should().Contain(c => c["name"]!.Value<string>() == "Sprint 1");
        }

        #endregion

        #region State Access Tests

        [Fact]
        public void State_ExposesInternalData()
        {
            // Can access state directly
            _server.State.Users.Should().HaveCountGreaterOrEqualTo(3);
            _server.State.Teams.Should().HaveCountGreaterOrEqualTo(1);
            _server.State.WorkflowStates.Should().HaveCountGreaterOrEqualTo(5);
            _server.State.Labels.Should().HaveCountGreaterOrEqualTo(3);
        }

        [Fact]
        public void State_CanBePrePopulated()
        {
            var customState = new LinearState();
            customState.AddUser(new Models.LinearUser
            {
                Id = "custom_user",
                Name = "Custom User",
                Email = "custom@example.com"
            });

            var customServer = new LinearSimulatedServer(customState);
            
            customServer.State.Users.Should().ContainKey("custom_user");
        }

        #endregion

        #region Additional Issue Operations Tests

        [Fact]
        public async Task IssueDelete_DeletesIssue()
        {
            // Create an issue first
            var createMutation = @"
                mutation {
                    issueCreate(input: {
                        teamId: ""team_00000001""
                        title: ""Issue To Delete""
                    }) {
                        success
                        issue { id }
                    }
                }";

            var createJson = await ExecuteGraphQL(createMutation);
            var issueId = createJson["data"]!["issueCreate"]!["issue"]!["id"]!.Value<string>();

            // Delete the issue
            var deleteMutation = $@"
                mutation {{
                    issueDelete(id: ""{issueId}"") {{
                        success
                    }}
                }}";

            var deleteJson = await ExecuteGraphQL(deleteMutation);

            deleteJson["data"]!["issueDelete"]!["success"]!.Value<bool>().Should().BeTrue();
            _server.State.Issues.Should().NotContainKey(issueId);
        }

        [Fact]
        public async Task IssueUnarchive_UnarchivesIssue()
        {
            // Create and archive an issue
            var createMutation = @"
                mutation {
                    issueCreate(input: {
                        teamId: ""team_00000001""
                        title: ""Issue To Unarchive""
                    }) {
                        success
                        issue { id }
                    }
                }";

            var createJson = await ExecuteGraphQL(createMutation);
            var issueId = createJson["data"]!["issueCreate"]!["issue"]!["id"]!.Value<string>();

            // Archive the issue
            await ExecuteGraphQL($"mutation {{ issueArchive(id: \"{issueId}\") {{ success }} }}");

            // Unarchive the issue
            var unarchiveMutation = $@"
                mutation {{
                    issueUnarchive(id: ""{issueId}"") {{
                        success
                        issue {{ id archivedAt }}
                    }}
                }}";

            var unarchiveJson = await ExecuteGraphQL(unarchiveMutation);

            unarchiveJson["data"]!["issueUnarchive"]!["success"]!.Value<bool>().Should().BeTrue();
            var archivedAt = unarchiveJson["data"]!["issueUnarchive"]!["issue"]!["archivedAt"];
            (archivedAt == null || archivedAt.Type == JTokenType.Null).Should().BeTrue();
        }

        [Fact]
        public async Task IssueAddLabel_AddsLabelToIssue()
        {
            // Create an issue
            var createMutation = @"
                mutation {
                    issueCreate(input: {
                        teamId: ""team_00000001""
                        title: ""Issue For Labels""
                    }) {
                        success
                        issue { id }
                    }
                }";

            var createJson = await ExecuteGraphQL(createMutation);
            var issueId = createJson["data"]!["issueCreate"]!["issue"]!["id"]!.Value<string>();

            // Add label to issue
            var addLabelMutation = $@"
                mutation {{
                    issueAddLabel(input: {{
                        id: ""{issueId}""
                        labelId: ""label_00000001""
                    }}) {{
                        success
                        issue {{
                            id
                            labels {{ nodes {{ id name }} }}
                        }}
                    }}
                }}";

            var addLabelJson = await ExecuteGraphQL(addLabelMutation);

            addLabelJson["data"]!["issueAddLabel"]!["success"]!.Value<bool>().Should().BeTrue();
            var labels = addLabelJson["data"]!["issueAddLabel"]!["issue"]!["labels"]!["nodes"]!.ToObject<List<JObject>>()!;
            labels.Should().Contain(l => l["name"]!.Value<string>() == "Bug");
        }

        [Fact]
        public async Task IssueRemoveLabel_RemovesLabelFromIssue()
        {
            // Create an issue with a label
            var createMutation = @"
                mutation {
                    issueCreate(input: {
                        teamId: ""team_00000001""
                        title: ""Issue With Label""
                    }) {
                        success
                        issue { id }
                    }
                }";

            var createJson = await ExecuteGraphQL(createMutation);
            var issueId = createJson["data"]!["issueCreate"]!["issue"]!["id"]!.Value<string>();

            // Add then remove label
            await ExecuteGraphQL($@"mutation {{ issueAddLabel(input: {{ id: ""{issueId}"", labelId: ""label_00000001"" }}) {{ success }} }}");

            var removeLabelMutation = $@"
                mutation {{
                    issueRemoveLabel(input: {{
                        id: ""{issueId}""
                        labelId: ""label_00000001""
                    }}) {{
                        success
                    }}
                }}";

            var removeJson = await ExecuteGraphQL(removeLabelMutation);

            removeJson["data"]!["issueRemoveLabel"]!["success"]!.Value<bool>().Should().BeTrue();
            _server.State.Issues[issueId!].LabelIds.Should().NotContain("label_00000001");
        }

        #endregion

        #region Comment Update/Delete Tests

        [Fact]
        public async Task CommentUpdate_UpdatesComment()
        {
            // Create issue and comment
            var createIssue = @"
                mutation {
                    issueCreate(input: {
                        teamId: ""team_00000001""
                        title: ""Issue For Comment Update""
                    }) {
                        success
                        issue { id }
                    }
                }";

            var issueJson = await ExecuteGraphQL(createIssue);
            var issueId = issueJson["data"]!["issueCreate"]!["issue"]!["id"]!.Value<string>();

            var createComment = $@"
                mutation {{
                    commentCreate(input: {{
                        issueId: ""{issueId}""
                        body: ""Original comment""
                    }}) {{
                        success
                        comment {{ id }}
                    }}
                }}";

            var commentJson = await ExecuteGraphQL(createComment);
            var commentId = commentJson["data"]!["commentCreate"]!["comment"]!["id"]!.Value<string>();

            // Update the comment
            var updateMutation = $@"
                mutation {{
                    commentUpdate(id: ""{commentId}"", input: {{
                        body: ""Updated comment""
                    }}) {{
                        success
                        comment {{ id body edited }}
                    }}
                }}";

            var updateJson = await ExecuteGraphQL(updateMutation);

            updateJson["data"]!["commentUpdate"]!["success"]!.Value<bool>().Should().BeTrue();
            updateJson["data"]!["commentUpdate"]!["comment"]!["body"]!.Value<string>().Should().Be("Updated comment");
            updateJson["data"]!["commentUpdate"]!["comment"]!["edited"]!.Value<bool>().Should().BeTrue();
        }

        [Fact]
        public async Task CommentDelete_DeletesComment()
        {
            // Create issue and comment
            var createIssue = @"
                mutation {
                    issueCreate(input: {
                        teamId: ""team_00000001""
                        title: ""Issue For Comment Delete""
                    }) {
                        success
                        issue { id }
                    }
                }";

            var issueJson = await ExecuteGraphQL(createIssue);
            var issueId = issueJson["data"]!["issueCreate"]!["issue"]!["id"]!.Value<string>();

            var createComment = $@"
                mutation {{
                    commentCreate(input: {{
                        issueId: ""{issueId}""
                        body: ""Comment to delete""
                    }}) {{
                        success
                        comment {{ id }}
                    }}
                }}";

            var commentJson = await ExecuteGraphQL(createComment);
            var commentId = commentJson["data"]!["commentCreate"]!["comment"]!["id"]!.Value<string>();

            // Delete the comment
            var deleteMutation = $@"
                mutation {{
                    commentDelete(id: ""{commentId}"") {{
                        success
                    }}
                }}";

            var deleteJson = await ExecuteGraphQL(deleteMutation);

            deleteJson["data"]!["commentDelete"]!["success"]!.Value<bool>().Should().BeTrue();
            _server.State.Comments.Should().NotContainKey(commentId);
        }

        #endregion

        #region Label CRUD Tests

        [Fact]
        public async Task LabelCreate_CreatesNewLabel()
        {
            var mutation = @"
                mutation {
                    issueLabelCreate(input: {
                        name: ""New Label""
                        color: ""#FF5733""
                        teamId: ""team_00000001""
                    }) {
                        success
                        issueLabel {
                            id
                            name
                            color
                        }
                    }
                }";

            var json = await ExecuteGraphQL(mutation);

            json["data"]!["issueLabelCreate"]!["success"]!.Value<bool>().Should().BeTrue();
            json["data"]!["issueLabelCreate"]!["issueLabel"]!["name"]!.Value<string>().Should().Be("New Label");
            json["data"]!["issueLabelCreate"]!["issueLabel"]!["color"]!.Value<string>().Should().Be("#FF5733");
        }

        [Fact]
        public async Task LabelUpdate_UpdatesLabel()
        {
            // Create a label first
            var createMutation = @"
                mutation {
                    issueLabelCreate(input: {
                        name: ""Label To Update""
                        teamId: ""team_00000001""
                    }) {
                        success
                        issueLabel { id }
                    }
                }";

            var createJson = await ExecuteGraphQL(createMutation);
            var labelId = createJson["data"]!["issueLabelCreate"]!["issueLabel"]!["id"]!.Value<string>();

            // Update the label
            var updateMutation = $@"
                mutation {{
                    issueLabelUpdate(id: ""{labelId}"", input: {{
                        name: ""Updated Label Name""
                        color: ""#00FF00""
                    }}) {{
                        success
                        issueLabel {{ id name color }}
                    }}
                }}";

            var updateJson = await ExecuteGraphQL(updateMutation);

            updateJson["data"]!["issueLabelUpdate"]!["success"]!.Value<bool>().Should().BeTrue();
            updateJson["data"]!["issueLabelUpdate"]!["issueLabel"]!["name"]!.Value<string>().Should().Be("Updated Label Name");
            updateJson["data"]!["issueLabelUpdate"]!["issueLabel"]!["color"]!.Value<string>().Should().Be("#00FF00");
        }

        [Fact]
        public async Task LabelDelete_DeletesLabel()
        {
            // Create a label first
            var createMutation = @"
                mutation {
                    issueLabelCreate(input: {
                        name: ""Label To Delete""
                        teamId: ""team_00000001""
                    }) {
                        success
                        issueLabel { id }
                    }
                }";

            var createJson = await ExecuteGraphQL(createMutation);
            var labelId = createJson["data"]!["issueLabelCreate"]!["issueLabel"]!["id"]!.Value<string>();

            // Delete the label
            var deleteMutation = $@"
                mutation {{
                    issueLabelDelete(id: ""{labelId}"") {{
                        success
                    }}
                }}";

            var deleteJson = await ExecuteGraphQL(deleteMutation);

            deleteJson["data"]!["issueLabelDelete"]!["success"]!.Value<bool>().Should().BeTrue();
            _server.State.Labels.Should().NotContainKey(labelId);
        }

        #endregion

        #region Cycle CRUD Tests

        [Fact]
        public async Task CycleCreate_CreatesNewCycle()
        {
            var mutation = @"
                mutation {
                    cycleCreate(input: {
                        teamId: ""team_00000001""
                        name: ""Sprint 42""
                    }) {
                        success
                        cycle {
                            id
                            name
                            number
                        }
                    }
                }";

            var json = await ExecuteGraphQL(mutation);

            json["data"]!["cycleCreate"]!["success"]!.Value<bool>().Should().BeTrue();
            json["data"]!["cycleCreate"]!["cycle"]!["name"]!.Value<string>().Should().Be("Sprint 42");
        }

        [Fact]
        public async Task CycleUpdate_UpdatesCycle()
        {
            // Create a cycle first
            var createMutation = @"
                mutation {
                    cycleCreate(input: {
                        teamId: ""team_00000001""
                        name: ""Cycle To Update""
                    }) {
                        success
                        cycle { id }
                    }
                }";

            var createJson = await ExecuteGraphQL(createMutation);
            var cycleId = createJson["data"]!["cycleCreate"]!["cycle"]!["id"]!.Value<string>();

            // Update the cycle
            var updateMutation = $@"
                mutation {{
                    cycleUpdate(id: ""{cycleId}"", input: {{
                        name: ""Updated Cycle Name""
                    }}) {{
                        success
                        cycle {{ id name }}
                    }}
                }}";

            var updateJson = await ExecuteGraphQL(updateMutation);

            updateJson["data"]!["cycleUpdate"]!["success"]!.Value<bool>().Should().BeTrue();
            updateJson["data"]!["cycleUpdate"]!["cycle"]!["name"]!.Value<string>().Should().Be("Updated Cycle Name");
        }

        [Fact]
        public async Task CycleArchive_ArchivesCycle()
        {
            // Create a cycle first
            var createMutation = @"
                mutation {
                    cycleCreate(input: {
                        teamId: ""team_00000001""
                        name: ""Cycle To Archive""
                    }) {
                        success
                        cycle { id }
                    }
                }";

            var createJson = await ExecuteGraphQL(createMutation);
            var cycleId = createJson["data"]!["cycleCreate"]!["cycle"]!["id"]!.Value<string>();

            // Archive the cycle
            var archiveMutation = $@"
                mutation {{
                    cycleArchive(id: ""{cycleId}"") {{
                        success
                    }}
                }}";

            var archiveJson = await ExecuteGraphQL(archiveMutation);

            archiveJson["data"]!["cycleArchive"]!["success"]!.Value<bool>().Should().BeTrue();
            _server.State.Cycles[cycleId!].CompletedAt.Should().NotBeNull();
        }

        #endregion

        #region Team CRUD Tests

        [Fact]
        public async Task TeamCreate_CreatesNewTeam()
        {
            var mutation = @"
                mutation {
                    teamCreate(input: {
                        name: ""New Team""
                        key: ""NEW""
                        description: ""A new team""
                    }) {
                        success
                        team {
                            id
                            name
                            key
                            states { nodes { id } }
                        }
                    }
                }";

            var json = await ExecuteGraphQL(mutation);

            json["data"]!["teamCreate"]!["success"]!.Value<bool>().Should().BeTrue();
            json["data"]!["teamCreate"]!["team"]!["name"]!.Value<string>().Should().Be("New Team");
            json["data"]!["teamCreate"]!["team"]!["key"]!.Value<string>().Should().Be("NEW");
            // New team should have default workflow states
            var states = json["data"]!["teamCreate"]!["team"]!["states"]!["nodes"]!.ToObject<List<JObject>>()!;
            states.Should().HaveCountGreaterOrEqualTo(4);
        }

        [Fact]
        public async Task TeamUpdate_UpdatesTeam()
        {
            // Update existing team
            var updateMutation = @"
                mutation {
                    teamUpdate(id: ""team_00000001"", input: {
                        description: ""Updated team description""
                    }) {
                        success
                        team { id description }
                    }
                }";

            var updateJson = await ExecuteGraphQL(updateMutation);

            updateJson["data"]!["teamUpdate"]!["success"]!.Value<bool>().Should().BeTrue();
            updateJson["data"]!["teamUpdate"]!["team"]!["description"]!.Value<string>().Should().Be("Updated team description");
        }

        #endregion

        #region Project Archive Tests

        [Fact]
        public async Task ProjectArchive_ArchivesProject()
        {
            // Create a project first
            var createMutation = @"
                mutation {
                    projectCreate(input: {
                        name: ""Project To Archive""
                    }) {
                        success
                        project { id }
                    }
                }";

            var createJson = await ExecuteGraphQL(createMutation);
            var projectId = createJson["data"]!["projectCreate"]!["project"]!["id"]!.Value<string>();

            // Archive the project
            var archiveMutation = $@"
                mutation {{
                    projectArchive(id: ""{projectId}"") {{
                        success
                        entity {{ id archivedAt }}
                    }}
                }}";

            var archiveJson = await ExecuteGraphQL(archiveMutation);

            archiveJson["data"]!["projectArchive"]!["success"]!.Value<bool>().Should().BeTrue();
            archiveJson["data"]!["projectArchive"]!["entity"]!["archivedAt"]!.Should().NotBeNull();
        }

        #endregion

        #region Attachment CRUD Tests

        [Fact]
        public async Task AttachmentUpdate_UpdatesAttachment()
        {
            // Create issue and attachment
            var createIssue = @"
                mutation {
                    issueCreate(input: {
                        teamId: ""team_00000001""
                        title: ""Issue For Attachment Update""
                    }) {
                        success
                        issue { id }
                    }
                }";

            var issueJson = await ExecuteGraphQL(createIssue);
            var issueId = issueJson["data"]!["issueCreate"]!["issue"]!["id"]!.Value<string>();

            var createAttachment = $@"
                mutation {{
                    attachmentCreate(input: {{
                        issueId: ""{issueId}""
                        title: ""Original Attachment""
                        url: ""https://example.com/file1""
                    }}) {{
                        success
                        attachment {{ id }}
                    }}
                }}";

            var attachmentJson = await ExecuteGraphQL(createAttachment);
            var attachmentId = attachmentJson["data"]!["attachmentCreate"]!["attachment"]!["id"]!.Value<string>();

            // Update the attachment
            var updateMutation = $@"
                mutation {{
                    attachmentUpdate(id: ""{attachmentId}"", input: {{
                        title: ""Updated Attachment""
                        url: ""https://example.com/file2""
                    }}) {{
                        success
                        attachment {{ id title url }}
                    }}
                }}";

            var updateJson = await ExecuteGraphQL(updateMutation);

            updateJson["data"]!["attachmentUpdate"]!["success"]!.Value<bool>().Should().BeTrue();
            updateJson["data"]!["attachmentUpdate"]!["attachment"]!["title"]!.Value<string>().Should().Be("Updated Attachment");
            updateJson["data"]!["attachmentUpdate"]!["attachment"]!["url"]!.Value<string>().Should().Be("https://example.com/file2");
        }

        [Fact]
        public async Task AttachmentDelete_DeletesAttachment()
        {
            // Create issue and attachment
            var createIssue = @"
                mutation {
                    issueCreate(input: {
                        teamId: ""team_00000001""
                        title: ""Issue For Attachment Delete""
                    }) {
                        success
                        issue { id }
                    }
                }";

            var issueJson = await ExecuteGraphQL(createIssue);
            var issueId = issueJson["data"]!["issueCreate"]!["issue"]!["id"]!.Value<string>();

            var createAttachment = $@"
                mutation {{
                    attachmentCreate(input: {{
                        issueId: ""{issueId}""
                        title: ""Attachment To Delete""
                        url: ""https://example.com/delete""
                    }}) {{
                        success
                        attachment {{ id }}
                    }}
                }}";

            var attachmentJson = await ExecuteGraphQL(createAttachment);
            var attachmentId = attachmentJson["data"]!["attachmentCreate"]!["attachment"]!["id"]!.Value<string>();

            // Delete the attachment
            var deleteMutation = $@"
                mutation {{
                    attachmentDelete(id: ""{attachmentId}"") {{
                        success
                    }}
                }}";

            var deleteJson = await ExecuteGraphQL(deleteMutation);

            deleteJson["data"]!["attachmentDelete"]!["success"]!.Value<bool>().Should().BeTrue();
            _server.State.Attachments.Should().NotContainKey(attachmentId);
        }

        #endregion

        #region Additional Query Tests

        [Fact]
        public async Task CycleQuery_ReturnsSingleCycle()
        {
            var query = "query { cycle(id: \"cycle_00000001\") { id name number } }";
            var json = await ExecuteGraphQL(query);

            json["data"]!["cycle"]!["id"]!.Value<string>().Should().Be("cycle_00000001");
            json["data"]!["cycle"]!["name"]!.Value<string>().Should().Be("Sprint 1");
        }

        [Fact]
        public async Task CommentsQuery_ReturnsAllComments()
        {
            // Create an issue and comment first
            var issueJson = await ExecuteGraphQL(@"
                mutation {
                    issueCreate(input: {
                        teamId: ""team_00000001""
                        title: ""Issue For Comments Query""
                    }) {
                        success
                        issue { id }
                    }
                }");

            var issueId = issueJson["data"]!["issueCreate"]!["issue"]!["id"]!.Value<string>();

            await ExecuteGraphQL($@"
                mutation {{
                    commentCreate(input: {{
                        issueId: ""{issueId}""
                        body: ""Test comment for query""
                    }}) {{
                        success
                    }}
                }}");

            var query = "query { comments { nodes { id body } } }";
            var json = await ExecuteGraphQL(query);

            var comments = json["data"]!["comments"]!["nodes"]!.ToObject<List<JObject>>()!;
            comments.Should().Contain(c => c["body"]!.Value<string>() == "Test comment for query");
        }

        [Fact]
        public async Task AttachmentsQuery_ReturnsAllAttachments()
        {
            // Create an issue and attachment first
            var issueJson = await ExecuteGraphQL(@"
                mutation {
                    issueCreate(input: {
                        teamId: ""team_00000001""
                        title: ""Issue For Attachments Query""
                    }) {
                        success
                        issue { id }
                    }
                }");

            var issueId = issueJson["data"]!["issueCreate"]!["issue"]!["id"]!.Value<string>();

            await ExecuteGraphQL($@"
                mutation {{
                    attachmentCreate(input: {{
                        issueId: ""{issueId}""
                        title: ""Test Attachment""
                        url: ""https://example.com/attach""
                    }}) {{
                        success
                    }}
                }}");

            var query = "query { attachments { nodes { id title url } } }";
            var json = await ExecuteGraphQL(query);

            var attachments = json["data"]!["attachments"]!["nodes"]!.ToObject<List<JObject>>()!;
            attachments.Should().Contain(a => a["title"]!.Value<string>() == "Test Attachment");
        }

        [Fact]
        public async Task IssueLabelQuery_ReturnsSingleLabel()
        {
            var query = "query { issueLabel(id: \"label_00000001\") { id name color } }";
            var json = await ExecuteGraphQL(query);

            json["data"]!["issueLabel"]!["id"]!.Value<string>().Should().Be("label_00000001");
            json["data"]!["issueLabel"]!["name"]!.Value<string>().Should().Be("Bug");
        }

        #endregion

        #region Roadmap Tests

        [Fact]
        public async Task RoadmapCreate_CreatesNewRoadmap()
        {
            var mutation = @"
                mutation {
                    roadmapCreate(input: {
                        name: ""Q1 2024 Roadmap""
                        description: ""Features for Q1""
                    }) {
                        success
                        roadmap { id name description slug }
                    }
                }";

            var json = await ExecuteGraphQL(mutation);

            json["data"]!["roadmapCreate"]!["success"]!.Value<bool>().Should().BeTrue();
            json["data"]!["roadmapCreate"]!["roadmap"]!["name"]!.Value<string>().Should().Be("Q1 2024 Roadmap");
            json["data"]!["roadmapCreate"]!["roadmap"]!["slug"]!.Value<string>().Should().Be("q1-2024-roadmap");
        }

        [Fact]
        public async Task RoadmapsQuery_ReturnsAllRoadmaps()
        {
            // Create a roadmap first
            await ExecuteGraphQL(@"
                mutation {
                    roadmapCreate(input: { name: ""Test Roadmap"" }) {
                        success
                    }
                }");

            var query = "query { roadmaps { nodes { id name } } }";
            var json = await ExecuteGraphQL(query);

            var roadmaps = json["data"]!["roadmaps"]!["nodes"]!.ToObject<List<JObject>>()!;
            roadmaps.Should().Contain(r => r["name"]!.Value<string>() == "Test Roadmap");
        }

        #endregion

        #region Document Tests

        [Fact]
        public async Task DocumentCreate_CreatesNewDocument()
        {
            var mutation = @"
                mutation {
                    documentCreate(input: {
                        title: ""Project Specs""
                        content: ""# Specification Document""
                    }) {
                        success
                        document { id title content }
                    }
                }";

            var json = await ExecuteGraphQL(mutation);

            json["data"]!["documentCreate"]!["success"]!.Value<bool>().Should().BeTrue();
            json["data"]!["documentCreate"]!["document"]!["title"]!.Value<string>().Should().Be("Project Specs");
            json["data"]!["documentCreate"]!["document"]!["content"]!.Value<string>().Should().Be("# Specification Document");
        }

        [Fact]
        public async Task DocumentUpdate_UpdatesDocument()
        {
            // Create a document first
            var createJson = await ExecuteGraphQL(@"
                mutation {
                    documentCreate(input: { title: ""Original Title"" }) {
                        success
                        document { id }
                    }
                }");

            var documentId = createJson["data"]!["documentCreate"]!["document"]!["id"]!.Value<string>();

            // Update it
            var updateMutation = $@"
                mutation {{
                    documentUpdate(id: ""{documentId}"", input: {{
                        title: ""Updated Title""
                        content: ""New content""
                    }}) {{
                        success
                        document {{ id title content }}
                    }}
                }}";

            var updateJson = await ExecuteGraphQL(updateMutation);

            updateJson["data"]!["documentUpdate"]!["success"]!.Value<bool>().Should().BeTrue();
            updateJson["data"]!["documentUpdate"]!["document"]!["title"]!.Value<string>().Should().Be("Updated Title");
        }

        #endregion

        #region Custom View Tests

        [Fact]
        public async Task CustomViewCreate_CreatesNewView()
        {
            var mutation = @"
                mutation {
                    customViewCreate(input: {
                        name: ""My Bugs View""
                        teamId: ""team_00000001""
                        shared: true
                    }) {
                        success
                        customView { id name shared }
                    }
                }";

            var json = await ExecuteGraphQL(mutation);

            json["data"]!["customViewCreate"]!["success"]!.Value<bool>().Should().BeTrue();
            json["data"]!["customViewCreate"]!["customView"]!["name"]!.Value<string>().Should().Be("My Bugs View");
            json["data"]!["customViewCreate"]!["customView"]!["shared"]!.Value<bool>().Should().BeTrue();
        }

        [Fact]
        public async Task CustomViewsQuery_ReturnsAllViews()
        {
            // Create a view first
            await ExecuteGraphQL(@"
                mutation {
                    customViewCreate(input: { name: ""Test View"" }) {
                        success
                    }
                }");

            var query = "query { customViews { nodes { id name } } }";
            var json = await ExecuteGraphQL(query);

            var views = json["data"]!["customViews"]!["nodes"]!.ToObject<List<JObject>>()!;
            views.Should().Contain(v => v["name"]!.Value<string>() == "Test View");
        }

        #endregion

        #region Favorite Tests

        [Fact]
        public async Task FavoriteCreate_CreatesNewFavorite()
        {
            // Create an issue to favorite
            var issueJson = await ExecuteGraphQL(@"
                mutation {
                    issueCreate(input: {
                        teamId: ""team_00000001""
                        title: ""Issue To Favorite""
                    }) {
                        success
                        issue { id }
                    }
                }");

            var issueId = issueJson["data"]!["issueCreate"]!["issue"]!["id"]!.Value<string>();

            var mutation = $@"
                mutation {{
                    favoriteCreate(input: {{
                        type: ""issue""
                        issueId: ""{issueId}""
                    }}) {{
                        success
                        favorite {{ id type }}
                    }}
                }}";

            var json = await ExecuteGraphQL(mutation);

            json["data"]!["favoriteCreate"]!["success"]!.Value<bool>().Should().BeTrue();
            json["data"]!["favoriteCreate"]!["favorite"]!["type"]!.Value<string>().Should().Be("issue");
        }

        [Fact]
        public async Task FavoritesQuery_ReturnsFavorites()
        {
            // Create a favorite
            await ExecuteGraphQL(@"
                mutation {
                    favoriteCreate(input: { type: ""project"", projectId: ""proj_00000001"" }) {
                        success
                    }
                }");

            var query = "query { favorites { nodes { id type } } }";
            var json = await ExecuteGraphQL(query);

            var favorites = json["data"]!["favorites"]!["nodes"]!.ToObject<List<JObject>>()!;
            favorites.Should().HaveCountGreaterOrEqualTo(1);
        }

        #endregion

        #region Reaction Tests

        [Fact]
        public async Task ReactionCreate_AddsReactionToComment()
        {
            // Create issue and comment
            var issueJson = await ExecuteGraphQL(@"
                mutation {
                    issueCreate(input: { teamId: ""team_00000001"", title: ""Issue For Reactions"" }) {
                        success
                        issue { id }
                    }
                }");
            var issueId = issueJson["data"]!["issueCreate"]!["issue"]!["id"]!.Value<string>();

            var commentJson = await ExecuteGraphQL($@"
                mutation {{
                    commentCreate(input: {{ issueId: ""{issueId}"", body: ""Comment for reaction"" }}) {{
                        success
                        comment {{ id }}
                    }}
                }}");
            var commentId = commentJson["data"]!["commentCreate"]!["comment"]!["id"]!.Value<string>();

            // Add reaction
            var mutation = $@"
                mutation {{
                    reactionCreate(input: {{
                        commentId: ""{commentId}""
                        emoji: ""👍""
                    }}) {{
                        success
                        reaction {{ id emoji }}
                    }}
                }}";

            var json = await ExecuteGraphQL(mutation);

            json["data"]!["reactionCreate"]!["success"]!.Value<bool>().Should().BeTrue();
            json["data"]!["reactionCreate"]!["reaction"]!["emoji"]!.Value<string>().Should().Be("👍");
        }

        #endregion

        #region Issue Relation Tests

        [Fact]
        public async Task IssueRelationCreate_CreatesRelation()
        {
            // Create two issues
            var issue1Json = await ExecuteGraphQL(@"
                mutation {
                    issueCreate(input: { teamId: ""team_00000001"", title: ""Issue 1"" }) {
                        success
                        issue { id }
                    }
                }");
            var issue1Id = issue1Json["data"]!["issueCreate"]!["issue"]!["id"]!.Value<string>();

            var issue2Json = await ExecuteGraphQL(@"
                mutation {
                    issueCreate(input: { teamId: ""team_00000001"", title: ""Issue 2"" }) {
                        success
                        issue { id }
                    }
                }");
            var issue2Id = issue2Json["data"]!["issueCreate"]!["issue"]!["id"]!.Value<string>();

            // Create relation
            var mutation = $@"
                mutation {{
                    issueRelationCreate(input: {{
                        issueId: ""{issue1Id}""
                        relatedIssueId: ""{issue2Id}""
                        type: ""blocks""
                    }}) {{
                        success
                        issueRelation {{ id type }}
                    }}
                }}";

            var json = await ExecuteGraphQL(mutation);

            json["data"]!["issueRelationCreate"]!["success"]!.Value<bool>().Should().BeTrue();
            json["data"]!["issueRelationCreate"]!["issueRelation"]!["type"]!.Value<string>().Should().Be("blocks");
        }

        #endregion

        #region Issue Subscribe Tests

        [Fact]
        public async Task IssueSubscribe_SubscribesToIssue()
        {
            // Create an issue
            var issueJson = await ExecuteGraphQL(@"
                mutation {
                    issueCreate(input: { teamId: ""team_00000001"", title: ""Issue To Subscribe"" }) {
                        success
                        issue { id }
                    }
                }");
            var issueId = issueJson["data"]!["issueCreate"]!["issue"]!["id"]!.Value<string>();

            // Subscribe
            var mutation = $@"
                mutation {{
                    issueSubscribe(id: ""{issueId}"") {{
                        success
                        issue {{ id }}
                    }}
                }}";

            var json = await ExecuteGraphQL(mutation);

            json["data"]!["issueSubscribe"]!["success"]!.Value<bool>().Should().BeTrue();
            _server.State.Issues[issueId!].SubscriberIds.Should().Contain("user_00000001");
        }

        [Fact]
        public async Task IssueUnsubscribe_UnsubscribesFromIssue()
        {
            // Create an issue and subscribe
            var issueJson = await ExecuteGraphQL(@"
                mutation {
                    issueCreate(input: { teamId: ""team_00000001"", title: ""Issue To Unsubscribe"" }) {
                        success
                        issue { id }
                    }
                }");
            var issueId = issueJson["data"]!["issueCreate"]!["issue"]!["id"]!.Value<string>();

            await ExecuteGraphQL($"mutation {{ issueSubscribe(id: \"{issueId}\") {{ success }} }}");

            // Unsubscribe
            var mutation = $@"
                mutation {{
                    issueUnsubscribe(id: ""{issueId}"") {{
                        success
                    }}
                }}";

            var json = await ExecuteGraphQL(mutation);

            json["data"]!["issueUnsubscribe"]!["success"]!.Value<bool>().Should().BeTrue();
            _server.State.Issues[issueId!].SubscriberIds.Should().NotContain("user_00000001");
        }

        #endregion

        #region Issue Search Tests

        [Fact]
        public async Task IssueSearch_SearchesByTitle()
        {
            // Create issues with specific titles
            await ExecuteGraphQL(@"
                mutation {
                    issueCreate(input: { teamId: ""team_00000001"", title: ""Searchable Bug Report"" }) {
                        success
                    }
                }");

            await ExecuteGraphQL(@"
                mutation {
                    issueCreate(input: { teamId: ""team_00000001"", title: ""Another Task"" }) {
                        success
                    }
                }");

            // Search for "Searchable"
            var query = "query { issueSearch(query: \"Searchable\") { nodes { id title } } }";
            var json = await ExecuteGraphQL(query);

            var results = json["data"]!["issueSearch"]!["nodes"]!.ToObject<List<JObject>>()!;
            results.Should().Contain(i => i["title"]!.Value<string>()!.Contains("Searchable"));
            results.Should().NotContain(i => i["title"]!.Value<string>() == "Another Task");
        }

        #endregion

        #region State Collections Tests

        [Fact]
        public void State_ExposesNewCollections()
        {
            // Verify new collections are accessible
            _server.State.IssueRelations.Should().NotBeNull();
            _server.State.Reactions.Should().NotBeNull();
            _server.State.Notifications.Should().NotBeNull();
            _server.State.Favorites.Should().NotBeNull();
            _server.State.Documents.Should().NotBeNull();
            _server.State.Roadmaps.Should().NotBeNull();
            _server.State.CustomViews.Should().NotBeNull();
        }

        #endregion
    }
}
