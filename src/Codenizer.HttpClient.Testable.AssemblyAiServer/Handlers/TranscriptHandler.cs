using System.Net;
using System.Text.Json;
using Codenizer.HttpClient.Testable.AssemblyAiServer.Models;

namespace Codenizer.HttpClient.Testable.AssemblyAiServer.Handlers
{
    public static class TranscriptHandler
    {
        public static async Task<HttpResponseMessage> HandleAsync(HttpRequestMessage request, AssemblyAiState state)
        {
            var path = request.RequestUri!.AbsolutePath.TrimStart('/');
            // path is like v2/transcript/...

            if (request.Method == HttpMethod.Post && path.Equals("v2/transcript", StringComparison.OrdinalIgnoreCase))
            {
                return await CreateTranscriptAsync(request, state);
            }
            
            if (request.Method == HttpMethod.Get && path.Equals("v2/transcript", StringComparison.OrdinalIgnoreCase))
            {
                return ListTranscripts(request, state);
            }

            var segments = path.Split('/');
            if (segments.Length >= 3)
            {
                var transcriptId = segments[2];

                if (segments.Length == 3)
                {
                    if (request.Method == HttpMethod.Get)
                    {
                        return GetTranscript(transcriptId, state);
                    }
                    
                    if (request.Method == HttpMethod.Delete)
                    {
                        return DeleteTranscript(transcriptId, state);
                    }
                }
                else if (segments.Length == 4 && segments[3].Equals("sentences", StringComparison.OrdinalIgnoreCase) && request.Method == HttpMethod.Get)
                {
                    return GetSentences(transcriptId, state);
                }
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static async Task<HttpResponseMessage> CreateTranscriptAsync(HttpRequestMessage request, AssemblyAiState state)
        {
            if (request.Content == null)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            var content = await request.Content.ReadAsStringAsync();
            var options = JsonSerializer.Deserialize<Transcript>(content); // Using Transcript model as DTO for creation options roughly
            
            if (options == null || string.IsNullOrEmpty(options.AudioUrl))
            {
                 return new HttpResponseMessage(HttpStatusCode.BadRequest)
                 {
                     Content = new StringContent("{\"error\":\"audio_url is required\"}", System.Text.Encoding.UTF8, "application/json")
                 };
            }

            var transcript = new Transcript
            {
                Id = Guid.NewGuid().ToString(),
                Status = "queued",
                AudioUrl = options.AudioUrl,
                Text = null,
                Confidence = null,
                AudioDuration = 120.5 // Mock duration
            };

            state.Transcripts.TryAdd(transcript.Id, transcript);
            
            // Simulate processing in background for next retrieval? 
            // For now, let's keep it queued or processing. 
            // In a real test scenario, the user might want to transition it manually or generic logic.
            // Let's set it to 'processing' immediately for fun, or keep 'queued'.
            // The user requested: "request changes state and subsuquent requests return valid data."
            // So subsequent GET should probably show it as processing or completed.
            // Let's rely on a convention: if accessed again, we might flip it to completed? 
            // Or maybe we don't auto-flip. Let's start with queued.
            
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                 Content = new StringContent(JsonSerializer.Serialize(transcript), System.Text.Encoding.UTF8, "application/json")
            };
        }

        private static HttpResponseMessage GetTranscript(string id, AssemblyAiState state)
        {
            if (!state.Transcripts.TryGetValue(id, out var transcript))
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            // Simulate state transition: Queued -> Processing -> Completed
            if (transcript.Status == "queued")
            {
                transcript.Status = "processing";
            }
            else if (transcript.Status == "processing")
            {
                transcript.Status = "completed";
                transcript.Text = "This is a simulated transcript text for AssemblyAI.";
                transcript.Confidence = 0.98;
                transcript.Words = new List<Word>
                {
                    new Word { Text = "This", Start = 0, End = 100, Confidence = 0.99 },
                    new Word { Text = "is", Start = 110, End = 200, Confidence = 0.98 },
                    new Word { Text = "a", Start = 210, End = 250, Confidence = 0.99 },
                    new Word { Text = "simulated", Start = 260, End = 800, Confidence = 0.95 },
                    new Word { Text = "transcript", Start = 810, End = 1200, Confidence = 0.97 },
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(transcript), System.Text.Encoding.UTF8, "application/json")
            };
        }
        
        private static HttpResponseMessage DeleteTranscript(string id, AssemblyAiState state)
        {
             if (state.Transcripts.TryRemove(id, out _))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
                };
            }
            
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static HttpResponseMessage ListTranscripts(HttpRequestMessage request, AssemblyAiState state)
        {
            var transcripts = state.Transcripts.Values.ToList();
            var response = new TranscriptListResponse
            {
                Transcripts = transcripts,
                PageDetails = new PageDetails
                {
                    Limit = 10,
                    ResultCount = transcripts.Count,
                    CurrentUrl = "https://api.assemblyai.com/v2/transcript"
                }
            };
            
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(response), System.Text.Encoding.UTF8, "application/json")
            };
        }
        
        private static HttpResponseMessage GetSentences(string id, AssemblyAiState state)
        {
             if (!state.Transcripts.TryGetValue(id, out var transcript))
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            
            if (transcript.Status != "completed")
            {
                // API returns empty or error if not completed? usually just wait.
                // But let's return empty list for now.
                 return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"sentences\":[]}", System.Text.Encoding.UTF8, "application/json")
                };
            }

            var sentences = new 
            {
                sentences = new [] 
                {
                    new { text = "This is a simulated transcript text for AssemblyAI.", start = 0, end = 1200, confidence = 0.98 }
                }
            };

             return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(sentences), System.Text.Encoding.UTF8, "application/json")
            };
        }
    }
}
