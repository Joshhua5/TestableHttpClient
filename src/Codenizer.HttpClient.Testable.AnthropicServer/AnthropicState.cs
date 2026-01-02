using System.Collections.Concurrent;
using Codenizer.HttpClient.Testable.AnthropicServer.Models;

namespace Codenizer.HttpClient.Testable.AnthropicServer
{
    public class AnthropicState
    {
        public ConcurrentDictionary<string, MessageBatch> Batches { get; } = new();
    }
}
