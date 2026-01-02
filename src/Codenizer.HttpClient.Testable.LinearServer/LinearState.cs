using Codenizer.HttpClient.Testable.LinearServer.Models;

namespace Codenizer.HttpClient.Testable.LinearServer
{
    /// <summary>
    /// Manages the stateful data for the Linear emulator.
    /// </summary>
    public class LinearState
    {
        private int _issueCounter = 1;
        private int _commentCounter = 1;
        private int _webhookCounter = 1;
        private int _attachmentCounter = 1;
        private int _relationCounter = 1;
        private int _reactionCounter = 1;
        private int _notificationCounter = 1;
        private int _favoriteCounter = 1;
        private int _documentCounter = 1;
        private int _roadmapCounter = 1;
        private int _customViewCounter = 1;

        public LinearOrganization Organization { get; set; } = new()
        {
            Id = "org_00000001",
            Name = "Test Organization",
            UrlKey = "test-org",
            UserCount = 3
        };

        public string CurrentUserId { get; set; } = "user_00000001";

        public Dictionary<string, LinearUser> Users { get; } = new();
        public Dictionary<string, LinearTeam> Teams { get; } = new();
        public Dictionary<string, LinearIssue> Issues { get; } = new();
        public Dictionary<string, LinearProject> Projects { get; } = new();
        public Dictionary<string, LinearCycle> Cycles { get; } = new();
        public Dictionary<string, LinearComment> Comments { get; } = new();
        public Dictionary<string, LinearLabel> Labels { get; } = new();
        public Dictionary<string, LinearWorkflowState> WorkflowStates { get; } = new();
        public Dictionary<string, LinearWebhook> Webhooks { get; } = new();
        public Dictionary<string, LinearAttachment> Attachments { get; } = new();
        
        // New entity collections
        public Dictionary<string, LinearIssueRelation> IssueRelations { get; } = new();
        public Dictionary<string, LinearReaction> Reactions { get; } = new();
        public Dictionary<string, LinearNotification> Notifications { get; } = new();
        public Dictionary<string, LinearFavorite> Favorites { get; } = new();
        public Dictionary<string, LinearDocument> Documents { get; } = new();
        public Dictionary<string, LinearRoadmap> Roadmaps { get; } = new();
        public Dictionary<string, LinearCustomView> CustomViews { get; } = new();

        public LinearState()
        {
            SeedDefaultData();
        }

        private void SeedDefaultData()
        {
            // Add default users
            AddUser(new LinearUser
            {
                Id = "user_00000001",
                Name = "Test User",
                DisplayName = "Test User",
                Email = "testuser@example.com",
                Admin = true,
                Url = $"https://linear.app/{Organization.UrlKey}/profiles/testuser"
            });

            AddUser(new LinearUser
            {
                Id = "user_00000002",
                Name = "Other User",
                DisplayName = "Other User",
                Email = "otheruser@example.com",
                Url = $"https://linear.app/{Organization.UrlKey}/profiles/otheruser"
            });

            AddUser(new LinearUser
            {
                Id = "user_00000003",
                Name = "Bot User",
                DisplayName = "Linear Bot",
                Email = "bot@example.com",
                Url = $"https://linear.app/{Organization.UrlKey}/profiles/bot"
            });

            // Add default team
            var team = new LinearTeam
            {
                Id = "team_00000001",
                Name = "Engineering",
                Key = "ENG",
                Description = "Engineering Team",
                MemberIds = new List<string> { "user_00000001", "user_00000002" }
            };
            AddTeam(team);

            // Add workflow states for the team
            AddWorkflowState(new LinearWorkflowState
            {
                Id = "state_triage_001",
                Name = "Triage",
                Type = "triage",
                Color = "#95A2B3",
                TeamId = team.Id,
                Position = 0
            });

            AddWorkflowState(new LinearWorkflowState
            {
                Id = "state_backlog_001",
                Name = "Backlog",
                Type = "backlog",
                Color = "#BBBFC7",
                TeamId = team.Id,
                Position = 1
            });

            AddWorkflowState(new LinearWorkflowState
            {
                Id = "state_todo_001",
                Name = "Todo",
                Type = "unstarted",
                Color = "#E2E2E2",
                TeamId = team.Id,
                Position = 2
            });

            AddWorkflowState(new LinearWorkflowState
            {
                Id = "state_inprogress_001",
                Name = "In Progress",
                Type = "started",
                Color = "#F2C94C",
                TeamId = team.Id,
                Position = 3
            });

            AddWorkflowState(new LinearWorkflowState
            {
                Id = "state_done_001",
                Name = "Done",
                Type = "completed",
                Color = "#5E6AD2",
                TeamId = team.Id,
                Position = 4
            });

            AddWorkflowState(new LinearWorkflowState
            {
                Id = "state_canceled_001",
                Name = "Canceled",
                Type = "canceled",
                Color = "#95A2B3",
                TeamId = team.Id,
                Position = 5
            });

            // Add default labels
            AddLabel(new LinearLabel
            {
                Id = "label_00000001",
                Name = "Bug",
                Color = "#EB5757",
                TeamId = team.Id
            });

            AddLabel(new LinearLabel
            {
                Id = "label_00000002",
                Name = "Feature",
                Color = "#6FCF97",
                TeamId = team.Id
            });

            AddLabel(new LinearLabel
            {
                Id = "label_00000003",
                Name = "Improvement",
                Color = "#56CCF2",
                TeamId = team.Id
            });

            // Add a default project
            AddProject(new LinearProject
            {
                Id = "project_00000001",
                Name = "Q1 Release",
                Description = "Q1 2024 Release Milestones",
                State = "started",
                TeamIds = new List<string> { team.Id },
                LeadId = "user_00000001",
                Url = $"https://linear.app/{Organization.UrlKey}/project/q1-release-{Guid.NewGuid().ToString()[..8]}"
            });

            // Add a default cycle
            AddCycle(new LinearCycle
            {
                Id = "cycle_00000001",
                Name = "Sprint 1",
                Number = 1,
                TeamId = team.Id,
                StartsAt = DateTime.UtcNow.AddDays(-7),
                EndsAt = DateTime.UtcNow.AddDays(7)
            });
        }

