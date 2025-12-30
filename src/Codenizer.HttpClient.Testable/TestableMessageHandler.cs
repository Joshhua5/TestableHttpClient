using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Codenizer.HttpClient.Testable
{
    /// <summary>
    /// Implements a message handler that allows to configure predefined responses to HTTP calls.
    /// </summary>
    public class TestableMessageHandler : HttpMessageHandler
    {
        private readonly JsonSerializerSettings _serializerSettings;
        private readonly ConcurrentBag<RequestBuilder> _configuredRequests;
        private ConfiguredRequests? _configuredRequestsTree;
        private ReadOnlyCollection<RequestBuilder>? _cachedConfiguredResponses;
        private bool _isDirty = true;
        private readonly object _dirtyLock = new object();
        private Exception? _exceptionToThrow;

        /// <summary>
        /// Returns the list of requests that were captured by this message handler
        /// </summary>
        public ConcurrentQueue<HttpRequestMessage> Requests { get; } = new ConcurrentQueue<HttpRequestMessage>();

        /// <summary>
        /// Returns the list of responses that are configured for this message handler
        /// </summary>
        public ReadOnlyCollection<RequestBuilder> ConfiguredResponses
        {
            get
            {
                if (_isDirty || _cachedConfiguredResponses == null)
                {
                    lock (_dirtyLock)
                    {
                        if (_isDirty || _cachedConfiguredResponses == null)
                        {
                            _cachedConfiguredResponses = Array.AsReadOnly(_configuredRequests.ToArray());
                        }
                    }
                }

                return _cachedConfiguredResponses;
            }
        }

        /// <summary>
        /// Creates a new instance without any predefined responses
        /// </summary>
        public TestableMessageHandler() : this(null)
        {
        }

        /// <summary>
        /// Creates a new instance without any predefined responses
        /// </summary>
        public TestableMessageHandler(JsonSerializerSettings? serializerSettings)
        {
            _serializerSettings = serializerSettings ?? new JsonSerializerSettings();
            _configuredRequests = new ConcurrentBag<RequestBuilder>();
        }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            Requests.Enqueue(await CloneRequestAsync(request));

            if (_exceptionToThrow != null)
            {
                throw _exceptionToThrow;
            }

            ConfiguredRequests tree;

            if (_isDirty || _configuredRequestsTree == null)
            {
                lock (_dirtyLock)
                {
                    if (_isDirty || _configuredRequestsTree == null)
                    {
                        _configuredRequestsTree = ConfiguredRequests.FromRequestBuilders(ConfiguredResponses);
                        _isDirty = false;
                    }
                }
            }

            tree = _configuredRequestsTree;

            var match = await tree.MatchAsync(request);

            if(match == null)
            {
                var pathAndQuery = request.RequestUri?.PathAndQuery ?? "unknown-url";
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent($"No response configured for {pathAndQuery}{Environment.NewLine}{tree.GetCurrentConfiguration()}")
                };
            }

            var responseBuilder = match;

            if (responseBuilder.SimulatedServer != null)
            {
                return await responseBuilder.SimulatedServer.HandleRequestAsync(request);
            }

            if (responseBuilder.ResponseSequence.Any())
            {
                if (responseBuilder.ResponseSequenceCounter >= responseBuilder.ResponseSequence.Count)
                {
                    var pathAndQuery = request.RequestUri?.PathAndQuery ?? "unknown-url";
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        Content = new StringContent($"Received request number {responseBuilder.ResponseSequenceCounter+1} for {pathAndQuery} but only {responseBuilder.ResponseSequence.Count} responses were configured")
                    };
                }

                responseBuilder = responseBuilder.ResponseSequence[responseBuilder.ResponseSequenceCounter++];
            }

            if (!string.IsNullOrWhiteSpace(responseBuilder.ContentType))
            {
                var requestContentType = request.Content?.Headers?.ContentType?.MediaType;

                if (requestContentType != responseBuilder.ContentType)
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.UnsupportedMediaType
                    };
                }
            }

            responseBuilder.ActionWhenCalled?.Invoke(request);

            var response = new HttpResponseMessage
            {
                StatusCode = responseBuilder.StatusCode
            };

            var responseBuilderData = responseBuilder.Data;

            if (responseBuilderData == null && responseBuilder.ResponseCallback != null)
            {
                responseBuilderData = responseBuilder.ResponseCallback(request);
            }

            if (responseBuilderData == null && responseBuilder.AsyncResponseCallback != null)
            {
                responseBuilderData = await responseBuilder.AsyncResponseCallback(request);
            }
            
            if (responseBuilder.CachedResponseContent != null)
            {
                response.Content = responseBuilder.CachedResponseContent;
            }
            else if (responseBuilderData != null)
            {
                if (responseBuilderData is byte[] buffer)
                {
                    response.Content = new ByteArrayContent(buffer);
                    if (responseBuilder.ResponseCallback == null && responseBuilder.AsyncResponseCallback == null)
                    {
                        responseBuilder.CachedResponseContent = response.Content;
                    }
                }
                else if (responseBuilderData is string content)
                {
                    response.Content = new StringContent(content, Encoding.UTF8, responseBuilder.MediaType);
                    if (responseBuilder.ResponseCallback == null && responseBuilder.AsyncResponseCallback == null)
                    {
                        responseBuilder.CachedResponseContent = response.Content;
                    }
                }
                else if (responseBuilder.MediaType == "application/json")
                {
                    response.Content = new StringContent(JsonConvert.SerializeObject(responseBuilderData, responseBuilder.SerializerSettings ?? _serializerSettings), Encoding.UTF8, responseBuilder.MediaType);
                    // Serialization output might depend on serializer settings which can change, 
                    // but since they are attached to the builder we can cache it.
                    if (responseBuilder.ResponseCallback == null && responseBuilder.AsyncResponseCallback == null)
                    {
                        responseBuilder.CachedResponseContent = response.Content;
                    }
                }
                else
                {
                    throw new InvalidOperationException(
                        "Unable to determine the response object to return as it's not a string, byte[] or object to return as application/json");
                }
            }

            foreach (var header in responseBuilder.Headers)
            {
                response.Headers.Add(header.Key, header.Value);
            }

            foreach (var cookie in responseBuilder.Cookies)
            {
                response.Headers.Add("Set-Cookie", cookie);
            }

            if (responseBuilder.Duration > TimeSpan.Zero)
            {
                var remainingDelay = responseBuilder.Duration - stopwatch.Elapsed;
                if (remainingDelay > TimeSpan.Zero)
                {
                    await Task.Delay(remainingDelay, cancellationToken);
                }
            }

            return response;
        }

        private async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Version = request.Version
            };

            foreach (var header in request.Headers)
            {
                clone.Headers.Add(header.Key, header.Value);
            }

