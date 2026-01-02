using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Codenizer.HttpClient.Testable.GitLabServer.Handlers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Codenizer.HttpClient.Testable.GitLabServer
{
    public class GitLabSimulatedServer : ISimulatedServer
    {
        private readonly GitLabState _state;
        private readonly JsonSerializerSettings _jsonSettings;
        private readonly ProjectsHandler _projectsHandler;
        private readonly IssuesHandler _issuesHandler;
        private readonly UsersHandler _usersHandler;
        private readonly GroupsHandler _groupsHandler;

        public GitLabSimulatedServer()
        {
            _state = new GitLabState();
            _jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                },
                NullValueHandling = NullValueHandling.Ignore
            };

            _projectsHandler = new ProjectsHandler(_state);
            _issuesHandler = new IssuesHandler(_state);
            _usersHandler = new UsersHandler(_state);
            _groupsHandler = new GroupsHandler(_state);
        }

        public string? RequiredToken { get; set; }

        public GitLabState State => _state;

        public async Task<HttpResponseMessage> HandleRequestAsync(HttpRequestMessage request)
        {
            if (RequiredToken != null)
            {
                var authHeader = request.Headers.Authorization;
                if (authHeader == null || authHeader.Scheme != "Bearer" || authHeader.Parameter != RequiredToken)
                {
                    // GitLab also supports PRIVATE-TOKEN header
                    if(!request.Headers.Contains("PRIVATE-TOKEN") || request.Headers.GetValues("PRIVATE-TOKEN").FirstOrDefault() != RequiredToken)
                    {
                        return CreateErrorResponse(HttpStatusCode.Unauthorized, "Unauthorized");
                    }
                }
            }

            var path = request.RequestUri?.AbsolutePath ?? "/";
            var method = request.Method;

            // Normalize path
            if (path.EndsWith("/")) path = path.TrimEnd('/');

            try
            {
                if (path.StartsWith("/api/v4/"))
                {
                    var apiPath = path.Substring(8); // Remove /api/v4/

                    // Projects
                    if (apiPath == "projects")
                    {
                        if (method == HttpMethod.Get)
                            return CreateResponse(HttpStatusCode.OK, _projectsHandler.List());
                        if (method == HttpMethod.Post)
                        {
                            var content = await request.Content!.ReadAsStringAsync();
                            dynamic? data = JsonConvert.DeserializeObject(content);
                            return CreateResponse(HttpStatusCode.Created, _projectsHandler.Create((string)data?.name, (string?)data?.path, (string?)data?.namespace_path, (string?)data?.visibility ?? "private"));
                        }
                    }
                    else if (apiPath.StartsWith("projects/"))
                    {
                        var remaining = apiPath.Substring(9);
                        
                        // projects/:id/issues
                        if (remaining.Contains("/issues"))
                        {
                            var parts = remaining.Split('/');
                            // parts[0] is project ID, parts[1] is issues
                            if (parts.Length == 2 && parts[1] == "issues" && method == HttpMethod.Post)
                            {
                                if(int.TryParse(parts[0], out var projId))
                                {
                                    var content = await request.Content!.ReadAsStringAsync();
                                    dynamic? data = JsonConvert.DeserializeObject(content);
                                    var issue = _issuesHandler.Create(projId, (string)data?.title, (string?)data?.description ?? "");
                                    return issue != null ? CreateResponse(HttpStatusCode.Created, issue) : CreateErrorResponse(HttpStatusCode.NotFound, "Project not found");
                                }
                            }
                            if (parts.Length == 2 && parts[1] == "issues" && method == HttpMethod.Get)
                            {
                                 if(int.TryParse(parts[0], out var projId))
                                {
                                    return CreateResponse(HttpStatusCode.OK, _issuesHandler.List(projId));
                                }
                            }
                        }
                        
                         // projects/:id
                        if (!remaining.Contains("/"))
                        {
                            if (method == HttpMethod.Get)
                            {
                                var proj = _projectsHandler.Get(remaining);
                                return proj != null ? CreateResponse(HttpStatusCode.OK, proj) : CreateErrorResponse(HttpStatusCode.NotFound, "Project not found");
                            }
                        }
                    }

                    // Issues (Global)
                    if (apiPath == "issues")
                    {
                        if (method == HttpMethod.Get)
                            return CreateResponse(HttpStatusCode.OK, _issuesHandler.List());
                    }

                    // Users
                    if (apiPath == "users")
                    {
                         if(method == HttpMethod.Post)
                         {
                             var content = await request.Content!.ReadAsStringAsync();
                             dynamic? data = JsonConvert.DeserializeObject(content);
                             return CreateResponse(HttpStatusCode.Created, _usersHandler.Create((string)data?.username, (string)data?.email, (string)data?.name));

                         }
                         if(method == HttpMethod.Get)
                            return CreateResponse(HttpStatusCode.OK, _usersHandler.List());
                    }
                    
                    // Groups
                    if (apiPath == "groups")
                    {
                        if (method == HttpMethod.Get)
                           return CreateResponse(HttpStatusCode.OK, _groupsHandler.List());
                        
                        if (method == HttpMethod.Post)
                        {
                            var content = await request.Content!.ReadAsStringAsync();
                            dynamic? data = JsonConvert.DeserializeObject(content);
                            return CreateResponse(HttpStatusCode.Created, _groupsHandler.Create((string)data?.name, (string)data?.path));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                 return CreateErrorResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private HttpResponseMessage CreateResponse(HttpStatusCode statusCode, object? value)
        {
            var json = JsonConvert.SerializeObject(value, _jsonSettings);
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        }

        private HttpResponseMessage CreateErrorResponse(HttpStatusCode statusCode, string message)
        {
            var result = new { message };
            var json = JsonConvert.SerializeObject(result, _jsonSettings);
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        }
    }
}
