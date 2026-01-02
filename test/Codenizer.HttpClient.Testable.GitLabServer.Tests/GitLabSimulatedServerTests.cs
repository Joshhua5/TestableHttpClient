using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Codenizer.HttpClient.Testable;
using Codenizer.HttpClient.Testable.GitLabServer;
using Codenizer.HttpClient.Testable.GitLabServer.Models;
using Newtonsoft.Json;
using Xunit;

namespace Codenizer.HttpClient.Testable.GitLabServer.Tests
{
    public class GitLabSimulatedServerTests
    {
        [Fact]
        public async Task CanCreateAndGetProject()
        {
            var handler = new TestableMessageHandler();
            var server = new GitLabSimulatedServer();
            
            // Map routes
            handler.RespondTo().Post().ForUrl("https://gitlab.com/api/v4/projects").HandledBy(server);
            handler.RespondTo().Get().ForUrl("https://gitlab.com/api/v4/projects/1").HandledBy(server); 

            var client = new System.Net.Http.HttpClient(handler);
            client.BaseAddress = new Uri("https://gitlab.com/api/v4/");

            var projectData = new
            {
                name = "My Test Project",
                path = "my-test-project"
            };

            var createResponse = await client.PostAsync("projects", new StringContent(JsonConvert.SerializeObject(projectData)));
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            
            var createdProject = JsonConvert.DeserializeObject<GitLabProject>(await createResponse.Content.ReadAsStringAsync());
            Assert.NotNull(createdProject);
            Assert.Equal("My Test Project", createdProject.Name);

            var getResponse = await client.GetAsync($"projects/{createdProject.Id}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            
            var fetchedProject = JsonConvert.DeserializeObject<GitLabProject>(await getResponse.Content.ReadAsStringAsync());
            Assert.Equal(createdProject.Id, fetchedProject.Id);
        }

        [Fact]
        public async Task CanCreateAndGetIssue()
        {
            var handler = new TestableMessageHandler();
            var server = new GitLabSimulatedServer();
            
            // Map routes
            handler.RespondTo().Post().ForUrl("https://gitlab.com/api/v4/projects").HandledBy(server);
            handler.RespondTo().Post().ForUrl("https://gitlab.com/api/v4/projects/1/issues").HandledBy(server);
            handler.RespondTo().Get().ForUrl("https://gitlab.com/api/v4/projects/1/issues").HandledBy(server);

            var client = new System.Net.Http.HttpClient(handler);
            client.BaseAddress = new Uri("https://gitlab.com/api/v4/");
            
            // Create Project
             var projectData = new
            {
                name = "Issue Project",
                path = "issue-project"
            };
            var createProjResponse = await client.PostAsync("projects", new StringContent(JsonConvert.SerializeObject(projectData)));
            var project = JsonConvert.DeserializeObject<GitLabProject>(await createProjResponse.Content.ReadAsStringAsync());
            
            // Create Issue
            var issueData = new
            {
                title = "My First Issue",
                description = "This is a test issue"
            };
            var createIssueResponse = await client.PostAsync($"projects/{project.Id}/issues", new StringContent(JsonConvert.SerializeObject(issueData)));
            Assert.Equal(HttpStatusCode.Created, createIssueResponse.StatusCode);
            
            var issue = JsonConvert.DeserializeObject<GitLabIssue>(await createIssueResponse.Content.ReadAsStringAsync());
            Assert.Equal("My First Issue", issue.Title);
            
            // List Issues for project
            var listResponse = await client.GetAsync($"projects/{project.Id}/issues");
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            var issues = JsonConvert.DeserializeObject<GitLabIssue[]>(await listResponse.Content.ReadAsStringAsync());
            Assert.Single(issues);
            Assert.Equal(issue.Id, issues[0].Id);
        }
    }
}
