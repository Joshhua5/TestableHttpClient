using System;
using System.Collections.Generic;
using Codenizer.HttpClient.Testable.ZepServer.Models;

namespace Codenizer.HttpClient.Testable.ZepServer.Handlers
{
    public class MemoryHandler
    {
        private readonly ZepState _state;

        public MemoryHandler(ZepState state)
        {
            _state = state;
        }

        public Memory Get(string sessionId)
        {
            if (_state.SessionMemory.TryGetValue(sessionId, out var memory))
            {
                return memory;
            }
            return null;
        }

        public string Add(string sessionId, Memory memory)
        {
            // Update or create memory for the session
            // In Zep, adding memory usually means adding messages and potentially updating summary
            
            _state.SessionMemory.AddOrUpdate(sessionId, memory, (key, existing) => 
            {
                existing.Messages.AddRange(memory.Messages);
                existing.Summary = memory.Summary ?? existing.Summary;
                existing.Metadata = memory.Metadata ?? existing.Metadata;
                return existing;
            });

            return "Memory added";
        }

        public string Delete(string sessionId)
        {
            _state.SessionMemory.TryRemove(sessionId, out _);
            return "Memory deleted";
        }
    }
}
