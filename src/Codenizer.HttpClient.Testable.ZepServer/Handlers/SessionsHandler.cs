using System;
using System.Collections.Generic;
using System.Linq;
using Codenizer.HttpClient.Testable.ZepServer.Models;

namespace Codenizer.HttpClient.Testable.ZepServer.Handlers
{
    public class SessionsHandler
    {
        private readonly ZepState _state;

        public SessionsHandler(ZepState state)
        {
            _state = state;
        }

        public List<Session> List()
        {
            return _state.Sessions.Values.ToList();
        }

        public Session Get(string sessionId)
        {
            if (_state.Sessions.TryGetValue(sessionId, out var session))
            {
                return session;
            }
            return null;
        }

        public Session Create(Session session)
        {
            if (string.IsNullOrEmpty(session.SessionId))
            {
                // In a real scenario, we might error or generate one, but assuming client provides one or we generate
                session.SessionId = Guid.NewGuid().ToString();
            }

            session.CreatedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;

            _state.Sessions.AddOrUpdate(session.SessionId, session, (key, existing) => session);
            return session;
        }

        public Session Update(string sessionId, Session session)
        {
            if (_state.Sessions.TryGetValue(sessionId, out var existingSession))
            {
                existingSession.UpdatedAt = DateTime.UtcNow;
                existingSession.Metadata = session.Metadata ?? existingSession.Metadata;
                // Only update fields that are allowed to be updated. 
                // For now, simple replace or update provided fields.
                
                return existingSession;
            }
            return null;
        }
    }
}
