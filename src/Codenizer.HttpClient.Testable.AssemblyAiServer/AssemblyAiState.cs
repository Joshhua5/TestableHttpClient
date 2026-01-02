using System.Collections.Concurrent;
using Codenizer.HttpClient.Testable.AssemblyAiServer.Models;

namespace Codenizer.HttpClient.Testable.AssemblyAiServer
{
    public class AssemblyAiState
    {
        public ConcurrentDictionary<string, Transcript> Transcripts { get; } = new();
        public ConcurrentDictionary<string, string> Uploads { get; } = new();
        
    }
}
