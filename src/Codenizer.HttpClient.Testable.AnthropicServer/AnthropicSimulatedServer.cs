using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Codenizer.HttpClient.Testable.AnthropicServer.Handlers;
using Codenizer.HttpClient.Testable.AnthropicServer.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Codenizer.HttpClient.Testable.AnthropicServer
{
    public class AnthropicSimulatedServer : ISimulatedServer
    {
        private readonly AnthropicState _state;
        private readonly MessagesHandler _messagesHandler;
        private readonly ModelsHandler _modelsHandler;
        private readonly BatchesHandler _batchesHandler;

        private readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            },
            NullValueHandling = NullValueHandling.Ignore
        };

        public AnthropicSimulatedServer() : this(new AnthropicState())
        {
        }

        public AnthropicSimulatedServer(AnthropicState state)
        {
            _state = state;
            _messagesHandler = new MessagesHandler();
            _modelsHandler = new ModelsHandler();
            _batchesHandler = new BatchesHandler(_state);
        }

        public string? ApiKey { get; set; }

        public async Task<HttpResponseMessage> HandleRequestAsync(HttpRequestMessage request)
        {
            if (ApiKey != null)
            {
                if (!request.Headers.TryGetValues("x-api-key", out var values) || !System.Linq.Enumerable.Contains(values, ApiKey))
                {
                    return CreateErrorResponse(HttpStatusCode.Unauthorized, "authentication_error", "Invalid API Key");
                }
            }

            var path = request.RequestUri?.AbsolutePath ?? "";
            // Normalize path
            if (path.StartsWith("/v1/"))
            {
                path = path.Substring(3); // Remove /v1
            }
            path = path.TrimStart('/');

            var method = request.Method;

            if (path == "messages" && method == HttpMethod.Post)
            {
                var content = await request.Content!.ReadAsStringAsync();
                var messageRequest = JsonConvert.DeserializeObject<MessageRequest>(content);
                var response = _messagesHandler.Create(messageRequest);
                return CreateResponse(response);
            }
            
            if (path == "models" && method == HttpMethod.Get)
            {
                var response = _modelsHandler.List();
                return CreateResponse(response);
            }

            if (path == "messages/count_tokens" && method == HttpMethod.Post)
            {
                return TokenCountingHandler.Handle(request);
            }

            if (path == "messages/batches" && method == HttpMethod.Post)
            {
                var content = await request.Content!.ReadAsStringAsync();
                var batchRequest = JsonConvert.DeserializeObject<CreateMessageBatchRequest>(content);
                var response = _batchesHandler.Create(batchRequest);
                return CreateResponse(response);
            }

            if (path.StartsWith("messages/batches/") && path.EndsWith("/cancel") && method == HttpMethod.Post)
            {
                var id = path.Replace("messages/batches/", "").Replace("/cancel", "");
                var response = _batchesHandler.Cancel(id);
                if (response == null) return CreateErrorResponse(HttpStatusCode.NotFound, "not_found_error", "Batch not found");
                return CreateResponse(response);
            }

            if (path.StartsWith("messages/batches/") && method == HttpMethod.Get)
            {
                var id = path.Replace("messages/batches/", "");
                var response = _batchesHandler.Retrieve(id);
                if (response == null) return CreateErrorResponse(HttpStatusCode.NotFound, "not_found_error", "Batch not found");
                return CreateResponse(response);
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
