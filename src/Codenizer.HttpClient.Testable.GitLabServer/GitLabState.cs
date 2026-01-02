using System.Collections.Concurrent;
using System.Collections.Generic;
using Codenizer.HttpClient.Testable.GitLabServer.Models;

namespace Codenizer.HttpClient.Testable.GitLabServer
{
    public class GitLabState
    {
        public ConcurrentBag<GitLabProject> Projects { get; } = new();
        public ConcurrentBag<GitLabIssue> Issues { get; } = new();
        public ConcurrentBag<GitLabUser> Users { get; } = new();
        public ConcurrentBag<GitLabGroup> Groups { get; } = new();
        public ConcurrentBag<GitLabPipeline> Pipelines { get; } = new();
        public ConcurrentBag<GitLabMergeRequest> MergeRequests { get; } = new();

        public GitLabState()
        {
            // Seed some data if needed, or keep empty
        }

        public void Clear()
        {
            // ConcurrentBag doesn't have Clear(), so we'd need to re-initialize or empty it.
            // For simulation purposes, creating a new server usually resets state, 
            // but if we need a Clear method we can implement it by pulling everything out.
            while (!Projects.IsEmpty) Projects.TryTake(out _);
            while (!Issues.IsEmpty) Issues.TryTake(out _);
            while (!Users.IsEmpty) Users.TryTake(out _);
            while (!Groups.IsEmpty) Groups.TryTake(out _);
            while (!Pipelines.IsEmpty) Pipelines.TryTake(out _);
            while (!MergeRequests.IsEmpty) MergeRequests.TryTake(out _);
        }
    }
}
