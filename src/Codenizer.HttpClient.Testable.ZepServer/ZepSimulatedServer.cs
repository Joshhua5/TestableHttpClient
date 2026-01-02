using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Codenizer.HttpClient.Testable.ZepServer.Handlers;
using Codenizer.HttpClient.Testable.ZepServer.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Codenizer.HttpClient.Testable.ZepServer
{
    public class ZepSimulatedServer : ISimulatedServer
    {
        private readonly ZepState _state;
        private readonly SessionsHandler _sessionsHandler;
        private readonly CollectionsHandler _collectionsHandler;
        private readonly MemoryHandler _memoryHandler;

        private readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            },
            NullValueHandling = NullValueHandling.Ignore
        };

        public ZepSimulatedServer() : this(new ZepState())
        {
        }

        public ZepSimulatedServer(ZepState state)
        {
            _state = state;
            _sessionsHandler = new SessionsHandler(_state);
            _collectionsHandler = new CollectionsHandler(_state);
            _memoryHandler = new MemoryHandler(_state);
        }

        public string? ApiKey { get; set; }

        public async Task<HttpResponseMessage> HandleRequestAsync(HttpRequestMessage request)
        {
             // Auth check (simple existence if ApiKey set)
            if (ApiKey != null)
            {
                if (!request.Headers.TryGetValues("Authorization", out var values) || !System.Linq.Enumerable.Any(values, v => v.Contains(ApiKey)))
                {
                    // Zep uses Bearer token usually, but check header
                    // actually Zep might use Api-Key header or Bearer.
                    // Let's assume Bearer or just simple check for now based on other servers.
                    // The other one used x-api-key.
                    // Zep docs say "Authorization: Bearer <token>"
                     if (!request.Headers.TryGetValues("Authorization", out var authValues) || !System.Linq.Enumerable.Any(authValues, v => v.Contains(ApiKey)))
                     {
                          return CreateErrorResponse(HttpStatusCode.Unauthorized, "authentication_error", "Invalid API Key");
                     }
                }
            }

            var path = request.RequestUri?.AbsolutePath ?? "";
            
            // Normalize path to remove /api/v1
            if (path.StartsWith("/api/v1/"))
            {
                path = path.Substring(8); 
            }
            else if (path.StartsWith("api/v1/"))
            {
                path = path.Substring(7);
            }
            
            path = path.TrimStart('/');
            var method = request.Method;

            // Sessions
            if (path == "sessions" && method == HttpMethod.Post)
            {
                var content = await request.Content!.ReadAsStringAsync();
                var session = JsonConvert.DeserializeObject<Session>(content);
                var created = _sessionsHandler.Create(session);
                return CreateResponse(created);
            }

            if (path == "sessions" && method == HttpMethod.Get)
            {
                var list = _sessionsHandler.List();
                return CreateResponse(list);
            }

            if (path.StartsWith("sessions/") && !path.Contains("/memory"))
            {
                var sessionId = path.Substring(9); // "sessions/".Length
                if (method == HttpMethod.Get)
                {
                    var session = _sessionsHandler.Get(sessionId);
                    if (session == null) return CreateErrorResponse(HttpStatusCode.NotFound, "not_found", "Session not found");
                    return CreateResponse(session);
                }
                if (method == HttpMethod.Patch)
                {
                    var content = await request.Content!.ReadAsStringAsync();
                    var session = JsonConvert.DeserializeObject<Session>(content);
                    var updated = _sessionsHandler.Update(sessionId, session);
                     if (updated == null) return CreateErrorResponse(HttpStatusCode.NotFound, "not_found", "Session not found");
                    return CreateResponse(updated);
                }
            }
            
            // Memory
            if (path.StartsWith("sessions/") && path.EndsWith("/memory"))
            {
                 var parts = path.Split('/');
                 if (parts.Length == 3 && parts[2] == "memory")
                 {
                     var sessionId = parts[1];
                     
                     if (method == HttpMethod.Post)
                     {
                        var content = await request.Content!.ReadAsStringAsync();
                        var memory = JsonConvert.DeserializeObject<Memory>(content);
                        var result = _memoryHandler.Add(sessionId, memory);
                        return CreateResponse(new { message = result });
                     }
                     
                     if (method == HttpMethod.Get)
                     {
                         var memory = _memoryHandler.Get(sessionId);
                         return CreateResponse(memory ?? new Memory()); // Return empty memory if none
                     }
                     
                      if (method == HttpMethod.Delete)
                     {
                         var result = _memoryHandler.Delete(sessionId);
                         return CreateResponse(new { message = result });
                     }
                 }
            }

            // Collections
            if (path == "collection" && method == HttpMethod.Get)
            {
                var list = _collectionsHandler.List();
                return CreateResponse(list);
            }
            
            if (path.StartsWith("collection/") && !path.Contains("/document") && !path.Contains("/search") && !path.Contains("/index"))
            {
                var name = path.Substring(11); // "collection/".Length
                
                if (method == HttpMethod.Post) 
                {
                     // Create collection
                    var content = await request.Content!.ReadAsStringAsync();
                    var collection = JsonConvert.DeserializeObject<DocumentCollection>(content);
                    var created = _collectionsHandler.Create(name, collection);
                    return CreateResponse(created);
                }

                if (method == HttpMethod.Get)
                {
                    var collection = _collectionsHandler.Get(name);
                    if (collection == null) return CreateErrorResponse(HttpStatusCode.NotFound, "not_found", "Collection not found");
                    return CreateResponse(collection);
                }
                
                if (method == HttpMethod.Delete)
                {
                    var deleted = _collectionsHandler.Delete(name);
                     if (!deleted) return CreateErrorResponse(HttpStatusCode.NotFound, "not_found", "Collection not found");
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }

                 if (method == HttpMethod.Patch)
                {
                    var content = await request.Content!.ReadAsStringAsync();
                    var collection = JsonConvert.DeserializeObject<DocumentCollection>(content);
                    var updated = _collectionsHandler.Update(name, collection);
                    if (updated == null) return CreateErrorResponse(HttpStatusCode.NotFound, "not_found", "Collection not found");
                    return CreateResponse(updated);
                }
            }

            return CreateErrorResponse(HttpStatusCode.NotFound, "not_found_error", "Resource not found");
        }

        private HttpResponseMessage CreateResponse(object? content)
        {
            var json = JsonConvert.SerializeObject(content, _jsonSettings);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        private HttpResponseMessage CreateErrorResponse(HttpStatusCode statusCode, string type, string message)
        {
            var error = new
            {
                type = "error",
                error = new
                {
                    type,
                    message
                }
            };
            var json = JsonConvert.SerializeObject(error, _jsonSettings);
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
    }
}
