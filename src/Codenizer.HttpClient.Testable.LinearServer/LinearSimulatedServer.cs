using System.Net;
using System.Net.Http;
using System.Web;
using Codenizer.HttpClient.Testable.LinearServer.Handlers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Codenizer.HttpClient.Testable.LinearServer
{
    /// <summary>
    /// A simulated Linear API server implementing ISimulatedServer.
    /// Provides stateful mock responses for Linear's GraphQL API.
    /// </summary>
    public class LinearSimulatedServer : ISimulatedServer
    {
        private readonly LinearState _state;
        private readonly GraphQLHandler _graphQLHandler;
        private readonly OAuthHandler _oAuthHandler;

        private readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ"
        };

        public LinearSimulatedServer() : this(new LinearState())
        {
        }

        public LinearSimulatedServer(LinearState state)
        {
            _state = state;
            _graphQLHandler = new GraphQLHandler(_state);
            _oAuthHandler = new OAuthHandler(_state);
        }

        /// <summary>
        /// Gets the internal state for testing purposes.
        /// </summary>
        public LinearState State => _state;

        /// <summary>
        /// Gets or sets the required token for authentication.
        /// If null, authentication is not enforced.
        /// Use 'lin_api_' prefix for API keys or 'lin_oauth_' for OAuth tokens.
        /// </summary>
        public string? RequiredToken { get; set; }

        /// <summary>
        /// Gets or sets whether to enforce authentication.
        /// When true, requests must have a valid Authorization header.
        /// </summary>
        public bool EnforceAuthentication { get; set; }

        public async Task<HttpResponseMessage> HandleRequestAsync(HttpRequestMessage request)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            var method = request.Method;

            // Handle OAuth endpoints (no auth required for token endpoints)
            if (path.StartsWith("/oauth/"))
            {
                return await HandleOAuthRequest(request, path);
            }

            // Validate authentication for GraphQL endpoint
            if (EnforceAuthentication || RequiredToken != null)
            {
                var authHeader = request.Headers.Authorization;
                if (authHeader == null || authHeader.Scheme != "Bearer")
                {
                    return CreateErrorResponse(HttpStatusCode.Unauthorized, "Unauthorized", 
                        "Authentication required. Please provide a valid access token.");
                }

                var token = authHeader.Parameter ?? "";
                
                if (RequiredToken != null && token != RequiredToken)
                {
                    return CreateErrorResponse(HttpStatusCode.Unauthorized, "Unauthorized", 
                        "Invalid access token.");
                }

                if (EnforceAuthentication && !_oAuthHandler.ValidateToken(token))
                {
                    return CreateErrorResponse(HttpStatusCode.Unauthorized, "Unauthorized", 
                        "Invalid access token.");
                }
            }

            // Handle GraphQL endpoint
            if (path == "/graphql" && method == HttpMethod.Post)
            {
                return await HandleGraphQLRequest(request);
            }

            // Unknown endpoint
            return CreateErrorResponse(HttpStatusCode.NotFound, "Not Found", 
                $"The endpoint {path} is not supported.");
        }

        private async Task<HttpResponseMessage> HandleGraphQLRequest(HttpRequestMessage request)
        {
            if (request.Content == null)
            {
                return CreateGraphQLErrorResponse("No content provided");
            }

            var content = await request.Content.ReadAsStringAsync();
            
            try
            {
                var requestObj = JsonConvert.DeserializeObject<JObject>(content);
                if (requestObj == null)
                {
                    return CreateGraphQLErrorResponse("Invalid JSON");
                }

                var query = requestObj["query"]?.ToString() ?? "";
                var variables = requestObj["variables"] as JObject;

                if (string.IsNullOrWhiteSpace(query))
                {
                    return CreateGraphQLErrorResponse("No query provided");
                }

                var result = _graphQLHandler.HandleRequest(query, variables);
                var json = JsonConvert.SerializeObject(result, _jsonSettings);

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            }
            catch (JsonException ex)
            {
                return CreateGraphQLErrorResponse($"Invalid JSON: {ex.Message}");
            }
        }

        private async Task<HttpResponseMessage> HandleOAuthRequest(HttpRequestMessage request, string path)
        {
            Dictionary<string, string> formData = new();

            if (request.Content != null)
            {
                var content = await request.Content.ReadAsStringAsync();
                var contentType = request.Content.Headers.ContentType?.MediaType ?? "";

                if (contentType == "application/x-www-form-urlencoded")
                {
                    formData = ParseFormData(content);
                }
                else if (contentType == "application/json")
                {
                    try
                    {
                        formData = JsonConvert.DeserializeObject<Dictionary<string, string>>(content) ?? new();
                    }
                    catch
                    {
                        // Ignore JSON parse errors
                    }
                }
            }

            object result;

            if (path == "/oauth/token")
            {
                result = _oAuthHandler.HandleTokenRequest(formData);
            }
            else if (path == "/oauth/revoke")
            {
                var bearerToken = request.Headers.Authorization?.Parameter;
                result = _oAuthHandler.HandleRevokeRequest(formData, bearerToken);
            }
            else
            {
                return CreateErrorResponse(HttpStatusCode.NotFound, "Not Found", 
                    $"OAuth endpoint {path} is not supported.");
            }

            var json = JsonConvert.SerializeObject(result, _jsonSettings);
            
            // Check if error response
            var resultObj = result as dynamic;
            var statusCode = HttpStatusCode.OK;
            try
            {
                if (resultObj != null && !string.IsNullOrEmpty((string?)GetPropertyValue(resultObj, "error")))
                {
                    statusCode = HttpStatusCode.BadRequest;
                }
            }
            catch
            {
                // Ignore reflection errors
            }

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        }

        private HttpResponseMessage CreateErrorResponse(HttpStatusCode statusCode, string error, string? description = null)
        {
            var result = new { error, error_description = description };
            var json = JsonConvert.SerializeObject(result, _jsonSettings);
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        }

        private HttpResponseMessage CreateGraphQLErrorResponse(string message)
        {
            var result = new
            {
                errors = new[]
                {
                    new { message, extensions = new { code = "INTERNAL_SERVER_ERROR" } }
                }
            };
            var json = JsonConvert.SerializeObject(result, _jsonSettings);
            return new HttpResponseMessage(HttpStatusCode.OK) // GraphQL returns 200 even for errors
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        }

        private static Dictionary<string, string> ParseFormData(string data)
        {
            var result = new Dictionary<string, string>();
            foreach (var pair in data.Split('&'))
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2)
                {
                    result[HttpUtility.UrlDecode(parts[0])] = HttpUtility.UrlDecode(parts[1]);
                }
            }
            return result;
        }

        private static string? GetPropertyValue(object obj, string propertyName)
        {
            var property = obj.GetType().GetProperty(propertyName);
            return property?.GetValue(obj)?.ToString();
        }
    }
}
