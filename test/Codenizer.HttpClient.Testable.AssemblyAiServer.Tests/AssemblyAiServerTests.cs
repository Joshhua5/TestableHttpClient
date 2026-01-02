using System.Net;
using System.Text.Json;
using Codenizer.HttpClient.Testable.AssemblyAiServer;
using Codenizer.HttpClient.Testable.AssemblyAiServer.Models;
using FluentAssertions;

namespace Codenizer.HttpClient.Testable.AssemblyAiServer.Tests
{
    public class AssemblyAiServerTests
    {
        private readonly AssemblyAiSimulatedServer _server;
        private readonly TestableMessageHandler _handler;
        private readonly System.Net.Http.HttpClient _client;

        public AssemblyAiServerTests()
        {
            _server = new AssemblyAiSimulatedServer();
            _handler = new TestableMessageHandler();
            _client = new System.Net.Http.HttpClient(_handler)
            {
                BaseAddress = new Uri("https://api.assemblyai.com")
            };
        }

        [Fact]
        public async Task Upload_ReturnsUploadUrl()
        {
            _handler.RespondTo().Post().ForUrl("/v2/upload").HandledBy(_server);

            var response = await _client.PostAsync("/v2/upload", new StringContent("simulated audio content"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var uploadResponse = JsonSerializer.Deserialize<UploadResponse>(content);
            uploadResponse.Should().NotBeNull();
            uploadResponse!.UploadUrl.Should().StartWith("https://cdn.assemblyai.com/upload/");
        }

        [Fact]
        public async Task CreateTranscript_ReturnsQueuedTranscript()
        {
            _handler.RespondTo().Post().ForUrl("/v2/transcript").HandledBy(_server);

            var transcriptRequest = new Transcript
            {
                AudioUrl = "https://example.com/audio.mp3"
            };

            var response = await _client.PostAsync("/v2/transcript", new StringContent(JsonSerializer.Serialize(transcriptRequest)));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var transcript = JsonSerializer.Deserialize<Transcript>(content);
            transcript.Should().NotBeNull();
            transcript!.Status.Should().Be("queued");
            transcript.Id.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetTranscript_TransitionsState()
        {
            _handler.RespondTo().Post().ForUrl("/v2/transcript").HandledBy(_server);
            _handler.RespondTo().Get().ForUrl("/v2/transcript/{id}").HandledBy(_server);

            // Create
            var creationResponse = await _client.PostAsync("/v2/transcript", new StringContent(JsonSerializer.Serialize(new { audio_url = "https://url.com" })));
            var createdTranscript = JsonSerializer.Deserialize<Transcript>(await creationResponse.Content.ReadAsStringAsync());
            var id = createdTranscript!.Id;

            // Get 1 (Queued -> Processing)
            var get1 = await _client.GetAsync($"/v2/transcript/{id}");
            var t1 = JsonSerializer.Deserialize<Transcript>(await get1.Content.ReadAsStringAsync());
            t1!.Status.Should().Be("processing"); // Based on our mock logic: queued -> processing

             // Get 2 (Processing -> Completed)
            var get2 = await _client.GetAsync($"/v2/transcript/{id}");
            var t2 = JsonSerializer.Deserialize<Transcript>(await get2.Content.ReadAsStringAsync());
            t2!.Status.Should().Be("completed");
            t2.Text.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ListTranscripts_ReturnsList()
        {
            _handler.RespondTo().Post().ForUrl("/v2/transcript").HandledBy(_server);
            _handler.RespondTo().Get().ForUrl("/v2/transcript").HandledBy(_server);
            
            await _client.PostAsync("/v2/transcript", new StringContent(JsonSerializer.Serialize(new { audio_url = "https://url.com" })));

            var response = await _client.GetAsync("/v2/transcript");
            
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var list = JsonSerializer.Deserialize<TranscriptListResponse>(await response.Content.ReadAsStringAsync());
            list!.Transcripts.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task DeleteTranscript_RemovesTranscript()
        {
            _handler.RespondTo().Post().ForUrl("/v2/transcript").HandledBy(_server);
            _handler.RespondTo().Delete().ForUrl("/v2/transcript/{id}").HandledBy(_server);
            _handler.RespondTo().Get().ForUrl("/v2/transcript/{id}").HandledBy(_server);

            var creationResponse = await _client.PostAsync("/v2/transcript", new StringContent(JsonSerializer.Serialize(new { audio_url = "https://url.com" })));
            var id = JsonSerializer.Deserialize<Transcript>(await creationResponse.Content.ReadAsStringAsync())!.Id;

            var deleteResponse = await _client.DeleteAsync($"/v2/transcript/{id}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var getResponse = await _client.GetAsync($"/v2/transcript/{id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task LeMUR_Task_ReturnsResponse()
        {
            _handler.RespondTo().Post().ForUrl("/v2/lemur/v3/task").HandledBy(_server);

            var response = await _client.PostAsync("/v2/lemur/v3/task", new StringContent("{}"));
            
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var lemurResponse = JsonSerializer.Deserialize<LemurTaskResponse>(await response.Content.ReadAsStringAsync());
            lemurResponse!.Response.Should().NotBeNullOrEmpty();
        }
    }
}