        public void AddUser(LinearUser user)
        {
            user.UpdatedAt = DateTime.UtcNow;
            Users[user.Id] = user;
            Organization.UserCount = Users.Count;
        }

        public void AddTeam(LinearTeam team)
        {
            team.UpdatedAt = DateTime.UtcNow;
            Teams[team.Id] = team;
        }

        public void AddWorkflowState(LinearWorkflowState state)
        {
            state.UpdatedAt = DateTime.UtcNow;
            WorkflowStates[state.Id] = state;
        }

        public void AddLabel(LinearLabel label)
        {
            label.UpdatedAt = DateTime.UtcNow;
            Labels[label.Id] = label;
        }

        public void AddProject(LinearProject project)
        {
            project.UpdatedAt = DateTime.UtcNow;
            Projects[project.Id] = project;
        }

        public void AddCycle(LinearCycle cycle)
        {
            cycle.UpdatedAt = DateTime.UtcNow;
            Cycles[cycle.Id] = cycle;
        }

        public LinearIssue CreateIssue(string teamId, string title, string? description = null, string? assigneeId = null,
            string? stateId = null, string? projectId = null, int priority = 0, List<string>? labelIds = null)
        {
            var team = Teams.GetValueOrDefault(teamId);
            if (team == null)
                throw new InvalidOperationException($"Team {teamId} not found");

            // Get the default state (first backlog state) if not specified
            if (stateId == null)
            {
                var defaultState = WorkflowStates.Values
                    .Where(s => s.TeamId == teamId && s.Type == "backlog")
                    .OrderBy(s => s.Position)
                    .FirstOrDefault();
                stateId = defaultState?.Id ?? WorkflowStates.Values.First(s => s.TeamId == teamId).Id;
            }

            var issueNumber = _issueCounter++;
            var issueId = $"issue_{issueNumber:00000000}";
            var identifier = $"{team.Key}-{issueNumber}";

            var issue = new LinearIssue
            {
                Id = issueId,
                Identifier = identifier,
                Number = issueNumber,
                Title = title,
                Description = description,
                TeamId = teamId,
                AssigneeId = assigneeId,
                StateId = stateId,
                ProjectId = projectId,
                Priority = priority,
                LabelIds = labelIds ?? new List<string>(),
                CreatorId = CurrentUserId,
                Url = $"https://linear.app/{Organization.UrlKey}/issue/{identifier}/{ToSlug(title)}"
            };

            Issues[issue.Id] = issue;
            team.IssueCount++;
            return issue;
        }

