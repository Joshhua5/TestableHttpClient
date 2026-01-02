using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using Codenizer.HttpClient.Testable.AirtableServer.Handlers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Codenizer.HttpClient.Testable.AirtableServer
{
    /// <summary>
    /// A simulated Airtable API server implementing ISimulatedServer.
    /// Provides stateful mock responses for the Airtable Web API.
    /// </summary>
    public class AirtableSimulatedServer : ISimulatedServer
    {
        private readonly AirtableState _state;
        private readonly RecordsHandler _recordsHandler;
        private readonly TablesHandler _tablesHandler;
        private readonly FieldsHandler _fieldsHandler;
        private readonly BasesHandler _basesHandler;
        private readonly ViewsHandler _viewsHandler;
        private readonly WebhooksHandler _webhooksHandler;
        private readonly CommentsHandler _commentsHandler;
        private readonly UserHandler _userHandler;

        private readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None
        };

        // URL patterns for routing
        private static readonly Regex RecordPattern = new(@"^/v0/([^/]+)/([^/]+)/([^/]+)$", RegexOptions.Compiled);
        private static readonly Regex TableRecordsPattern = new(@"^/v0/([^/]+)/([^/]+)$", RegexOptions.Compiled);
        private static readonly Regex CommentPattern = new(@"^/v0/([^/]+)/([^/]+)/([^/]+)/comments/([^/]+)$", RegexOptions.Compiled);
        private static readonly Regex CommentsPattern = new(@"^/v0/([^/]+)/([^/]+)/([^/]+)/comments$", RegexOptions.Compiled);
        private static readonly Regex MetaBasesPattern = new(@"^/v0/meta/bases$", RegexOptions.Compiled);
        private static readonly Regex MetaBasePattern = new(@"^/v0/meta/bases/([^/]+)$", RegexOptions.Compiled);
        private static readonly Regex MetaTablesPattern = new(@"^/v0/meta/bases/([^/]+)/tables$", RegexOptions.Compiled);
        private static readonly Regex MetaTablePattern = new(@"^/v0/meta/bases/([^/]+)/tables/([^/]+)$", RegexOptions.Compiled);
        private static readonly Regex MetaFieldsPattern = new(@"^/v0/meta/bases/([^/]+)/tables/([^/]+)/fields$", RegexOptions.Compiled);
        private static readonly Regex MetaFieldPattern = new(@"^/v0/meta/bases/([^/]+)/tables/([^/]+)/fields/([^/]+)$", RegexOptions.Compiled);
        private static readonly Regex MetaViewsPattern = new(@"^/v0/meta/bases/([^/]+)/views$", RegexOptions.Compiled);
        private static readonly Regex MetaViewPattern = new(@"^/v0/meta/bases/([^/]+)/views/([^/]+)$", RegexOptions.Compiled);
        private static readonly Regex WebhooksPattern = new(@"^/v0/bases/([^/]+)/webhooks$", RegexOptions.Compiled);
        private static readonly Regex WebhookPattern = new(@"^/v0/bases/([^/]+)/webhooks/([^/]+)$", RegexOptions.Compiled);
        private static readonly Regex WebhookPayloadsPattern = new(@"^/v0/bases/([^/]+)/webhooks/([^/]+)/payloads$", RegexOptions.Compiled);
        private static readonly Regex WhoamiPattern = new(@"^/v0/meta/whoami$", RegexOptions.Compiled);

        public AirtableSimulatedServer() : this(new AirtableState())
        {
        }

        public AirtableSimulatedServer(AirtableState state)
        {
            _state = state;
            _recordsHandler = new RecordsHandler(_state);
            _tablesHandler = new TablesHandler(_state);
            _fieldsHandler = new FieldsHandler(_state);
            _basesHandler = new BasesHandler(_state);
            _viewsHandler = new ViewsHandler(_state);
            _webhooksHandler = new WebhooksHandler(_state);
            _commentsHandler = new CommentsHandler(_state);
            _userHandler = new UserHandler(_state);
        }

        /// <summary>
        /// Gets the internal state for testing purposes.
        /// </summary>
        public AirtableState State => _state;

        /// <summary>
        /// Gets or sets the required token for authentication.
        /// If null, authentication is not enforced.
        /// </summary>
        public string? RequiredToken { get; set; }

        public async Task<HttpResponseMessage> HandleRequestAsync(HttpRequestMessage request)
        {
            // Validate authentication if required
            if (RequiredToken != null)
            {
                var authHeader = request.Headers.Authorization;
                if (authHeader == null || authHeader.Scheme != "Bearer" || authHeader.Parameter != RequiredToken)
                {
                    return CreateErrorResponse(HttpStatusCode.Unauthorized, "AUTHENTICATION_REQUIRED", "Authentication required");
                }
            }

            var path = request.RequestUri?.AbsolutePath ?? "";
            var method = request.Method;

            // Parse query parameters
            var queryParams = ParseQueryString(request.RequestUri?.Query ?? "");

            // Parse request body if present
            JObject? body = null;
            if (request.Content != null && (method == HttpMethod.Post || method == HttpMethod.Patch || method == HttpMethod.Put))
            {
                var content = await request.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(content))
                {
                    try
                    {
                        body = JObject.Parse(content);
                    }
                    catch (JsonException)
                    {
                        return CreateErrorResponse(HttpStatusCode.BadRequest, "INVALID_REQUEST_BODY", "Invalid JSON in request body");
                    }
                }
            }

            // Route the request
            object? result = RouteRequest(method, path, queryParams, body);

            // Check if the result contains an error
            if (result is JObject resultObj && resultObj.ContainsKey("error"))
            {
                var errorType = resultObj["error"]?["type"]?.ToString() ?? "UNKNOWN_ERROR";
                var statusCode = GetStatusCodeForError(errorType);
                return CreateJsonResponse(statusCode, result);
            }

            // Check if anonymously typed object has error
            var resultJson = JsonConvert.SerializeObject(result, _jsonSettings);
            if (resultJson.Contains("\"error\":{"))
            {
                var parsedResult = JObject.Parse(resultJson);
                var errorType = parsedResult["error"]?["type"]?.ToString() ?? "UNKNOWN_ERROR";
                var statusCode = GetStatusCodeForError(errorType);
                return CreateJsonResponse(statusCode, result);
            }

            return CreateJsonResponse(HttpStatusCode.OK, result);
        }

        private object? RouteRequest(HttpMethod method, string path, Dictionary<string, string> queryParams, JObject? body)
        {
            Match match;

            // Whoami
            if (WhoamiPattern.IsMatch(path) && method == HttpMethod.Get)
            {
                return _userHandler.WhoAmI();
            }

            // Meta: List bases
            if (MetaBasesPattern.IsMatch(path) && method == HttpMethod.Get)
            {
                return _basesHandler.List(queryParams);
            }

            // Meta: Get base metadata
            match = MetaBasePattern.Match(path);
            if (match.Success && method == HttpMethod.Get)
            {
                return _basesHandler.GetMetadata(match.Groups[1].Value);
            }

            // Meta: Tables (list/create)
            match = MetaTablesPattern.Match(path);
            if (match.Success)
            {
                var baseId = match.Groups[1].Value;
                return method.Method switch
                {
                    "GET" => _tablesHandler.List(baseId),
                    "POST" => _tablesHandler.Create(baseId, body ?? new JObject()),
                    _ => CreateUnknownMethodResult()
                };
            }

            // Meta: Single table (update)
            match = MetaTablePattern.Match(path);
            if (match.Success)
            {
                var baseId = match.Groups[1].Value;
                var tableIdOrName = match.Groups[2].Value;
                return method.Method switch
                {
                    "PATCH" => _tablesHandler.Update(baseId, tableIdOrName, body ?? new JObject()),
                    _ => CreateUnknownMethodResult()
                };
            }

            // Meta: Fields (create)
            match = MetaFieldsPattern.Match(path);
            if (match.Success && method == HttpMethod.Post)
            {
                var baseId = match.Groups[1].Value;
                var tableIdOrName = match.Groups[2].Value;
                return _fieldsHandler.Create(baseId, tableIdOrName, body ?? new JObject());
            }

            // Meta: Single field (update)
            match = MetaFieldPattern.Match(path);
            if (match.Success && method == HttpMethod.Patch)
            {
                var baseId = match.Groups[1].Value;
                var tableIdOrName = match.Groups[2].Value;
                var fieldIdOrName = match.Groups[3].Value;
                return _fieldsHandler.Update(baseId, tableIdOrName, fieldIdOrName, body ?? new JObject());
            }

            // Meta: Views (list)
            match = MetaViewsPattern.Match(path);
            if (match.Success && method == HttpMethod.Get)
            {
                return _viewsHandler.List(match.Groups[1].Value);
            }

            // Meta: Single view (get/delete)
            match = MetaViewPattern.Match(path);
            if (match.Success)
            {
                var baseId = match.Groups[1].Value;
                var viewId = match.Groups[2].Value;
                return method.Method switch
                {
                    "GET" => _viewsHandler.Get(baseId, viewId),
                    "DELETE" => _viewsHandler.Delete(baseId, viewId),
                    _ => CreateUnknownMethodResult()
                };
            }

            // Webhooks: list/create
            match = WebhooksPattern.Match(path);
            if (match.Success)
            {
                var baseId = match.Groups[1].Value;
                return method.Method switch
                {
                    "GET" => _webhooksHandler.List(baseId),
                    "POST" => _webhooksHandler.Create(baseId, body ?? new JObject()),
                    _ => CreateUnknownMethodResult()
                };
            }

            // Webhook payloads
            match = WebhookPayloadsPattern.Match(path);
            if (match.Success && method == HttpMethod.Get)
            {
                var baseId = match.Groups[1].Value;
                var webhookId = match.Groups[2].Value;
                return _webhooksHandler.ListPayloads(baseId, webhookId, queryParams);
            }

            // Single webhook: update/delete
            match = WebhookPattern.Match(path);
            if (match.Success)
            {
                var baseId = match.Groups[1].Value;
                var webhookId = match.Groups[2].Value;
                return method.Method switch
                {
                    "PATCH" => _webhooksHandler.Update(baseId, webhookId, body ?? new JObject()),
                    "DELETE" => _webhooksHandler.Delete(baseId, webhookId),
                    _ => CreateUnknownMethodResult()
                };
            }

            // Comments: single comment (update/delete)
            match = CommentPattern.Match(path);
            if (match.Success)
            {
                var baseId = match.Groups[1].Value;
                var tableIdOrName = match.Groups[2].Value;
                var recordId = match.Groups[3].Value;
                var commentId = match.Groups[4].Value;
                return method.Method switch
                {
                    "PATCH" => _commentsHandler.Update(baseId, tableIdOrName, recordId, commentId, body ?? new JObject()),
                    "DELETE" => _commentsHandler.Delete(baseId, tableIdOrName, recordId, commentId),
                    _ => CreateUnknownMethodResult()
                };
            }

            // Comments: list/create
            match = CommentsPattern.Match(path);
            if (match.Success)
            {
                var baseId = match.Groups[1].Value;
                var tableIdOrName = match.Groups[2].Value;
                var recordId = match.Groups[3].Value;
                return method.Method switch
                {
                    "GET" => _commentsHandler.List(baseId, tableIdOrName, recordId, queryParams),
                    "POST" => _commentsHandler.Create(baseId, tableIdOrName, recordId, body ?? new JObject()),
                    _ => CreateUnknownMethodResult()
                };
            }

            // Records: single record (get/update/delete)
            match = RecordPattern.Match(path);
            if (match.Success)
            {
                var baseId = match.Groups[1].Value;
                var tableIdOrName = match.Groups[2].Value;
                var recordId = match.Groups[3].Value;

                // Skip if this matches comments pattern (already handled above)
                if (!CommentsPattern.IsMatch(path) && !CommentPattern.IsMatch(path))
                {
                    return method.Method switch
                    {
                        "GET" => _recordsHandler.Get(baseId, tableIdOrName, recordId),
                        "PATCH" => _recordsHandler.UpdateSingle(baseId, tableIdOrName, recordId, body ?? new JObject(), false),
                        "PUT" => _recordsHandler.UpdateSingle(baseId, tableIdOrName, recordId, body ?? new JObject(), true),
                        "DELETE" => _recordsHandler.DeleteSingle(baseId, tableIdOrName, recordId),
                        _ => CreateUnknownMethodResult()
                    };
                }
            }

            // Records: list/create/batch update/delete
            match = TableRecordsPattern.Match(path);
            if (match.Success)
            {
                var baseId = match.Groups[1].Value;
                var tableIdOrName = match.Groups[2].Value;

                // Skip meta paths
                if (baseId != "meta" && baseId != "bases")
                {
                    return method.Method switch
                    {
                        "GET" => _recordsHandler.List(baseId, tableIdOrName, queryParams),
                        "POST" => _recordsHandler.Create(baseId, tableIdOrName, body ?? new JObject()),
                        "PATCH" => _recordsHandler.UpdateMultiple(baseId, tableIdOrName, body ?? new JObject(), false),
                        "PUT" => _recordsHandler.UpdateMultiple(baseId, tableIdOrName, body ?? new JObject(), true),
                        "DELETE" => HandleBatchDelete(baseId, tableIdOrName, queryParams),
                        _ => CreateUnknownMethodResult()
                    };
                }
            }

            return CreateUnknownMethodResult();
        }

        private object HandleBatchDelete(string baseId, string tableIdOrName, Dictionary<string, string> queryParams)
        {
            // Parse records[] query params
            var recordIds = new List<string>();
            
            // Check for records[] array notation
            foreach (var key in queryParams.Keys)
            {
                if (key == "records[]" || key.StartsWith("records["))
                {
                    recordIds.Add(queryParams[key]);
                }
            }

            // Also check for comma-separated records parameter
            if (queryParams.TryGetValue("records", out var recordsValue))
            {
                recordIds.AddRange(recordsValue.Split(',', StringSplitOptions.RemoveEmptyEntries));
            }

            if (recordIds.Count == 0)
            {
                return new { error = new { type = "INVALID_REQUEST_UNKNOWN", message = "No records specified for deletion" } };
            }

            return _recordsHandler.DeleteMultiple(baseId, tableIdOrName, recordIds);
        }

        private static object CreateUnknownMethodResult()
        {
            return new { error = new { type = "NOT_FOUND", message = "Unknown endpoint or method" } };
        }

        private HttpStatusCode GetStatusCodeForError(string errorType)
        {
            return errorType switch
            {
                "AUTHENTICATION_REQUIRED" => HttpStatusCode.Unauthorized,
                "INVALID_PERMISSIONS_OR_MODEL_NOT_FOUND" => HttpStatusCode.Forbidden,
                "NOT_FOUND" => HttpStatusCode.NotFound,
                "TABLE_NOT_FOUND" => HttpStatusCode.NotFound,
                "INVALID_REQUEST_UNKNOWN" => HttpStatusCode.UnprocessableEntity,
                "INVALID_REQUEST_BODY" => HttpStatusCode.BadRequest,
                "RATE_LIMIT_EXCEEDED" => HttpStatusCode.TooManyRequests,
                _ => HttpStatusCode.BadRequest
            };
        }

        private HttpResponseMessage CreateErrorResponse(HttpStatusCode statusCode, string errorType, string message)
        {
            var error = new { error = new { type = errorType, message } };
            return CreateJsonResponse(statusCode, error);
        }

        private HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, object? content)
        {
            var json = JsonConvert.SerializeObject(content, _jsonSettings);
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        }

        private static Dictionary<string, string> ParseQueryString(string query)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(query))
                return result;

            var queryWithoutPrefix = query.TrimStart('?');
            var pairs = queryWithoutPrefix.Split('&', StringSplitOptions.RemoveEmptyEntries);

            foreach (var pair in pairs)
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = HttpUtility.UrlDecode(parts[0]);
                    var value = HttpUtility.UrlDecode(parts[1]);
                    
                    // Handle array notation (records[]=xxx)
                    // Store with an index to preserve multiple values
                    if (key.EndsWith("[]"))
                    {
                        var baseKey = key;
                        var index = 0;
                        while (result.ContainsKey($"{baseKey}_{index}"))
                            index++;
                        result[$"{baseKey}_{index}"] = value;
                    }
                    else
                    {
                        result[key] = value;
                    }
                }
            }

            return result;
        }
    }
}
