using System.Net;
using System.Text.Json;
using Codenizer.HttpClient.Testable.AssemblyAiServer.Handlers;

namespace Codenizer.HttpClient.Testable.AssemblyAiServer
{
    public class AssemblyAiSimulatedServer : ISimulatedServer
    {
        private readonly AssemblyAiState _state;

        public AssemblyAiSimulatedServer()
        {
            _state = new AssemblyAiState();
        }

        public async Task<HttpResponseMessage> HandleRequestAsync(HttpRequestMessage request)
        {
            var path = request.RequestUri?.AbsolutePath.TrimStart('/');
            
            if (string.IsNullOrEmpty(path))
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            if (path.Equals("v2/upload", StringComparison.OrdinalIgnoreCase) && request.Method == HttpMethod.Post)
            {
                return await UploadHandler.HandleAsync(request, _state);
            }

            if (path.StartsWith("v2/transcript", StringComparison.OrdinalIgnoreCase))
            {
                return await TranscriptHandler.HandleAsync(request, _state);
            }
            
            if (path.StartsWith("v2/lemur", StringComparison.OrdinalIgnoreCase))
            {
                return await LemurHandler.HandleAsync(request);
            }

            if (path.Equals("v2/realtime/token", StringComparison.OrdinalIgnoreCase) && request.Method == HttpMethod.Get)
            {
                return RealtimeHandler.HandleTokenRequest();
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }
}
