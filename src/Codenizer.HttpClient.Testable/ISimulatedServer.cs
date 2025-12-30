namespace Codenizer.HttpClient.Testable
{
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the contract for a simulated server that can handle HTTP requests.
    /// </summary>
    public interface ISimulatedServer
    {
        /// <summary>
        /// Handles the incoming HTTP request and returns a full response message.
        /// </summary>
        /// <param name="request">The captured HTTP request.</param>
        /// <returns>A Task returning the HTTP response message.</returns>
        Task<HttpResponseMessage> HandleRequestAsync(HttpRequestMessage request);
    }
}
