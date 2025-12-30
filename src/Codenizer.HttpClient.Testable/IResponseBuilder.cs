using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Codenizer.HttpClient.Testable
{
    /// <summary>
    /// Implements a builder to configure a response
    /// </summary>
    public interface IResponseBuilder
    {
        /// <summary>
        /// Configuration the response to have the specific status code
        /// </summary>
        /// <param name="statusCode">The status code to report</param>
        /// <returns>The current <see cref="IResponseBuilder"/> instance</returns>
        IResponseBuilder With(HttpStatusCode statusCode);

        /// <summary>
        /// Configures the response to contain the specified content
        /// </summary>
        /// <param name="mimeType">The MIME type of the content</param>
        /// <param name="data">The content</param>
        /// <returns>The current <see cref="IResponseBuilder"/> instance</returns>
        IResponseBuilder AndContent(string mimeType, object data);

        /// <summary>
        /// Configures the response to contain the content returned by the callback
        /// </summary>
        /// <param name="mimeType">The MIME type of the content</param>
        /// <param name="callback">The callback to invoke to get the content</param>
        /// <returns>The current <see cref="IResponseBuilder"/> instance</returns>
        IResponseBuilder AndContent(string mimeType, Func<HttpRequestMessage, object> callback);

        /// <summary>
        /// Configures the response to contain the content returned by the async callback
        /// </summary>
        /// <param name="mimeType">The MIME type of the content</param>
        /// <param name="callback">The async callback to invoke to get the content</param>
        /// <returns>The current <see cref="IResponseBuilder"/> instance</returns>
        IResponseBuilder AndContent(string mimeType, Func<HttpRequestMessage, Task<object>> callback);

        /// <summary>
        /// Configures the response to contain the specified headers
        /// </summary>
        /// <param name="headers">The headers to add to the response</param>
        /// <returns>The current <see cref="IResponseBuilder"/> instance</returns>
        IResponseBuilder AndHeaders(Dictionary<string, string> headers);

        /// <summary>
        /// Configures the response to take the specified amount of time
        /// </summary>
        /// <param name="time">The time to take</param>
        /// <returns>The current <see cref="IResponseBuilder"/> instance</returns>
        IResponseBuilder Taking(TimeSpan time);

        /// <summary>
        /// Configures an action to be invoked when the request is called
        /// </summary>
        /// <param name="action">The action to invoke</param>
        /// <returns>The current <see cref="IResponseBuilder"/> instance</returns>
        IResponseBuilder WhenCalled(Action<HttpRequestMessage> action);

        /// <summary>
        /// Configures a cookie to be added to the response
        /// </summary>
        /// <param name="name">The name of the cookie</param>
        /// <param name="value">The value of the cookie</param>
        /// <param name="expiresAt">The expiration date of the cookie</param>
        /// <param name="sameSite">The SameSite attribute of the cookie</param>
        /// <param name="secure">The Secure attribute of the cookie</param>
        /// <param name="path">The Path attribute of the cookie</param>
        /// <param name="domain">The Domain attribute of the cookie</param>
        /// <param name="maxAge">The MaxAge attribute of the cookie</param>
        /// <returns>The current <see cref="IResponseBuilder"/> instance</returns>
        IResponseBuilder AndCookie(string name,
            string value,
            DateTime? expiresAt = null,
            string? sameSite = null,
            bool? secure = null,
            string? path = null,
            string? domain = null,
            int? maxAge = null);

        /// <summary>
        /// Configures the response to contain the specified object serialized as JSON
        /// </summary>
        /// <param name="value">The object to serialize</param>
        /// <param name="serializerSettings">The serializer settings to use</param>
        /// <returns>The current <see cref="IResponseBuilder"/> instance</returns>
        IResponseBuilder AndJsonContent(object value, JsonSerializerSettings? serializerSettings = null);

        /// <summary>
        /// Configures the response to be handled by the specified simulated server
        /// </summary>
        /// <param name="server">The simulated server to handle the request</param>
        /// <returns>The current <see cref="IResponseBuilder"/> instance</returns>
        IResponseBuilder HandledBy(ISimulatedServer server);
    }
}