        public LinearIssue? UpdateIssue(string issueId, string? title = null, string? description = null,
            string? assigneeId = null, string? stateId = null, string? projectId = null, int? priority = null,
            List<string>? labelIds = null)
        {
            if (!Issues.TryGetValue(issueId, out var issue))
            {
                // Try to find by identifier (e.g., "ENG-1")
                issue = Issues.Values.FirstOrDefault(i => i.Identifier == issueId);
            }

            if (issue == null) return null;

            if (title != null) issue.Title = title;
            if (description != null) issue.Description = description;
            if (assigneeId != null) issue.AssigneeId = assigneeId;
            if (stateId != null)
            {
                var newState = WorkflowStates.GetValueOrDefault(stateId);
                if (newState != null)
                {
                    issue.StateId = stateId;
                    if (newState.Type == "started" && issue.StartedAt == null)
                        issue.StartedAt = DateTime.UtcNow;
                    if (newState.Type == "completed")
                        issue.CompletedAt = DateTime.UtcNow;
                    if (newState.Type == "canceled")
                        issue.CanceledAt = DateTime.UtcNow;
                }
            }
            if (projectId != null) issue.ProjectId = projectId;
            if (priority != null) issue.Priority = priority.Value;
            if (labelIds != null) issue.LabelIds = labelIds;
            issue.UpdatedAt = DateTime.UtcNow;

            return issue;
        }

        public bool ArchiveIssue(string issueId)
        {
            if (!Issues.TryGetValue(issueId, out var issue))
            {
                issue = Issues.Values.FirstOrDefault(i => i.Identifier == issueId);
            }

            if (issue == null) return false;

            issue.ArchivedAt = DateTime.UtcNow;
            issue.UpdatedAt = DateTime.UtcNow;
            return true;
        }

        public LinearComment CreateComment(string issueId, string body, string? parentId = null)
        {
            var issue = Issues.GetValueOrDefault(issueId);
            if (issue == null)
            {
                issue = Issues.Values.FirstOrDefault(i => i.Identifier == issueId);
            }
            if (issue == null)
                throw new InvalidOperationException($"Issue {issueId} not found");

            var commentId = $"comment_{_commentCounter++:00000000}";
            var comment = new LinearComment
            {
                Id = commentId,
                Body = body,
                IssueId = issue.Id,
                UserId = CurrentUserId,
                ParentId = parentId,
                Url = $"{issue.Url}#comment-{commentId}"
            };

            Comments[comment.Id] = comment;
            return comment;
        }

        public LinearWebhook CreateWebhook(string url, string? teamId = null, bool allPublicTeams = false,
            List<string>? resourceTypes = null, string? label = null)
        {
            var webhookId = $"webhook_{_webhookCounter++:00000000}";
            var webhook = new LinearWebhook
            {
                Id = webhookId,
                Url = url,
                TeamId = teamId,
                AllPublicTeams = allPublicTeams,
                ResourceTypes = resourceTypes ?? new List<string> { "Issue" },
                Label = label,
                Enabled = true,
                Secret = Guid.NewGuid().ToString()
            };

            Webhooks[webhook.Id] = webhook;
            return webhook;
        }

        public bool DeleteWebhook(string webhookId)
        {
            return Webhooks.Remove(webhookId);
        }

        public LinearAttachment CreateAttachment(string issueId, string title, string url, string? subtitle = null)
        {
            var issue = Issues.GetValueOrDefault(issueId);
            if (issue == null)
            {
                issue = Issues.Values.FirstOrDefault(i => i.Identifier == issueId);
            }
            if (issue == null)
                throw new InvalidOperationException($"Issue {issueId} not found");

            var attachmentId = $"attachment_{_attachmentCounter++:00000000}";
            var attachment = new LinearAttachment
            {
                Id = attachmentId,
                Title = title,
                Subtitle = subtitle,
                Url = url,
                IssueId = issue.Id,
                CreatorId = CurrentUserId
            };

            Attachments[attachment.Id] = attachment;
            return attachment;
        }

        public LinearUser? GetCurrentUser()
        {
            return Users.GetValueOrDefault(CurrentUserId);
        }

        public List<LinearWorkflowState> GetWorkflowStatesForTeam(string teamId)
        {
            return WorkflowStates.Values
                .Where(s => s.TeamId == teamId && s.ArchivedAt == null)
                .OrderBy(s => s.Position)
                .ToList();
        }

        public List<LinearIssue> GetIssuesForTeam(string teamId, bool includeArchived = false)
        {
            return Issues.Values
                .Where(i => i.TeamId == teamId && (includeArchived || i.ArchivedAt == null))
                .OrderByDescending(i => i.CreatedAt)
                .ToList();
        }

