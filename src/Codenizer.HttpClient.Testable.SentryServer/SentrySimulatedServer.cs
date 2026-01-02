using System.Net;
using System.Text.RegularExpressions;
using Codenizer.HttpClient.Testable.SentryServer.Handlers;
using Codenizer.HttpClient.Testable.SentryServer.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace Codenizer.HttpClient.Testable.SentryServer
{
    public class SentrySimulatedServer : ISimulatedServer
    {
        private readonly SentryState _state;
        private readonly JsonSerializerSettings _jsonSettings;
        private readonly OrganizationsHandler _orgsHandler;
        private readonly ProjectsHandler _projectsHandler;
        private readonly IssuesHandler _issuesHandler;
        private readonly EventsHandler _eventsHandler;
        private readonly TeamsHandler _teamsHandler;
        private readonly ReleasesHandler _releasesHandler;
        private readonly ProjectKeysHandler _projectKeysHandler;
        private readonly CommentsHandler _commentsHandler;
        private readonly UserReportsHandler _userReportsHandler;

        public SentrySimulatedServer()
        {
            _state = new SentryState();
            _jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                },
                NullValueHandling = NullValueHandling.Ignore
            };
            
            _orgsHandler = new OrganizationsHandler(_state);
            _projectsHandler = new ProjectsHandler(_state);
            _issuesHandler = new IssuesHandler(_state);
            _eventsHandler = new EventsHandler(_state);
            _teamsHandler = new TeamsHandler(_state);
            _releasesHandler = new ReleasesHandler(_state);
            _projectKeysHandler = new ProjectKeysHandler(_state);
            _commentsHandler = new CommentsHandler(_state);
            _userReportsHandler = new UserReportsHandler(_state);
        }

        public string? RequiredToken { get; set; }

        public SentryState State => _state;

        public async Task<HttpResponseMessage> HandleRequestAsync(HttpRequestMessage request)
        {
            if (RequiredToken != null)
            {
                var authHeader = request.Headers.Authorization;
                if (authHeader == null || authHeader.Scheme != "Bearer" || authHeader.Parameter != RequiredToken)
                {
                    return CreateErrorResponse(HttpStatusCode.Unauthorized, "Invalid authentication token");
                }
            }

            var path = request.RequestUri?.AbsolutePath ?? "/";
            var method = request.Method;

            // Normalize path
            if (path.EndsWith("/")) path = path.TrimEnd('/');

            try
            {
                // Events Store
                var storeMatch = Regex.Match(path, @"^/api/(\d+)/store$");
                if (storeMatch.Success && method == HttpMethod.Post)
                {
                    var projectId = storeMatch.Groups[1].Value;
                    var content = await request.Content!.ReadAsStringAsync();
                    var evt = JsonConvert.DeserializeObject<SentryEvent>(content);
                    if (evt == null) return CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid event data");
                    
                    var result = _eventsHandler.Store(projectId, evt);
                    return CreateResponse(HttpStatusCode.OK, result);
                }

                if (path.StartsWith("/api/0/"))
                {
                    var apiPath = path.Substring(7); // Remove /api/0/

                    // Organizations
                    if (apiPath == "organizations")
                    {
                        if (method == HttpMethod.Get)
                            return CreateResponse(HttpStatusCode.OK, _orgsHandler.List());
                        if (method == HttpMethod.Post)
                        {
                            var content = await request.Content!.ReadAsStringAsync();
                            dynamic? data = JsonConvert.DeserializeObject(content);
                            return CreateResponse(HttpStatusCode.Created, _orgsHandler.Create((string)data?.name, (string?)data?.slug));
                        }
                    }
                    else if (apiPath.StartsWith("organizations/"))
                    {
                        var remaining = apiPath.Substring(14); // Remove organizations/
                        // Check for nested resources under organization
                        
                        // /organizations/{orgSlug}/teams/
                        if (remaining.Contains("/teams")) 
                        {
                            var parts = remaining.Split('/');
                            if(parts.Length == 2 && parts[1] == "teams")
                            {
                                if (method == HttpMethod.Get)
                                    return CreateResponse(HttpStatusCode.OK, _teamsHandler.List(parts[0]));
                                if (method == HttpMethod.Post)
                                {
                                    var content = await request.Content!.ReadAsStringAsync();
                                    dynamic? data = JsonConvert.DeserializeObject(content);
                                    var team = _teamsHandler.Create(parts[0], (string)data?.name, (string?)data?.slug);
                                    return team != null ? CreateResponse(HttpStatusCode.Created, team) : CreateErrorResponse(HttpStatusCode.NotFound, "Organization not found");
                                }
                            }
                        }
                        
                        // /organizations/{orgSlug}/releases/
                         if (remaining.Contains("/releases")) 
                        {
                            var parts = remaining.Split('/');
                            if(parts.Length == 2 && parts[1] == "releases")
                            {
                                if (method == HttpMethod.Get)
                                    return CreateResponse(HttpStatusCode.OK, _releasesHandler.List(parts[0]));
                                if (method == HttpMethod.Post)
                                {
                                    var content = await request.Content!.ReadAsStringAsync();
                                    dynamic? data = JsonConvert.DeserializeObject(content);
                                    var projectsObj = data?.projects;
                                    List<string>? projects = null;
                                    if(projectsObj != null) projects = ((Newtonsoft.Json.Linq.JArray)projectsObj).ToObject<List<string>>();

                                    var release = _releasesHandler.Create(parts[0], (string)data?.version, projects);
                                    return release != null ? CreateResponse(HttpStatusCode.Created, release) : CreateErrorResponse(HttpStatusCode.NotFound, "Organization not found");
                                }
                            }
                        }

                        // Basic org get
                        if (!remaining.Contains("/"))
                        {
                            if (method == HttpMethod.Get)
                            {
                                var org = _orgsHandler.Get(remaining);
                                return org != null ? CreateResponse(HttpStatusCode.OK, org) : CreateErrorResponse(HttpStatusCode.NotFound, "Organization not found");
                            }
                        }
                    }

                    // Teams (Direct)
                    if (apiPath.StartsWith("teams/"))
                    {
                        var remaining = apiPath.Substring(6); // Remove teams/
                         // teams/{orgSlug}/{teamSlug}/projects/
                        if (remaining.Contains("/projects"))
                        {
                             var parts = remaining.Split('/');
                             if(parts.Length == 3 && parts[2] == "projects" && method == HttpMethod.Post)
                             {
                                var content = await request.Content!.ReadAsStringAsync();
                                dynamic? data = JsonConvert.DeserializeObject(content);
                                var proj = _projectsHandler.Create(parts[0], parts[1], (string)data?.name, (string?)data?.slug);
                                return proj != null ? CreateResponse(HttpStatusCode.Created, proj) : CreateErrorResponse(HttpStatusCode.NotFound, "Organization or Team not found");
                             }
                        }
                        // teams/{orgSlug}/{teamSlug}/
                        else 
                        {
                            var parts = remaining.Split('/');
                            if(parts.Length == 2)
                            {
                                if(method == HttpMethod.Put)
                                {
                                     var content = await request.Content!.ReadAsStringAsync();
                                     dynamic? data = JsonConvert.DeserializeObject(content);
                                     var team = _teamsHandler.Update(parts[0], parts[1], (string?)data?.name, (string?)data?.slug);
                                     return team != null ? CreateResponse(HttpStatusCode.OK, team) : CreateErrorResponse(HttpStatusCode.NotFound, "Team not found");
                                }
                                if(method == HttpMethod.Delete)
                                {
                                    var deleted = _teamsHandler.Delete(parts[0], parts[1]);
                                    return deleted ? new HttpResponseMessage(HttpStatusCode.NoContent) : CreateErrorResponse(HttpStatusCode.NotFound, "Team not found");
                                }
                            }
                        }
                    }

                    // Projects
                    if (apiPath == "projects")
                    {
                        if (method == HttpMethod.Get)
                            return CreateResponse(HttpStatusCode.OK, _projectsHandler.List());
                    }
                    else if (apiPath.StartsWith("projects/"))
                    {
                        var remaining = apiPath.Substring(9); // Remove projects/
                        
                        // projects/{orgSlug}/{projectSlug}/keys/
                        if (remaining.Contains("/keys"))
                        {
                             var parts = remaining.Split('/');
                             if(parts.Length == 3 && parts[2] == "keys")
                             {
                                 if (method == HttpMethod.Get)
                                     return CreateResponse(HttpStatusCode.OK, _projectKeysHandler.List(parts[0], parts[1]));
                                 if (method == HttpMethod.Post)
                                 {
                                     var content = await request.Content!.ReadAsStringAsync();
                                     dynamic? data = JsonConvert.DeserializeObject(content);
                                     var key = _projectKeysHandler.Create(parts[0], parts[1], (string)data?.name);
                                     return key != null ? CreateResponse(HttpStatusCode.Created, key) : CreateErrorResponse(HttpStatusCode.NotFound, "Project not found");
                                 }
                             }
                        }
                        // projects/{orgSlug}/{projectSlug}
                        else 
                        {
                            var parts = remaining.Split('/');
                            if (parts.Length == 2)
                            {
                                if (method == HttpMethod.Get)
                                {
                                    var proj = _projectsHandler.Get(parts[0], parts[1]);
                                    return proj != null ? CreateResponse(HttpStatusCode.OK, proj) : CreateErrorResponse(HttpStatusCode.NotFound, "Project not found");
                                }
                            }
                        }
                    }

                    // Issues
                    if (apiPath.StartsWith("issues/"))
                    {
                         var remaining = apiPath.Substring(7); // Remove issues/
                        
                         // issues/{issueId}/comments/
                         if (remaining.Contains("/comments"))
                         {
                             var parts = remaining.Split('/');
                             if(parts.Length == 2 && parts[1] == "comments")
                             {
                                 if (method == HttpMethod.Get)
                                     return CreateResponse(HttpStatusCode.OK, _commentsHandler.List(parts[0]));
                                 if (method == HttpMethod.Post)
                                 {
                                     var content = await request.Content!.ReadAsStringAsync();
                                     var jObj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(content);
                                     var data = new Dictionary<string, object>();
                                     
                                     if (jObj != null && jObj.TryGetValue("data", out var innerData) && innerData is Newtonsoft.Json.Linq.JObject innerObj)
                                     {
                                         data = innerObj.ToObject<Dictionary<string, object>>();
                                     }
                                     else if (jObj != null)
                                     {
                                         data = jObj.ToObject<Dictionary<string, object>>();
                                     }

                                     var comment = _commentsHandler.Create(parts[0], data ?? new Dictionary<string,object>());
                                     return comment != null ? CreateResponse(HttpStatusCode.Created, comment) : CreateErrorResponse(HttpStatusCode.NotFound, "Issue not found");
                                 }
                             }
                         }
                        
                        var issueId = remaining;
                         if (method == HttpMethod.Get)
                        {
                            var issue = _issuesHandler.Get(issueId);
                            return issue != null ? CreateResponse(HttpStatusCode.OK, issue) : CreateErrorResponse(HttpStatusCode.NotFound, "Issue not found");
                        }
                        if (method == HttpMethod.Put)
                        {
                            var content = await request.Content!.ReadAsStringAsync();
                            var jObj = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(content);
                            
                            string? status = jObj?.Value<string>("status");
                            string? title = jObj?.Value<string>("title");
                            object? assignedTo = jObj?.GetValue("assignedTo"); // This returns JToken?

                            if (assignedTo is Newtonsoft.Json.Linq.JValue jVal && jVal.Type == Newtonsoft.Json.Linq.JTokenType.String)
                            {
                                assignedTo = (string?)jVal;
                            }
                            
                            var issue = _issuesHandler.Update(issueId, status, title, assignedTo);
                             return issue != null ? CreateResponse(HttpStatusCode.OK, issue) : CreateErrorResponse(HttpStatusCode.NotFound, "Issue not found");
                        }
                    }

                    // User Reports
                    // /api/0/projects/{org}/{proj}/user-reports/
                    if (apiPath.Contains("user-reports"))
                    {
                        if (method == HttpMethod.Get)
                            return CreateResponse(HttpStatusCode.OK, _userReportsHandler.List(""));
                        if (method == HttpMethod.Post)
                        {
                            var content = await request.Content!.ReadAsStringAsync();
                            var report = JsonConvert.DeserializeObject<SentryUserReport>(content);
                            // Extract project info/validation if needed
                            return report != null ? CreateResponse(HttpStatusCode.Created, _userReportsHandler.Create("", report)) : CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid report");
                        }
                    }
                }
                
                // Envelopes
                var envelopeMatch = Regex.Match(path, @"/api/(\d+)/envelope$");
                if (envelopeMatch.Success && method == HttpMethod.Post)
                {
                    var projectId = envelopeMatch.Groups[1].Value;
                    var content = await request.Content!.ReadAsStringAsync();
                    
                    var items = EnvelopeParser.Parse(content);
                    var storedIds = new List<string>();
                    
                    foreach (var item in items)
                    {
                        if (item is SentryEvent evt)
                        {
                            var result = _eventsHandler.Store(projectId, evt);
                            // result has { id }
                             // Reflection to get ID because result is anonymous object... 
                              // Actually EventsHandler.Store returns anonymous object.
                              // Let's trust it works side-effect wise.
                            storedIds.Add(evt.EventId);
                        }
                        else if (item is SentryUserReport report)
                        {
                            _userReportsHandler.Create(projectId, report);
                            storedIds.Add(report.Id);
                        }
                    }
                    
                    return CreateResponse(HttpStatusCode.OK, new { id = storedIds.FirstOrDefault() });
                }


            }
            catch (Exception ex)
            {
                 return CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
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

        private HttpResponseMessage CreateErrorResponse(HttpStatusCode statusCode, string detail)
        {
            var result = new { detail };
            var json = JsonConvert.SerializeObject(result, _jsonSettings);
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        }
    }
}