#if NET5_0_OR_GREATER
            foreach (var option in request.Options)
            {
                clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
            }
#else
            foreach (var property in request.Properties)
            {
                clone.Properties.Add(property.Key, property.Value);
            }
#endif

            if (request.Content != null)
            {
                switch (request.Content)
                {
                    case StringContent stringContent:
                        clone.Content = new StringContent(await stringContent.ReadAsStringAsync());
                        break;
                    // FormUrlEncodedContent needs to be before ByteArrayContent
                    // because it inherits from it, and we need to handle it differently
                    case FormUrlEncodedContent formContent:
                        var serialized = await formContent.ReadAsStringAsync();
                        // serialized looks like field1=val1&field2=val2
                        var formValues = serialized
                            .Split('&')
                            .Select(kv => kv.Split('='))
                            .Select(parts => new KeyValuePair<string, string>(parts[0], parts[1]))
                            .ToArray();
                        clone.Content = new FormUrlEncodedContent(formValues);
                        break;
                    case ByteArrayContent byteArrayContent:
                        clone.Content = new ByteArrayContent(await byteArrayContent.ReadAsByteArrayAsync());
                        break;
                    case MultipartContent multipartContent:
                        var clonedMultipartContent = new MultipartContent();

                        foreach (var part in multipartContent)
                        {
                            clonedMultipartContent.Add(part);
                        }

                        clone.Content = clonedMultipartContent;
                        break;
                    case StreamContent streamContent:
                        var memoryStream = new MemoryStream();
                        await streamContent.CopyToAsync(memoryStream);
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        clone.Content = new StreamContent(memoryStream);
                        break;
                    default:
                        var buffer = await request.Content.ReadAsByteArrayAsync();
                        clone.Content = new ByteArrayContent(buffer);
                        break;
                }

                // Ensure we start with a clear slate
                clone.Content.Headers.Clear();

                // Copy all original content headers.
                // The "other" request headers have already been copied above
                foreach (var header in request.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return clone;
        }

        /// <summary>
        /// Respond to a GET request for the given relative path and query string
        /// </summary>
        /// <returns>A <see cref="IRequestBuilder"/> instance that can be used to further configure the response</returns>
        public IRequestBuilder RespondTo()
        {
            var requestBuilder = new RequestBuilder();

            _isDirty = true;
            _configuredRequests.Add(requestBuilder);

            return requestBuilder;
        }

        /// <summary>
        /// Respond to a GET request for the given relative path and query string
        /// </summary>
        /// <param name="pathAndQuery">The path and query string to match</param>
        /// <returns>A <see cref="IRequestBuilder"/> instance that can be used to further configure the response</returns>
        /// <remarks>A more fluent approach is available through RespondTo().Get().Url()</remarks>
        [Obsolete("A more fluent approach is available through RespondTo().Get()", false)]
        public IRequestBuilder RespondTo(string pathAndQuery)
        {
            return RespondTo(HttpMethod.Get, pathAndQuery);
        }

        /// <summary>
        /// Respond to a request for the given HTTP method, relative path and query string
        /// </summary>
        /// <param name="method">The HTTP method to match</param>
        /// <param name="pathAndQuery">The path and query string to match</param>
        /// <returns>A <see cref="IRequestBuilder"/> instance that can be used to further configure the response</returns>
        /// <remarks>A more fluent approach is available through RespondTo().Get().Url()</remarks>
        [Obsolete("A more fluent approach is available through RespondTo().Get()", false)]
        public IRequestBuilder RespondTo(HttpMethod method, string pathAndQuery)
        {
            return RespondTo(method, pathAndQuery, null);
        }

        /// <summary>
        /// Respond to a request for the given HTTP method, relative path and query string and Content-Type header
        /// </summary>
        /// <param name="method">The HTTP method to match</param>
        /// <param name="pathAndQuery">The path and query string to match</param>
        /// <param name="contentType">The MIME type to match</param>
        /// <returns>A <see cref="IRequestBuilder"/> instance that can be used to further configure the response</returns>
        /// <remarks>A more fluent approach is available through RespondTo().Get().Url()</remarks>
        [Obsolete("A more fluent approach is available through RespondTo().Get()", false)]
        public IRequestBuilder RespondTo(HttpMethod method, string pathAndQuery, string? contentType)
        {
            var requestBuilder = new RequestBuilder(method, pathAndQuery, contentType);

            _isDirty = true;
            _configuredRequests.Add(requestBuilder);

            return requestBuilder;
        }

        /// <summary>
        /// Configures the handler to throw the given exception on any request that is made
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> instance to throw</param>
        public void ShouldThrow(Exception exception)
        {
            _exceptionToThrow = exception;
        }
        
        /// <summary>
        /// Clears the list of configured responses
        /// </summary>
        public void ClearConfiguredResponses()
        {
            _isDirty = true;
            // ConcurrentBag doesn't have Clear(), so we create a new instance
            // Note: This is not atomic, but for testing purposes it's acceptable
            while (_configuredRequests.TryTake(out _)) { }
        }
        
        /// <summary>
        /// Returns the currently configured requests and their responses as a string
        /// </summary>
        /// <returns>A string representing the configured requests</returns>
        public string GetCurrentConfiguration()
        {
            var requests = ConfiguredRequests.FromRequestBuilders(ConfiguredResponses);

            return requests.GetCurrentConfiguration();
        }
    }
}
