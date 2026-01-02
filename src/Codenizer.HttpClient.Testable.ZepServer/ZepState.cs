using System.Collections.Concurrent;
using System.Collections.Generic;
using Codenizer.HttpClient.Testable.ZepServer.Models;

namespace Codenizer.HttpClient.Testable.ZepServer
{
    public class ZepState
    {
        public ConcurrentDictionary<string, Session> Sessions { get; } = new();
        public ConcurrentDictionary<string, List<Message>> SessionMessages { get; } = new();
        public ConcurrentDictionary<string, Memory> SessionMemory { get; } = new();
        public ConcurrentDictionary<string, DocumentCollection> Collections { get; } = new();

        public void Reset()
        {
            Sessions.Clear();
            SessionMessages.Clear();
            SessionMemory.Clear();
            Collections.Clear();
        }
    }
}