        public List<LinearComment> GetCommentsForIssue(string issueId)
        {
            var issue = Issues.GetValueOrDefault(issueId);
            if (issue == null)
            {
                issue = Issues.Values.FirstOrDefault(i => i.Identifier == issueId);
            }
            if (issue == null) return new List<LinearComment>();

            return Comments.Values
                .Where(c => c.IssueId == issue.Id && c.ArchivedAt == null)
                .OrderBy(c => c.CreatedAt)
                .ToList();
        }

        private static string ToSlug(string text)
        {
            return text.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("_", "-")
                .Replace(".", "-")
                .Replace("'", "")
                .Replace("\"", "");
        }

        // ===== Additional CRUD Operations =====

        private int _labelCounter = 100;
        private int _cycleCounter = 100;
        private int _teamCounter = 100;

        public bool DeleteIssue(string issueId)
        {
            if (!Issues.TryGetValue(issueId, out var issue))
            {
                issue = Issues.Values.FirstOrDefault(i => i.Identifier == issueId);
                if (issue == null) return false;
                issueId = issue.Id;
            }

            // Remove related comments
            var commentsToRemove = Comments.Values.Where(c => c.IssueId == issueId).ToList();
            foreach (var comment in commentsToRemove)
            {
                Comments.Remove(comment.Id);
            }

            // Remove related attachments
            var attachmentsToRemove = Attachments.Values.Where(a => a.IssueId == issueId).ToList();
            foreach (var attachment in attachmentsToRemove)
            {
                Attachments.Remove(attachment.Id);
            }

            // Decrement team issue count
            if (Teams.TryGetValue(issue!.TeamId, out var team))
            {
                team.IssueCount = Math.Max(0, team.IssueCount - 1);
            }

            return Issues.Remove(issueId);
        }

        public LinearComment? UpdateComment(string commentId, string? body = null)
        {
            if (!Comments.TryGetValue(commentId, out var comment))
                return null;

            if (body != null)
            {
                comment.Body = body;
                comment.Edited = true;
            }
            comment.UpdatedAt = DateTime.UtcNow;
            return comment;
        }

        public bool DeleteComment(string commentId)
        {
            return Comments.Remove(commentId);
        }

        public LinearLabel CreateLabel(string name, string? teamId = null, string? color = null, string? description = null)
        {
            var labelId = $"label_{_labelCounter++:00000000}";
            var label = new LinearLabel
            {
                Id = labelId,
                Name = name,
                TeamId = teamId,
                Color = color ?? "#6B7280",
                Description = description
            };

            Labels[label.Id] = label;
            return label;
        }

        public LinearLabel? UpdateLabel(string labelId, string? name = null, string? color = null, string? description = null)
        {
            if (!Labels.TryGetValue(labelId, out var label))
                return null;

            if (name != null) label.Name = name;
            if (color != null) label.Color = color;
            if (description != null) label.Description = description;
            label.UpdatedAt = DateTime.UtcNow;
            return label;
        }

        public bool DeleteLabel(string labelId)
        {
            // Remove label from all issues
            foreach (var issue in Issues.Values)
            {
                issue.LabelIds.Remove(labelId);
            }
            return Labels.Remove(labelId);
        }

        public bool ArchiveLabel(string labelId)
        {
            if (!Labels.TryGetValue(labelId, out var label))
                return false;

            label.ArchivedAt = DateTime.UtcNow;
            label.UpdatedAt = DateTime.UtcNow;
            return true;
        }

        public LinearCycle CreateCycle(string teamId, string? name = null, DateTime? startsAt = null, DateTime? endsAt = null)
        {
            if (!Teams.TryGetValue(teamId, out var team))
                throw new InvalidOperationException($"Team {teamId} not found");

            var number = Cycles.Values.Count(c => c.TeamId == teamId) + 1;
            var cycleId = $"cycle_{_cycleCounter++:00000000}";

            var cycle = new LinearCycle
            {
                Id = cycleId,
                Name = name ?? $"Sprint {number}",
                Number = number,
                TeamId = teamId,
                StartsAt = startsAt ?? DateTime.UtcNow,
                EndsAt = endsAt ?? DateTime.UtcNow.AddDays(14)
            };

            Cycles[cycle.Id] = cycle;
            return cycle;
        }

