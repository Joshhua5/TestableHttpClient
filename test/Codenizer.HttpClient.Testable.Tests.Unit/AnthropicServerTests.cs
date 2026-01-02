using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Codenizer.HttpClient.Testable.AnthropicServer.Models;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Codenizer.HttpClient.Testable.AnthropicServer.Tests
{
    public class AnthropicServerTests
    {
        [Fact]
        public async Task CanListModels()
        {
            var handler = new TestableMessageHandler();
            var server = new AnthropicSimulatedServer();
            handler.RespondTo().Get().ForUrl("https://api.anthropic.com/v1/models").HandledBy(server);
            handler.RespondTo().Post().ForUrl("https://api.anthropic.com/v1/messages").HandledBy(server);
            var client = new System.Net.Http.HttpClient(handler);

            var response = await client.GetAsync("https://api.anthropic.com/v1/models");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ModelListResponse>(content);
            result.Should().NotBeNull();
            result.Data.Should().NotBeEmpty();
            result.Data.Should().Contain(m => m.Id == "claude-3-opus-20240229");
        }

        [Fact]
        public async Task CanCreateMessage()
        {
            var handler = new TestableMessageHandler();
            var server = new AnthropicSimulatedServer();
            handler.RespondTo().Post().ForUrl("https://api.anthropic.com/v1/messages").HandledBy(server);
            var client = new System.Net.Http.HttpClient(handler);

            var request = new MessageRequest
            {
                Model = "claude-3-opus-20240229",
                MaxTokens = 1024,
                Messages = new List<Message>
                {
                    new Message { Role = "user", Content = "Hello, world" }
                }
            };

            var response = await client.PostAsync("https://api.anthropic.com/v1/messages", 
                new StringContent(JsonConvert.SerializeObject(request), System.Text.Encoding.UTF8, "application/json"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<MessageResponse>(content);
            result.Should().NotBeNull();
            result.Role.Should().Be("assistant");
            result.Content.Should().Contain(c => c.Text == "This is a simulated response from the Anthropic API.");
        }

        [Fact]
        public async Task CanManageBatches()
        {
            var handler = new TestableMessageHandler();
            var server = new AnthropicSimulatedServer();
            handler.RespondTo().Post().ForUrl("/v1/messages/batches").HandledBy(server);
            handler.RespondTo().Get().ForUrl("/v1/messages/batches/{id}").HandledBy(server);
            handler.RespondTo().Post().ForUrl("/v1/messages/batches/{id}/cancel").HandledBy(server);
            var client = new System.Net.Http.HttpClient(handler);

            // 1. Create Batch
            var batchRequest = new CreateMessageBatchRequest
            {
                Requests = new List<MessageBatchRequestItem>
                {
                    new MessageBatchRequestItem
                    {
                        CustomId = "req-1",
                        Params = new MessageRequest { Model = "claude-3-haiku-20240307", MaxTokens = 100 }
                    }
                }
            };

            var createResponse = await client.PostAsync("https://api.anthropic.com/v1/messages/batches",
                new StringContent(JsonConvert.SerializeObject(batchRequest), System.Text.Encoding.UTF8, "application/json"));
            
            createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var createdBatch = JsonConvert.DeserializeObject<MessageBatch>(await createResponse.Content.ReadAsStringAsync());
            createdBatch.Should().NotBeNull();
            createdBatch.Id.Should().NotBeNullOrEmpty();
            createdBatch.ProcessingStatus.Should().Be("in_progress");

            // 2. Retrieve Batch
            var getResponse = await client.GetAsync($"https://api.anthropic.com/v1/messages/batches/{createdBatch.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var retrievedBatch = JsonConvert.DeserializeObject<MessageBatch>(await getResponse.Content.ReadAsStringAsync());
            retrievedBatch.Should().NotBeNull();
            retrievedBatch.Id.Should().Be(createdBatch.Id);
            retrievedBatch.ProcessingStatus.Should().Be("in_progress");

            // 3. Cancel Batch
            var cancelResponse = await client.PostAsync($"https://api.anthropic.com/v1/messages/batches/{createdBatch.Id}/cancel", null);
            cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var cancelledBatch = JsonConvert.DeserializeObject<MessageBatch>(await cancelResponse.Content.ReadAsStringAsync());
            cancelledBatch.Should().NotBeNull();
            cancelledBatch.ProcessingStatus.Should().Be("canceled");

            // 4. Verify Cancel State Persisted
            var getResponse2 = await client.GetAsync($"https://api.anthropic.com/v1/messages/batches/{createdBatch.Id}");
            var finalBatch = JsonConvert.DeserializeObject<MessageBatch>(await getResponse2.Content.ReadAsStringAsync());
            finalBatch.Should().NotBeNull();
            finalBatch.ProcessingStatus.Should().Be("canceled");
        }
        [Fact]
        public async Task CanCountTokens()
        {
            var handler = new TestableMessageHandler();
            var server = new AnthropicSimulatedServer();
            handler.RespondTo().Post().ForUrl("/v1/messages/count_tokens").HandledBy(server);
            var client = new System.Net.Http.HttpClient(handler);

            var request = new MessageRequest
            {
                Model = "claude-3-opus-20240229",
                Messages = new List<Message>
                {
                    new Message { Role = "user", Content = "How many tokens is this?" }
                }
            };

            var response = await client.PostAsync("https://api.anthropic.com/v1/messages/count_tokens",
                new StringContent(JsonConvert.SerializeObject(request), System.Text.Encoding.UTF8, "application/json"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<CountTokensResponse>(content);
            result.Should().NotBeNull();
            result.InputTokens.Should().BeGreaterThan(0);
        }
    }
}
