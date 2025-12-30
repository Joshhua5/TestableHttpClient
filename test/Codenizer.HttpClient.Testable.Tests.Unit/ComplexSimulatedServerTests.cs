using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Codenizer.HttpClient.Testable.Tests.Unit
{
    public class ComplexSimulatedServerTests
    {
        [Fact]
        public async Task AuthServer_Returns401_WhenAuthHeaderIsMissing()
        {
            var handler = new TestableMessageHandler();
            var client = new System.Net.Http.HttpClient(handler);
            var server = new AuthServer();

            handler.RespondTo().Get().ForUrl("/api/secure").HandledBy(server);

            var response = await client.GetAsync("http://localhost/api/secure");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task AuthServer_Returns200_WhenAuthHeaderIsCorrect()
        {
            var handler = new TestableMessageHandler();
            var client = new System.Net.Http.HttpClient(handler);
            var server = new AuthServer();

            handler.RespondTo().Get().ForUrl("/api/secure").HandledBy(server);

            client.DefaultRequestHeaders.Add("Authorization", "Bearer valid-token");
            var response = await client.GetAsync("http://localhost/api/secure");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Be("Secure Data");
        }

        [Fact]
        public async Task SearchServer_ReturnsFilteredResults_WhenQueryParamIsPresent()
        {
            var handler = new TestableMessageHandler();
            var client = new System.Net.Http.HttpClient(handler);
            var server = new SearchServer();

            handler.RespondTo().Get().ForUrl("/api/search?q=apple").HandledBy(server);

            var response = await client.GetAsync("http://localhost/api/search?q=apple");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var results = JsonConvert.DeserializeObject<List<string>>(await response.Content.ReadAsStringAsync());
            results.Should().ContainSingle().Which.Should().Be("Apple");
        }

        [Fact]
        public async Task ChaosServer_PropagatesExceptions_WhenInternalErrorOccurs()
        {
            var handler = new TestableMessageHandler();
            var client = new System.Net.Http.HttpClient(handler);
            var server = new ChaosServer();

            handler.RespondTo().Get().ForUrl("/api/chaos?trigger=error").HandledBy(server);

            Func<Task> act = async () => await client.GetAsync("http://localhost/api/chaos?trigger=error");

            await act.Should().ThrowAsync<HttpRequestException>()
                .WithMessage("Server exploded!");
        }

        // --- Server Implementations ---

        private class AuthServer : ISimulatedServer
        {
            public Task<HttpResponseMessage> HandleRequestAsync(HttpRequestMessage request)
            {
                if (request.Headers.Authorization?.Parameter == "valid-token")
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("Secure Data")
                    });
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            }
        }

        private class SearchServer : ISimulatedServer
        {
            private readonly List<string> _data = new List<string> { "Apple", "Banana", "Cherry", "Date" };

            public Task<HttpResponseMessage> HandleRequestAsync(HttpRequestMessage request)
            {
                var query = request.RequestUri.Query;
                var searchTerm = "";
                
                if (!string.IsNullOrEmpty(query))
                {
                    var qParam = query.TrimStart('?').Split('&').FirstOrDefault(p => p.StartsWith("q="));
                    if (qParam != null)
                    {
                        searchTerm = qParam.Substring(2);
                    }
                }

                var results = _data.Where(d => d.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(results))
                });
            }
        }

        private class ChaosServer : ISimulatedServer
        {
            public Task<HttpResponseMessage> HandleRequestAsync(HttpRequestMessage request)
            {
                if (request.RequestUri.Query.Contains("trigger=error"))
                {
                    // In a real scenario, this might be a 500, but here we test exception propagation
                    throw new HttpRequestException("Server exploded!");
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
        }
    }
}