        public LinearCycle? UpdateCycle(string cycleId, string? name = null, DateTime? startsAt = null, DateTime? endsAt = null)
        {
            if (!Cycles.TryGetValue(cycleId, out var cycle))
                return null;

            if (name != null) cycle.Name = name;
            if (startsAt != null) cycle.StartsAt = startsAt.Value;
            if (endsAt != null) cycle.EndsAt = endsAt.Value;
            cycle.UpdatedAt = DateTime.UtcNow;
            return cycle;
        }

        public bool ArchiveCycle(string cycleId)
        {
            if (!Cycles.TryGetValue(cycleId, out var cycle))
                return false;

            cycle.CompletedAt = DateTime.UtcNow;
            cycle.UpdatedAt = DateTime.UtcNow;
            return true;
        }

        public LinearTeam CreateTeam(string name, string key, string? description = null)
        {
            var teamId = $"team_{_teamCounter++:00000000}";
            var team = new LinearTeam
            {
                Id = teamId,
                Name = name,
                Key = key.ToUpperInvariant(),
                Description = description,
                MemberIds = new List<string> { CurrentUserId }
            };

            Teams[team.Id] = team;

            // Create default workflow states for the team
            CreateDefaultWorkflowStates(teamId);

            return team;
        }

        public LinearTeam? UpdateTeam(string teamId, string? name = null, string? description = null)
        {
            if (!Teams.TryGetValue(teamId, out var team))
                return null;

            if (name != null) team.Name = name;
            if (description != null) team.Description = description;
            team.UpdatedAt = DateTime.UtcNow;
            return team;
        }

        private void CreateDefaultWorkflowStates(string teamId)
        {
            var stateId = Guid.NewGuid().ToString("N")[..8];
            AddWorkflowState(new LinearWorkflowState { Id = $"state_backlog_{stateId}", Name = "Backlog", Type = "backlog", Color = "#BBBFC7", TeamId = teamId, Position = 0 });
            AddWorkflowState(new LinearWorkflowState { Id = $"state_todo_{stateId}", Name = "Todo", Type = "unstarted", Color = "#E2E2E2", TeamId = teamId, Position = 1 });
            AddWorkflowState(new LinearWorkflowState { Id = $"state_inprogress_{stateId}", Name = "In Progress", Type = "started", Color = "#F2C94C", TeamId = teamId, Position = 2 });
            AddWorkflowState(new LinearWorkflowState { Id = $"state_done_{stateId}", Name = "Done", Type = "completed", Color = "#5E6AD2", TeamId = teamId, Position = 3 });
            AddWorkflowState(new LinearWorkflowState { Id = $"state_canceled_{stateId}", Name = "Canceled", Type = "canceled", Color = "#95A2B3", TeamId = teamId, Position = 4 });
        }

        public bool ArchiveProject(string projectId)
        {
            if (!Projects.TryGetValue(projectId, out var project))
                return false;

            project.ArchivedAt = DateTime.UtcNow;
            project.UpdatedAt = DateTime.UtcNow;
            return true;
        }

        public LinearAttachment? UpdateAttachment(string attachmentId, string? title = null, string? subtitle = null, string? url = null)
        {
            if (!Attachments.TryGetValue(attachmentId, out var attachment))
                return null;

            if (title != null) attachment.Title = title;
            if (subtitle != null) attachment.Subtitle = subtitle;
            if (url != null) attachment.Url = url;
            attachment.UpdatedAt = DateTime.UtcNow;
            return attachment;
        }

        public bool DeleteAttachment(string attachmentId)
        {
            return Attachments.Remove(attachmentId);
        }

        public bool AddLabelToIssue(string issueId, string labelId)
        {
            var issue = Issues.GetValueOrDefault(issueId) ?? Issues.Values.FirstOrDefault(i => i.Identifier == issueId);
            if (issue == null || !Labels.ContainsKey(labelId))
                return false;

            if (!issue.LabelIds.Contains(labelId))
            {
                issue.LabelIds.Add(labelId);
                issue.UpdatedAt = DateTime.UtcNow;
            }
            return true;
        }

        public bool RemoveLabelFromIssue(string issueId, string labelId)
        {
            var issue = Issues.GetValueOrDefault(issueId) ?? Issues.Values.FirstOrDefault(i => i.Identifier == issueId);
            if (issue == null)
                return false;

            var removed = issue.LabelIds.Remove(labelId);
            if (removed)
            {
                issue.UpdatedAt = DateTime.UtcNow;
            }
            return removed;
        }

