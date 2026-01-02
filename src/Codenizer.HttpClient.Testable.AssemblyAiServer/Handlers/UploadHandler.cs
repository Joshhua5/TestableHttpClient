using System.Net;
using System.Text.Json;
using Codenizer.HttpClient.Testable.AssemblyAiServer.Models;

namespace Codenizer.HttpClient.Testable.AssemblyAiServer.Handlers
{
    public static class UploadHandler
    {
        public static async Task<HttpResponseMessage> HandleAsync(HttpRequestMessage request, AssemblyAiState state)
        {
            var uploadId = Guid.NewGuid().ToString();
            var uploadUrl = $"https://cdn.assemblyai.com/upload/{uploadId}";
            
            state.Uploads.TryAdd(uploadUrl, "uploaded_content_placeholder");

            var response = new UploadResponse
            {
                UploadUrl = uploadUrl
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(response), System.Text.Encoding.UTF8, "application/json")
            };
        }
    }
}
