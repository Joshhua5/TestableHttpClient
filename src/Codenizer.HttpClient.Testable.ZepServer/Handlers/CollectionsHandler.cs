using System;
using System.Collections.Generic;
using System.Linq;
using Codenizer.HttpClient.Testable.ZepServer.Models;

namespace Codenizer.HttpClient.Testable.ZepServer.Handlers
{
    public class CollectionsHandler
    {
        private readonly ZepState _state;

        public CollectionsHandler(ZepState state)
        {
            _state = state;
        }

        public List<DocumentCollection> List()
        {
            return _state.Collections.Values.ToList();
        }

        public DocumentCollection Get(string name)
        {
            if (_state.Collections.TryGetValue(name, out var collection))
            {
                return collection;
            }
            return null;
        }

        public DocumentCollection Create(string name, DocumentCollection collection)
        {
            collection.Name = name;
            collection.CreatedAt = DateTime.UtcNow;
            
            _state.Collections.AddOrUpdate(name, collection, (key, existing) => collection);
            return collection;
        }

        public DocumentCollection Update(string name, DocumentCollection collection)
        {
             if (_state.Collections.TryGetValue(name, out var existing))
            {
                existing.Description = collection.Description;
                existing.Metadata = collection.Metadata ?? existing.Metadata;
                existing.UpdatedAt = DateTime.UtcNow;
                return existing;
            }
            return null;
        }

        public bool Delete(string name)
        {
            return _state.Collections.TryRemove(name, out _);
        }
    }
}