        public List<LinearIssue> BatchUpdateIssues(List<string> issueIds, string? stateId = null, string? assigneeId = null, 
            string? projectId = null, int? priority = null)
        {
            var updated = new List<LinearIssue>();
            foreach (var issueId in issueIds)
            {
                var issue = UpdateIssue(issueId, stateId: stateId, assigneeId: assigneeId, projectId: projectId, priority: priority);
                if (issue != null)
                {
                    updated.Add(issue);
                }
            }
            return updated;
        }

        public bool UnarchiveIssue(string issueId)
        {
            if (!Issues.TryGetValue(issueId, out var issue))
            {
                issue = Issues.Values.FirstOrDefault(i => i.Identifier == issueId);
            }

            if (issue == null) return false;

            issue.ArchivedAt = null;
            issue.UpdatedAt = DateTime.UtcNow;
            return true;
        }

        public List<LinearAttachment> GetAttachmentsForIssue(string issueId)
        {
            var issue = Issues.GetValueOrDefault(issueId) ?? Issues.Values.FirstOrDefault(i => i.Identifier == issueId);
            if (issue == null) return new List<LinearAttachment>();

            return Attachments.Values
                .Where(a => a.IssueId == issue.Id && a.ArchivedAt == null)
                .OrderByDescending(a => a.CreatedAt)
                .ToList();
        }

        public List<LinearLabel> GetLabelsForTeam(string teamId)
        {
            return Labels.Values
                .Where(l => l.TeamId == teamId && l.ArchivedAt == null)
                .OrderBy(l => l.Name)
                .ToList();
        }

        public List<LinearCycle> GetCyclesForTeam(string teamId)
        {
            return Cycles.Values
                .Where(c => c.TeamId == teamId)
                .OrderByDescending(c => c.StartsAt)
                .ToList();
        }

        // ===== Issue Relation Operations =====

        public LinearIssueRelation CreateIssueRelation(string issueId, string relatedIssueId, string type = "related")
        {
            var issue = Issues.GetValueOrDefault(issueId) ?? Issues.Values.FirstOrDefault(i => i.Identifier == issueId);
            var relatedIssue = Issues.GetValueOrDefault(relatedIssueId) ?? Issues.Values.FirstOrDefault(i => i.Identifier == relatedIssueId);
            
            if (issue == null || relatedIssue == null)
                throw new InvalidOperationException("One or both issues not found");

            var relationId = $"relation_{_relationCounter++:00000000}";
            var relation = new LinearIssueRelation
            {
                Id = relationId,
                Type = type,
                IssueId = issue.Id,
                RelatedIssueId = relatedIssue.Id
            };

            IssueRelations[relation.Id] = relation;
            return relation;
        }

        public bool DeleteIssueRelation(string relationId)
        {
            return IssueRelations.Remove(relationId);
        }

        public List<LinearIssueRelation> GetRelationsForIssue(string issueId)
        {
            var issue = Issues.GetValueOrDefault(issueId) ?? Issues.Values.FirstOrDefault(i => i.Identifier == issueId);
            if (issue == null) return new List<LinearIssueRelation>();

            return IssueRelations.Values
                .Where(r => r.IssueId == issue.Id || r.RelatedIssueId == issue.Id)
                .ToList();
        }

        // ===== Reaction Operations =====

        public LinearReaction CreateReaction(string commentId, string emoji)
        {
            if (!Comments.ContainsKey(commentId))
                throw new InvalidOperationException($"Comment {commentId} not found");

            var reactionId = $"reaction_{_reactionCounter++:00000000}";
            var reaction = new LinearReaction
            {
                Id = reactionId,
                CommentId = commentId,
                Emoji = emoji,
                UserId = CurrentUserId
            };

            Reactions[reaction.Id] = reaction;
            return reaction;
        }

        public bool DeleteReaction(string reactionId)
        {
            return Reactions.Remove(reactionId);
        }

        public List<LinearReaction> GetReactionsForComment(string commentId)
        {
            return Reactions.Values
                .Where(r => r.CommentId == commentId)
                .OrderBy(r => r.CreatedAt)
                .ToList();
        }

        // ===== Notification Operations =====

        public LinearNotification CreateNotification(string userId, string type, string? issueId = null, string? commentId = null)
        {
            var notificationId = $"notification_{_notificationCounter++:00000000}";
            var notification = new LinearNotification
            {
                Id = notificationId,
                UserId = userId,
                Type = type,
                IssueId = issueId,
                CommentId = commentId,
                ActorId = CurrentUserId
            };

            Notifications[notification.Id] = notification;
            return notification;
        }

