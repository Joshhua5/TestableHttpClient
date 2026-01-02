using System.Text.RegularExpressions;
using Codenizer.HttpClient.Testable.LinearServer.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Codenizer.HttpClient.Testable.LinearServer.Handlers
{
    /// <summary>
    /// Handles GraphQL queries and mutations for the Linear API.
    /// </summary>
    public class GraphQLHandler
    {
        private readonly LinearState _state;
        private readonly JsonSerializerSettings _jsonSettings;

        public GraphQLHandler(LinearState state)
        {
            _state = state;
            _jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ"
            };
        }

        public object HandleRequest(string query, JObject? variables)
        {
            // Determine if it's a query or mutation
            var isMutation = query.TrimStart().StartsWith("mutation", StringComparison.OrdinalIgnoreCase);

            // Extract the operation name and content
            var operationMatch = Regex.Match(query, @"(query|mutation)\s*(\w+)?\s*(?:\([^)]*\))?\s*\{([^}]+(?:\{[^}]*\}[^}]*)*)\}", 
                RegexOptions.Singleline);

            if (!operationMatch.Success)
            {
                // Try simpler pattern for anonymous operations
                operationMatch = Regex.Match(query, @"\{([^}]+(?:\{[^}]*\}[^}]*)*)\}", RegexOptions.Singleline);
            }

            var operationBody = operationMatch.Success ? operationMatch.Groups[^1].Value : query;

            try
            {
                if (isMutation)
                {
                    return HandleMutation(operationBody, variables);
                }
                else
                {
                    return HandleQuery(operationBody, variables);
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    errors = new[]
                    {
                        new { message = ex.Message, extensions = new { code = "INTERNAL_SERVER_ERROR" } }
                    }
                };
            }
        }

        private object HandleQuery(string body, JObject? variables)
        {
            var data = new Dictionary<string, object?>();

            // Check for viewer query
            if (ContainsField(body, "viewer"))
            {
                data["viewer"] = MapUser(_state.GetCurrentUser());
            }

            // Check for organization query
            if (ContainsField(body, "organization"))
            {
                data["organization"] = MapOrganization(_state.Organization);
            }

            // Check for teams query
            if (ContainsField(body, "teams"))
            {
                data["teams"] = new
                {
                    nodes = _state.Teams.Values.Select(MapTeam).ToList(),
                    pageInfo = new { hasNextPage = false, hasPreviousPage = false }
                };
            }

            // Check for single team query
            var teamMatch = Regex.Match(body, @"team\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (teamMatch.Success)
            {
                var teamId = ResolveVariable(teamMatch.Groups[1].Value, variables);
                var team = _state.Teams.GetValueOrDefault(teamId);
                data["team"] = team != null ? MapTeam(team) : null;
            }

            // Check for issues query
            if (ContainsField(body, "issues"))
            {
                var includeArchived = false;
                var archivedMatch = Regex.Match(body, @"issues\s*\([^)]*includeArchived\s*:\s*(true|false)");
                if (archivedMatch.Success)
                {
                    includeArchived = archivedMatch.Groups[1].Value == "true";
                }

                data["issues"] = new
                {
                    nodes = _state.Issues.Values
                        .Where(i => includeArchived || i.ArchivedAt == null)
                        .Select(MapIssue).ToList(),
                    pageInfo = new { hasNextPage = false, hasPreviousPage = false }
                };
            }

            // Check for single issue query
            var issueMatch = Regex.Match(body, @"issue\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (issueMatch.Success)
            {
                var issueId = ResolveVariable(issueMatch.Groups[1].Value, variables);
                var issue = _state.Issues.GetValueOrDefault(issueId) 
                    ?? _state.Issues.Values.FirstOrDefault(i => i.Identifier == issueId);
                data["issue"] = issue != null ? MapIssue(issue) : null;
            }

            // Check for projects query
            if (ContainsField(body, "projects"))
            {
                data["projects"] = new
                {
                    nodes = _state.Projects.Values
                        .Where(p => p.ArchivedAt == null)
                        .Select(MapProject).ToList(),
                    pageInfo = new { hasNextPage = false, hasPreviousPage = false }
                };
            }

            // Check for single project query
            var projectMatch = Regex.Match(body, @"project\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (projectMatch.Success)
            {
                var projectId = ResolveVariable(projectMatch.Groups[1].Value, variables);
                var project = _state.Projects.GetValueOrDefault(projectId);
                data["project"] = project != null ? MapProject(project) : null;
            }

            // Check for users query
            if (ContainsField(body, "users"))
            {
                data["users"] = new
                {
                    nodes = _state.Users.Values.Select(MapUser).ToList(),
                    pageInfo = new { hasNextPage = false, hasPreviousPage = false }
                };
            }

            // Check for single user query
            var userMatch = Regex.Match(body, @"user\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (userMatch.Success)
            {
                var userId = ResolveVariable(userMatch.Groups[1].Value, variables);
                var user = _state.Users.GetValueOrDefault(userId);
                data["user"] = user != null ? MapUser(user) : null;
            }

            // Check for workflowStates query
            if (ContainsField(body, "workflowStates"))
            {
                data["workflowStates"] = new
                {
                    nodes = _state.WorkflowStates.Values
                        .Where(s => s.ArchivedAt == null)
                        .OrderBy(s => s.TeamId)
                        .ThenBy(s => s.Position)
                        .Select(MapWorkflowState).ToList(),
                    pageInfo = new { hasNextPage = false, hasPreviousPage = false }
                };
            }

            // Check for cycles query
            if (ContainsField(body, "cycles"))
            {
                data["cycles"] = new
                {
                    nodes = _state.Cycles.Values.Select(MapCycle).ToList(),
                    pageInfo = new { hasNextPage = false, hasPreviousPage = false }
                };
            }

            // Check for labels query
            if (ContainsField(body, "issueLabels"))
            {
                data["issueLabels"] = new
                {
                    nodes = _state.Labels.Values
                        .Where(l => l.ArchivedAt == null)
                        .Select(MapLabel).ToList(),
                    pageInfo = new { hasNextPage = false, hasPreviousPage = false }
                };
            }

            // Check for webhooks query
            if (ContainsField(body, "webhooks"))
            {
                data["webhooks"] = new
                {
                    nodes = _state.Webhooks.Values.Select(MapWebhook).ToList(),
                    pageInfo = new { hasNextPage = false, hasPreviousPage = false }
                };
            }

            // Check for single cycle query
            var cycleMatch = Regex.Match(body, @"cycle\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (cycleMatch.Success)
            {
                var cycleId = ResolveVariable(cycleMatch.Groups[1].Value, variables);
                var cycle = _state.Cycles.GetValueOrDefault(cycleId);
                data["cycle"] = cycle != null ? MapCycle(cycle) : null;
            }

            // Check for comments query
            if (ContainsField(body, "comments") && !body.Contains("commentCreate"))
            {
                data["comments"] = new
                {
                    nodes = _state.Comments.Values
                        .Where(c => c.ArchivedAt == null)
                        .Select(MapComment).ToList(),
                    pageInfo = new { hasNextPage = false, hasPreviousPage = false }
                };
            }

            // Check for single comment query
            var commentMatch = Regex.Match(body, @"comment\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (commentMatch.Success)
            {
                var commentId = ResolveVariable(commentMatch.Groups[1].Value, variables);
                var comment = _state.Comments.GetValueOrDefault(commentId);
                data["comment"] = comment != null ? MapComment(comment) : null;
            }

            // Check for attachments query
            if (ContainsField(body, "attachments") && !body.Contains("attachmentCreate"))
            {
                data["attachments"] = new
                {
                    nodes = _state.Attachments.Values
                        .Where(a => a.ArchivedAt == null)
                        .Select(MapAttachment).ToList(),
                    pageInfo = new { hasNextPage = false, hasPreviousPage = false }
                };
            }

            // Check for single attachment query
            var attachmentMatch = Regex.Match(body, @"attachment\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (attachmentMatch.Success)
            {
                var attachmentId = ResolveVariable(attachmentMatch.Groups[1].Value, variables);
                var attachment = _state.Attachments.GetValueOrDefault(attachmentId);
                data["attachment"] = attachment != null ? MapAttachment(attachment) : null;
            }

            // Check for single label query
            var labelMatch = Regex.Match(body, @"issueLabel\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (labelMatch.Success)
            {
                var labelId = ResolveVariable(labelMatch.Groups[1].Value, variables);
                var label = _state.Labels.GetValueOrDefault(labelId);
                data["issueLabel"] = label != null ? MapLabel(label) : null;
            }

            // ===== Additional Queries =====

            // Check for roadmaps query
            if (ContainsField(body, "roadmaps"))
            {
                data["roadmaps"] = new
                {
                    nodes = _state.Roadmaps.Values
                        .Where(r => r.ArchivedAt == null)
                        .Select(MapRoadmap).ToList(),
                    pageInfo = new { hasNextPage = false, hasPreviousPage = false }
                };
            }

            // Check for single roadmap query
            var roadmapMatch = Regex.Match(body, @"roadmap\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (roadmapMatch.Success)
            {
                var roadmapId = ResolveVariable(roadmapMatch.Groups[1].Value, variables);
                var roadmap = _state.Roadmaps.GetValueOrDefault(roadmapId);
                data["roadmap"] = roadmap != null ? MapRoadmap(roadmap) : null;
            }

            // Check for documents query
            if (ContainsField(body, "documents"))
            {
                data["documents"] = new
                {
                    nodes = _state.Documents.Values
                        .Where(d => d.ArchivedAt == null)
                        .Select(MapDocument).ToList(),
                    pageInfo = new { hasNextPage = false, hasPreviousPage = false }
                };
            }

            // Check for single document query
            var documentMatch = Regex.Match(body, @"document\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (documentMatch.Success)
            {
                var documentId = ResolveVariable(documentMatch.Groups[1].Value, variables);
                var document = _state.Documents.GetValueOrDefault(documentId);
                data["document"] = document != null ? MapDocument(document) : null;
            }

            // Check for favorites query
            if (ContainsField(body, "favorites"))
            {
                data["favorites"] = new
                {
                    nodes = _state.GetFavoritesForUser().Select(MapFavorite).ToList(),
                    pageInfo = new { hasNextPage = false, hasPreviousPage = false }
                };
            }

            // Check for notifications query
            if (ContainsField(body, "notifications"))
            {
                data["notifications"] = new
                {
                    nodes = _state.GetNotificationsForUser().Select(MapNotification).ToList(),
                    pageInfo = new { hasNextPage = false, hasPreviousPage = false }
                };
            }

            // Check for customViews query
            if (ContainsField(body, "customViews"))
            {
                data["customViews"] = new
                {
                    nodes = _state.CustomViews.Values.Select(MapCustomView).ToList(),
                    pageInfo = new { hasNextPage = false, hasPreviousPage = false }
                };
            }

            // Check for single customView query
            var customViewMatch = Regex.Match(body, @"customView\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (customViewMatch.Success)
            {
                var viewId = ResolveVariable(customViewMatch.Groups[1].Value, variables);
                var view = _state.CustomViews.GetValueOrDefault(viewId);
                data["customView"] = view != null ? MapCustomView(view) : null;
            }

            // Check for issueSearch query
            var issueSearchMatch = Regex.Match(body, @"issueSearch\s*\(");
            if (issueSearchMatch.Success)
            {
                // Extract query parameter
                var queryMatch = Regex.Match(body, @"issueSearch\s*\([^)]*query\s*:\s*[""']([^""']+)[""']");
                var query = queryMatch.Success ? queryMatch.Groups[1].Value : null;
                
                // Extract filter parameters
                var teamIdMatch = Regex.Match(body, @"issueSearch\s*\([^)]*teamId\s*:\s*[""']([^""']+)[""']");
                var teamId = teamIdMatch.Success ? teamIdMatch.Groups[1].Value : null;
                
                var assigneeIdMatch = Regex.Match(body, @"issueSearch\s*\([^)]*assigneeId\s*:\s*[""']([^""']+)[""']");
                var assigneeId = assigneeIdMatch.Success ? assigneeIdMatch.Groups[1].Value : null;
                
                var stateIdMatch = Regex.Match(body, @"issueSearch\s*\([^)]*stateId\s*:\s*[""']([^""']+)[""']");
                var stateId = stateIdMatch.Success ? stateIdMatch.Groups[1].Value : null;
                
                var results = _state.SearchIssues(query, teamId, stateId, assigneeId);
                
                data["issueSearch"] = new
                {
                    nodes = results.Select(MapIssue).ToList(),
                    pageInfo = new { hasNextPage = false, hasPreviousPage = false }
                };
            }

            // Check for issueRelations on an issue
            if (ContainsField(body, "relations"))
            {
                // This is typically used within an issue query context
                // The relations would be returned as part of the issue query
            }

            return new { data };
        }

        private object HandleMutation(string body, JObject? variables)
        {
            var data = new Dictionary<string, object?>();

            // issueCreate mutation
            var issueCreateMatch = Regex.Match(body, @"issueCreate\s*\(\s*input\s*:");
            if (issueCreateMatch.Success)
            {
                var input = ExtractInputObject(body, "issueCreate", variables);
                var teamId = input.GetValue<string>("teamId") ?? _state.Teams.Keys.First();
                var title = input.GetValue<string>("title") ?? "Untitled";
                var description = input.GetValue<string>("description");
                var assigneeId = input.GetValue<string>("assigneeId");
                var stateId = input.GetValue<string>("stateId");
                var projectId = input.GetValue<string>("projectId");
                var priority = input.GetValue<int?>("priority") ?? 0;
                var labelIds = input.GetValue<List<string>>("labelIds");

                var issue = _state.CreateIssue(teamId, title, description, assigneeId, stateId, projectId, priority, labelIds);
                data["issueCreate"] = new
                {
                    success = true,
                    issue = MapIssue(issue)
                };
            }

            // issueUpdate mutation
            var issueUpdateMatch = Regex.Match(body, @"issueUpdate\s*\(");
            if (issueUpdateMatch.Success)
            {
                var idMatch = Regex.Match(body, @"issueUpdate\s*\(\s*id\s*:\s*[""']?([^""'\s,\)]+)[""']?");
                var issueId = idMatch.Success ? ResolveVariable(idMatch.Groups[1].Value, variables) : "";
                var input = ExtractInputObject(body, "issueUpdate", variables);
                
                var issue = _state.UpdateIssue(
                    issueId,
                    input.GetValue<string>("title"),
                    input.GetValue<string>("description"),
                    input.GetValue<string>("assigneeId"),
                    input.GetValue<string>("stateId"),
                    input.GetValue<string>("projectId"),
                    input.GetValue<int?>("priority"),
                    input.GetValue<List<string>>("labelIds")
                );

                data["issueUpdate"] = new
                {
                    success = issue != null,
                    issue = issue != null ? MapIssue(issue) : null
                };
            }

            // issueArchive mutation
            var issueArchiveMatch = Regex.Match(body, @"issueArchive\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (issueArchiveMatch.Success)
            {
                var issueId = ResolveVariable(issueArchiveMatch.Groups[1].Value, variables);
                var success = _state.ArchiveIssue(issueId);
                var issue = _state.Issues.GetValueOrDefault(issueId) 
                    ?? _state.Issues.Values.FirstOrDefault(i => i.Identifier == issueId);
                
                data["issueArchive"] = new
                {
                    success,
                    entity = issue != null ? MapIssue(issue) : null
                };
            }

            // commentCreate mutation
            var commentCreateMatch = Regex.Match(body, @"commentCreate\s*\(\s*input\s*:");
            if (commentCreateMatch.Success)
            {
                var input = ExtractInputObject(body, "commentCreate", variables);
                var issueId = input.GetValue<string>("issueId") ?? "";
                var commentBody = input.GetValue<string>("body") ?? "";
                var parentId = input.GetValue<string>("parentId");

                try
                {
                    var comment = _state.CreateComment(issueId, commentBody, parentId);
                    data["commentCreate"] = new
                    {
                        success = true,
                        comment = MapComment(comment)
                    };
                }
                catch
                {
                    data["commentCreate"] = new { success = false };
                }
            }

            // projectCreate mutation
            var projectCreateMatch = Regex.Match(body, @"projectCreate\s*\(\s*input\s*:");
            if (projectCreateMatch.Success)
            {
                var input = ExtractInputObject(body, "projectCreate", variables);
                var project = new LinearProject
                {
                    Id = $"project_{Guid.NewGuid():N}",
                    Name = input.GetValue<string>("name") ?? "Untitled Project",
                    Description = input.GetValue<string>("description"),
                    State = input.GetValue<string>("state") ?? "planned",
                    LeadId = input.GetValue<string>("leadId"),
                    TeamIds = input.GetValue<List<string>>("teamIds") ?? new List<string>(),
                    Url = $"https://linear.app/{_state.Organization.UrlKey}/project/{Guid.NewGuid():N}"
                };
                _state.AddProject(project);

                data["projectCreate"] = new
                {
                    success = true,
                    project = MapProject(project)
                };
            }

            // projectUpdate mutation
            var projectUpdateMatch = Regex.Match(body, @"projectUpdate\s*\(");
            if (projectUpdateMatch.Success)
            {
                var idMatch = Regex.Match(body, @"projectUpdate\s*\(\s*id\s*:\s*[""']?([^""'\s,\)]+)[""']?");
                var projectId = idMatch.Success ? ResolveVariable(idMatch.Groups[1].Value, variables) : "";
                var input = ExtractInputObject(body, "projectUpdate", variables);

                var project = _state.Projects.GetValueOrDefault(projectId);
                if (project != null)
                {
                    if (input.GetValue<string>("name") is string name) project.Name = name;
                    if (input.GetValue<string>("description") is string desc) project.Description = desc;
                    if (input.GetValue<string>("state") is string state) project.State = state;
                    project.UpdatedAt = DateTime.UtcNow;
                }

                data["projectUpdate"] = new
                {
                    success = project != null,
                    project = project != null ? MapProject(project) : null
                };
            }

            // webhookCreate mutation
            var webhookCreateMatch = Regex.Match(body, @"webhookCreate\s*\(\s*input\s*:");
            if (webhookCreateMatch.Success)
            {
                var input = ExtractInputObject(body, "webhookCreate", variables);
                var webhook = _state.CreateWebhook(
                    input.GetValue<string>("url") ?? "",
                    input.GetValue<string>("teamId"),
                    input.GetValue<bool?>("allPublicTeams") ?? false,
                    input.GetValue<List<string>>("resourceTypes"),
                    input.GetValue<string>("label")
                );

                data["webhookCreate"] = new
                {
                    success = true,
                    webhook = MapWebhook(webhook)
                };
            }

            // webhookDelete mutation
            var webhookDeleteMatch = Regex.Match(body, @"webhookDelete\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (webhookDeleteMatch.Success)
            {
                var webhookId = ResolveVariable(webhookDeleteMatch.Groups[1].Value, variables);
                var success = _state.DeleteWebhook(webhookId);

                data["webhookDelete"] = new
                {
                    success
                };
            }

            // attachmentCreate mutation
            var attachmentCreateMatch = Regex.Match(body, @"attachmentCreate\s*\(\s*input\s*:");
            if (attachmentCreateMatch.Success)
            {
                var input = ExtractInputObject(body, "attachmentCreate", variables);
                try
                {
                    var attachment = _state.CreateAttachment(
                        input.GetValue<string>("issueId") ?? "",
                        input.GetValue<string>("title") ?? "",
                        input.GetValue<string>("url") ?? "",
                        input.GetValue<string>("subtitle")
                    );

                    data["attachmentCreate"] = new
                    {
                        success = true,
                        attachment = MapAttachment(attachment)
                    };
                }
                catch
                {
                    data["attachmentCreate"] = new { success = false };
                }
            }

            // ===== Additional Mutations =====

            // issueDelete mutation
            var issueDeleteMatch = Regex.Match(body, @"issueDelete\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (issueDeleteMatch.Success)
            {
                var issueId = ResolveVariable(issueDeleteMatch.Groups[1].Value, variables);
                var success = _state.DeleteIssue(issueId);
                data["issueDelete"] = new { success };
            }

            // issueUnarchive mutation
            var issueUnarchiveMatch = Regex.Match(body, @"issueUnarchive\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (issueUnarchiveMatch.Success)
            {
                var issueId = ResolveVariable(issueUnarchiveMatch.Groups[1].Value, variables);
                var success = _state.UnarchiveIssue(issueId);
                var issue = _state.Issues.GetValueOrDefault(issueId) 
                    ?? _state.Issues.Values.FirstOrDefault(i => i.Identifier == issueId);
                data["issueUnarchive"] = new
                {
                    success,
                    issue = issue != null ? MapIssue(issue) : null
                };
            }

            // issueBatchUpdate mutation
            var issueBatchUpdateMatch = Regex.Match(body, @"issueBatchUpdate\s*\(");
            if (issueBatchUpdateMatch.Success)
            {
                var input = ExtractInputObject(body, "issueBatchUpdate", variables);
                var issueIds = input.GetValue<List<string>>("ids") ?? new List<string>();
                var stateId = input.GetValue<string>("stateId");
                var assigneeId = input.GetValue<string>("assigneeId");
                var projectId = input.GetValue<string>("projectId");
                var priority = input.GetValue<int?>("priority");

                var issues = _state.BatchUpdateIssues(issueIds, stateId, assigneeId, projectId, priority);
                data["issueBatchUpdate"] = new
                {
                    success = true,
                    issues = issues.Select(MapIssue).ToList()
                };
            }

            // issueAddLabel mutation
            var issueAddLabelMatch = Regex.Match(body, @"issueAddLabel\s*\(");
            if (issueAddLabelMatch.Success)
            {
                var input = ExtractInputObject(body, "issueAddLabel", variables);
                var issueId = input.GetValue<string>("id") ?? "";
                var labelId = input.GetValue<string>("labelId") ?? "";
                var success = _state.AddLabelToIssue(issueId, labelId);
                var issue = _state.Issues.GetValueOrDefault(issueId) 
                    ?? _state.Issues.Values.FirstOrDefault(i => i.Identifier == issueId);
                data["issueAddLabel"] = new
                {
                    success,
                    issue = issue != null ? MapIssue(issue) : null
                };
            }

            // issueRemoveLabel mutation
            var issueRemoveLabelMatch = Regex.Match(body, @"issueRemoveLabel\s*\(");
            if (issueRemoveLabelMatch.Success)
            {
                var input = ExtractInputObject(body, "issueRemoveLabel", variables);
                var issueId = input.GetValue<string>("id") ?? "";
                var labelId = input.GetValue<string>("labelId") ?? "";
                var success = _state.RemoveLabelFromIssue(issueId, labelId);
                var issue = _state.Issues.GetValueOrDefault(issueId) 
                    ?? _state.Issues.Values.FirstOrDefault(i => i.Identifier == issueId);
                data["issueRemoveLabel"] = new
                {
                    success,
                    issue = issue != null ? MapIssue(issue) : null
                };
            }

            // commentUpdate mutation
            var commentUpdateMatch = Regex.Match(body, @"commentUpdate\s*\(");
            if (commentUpdateMatch.Success)
            {
                var idMatch = Regex.Match(body, @"commentUpdate\s*\(\s*id\s*:\s*[""']?([^""'\s,\)]+)[""']?");
                var commentId = idMatch.Success ? ResolveVariable(idMatch.Groups[1].Value, variables) : "";
                var input = ExtractInputObject(body, "commentUpdate", variables);
                var comment = _state.UpdateComment(commentId, input.GetValue<string>("body"));
                data["commentUpdate"] = new
                {
                    success = comment != null,
                    comment = comment != null ? MapComment(comment) : null
                };
            }

            // commentDelete mutation
            var commentDeleteMatch = Regex.Match(body, @"commentDelete\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (commentDeleteMatch.Success)
            {
                var commentId = ResolveVariable(commentDeleteMatch.Groups[1].Value, variables);
                var success = _state.DeleteComment(commentId);
                data["commentDelete"] = new { success };
            }

            // labelCreate / issueLabelCreate mutation
            var labelCreateMatch = Regex.Match(body, @"(issueLabel|label)Create\s*\(\s*input\s*:");
            if (labelCreateMatch.Success)
            {
                var mutationName = labelCreateMatch.Groups[1].Value + "Create";
                var input = ExtractInputObject(body, mutationName, variables);
                var label = _state.CreateLabel(
                    input.GetValue<string>("name") ?? "New Label",
                    input.GetValue<string>("teamId"),
                    input.GetValue<string>("color"),
                    input.GetValue<string>("description")
                );
                data[mutationName] = new
                {
                    success = true,
                    issueLabel = MapLabel(label)
                };
            }

            // labelUpdate / issueLabelUpdate mutation
            var labelUpdateMatch = Regex.Match(body, @"(issueLabel|label)Update\s*\(");
            if (labelUpdateMatch.Success)
            {
                var mutationName = labelUpdateMatch.Groups[1].Value + "Update";
                var idMatch = Regex.Match(body, $@"{mutationName}\s*\(\s*id\s*:\s*[""']?([^""'\s,\)]+)[""']?");
                var labelId = idMatch.Success ? ResolveVariable(idMatch.Groups[1].Value, variables) : "";
                var input = ExtractInputObject(body, mutationName, variables);
                var label = _state.UpdateLabel(
                    labelId,
                    input.GetValue<string>("name"),
                    input.GetValue<string>("color"),
                    input.GetValue<string>("description")
                );
                data[mutationName] = new
                {
                    success = label != null,
                    issueLabel = label != null ? MapLabel(label) : null
                };
            }

            // labelDelete / issueLabelDelete mutation
            var labelDeleteMatch = Regex.Match(body, @"(issueLabel|label)Delete\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (labelDeleteMatch.Success)
            {
                var mutationName = labelDeleteMatch.Groups[1].Value + "Delete";
                var labelId = ResolveVariable(labelDeleteMatch.Groups[2].Value, variables);
                var success = _state.DeleteLabel(labelId);
                data[mutationName] = new { success };
            }

            // labelArchive / issueLabelArchive mutation
            var labelArchiveMatch = Regex.Match(body, @"(issueLabel|label)Archive\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (labelArchiveMatch.Success)
            {
                var mutationName = labelArchiveMatch.Groups[1].Value + "Archive";
                var labelId = ResolveVariable(labelArchiveMatch.Groups[2].Value, variables);
                var success = _state.ArchiveLabel(labelId);
                data[mutationName] = new { success };
            }

            // cycleCreate mutation
            var cycleCreateMatch = Regex.Match(body, @"cycleCreate\s*\(\s*input\s*:");
            if (cycleCreateMatch.Success)
            {
                var input = ExtractInputObject(body, "cycleCreate", variables);
                try
                {
                    var cycle = _state.CreateCycle(
                        input.GetValue<string>("teamId") ?? _state.Teams.Keys.First(),
                        input.GetValue<string>("name")
                    );
                    data["cycleCreate"] = new
                    {
                        success = true,
                        cycle = MapCycle(cycle)
                    };
                }
                catch
                {
                    data["cycleCreate"] = new { success = false };
                }
            }

            // cycleUpdate mutation
            var cycleUpdateMatch = Regex.Match(body, @"cycleUpdate\s*\(");
            if (cycleUpdateMatch.Success)
            {
                var idMatch = Regex.Match(body, @"cycleUpdate\s*\(\s*id\s*:\s*[""']?([^""'\s,\)]+)[""']?");
                var cycleId = idMatch.Success ? ResolveVariable(idMatch.Groups[1].Value, variables) : "";
                var input = ExtractInputObject(body, "cycleUpdate", variables);
                var cycle = _state.UpdateCycle(cycleId, input.GetValue<string>("name"));
                data["cycleUpdate"] = new
                {
                    success = cycle != null,
                    cycle = cycle != null ? MapCycle(cycle) : null
                };
            }

            // cycleArchive mutation
            var cycleArchiveMatch = Regex.Match(body, @"cycleArchive\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (cycleArchiveMatch.Success)
            {
                var cycleId = ResolveVariable(cycleArchiveMatch.Groups[1].Value, variables);
                var success = _state.ArchiveCycle(cycleId);
                data["cycleArchive"] = new { success };
            }

            // teamCreate mutation
            var teamCreateMatch = Regex.Match(body, @"teamCreate\s*\(\s*input\s*:");
            if (teamCreateMatch.Success)
            {
                var input = ExtractInputObject(body, "teamCreate", variables);
                var team = _state.CreateTeam(
                    input.GetValue<string>("name") ?? "New Team",
                    input.GetValue<string>("key") ?? "NEW",
                    input.GetValue<string>("description")
                );
                data["teamCreate"] = new
                {
                    success = true,
                    team = MapTeam(team)
                };
            }

            // teamUpdate mutation
            var teamUpdateMatch = Regex.Match(body, @"teamUpdate\s*\(");
            if (teamUpdateMatch.Success)
            {
                var idMatch = Regex.Match(body, @"teamUpdate\s*\(\s*id\s*:\s*[""']?([^""'\s,\)]+)[""']?");
                var teamId = idMatch.Success ? ResolveVariable(idMatch.Groups[1].Value, variables) : "";
                var input = ExtractInputObject(body, "teamUpdate", variables);
                var team = _state.UpdateTeam(
                    teamId,
                    input.GetValue<string>("name"),
                    input.GetValue<string>("description")
                );
                data["teamUpdate"] = new
                {
                    success = team != null,
                    team = team != null ? MapTeam(team) : null
                };
            }

            // projectArchive mutation
            var projectArchiveMatch = Regex.Match(body, @"projectArchive\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (projectArchiveMatch.Success)
            {
                var projectId = ResolveVariable(projectArchiveMatch.Groups[1].Value, variables);
                var success = _state.ArchiveProject(projectId);
                var project = _state.Projects.GetValueOrDefault(projectId);
                data["projectArchive"] = new
                {
                    success,
                    entity = project != null ? MapProject(project) : null
                };
            }

            // attachmentUpdate mutation
            var attachmentUpdateMatch = Regex.Match(body, @"attachmentUpdate\s*\(");
            if (attachmentUpdateMatch.Success)
            {
                var idMatch = Regex.Match(body, @"attachmentUpdate\s*\(\s*id\s*:\s*[""']?([^""'\s,\)]+)[""']?");
                var attachmentId = idMatch.Success ? ResolveVariable(idMatch.Groups[1].Value, variables) : "";
                var input = ExtractInputObject(body, "attachmentUpdate", variables);
                var attachment = _state.UpdateAttachment(
                    attachmentId,
                    input.GetValue<string>("title"),
                    input.GetValue<string>("subtitle"),
                    input.GetValue<string>("url")
                );
                data["attachmentUpdate"] = new
                {
                    success = attachment != null,
                    attachment = attachment != null ? MapAttachment(attachment) : null
                };
            }

            // attachmentDelete mutation
            var attachmentDeleteMatch = Regex.Match(body, @"attachmentDelete\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (attachmentDeleteMatch.Success)
            {
                var attachmentId = ResolveVariable(attachmentDeleteMatch.Groups[1].Value, variables);
                var success = _state.DeleteAttachment(attachmentId);
                data["attachmentDelete"] = new { success };
            }

            // ===== Issue Relation Mutations =====

            // issueRelationCreate mutation
            var relationCreateMatch = Regex.Match(body, @"issueRelationCreate\s*\(\s*input\s*:");
            if (relationCreateMatch.Success)
            {
                var input = ExtractInputObject(body, "issueRelationCreate", variables);
                try
                {
                    var relation = _state.CreateIssueRelation(
                        input.GetValue<string>("issueId") ?? "",
                        input.GetValue<string>("relatedIssueId") ?? "",
                        input.GetValue<string>("type") ?? "related"
                    );
                    data["issueRelationCreate"] = new
                    {
                        success = true,
                        issueRelation = MapIssueRelation(relation)
                    };
                }
                catch
                {
                    data["issueRelationCreate"] = new { success = false };
                }
            }

            // issueRelationDelete mutation
            var relationDeleteMatch = Regex.Match(body, @"issueRelationDelete\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (relationDeleteMatch.Success)
            {
                var relationId = ResolveVariable(relationDeleteMatch.Groups[1].Value, variables);
                var success = _state.DeleteIssueRelation(relationId);
                data["issueRelationDelete"] = new { success };
            }

            // ===== Reaction Mutations =====

            // reactionCreate mutation
            var reactionCreateMatch = Regex.Match(body, @"reactionCreate\s*\(\s*input\s*:");
            if (reactionCreateMatch.Success)
            {
                var input = ExtractInputObject(body, "reactionCreate", variables);
                try
                {
                    var reaction = _state.CreateReaction(
                        input.GetValue<string>("commentId") ?? "",
                        input.GetValue<string>("emoji") ?? "👍"
                    );
                    data["reactionCreate"] = new
                    {
                        success = true,
                        reaction = MapReaction(reaction)
                    };
                }
                catch
                {
                    data["reactionCreate"] = new { success = false };
                }
            }

            // reactionDelete mutation
            var reactionDeleteMatch = Regex.Match(body, @"reactionDelete\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (reactionDeleteMatch.Success)
            {
                var reactionId = ResolveVariable(reactionDeleteMatch.Groups[1].Value, variables);
                var success = _state.DeleteReaction(reactionId);
                data["reactionDelete"] = new { success };
            }

            // ===== Notification Mutations =====

            // notificationMarkAsRead mutation
            var notificationReadMatch = Regex.Match(body, @"notificationMarkAsRead\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (notificationReadMatch.Success)
            {
                var notificationId = ResolveVariable(notificationReadMatch.Groups[1].Value, variables);
                var success = _state.MarkNotificationAsRead(notificationId);
                data["notificationMarkAsRead"] = new { success };
            }

            // notificationArchive mutation
            var notificationArchiveMatch = Regex.Match(body, @"notificationArchive\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (notificationArchiveMatch.Success)
            {
                var notificationId = ResolveVariable(notificationArchiveMatch.Groups[1].Value, variables);
                var success = _state.ArchiveNotification(notificationId);
                data["notificationArchive"] = new { success };
            }

            // ===== Favorite Mutations =====

            // favoriteCreate mutation
            var favoriteCreateMatch = Regex.Match(body, @"favoriteCreate\s*\(\s*input\s*:");
            if (favoriteCreateMatch.Success)
            {
                var input = ExtractInputObject(body, "favoriteCreate", variables);
                var favorite = _state.CreateFavorite(
                    input.GetValue<string>("type") ?? "issue",
                    input.GetValue<string>("issueId"),
                    input.GetValue<string>("projectId"),
                    input.GetValue<string>("cycleId"),
                    input.GetValue<string>("labelId")
                );
                data["favoriteCreate"] = new
                {
                    success = true,
                    favorite = MapFavorite(favorite)
                };
            }

            // favoriteDelete mutation
            var favoriteDeleteMatch = Regex.Match(body, @"favoriteDelete\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (favoriteDeleteMatch.Success)
            {
                var favoriteId = ResolveVariable(favoriteDeleteMatch.Groups[1].Value, variables);
                var success = _state.DeleteFavorite(favoriteId);
                data["favoriteDelete"] = new { success };
            }

            // ===== Document Mutations =====

            // documentCreate mutation
            var documentCreateMatch = Regex.Match(body, @"documentCreate\s*\(\s*input\s*:");
            if (documentCreateMatch.Success)
            {
                var input = ExtractInputObject(body, "documentCreate", variables);
                var document = _state.CreateDocument(
                    input.GetValue<string>("title") ?? "Untitled",
                    input.GetValue<string>("projectId"),
                    input.GetValue<string>("content")
                );
                data["documentCreate"] = new
                {
                    success = true,
                    document = MapDocument(document)
                };
            }

            // documentUpdate mutation
            var documentUpdateMatch = Regex.Match(body, @"documentUpdate\s*\(");
            if (documentUpdateMatch.Success)
            {
                var idMatch = Regex.Match(body, @"documentUpdate\s*\(\s*id\s*:\s*[""']?([^""'\s,\)]+)[""']?");
                var documentId = idMatch.Success ? ResolveVariable(idMatch.Groups[1].Value, variables) : "";
                var input = ExtractInputObject(body, "documentUpdate", variables);
                var document = _state.UpdateDocument(
                    documentId,
                    input.GetValue<string>("title"),
                    input.GetValue<string>("content")
                );
                data["documentUpdate"] = new
                {
                    success = document != null,
                    document = document != null ? MapDocument(document) : null
                };
            }

            // documentDelete mutation
            var documentDeleteMatch = Regex.Match(body, @"documentDelete\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (documentDeleteMatch.Success)
            {
                var documentId = ResolveVariable(documentDeleteMatch.Groups[1].Value, variables);
                var success = _state.DeleteDocument(documentId);
                data["documentDelete"] = new { success };
            }

            // ===== Roadmap Mutations =====

            // roadmapCreate mutation
            var roadmapCreateMatch = Regex.Match(body, @"roadmapCreate\s*\(\s*input\s*:");
            if (roadmapCreateMatch.Success)
            {
                var input = ExtractInputObject(body, "roadmapCreate", variables);
                var roadmap = _state.CreateRoadmap(
                    input.GetValue<string>("name") ?? "New Roadmap",
                    input.GetValue<string>("description")
                );
                data["roadmapCreate"] = new
                {
                    success = true,
                    roadmap = MapRoadmap(roadmap)
                };
            }

            // roadmapUpdate mutation
            var roadmapUpdateMatch = Regex.Match(body, @"roadmapUpdate\s*\(");
            if (roadmapUpdateMatch.Success)
            {
                var idMatch = Regex.Match(body, @"roadmapUpdate\s*\(\s*id\s*:\s*[""']?([^""'\s,\)]+)[""']?");
                var roadmapId = idMatch.Success ? ResolveVariable(idMatch.Groups[1].Value, variables) : "";
                var input = ExtractInputObject(body, "roadmapUpdate", variables);
                var roadmap = _state.UpdateRoadmap(
                    roadmapId,
                    input.GetValue<string>("name"),
                    input.GetValue<string>("description")
                );
                data["roadmapUpdate"] = new
                {
                    success = roadmap != null,
                    roadmap = roadmap != null ? MapRoadmap(roadmap) : null
                };
            }

            // roadmapArchive mutation
            var roadmapArchiveMatch = Regex.Match(body, @"roadmapArchive\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (roadmapArchiveMatch.Success)
            {
                var roadmapId = ResolveVariable(roadmapArchiveMatch.Groups[1].Value, variables);
                var success = _state.ArchiveRoadmap(roadmapId);
                data["roadmapArchive"] = new { success };
            }

            // ===== Custom View Mutations =====

            // customViewCreate mutation
            var customViewCreateMatch = Regex.Match(body, @"customViewCreate\s*\(\s*input\s*:");
            if (customViewCreateMatch.Success)
            {
                var input = ExtractInputObject(body, "customViewCreate", variables);
                var view = _state.CreateCustomView(
                    input.GetValue<string>("name") ?? "New View",
                    input.GetValue<string>("teamId"),
                    input.GetValue<string>("filterData"),
                    input.GetValue<bool?>("shared") ?? false
                );
                data["customViewCreate"] = new
                {
                    success = true,
                    customView = MapCustomView(view)
                };
            }

            // customViewUpdate mutation
            var customViewUpdateMatch = Regex.Match(body, @"customViewUpdate\s*\(");
            if (customViewUpdateMatch.Success)
            {
                var idMatch = Regex.Match(body, @"customViewUpdate\s*\(\s*id\s*:\s*[""']?([^""'\s,\)]+)[""']?");
                var viewId = idMatch.Success ? ResolveVariable(idMatch.Groups[1].Value, variables) : "";
                var input = ExtractInputObject(body, "customViewUpdate", variables);
                var view = _state.UpdateCustomView(
                    viewId,
                    input.GetValue<string>("name"),
                    input.GetValue<string>("filterData"),
                    input.GetValue<bool?>("shared")
                );
                data["customViewUpdate"] = new
                {
                    success = view != null,
                    customView = view != null ? MapCustomView(view) : null
                };
            }

            // customViewDelete mutation
            var customViewDeleteMatch = Regex.Match(body, @"customViewDelete\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (customViewDeleteMatch.Success)
            {
                var viewId = ResolveVariable(customViewDeleteMatch.Groups[1].Value, variables);
                var success = _state.DeleteCustomView(viewId);
                data["customViewDelete"] = new { success };
            }

            // ===== Subscriber Mutations =====

            // issueSubscribe mutation
            var issueSubscribeMatch = Regex.Match(body, @"issueSubscribe\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (issueSubscribeMatch.Success)
            {
                var issueId = ResolveVariable(issueSubscribeMatch.Groups[1].Value, variables);
                var success = _state.SubscribeToIssue(issueId);
                var issue = _state.Issues.GetValueOrDefault(issueId) 
                    ?? _state.Issues.Values.FirstOrDefault(i => i.Identifier == issueId);
                data["issueSubscribe"] = new
                {
                    success,
                    issue = issue != null ? MapIssue(issue) : null
                };
            }

            // issueUnsubscribe mutation
            var issueUnsubscribeMatch = Regex.Match(body, @"issueUnsubscribe\s*\(\s*id\s*:\s*[""']?([^""'\s\)]+)[""']?\s*\)");
            if (issueUnsubscribeMatch.Success)
            {
                var issueId = ResolveVariable(issueUnsubscribeMatch.Groups[1].Value, variables);
                var success = _state.UnsubscribeFromIssue(issueId);
                var issue = _state.Issues.GetValueOrDefault(issueId) 
                    ?? _state.Issues.Values.FirstOrDefault(i => i.Identifier == issueId);
                data["issueUnsubscribe"] = new
                {
                    success,
                    issue = issue != null ? MapIssue(issue) : null
                };
            }

            return new { data };
        }

        private static bool ContainsField(string body, string fieldName)
        {
            // Check for field at root level (not as part of another word)
            var pattern = $@"(?<![a-zA-Z]){fieldName}(?:\s*\(|\s*\{{|\s*$)";
            return Regex.IsMatch(body, pattern, RegexOptions.Multiline);
        }

        private static string ResolveVariable(string value, JObject? variables)
        {
            if (value.StartsWith("$") && variables != null)
            {
                var varName = value[1..];
                return variables[varName]?.ToString() ?? value;
            }
            return value.Trim('"', '\'');
        }

        private InputHelper ExtractInputObject(string body, string mutationName, JObject? variables)
        {
            // Try to find input in variables first
            if (variables != null && variables["input"] is JObject inputVar)
            {
                return new InputHelper(inputVar);
            }

            // Find the start of the input object
            var inputStartPattern = $@"{mutationName}\s*\([^{{]*input\s*:\s*\{{";
            var inputStartMatch = Regex.Match(body, inputStartPattern, RegexOptions.Singleline);
            
            if (inputStartMatch.Success)
            {
                var startIndex = inputStartMatch.Index + inputStartMatch.Length;
                var braceCount = 1;
                var endIndex = startIndex;
                
                // Find matching closing brace
                for (int i = startIndex; i < body.Length && braceCount > 0; i++)
                {
                    if (body[i] == '{') braceCount++;
                    else if (body[i] == '}') braceCount--;
                    endIndex = i;
                }
                
                var inputContent = body.Substring(startIndex, endIndex - startIndex);
                var inputObj = new JObject();

                // Parse key-value pairs including those with arrays
                // Handle string values: key: "value"
                var strMatches = Regex.Matches(inputContent, @"(\w+)\s*:\s*""([^""]*)""");
                foreach (Match match in strMatches)
                {
                    inputObj[match.Groups[1].Value] = match.Groups[2].Value;
                }
                
                // Handle variable references: key: $varName
                var varMatches = Regex.Matches(inputContent, @"(\w+)\s*:\s*\$(\w+)");
                foreach (Match match in varMatches)
                {
                    var key = match.Groups[1].Value;
                    var varName = match.Groups[2].Value;
                    if (variables != null && variables[varName] != null)
                    {
                        inputObj[key] = variables[varName];
                    }
                }
                
                // Handle numeric values: key: 123
                var numMatches = Regex.Matches(inputContent, @"(\w+)\s*:\s*(\d+)(?!\w)");
                foreach (Match match in numMatches)
                {
                    var key = match.Groups[1].Value;
                    if (!inputObj.ContainsKey(key)) // Don't override string values
                    {
                        inputObj[key] = int.Parse(match.Groups[2].Value);
                    }
                }
                
                // Handle boolean values: key: true/false
                var boolMatches = Regex.Matches(inputContent, @"(\w+)\s*:\s*(true|false)(?!\w)");
                foreach (Match match in boolMatches)
                {
                    var key = match.Groups[1].Value;
                    if (!inputObj.ContainsKey(key))
                    {
                        inputObj[key] = match.Groups[2].Value == "true";
                    }
                }
                
                // Handle array values: key: ["val1", "val2"]
                var arrayMatches = Regex.Matches(inputContent, @"(\w+)\s*:\s*\[([^\]]*)\]");
                foreach (Match match in arrayMatches)
                {
                    var key = match.Groups[1].Value;
                    var arrayContent = match.Groups[2].Value;
                    var items = Regex.Matches(arrayContent, @"""([^""]*)""")
                        .Select(m => m.Groups[1].Value)
                        .ToList();
                    inputObj[key] = new JArray(items);
                }

                return new InputHelper(inputObj);
            }

            return new InputHelper(new JObject());
        }

        #region Mapping Methods

        private object? MapUser(LinearUser? user)
        {
            if (user == null) return null;
            return new
            {
                id = user.Id,
                name = user.Name,
                displayName = user.DisplayName,
                email = user.Email,
                url = user.Url,
                active = user.Active,
                admin = user.Admin,
                createdAt = user.CreatedAt,
                updatedAt = user.UpdatedAt,
                avatarUrl = user.AvatarUrl
            };
        }

        private object MapOrganization(LinearOrganization org)
        {
            return new
            {
                id = org.Id,
                name = org.Name,
                urlKey = org.UrlKey,
                logoUrl = org.LogoUrl,
                createdAt = org.CreatedAt,
                samlEnabled = org.SamlEnabled,
                scimEnabled = org.ScimEnabled,
                userCount = org.UserCount,
                gitBranchFormat = org.GitBranchFormat
            };
        }

        private object MapTeam(LinearTeam team)
        {
            return new
            {
                id = team.Id,
                name = team.Name,
                key = team.Key,
                description = team.Description,
                createdAt = team.CreatedAt,
                updatedAt = team.UpdatedAt,
                issueCount = team.IssueCount,
                @private = team.Private,
                members = new
                {
                    nodes = team.MemberIds.Select(id => MapUser(_state.Users.GetValueOrDefault(id))).Where(u => u != null).ToList()
                },
                states = new
                {
                    nodes = _state.GetWorkflowStatesForTeam(team.Id).Select(MapWorkflowState).ToList()
                }
            };
        }

        private object MapIssue(LinearIssue issue)
        {
            return new
            {
                id = issue.Id,
                identifier = issue.Identifier,
                number = issue.Number,
                title = issue.Title,
                description = issue.Description,
                priority = issue.Priority,
                estimate = issue.Estimate,
                url = issue.Url,
                createdAt = issue.CreatedAt,
                updatedAt = issue.UpdatedAt,
                archivedAt = issue.ArchivedAt,
                startedAt = issue.StartedAt,
                completedAt = issue.CompletedAt,
                canceledAt = issue.CanceledAt,
                dueDate = issue.DueDate,
                team = _state.Teams.TryGetValue(issue.TeamId, out var team) ? new { id = team.Id, name = team.Name, key = team.Key } : null,
                assignee = issue.AssigneeId != null ? MapUser(_state.Users.GetValueOrDefault(issue.AssigneeId)) : null,
                creator = MapUser(_state.Users.GetValueOrDefault(issue.CreatorId)),
                state = _state.WorkflowStates.TryGetValue(issue.StateId, out var state) ? MapWorkflowState(state) : null,
                project = issue.ProjectId != null && _state.Projects.TryGetValue(issue.ProjectId, out var proj) 
                    ? new { id = proj.Id, name = proj.Name } : null,
                cycle = issue.CycleId != null && _state.Cycles.TryGetValue(issue.CycleId, out var cycle) 
                    ? new { id = cycle.Id, name = cycle.Name, number = cycle.Number } : null,
                labels = new
                {
                    nodes = issue.LabelIds
                        .Where(id => _state.Labels.ContainsKey(id))
                        .Select(id => MapLabel(_state.Labels[id])).ToList()
                },
                comments = new
                {
                    nodes = _state.GetCommentsForIssue(issue.Id).Select(MapComment).ToList()
                }
            };
        }

        private object MapProject(LinearProject project)
        {
            return new
            {
                id = project.Id,
                name = project.Name,
                description = project.Description,
                state = project.State,
                icon = project.Icon,
                color = project.Color,
                progress = project.Progress,
                url = project.Url,
                createdAt = project.CreatedAt,
                updatedAt = project.UpdatedAt,
                archivedAt = project.ArchivedAt,
                startDate = project.StartDate,
                targetDate = project.TargetDate,
                lead = project.LeadId != null ? MapUser(_state.Users.GetValueOrDefault(project.LeadId)) : null,
                members = new
                {
                    nodes = project.MemberIds.Select(id => MapUser(_state.Users.GetValueOrDefault(id))).Where(u => u != null).ToList()
                },
                teams = new
                {
                    nodes = project.TeamIds.Where(id => _state.Teams.ContainsKey(id))
                        .Select(id => new { id = _state.Teams[id].Id, name = _state.Teams[id].Name }).ToList()
                }
            };
        }

        private object MapCycle(LinearCycle cycle)
        {
            return new
            {
                id = cycle.Id,
                name = cycle.Name,
                number = cycle.Number,
                startsAt = cycle.StartsAt,
                endsAt = cycle.EndsAt,
                createdAt = cycle.CreatedAt,
                updatedAt = cycle.UpdatedAt,
                completedAt = cycle.CompletedAt,
                progress = cycle.Progress,
                issueCountScope = cycle.IssueCountScope,
                completedIssueCountScope = cycle.CompletedIssueCountScope,
                team = _state.Teams.TryGetValue(cycle.TeamId, out var team) ? new { id = team.Id, name = team.Name } : null
            };
        }

        private object MapComment(LinearComment comment)
        {
            return new
            {
                id = comment.Id,
                body = comment.Body,
                url = comment.Url,
                createdAt = comment.CreatedAt,
                updatedAt = comment.UpdatedAt,
                archivedAt = comment.ArchivedAt,
                edited = comment.Edited,
                user = MapUser(_state.Users.GetValueOrDefault(comment.UserId)),
                issue = _state.Issues.TryGetValue(comment.IssueId, out var issue) 
                    ? new { id = issue.Id, identifier = issue.Identifier, title = issue.Title } : null,
                parent = comment.ParentId != null && _state.Comments.TryGetValue(comment.ParentId, out var parent) 
                    ? new { id = parent.Id } : null
            };
        }

        private static object MapLabel(LinearLabel label)
        {
            return new
            {
                id = label.Id,
                name = label.Name,
                color = label.Color,
                description = label.Description,
                createdAt = label.CreatedAt,
                updatedAt = label.UpdatedAt,
                isGroup = label.IsGroup
            };
        }

        private static object MapWorkflowState(LinearWorkflowState state)
        {
            return new
            {
                id = state.Id,
                name = state.Name,
                type = state.Type,
                color = state.Color,
                description = state.Description,
                position = state.Position,
                createdAt = state.CreatedAt,
                updatedAt = state.UpdatedAt
            };
        }

        private static object MapWebhook(LinearWebhook webhook)
        {
            return new
            {
                id = webhook.Id,
                url = webhook.Url,
                enabled = webhook.Enabled,
                label = webhook.Label,
                allPublicTeams = webhook.AllPublicTeams,
                resourceTypes = webhook.ResourceTypes,
                createdAt = webhook.CreatedAt,
                updatedAt = webhook.UpdatedAt
            };
        }

        private static object MapAttachment(LinearAttachment attachment)
        {
            return new
            {
                id = attachment.Id,
                title = attachment.Title,
                subtitle = attachment.Subtitle,
                url = attachment.Url,
                createdAt = attachment.CreatedAt,
                updatedAt = attachment.UpdatedAt
            };
        }

        private static object MapIssueRelation(LinearIssueRelation relation)
        {
            return new
            {
                id = relation.Id,
                type = relation.Type,
                createdAt = relation.CreatedAt,
                updatedAt = relation.UpdatedAt
            };
        }

        private object MapReaction(LinearReaction reaction)
        {
            return new
            {
                id = reaction.Id,
                emoji = reaction.Emoji,
                user = MapUser(_state.Users.GetValueOrDefault(reaction.UserId)),
                createdAt = reaction.CreatedAt
            };
        }

        private object MapNotification(LinearNotification notification)
        {
            return new
            {
                id = notification.Id,
                type = notification.Type,
                readAt = notification.ReadAt,
                archivedAt = notification.ArchivedAt,
                issue = notification.IssueId != null 
                    ? MapIssue(_state.Issues.GetValueOrDefault(notification.IssueId)!) 
                    : null,
                comment = notification.CommentId != null 
                    ? MapComment(_state.Comments.GetValueOrDefault(notification.CommentId)!) 
                    : null,
                actor = notification.ActorId != null 
                    ? MapUser(_state.Users.GetValueOrDefault(notification.ActorId)) 
                    : null,
                createdAt = notification.CreatedAt,
                updatedAt = notification.UpdatedAt
            };
        }

        private static object MapFavorite(LinearFavorite favorite)
        {
            return new
            {
                id = favorite.Id,
                type = favorite.Type,
                sortOrder = favorite.SortOrder,
                createdAt = favorite.CreatedAt,
                updatedAt = favorite.UpdatedAt
            };
        }

        private object MapDocument(LinearDocument document)
        {
            return new
            {
                id = document.Id,
                title = document.Title,
                content = document.Content,
                icon = document.Icon,
                color = document.Color,
                url = document.Url,
                sortOrder = document.SortOrder,
                creator = MapUser(_state.Users.GetValueOrDefault(document.CreatorId)),
                createdAt = document.CreatedAt,
                updatedAt = document.UpdatedAt,
                archivedAt = document.ArchivedAt
            };
        }

        private object MapRoadmap(LinearRoadmap roadmap)
        {
            return new
            {
                id = roadmap.Id,
                name = roadmap.Name,
                description = roadmap.Description,
                slug = roadmap.Slug,
                sortOrder = roadmap.SortOrder,
                creator = MapUser(_state.Users.GetValueOrDefault(roadmap.CreatorId)),
                owner = MapUser(_state.Users.GetValueOrDefault(roadmap.OwnerId)),
                createdAt = roadmap.CreatedAt,
                updatedAt = roadmap.UpdatedAt,
                archivedAt = roadmap.ArchivedAt
            };
        }

        private object MapCustomView(LinearCustomView view)
        {
            return new
            {
                id = view.Id,
                name = view.Name,
                description = view.Description,
                icon = view.Icon,
                color = view.Color,
                filterData = view.FilterData,
                shared = view.Shared,
                creator = MapUser(_state.Users.GetValueOrDefault(view.CreatorId)),
                createdAt = view.CreatedAt,
                updatedAt = view.UpdatedAt
            };
        }

        #endregion
    }

    internal class InputHelper
    {
        private readonly JObject _obj;

        public InputHelper(JObject obj)
        {
            _obj = obj;
        }

        public T? GetValue<T>(string key)
        {
            if (!_obj.TryGetValue(key, out var token))
                return default;

            if (typeof(T) == typeof(string))
                return (T)(object)(token.ToString());

            if (typeof(T) == typeof(int?))
            {
                if (int.TryParse(token.ToString(), out var intVal))
                    return (T)(object)intVal;
                return default;
            }

            if (typeof(T) == typeof(bool?))
            {
                if (bool.TryParse(token.ToString(), out var boolVal))
                    return (T)(object)boolVal;
                return default;
            }

            if (typeof(T) == typeof(List<string>))
            {
                if (token is JArray arr)
                    return (T)(object)arr.Select(t => t.ToString()).ToList();
                return default;
            }

            return token.ToObject<T>();
        }
    }
}