        public bool MarkNotificationAsRead(string notificationId)
        {
            if (!Notifications.TryGetValue(notificationId, out var notification))
                return false;

            notification.ReadAt = true;
            notification.UpdatedAt = DateTime.UtcNow;
            return true;
        }

        public bool ArchiveNotification(string notificationId)
        {
            if (!Notifications.TryGetValue(notificationId, out var notification))
                return false;

            notification.ArchivedAt = DateTime.UtcNow;
            notification.UpdatedAt = DateTime.UtcNow;
            return true;
        }

        public List<LinearNotification> GetNotificationsForUser(string? userId = null)
        {
            userId ??= CurrentUserId;
            return Notifications.Values
                .Where(n => n.UserId == userId && n.ArchivedAt == null)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
        }

        // ===== Favorite Operations =====

        public LinearFavorite CreateFavorite(string type, string? issueId = null, string? projectId = null, string? cycleId = null, string? labelId = null)
        {
            var favoriteId = $"favorite_{_favoriteCounter++:00000000}";
            var favorite = new LinearFavorite
            {
                Id = favoriteId,
                UserId = CurrentUserId,
                Type = type,
                IssueId = issueId,
                ProjectId = projectId,
                CycleId = cycleId,
                LabelId = labelId,
                SortOrder = Favorites.Values.Count(f => f.UserId == CurrentUserId)
            };

            Favorites[favorite.Id] = favorite;
            return favorite;
        }

        public bool DeleteFavorite(string favoriteId)
        {
            return Favorites.Remove(favoriteId);
        }

        public List<LinearFavorite> GetFavoritesForUser(string? userId = null)
        {
            userId ??= CurrentUserId;
            return Favorites.Values
                .Where(f => f.UserId == userId)
                .OrderBy(f => f.SortOrder)
                .ToList();
        }

        // ===== Document Operations =====

        public LinearDocument CreateDocument(string title, string? projectId = null, string? content = null)
        {
            var documentId = $"document_{_documentCounter++:00000000}";
            var document = new LinearDocument
            {
                Id = documentId,
                Title = title,
                Content = content,
                ProjectId = projectId,
                CreatorId = CurrentUserId,
                Url = $"https://linear.app/{Organization.UrlKey}/document/{documentId}"
            };

            Documents[document.Id] = document;
            return document;
        }

        public LinearDocument? UpdateDocument(string documentId, string? title = null, string? content = null)
        {
            if (!Documents.TryGetValue(documentId, out var document))
                return null;

            if (title != null) document.Title = title;
            if (content != null) document.Content = content;
            document.UpdatedAt = DateTime.UtcNow;
            return document;
        }

        public bool DeleteDocument(string documentId)
        {
            return Documents.Remove(documentId);
        }

        public List<LinearDocument> GetDocumentsForProject(string projectId)
        {
            return Documents.Values
                .Where(d => d.ProjectId == projectId && d.ArchivedAt == null)
                .OrderBy(d => d.SortOrder)
                .ToList();
        }

        // ===== Roadmap Operations =====

        public LinearRoadmap CreateRoadmap(string name, string? description = null)
        {
            var roadmapId = $"roadmap_{_roadmapCounter++:00000000}";
            var roadmap = new LinearRoadmap
            {
                Id = roadmapId,
                Name = name,
                Description = description,
                CreatorId = CurrentUserId,
                OwnerId = CurrentUserId,
                Slug = name.ToLowerInvariant().Replace(" ", "-")
            };

            Roadmaps[roadmap.Id] = roadmap;
            return roadmap;
        }

        public LinearRoadmap? UpdateRoadmap(string roadmapId, string? name = null, string? description = null)
        {
            if (!Roadmaps.TryGetValue(roadmapId, out var roadmap))
                return null;

            if (name != null)
            {
                roadmap.Name = name;
                roadmap.Slug = name.ToLowerInvariant().Replace(" ", "-");
            }
            if (description != null) roadmap.Description = description;
            roadmap.UpdatedAt = DateTime.UtcNow;
            return roadmap;
        }

        public bool ArchiveRoadmap(string roadmapId)
        {
            if (!Roadmaps.TryGetValue(roadmapId, out var roadmap))
                return false;

            roadmap.ArchivedAt = DateTime.UtcNow;
            roadmap.UpdatedAt = DateTime.UtcNow;
            return true;
        }

        // ===== Custom View Operations =====

        public LinearCustomView CreateCustomView(string name, string? teamId = null, string? filterData = null, bool shared = false)
        {
            var viewId = $"view_{_customViewCounter++:00000000}";
            var view = new LinearCustomView
            {
                Id = viewId,
                Name = name,
                TeamId = teamId,
                FilterData = filterData,
                Shared = shared,
                CreatorId = CurrentUserId
            };

            CustomViews[view.Id] = view;
            return view;
        }

        public LinearCustomView? UpdateCustomView(string viewId, string? name = null, string? filterData = null, bool? shared = null)
        {
            if (!CustomViews.TryGetValue(viewId, out var view))
                return null;

            if (name != null) view.Name = name;
            if (filterData != null) view.FilterData = filterData;
            if (shared != null) view.Shared = shared.Value;
            view.UpdatedAt = DateTime.UtcNow;
            return view;
        }

        public bool DeleteCustomView(string viewId)
        {
            return CustomViews.Remove(viewId);
        }

        public List<LinearCustomView> GetCustomViewsForTeam(string? teamId = null)
        {
            return CustomViews.Values
                .Where(v => v.TeamId == teamId)
                .OrderBy(v => v.Name)
                .ToList();
        }

        // ===== Issue Subscriber Operations =====

        public bool SubscribeToIssue(string issueId, string? userId = null)
        {
            userId ??= CurrentUserId;
            var issue = Issues.GetValueOrDefault(issueId) ?? Issues.Values.FirstOrDefault(i => i.Identifier == issueId);
            if (issue == null) return false;

            issue.SubscriberIds ??= new List<string>();
            if (!issue.SubscriberIds.Contains(userId))
            {
                issue.SubscriberIds.Add(userId);
                issue.UpdatedAt = DateTime.UtcNow;
            }
            return true;
        }

        public bool UnsubscribeFromIssue(string issueId, string? userId = null)
        {
            userId ??= CurrentUserId;
            var issue = Issues.GetValueOrDefault(issueId) ?? Issues.Values.FirstOrDefault(i => i.Identifier == issueId);
            if (issue == null || issue.SubscriberIds == null) return false;

            var removed = issue.SubscriberIds.Remove(userId);
            if (removed) issue.UpdatedAt = DateTime.UtcNow;
            return removed;
        }

        // ===== Search Operations =====

        public List<LinearIssue> SearchIssues(string? query = null, string? teamId = null, string? stateId = null, 
            string? assigneeId = null, string? projectId = null, string? labelId = null, bool includeArchived = false)
        {
            var results = Issues.Values.AsEnumerable();

            if (!includeArchived)
                results = results.Where(i => i.ArchivedAt == null);

            if (!string.IsNullOrEmpty(query))
                results = results.Where(i => 
                    (i.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (i.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (i.Identifier?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false));

            if (!string.IsNullOrEmpty(teamId))
                results = results.Where(i => i.TeamId == teamId);

            if (!string.IsNullOrEmpty(stateId))
                results = results.Where(i => i.StateId == stateId);

            if (!string.IsNullOrEmpty(assigneeId))
                results = results.Where(i => i.AssigneeId == assigneeId);

            if (!string.IsNullOrEmpty(projectId))
                results = results.Where(i => i.ProjectId == projectId);

            if (!string.IsNullOrEmpty(labelId))
                results = results.Where(i => i.LabelIds.Contains(labelId));

            return results.OrderByDescending(i => i.UpdatedAt).ToList();
        }

        // ===== Issue Parent/Child Operations =====

        public bool SetIssueParent(string issueId, string? parentId)
        {
            var issue = Issues.GetValueOrDefault(issueId) ?? Issues.Values.FirstOrDefault(i => i.Identifier == issueId);
            if (issue == null) return false;

            if (parentId != null)
            {
                var parent = Issues.GetValueOrDefault(parentId) ?? Issues.Values.FirstOrDefault(i => i.Identifier == parentId);
                if (parent == null) return false;
                issue.ParentId = parent.Id;
            }
            else
            {
                issue.ParentId = null;
            }

            issue.UpdatedAt = DateTime.UtcNow;
            return true;
        }

        public List<LinearIssue> GetChildIssues(string issueId)
        {
            var issue = Issues.GetValueOrDefault(issueId) ?? Issues.Values.FirstOrDefault(i => i.Identifier == issueId);
            if (issue == null) return new List<LinearIssue>();

            return Issues.Values
                .Where(i => i.ParentId == issue.Id && i.ArchivedAt == null)
                .OrderBy(i => i.SortOrder)
                .ToList();
        }
    }
}
