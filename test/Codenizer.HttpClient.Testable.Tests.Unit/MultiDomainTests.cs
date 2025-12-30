using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Codenizer.HttpClient.Testable.Tests.Unit
{
    public class MultiDomainTests
    {
        [Fact]
        public async Task DifferentDomains_ReturnDifferentResponses()
        {
            var handler = new TestableMessageHandler();
            var client = new System.Net.Http.HttpClient(handler);

            handler.RespondTo().Get().ForUrl("https://api-a.example.com/data")
                .With(HttpStatusCode.OK).AndContent("application/json", "{\"source\":\"A\"}");

            handler.RespondTo().Get().ForUrl("https://api-b.example.com/data")
                .With(HttpStatusCode.OK).AndContent("application/json", "{\"source\":\"B\"}");

            var responseA = await client.GetAsync("https://api-a.example.com/data");
            var responseB = await client.GetAsync("https://api-b.example.com/data");

            (await responseA.Content.ReadAsStringAsync()).Should().Be("{\"source\":\"A\"}");
            (await responseB.Content.ReadAsStringAsync()).Should().Be("{\"source\":\"B\"}");
        }

        [Fact]
        public async Task DifferentSchemes_ReturnDifferentResponses()
        {
            var handler = new TestableMessageHandler();
            var client = new System.Net.Http.HttpClient(handler);

            handler.RespondTo().Get().ForUrl("http://example.com/api")
                .With(HttpStatusCode.OK).AndContent("text/plain", "HTTP");

            handler.RespondTo().Get().ForUrl("https://example.com/api")
                .With(HttpStatusCode.OK).AndContent("text/plain", "HTTPS");

            var httpResponse = await client.GetAsync("http://example.com/api");
            var httpsResponse = await client.GetAsync("https://example.com/api");

            (await httpResponse.Content.ReadAsStringAsync()).Should().Be("HTTP");
            (await httpsResponse.Content.ReadAsStringAsync()).Should().Be("HTTPS");
        }

        [Fact]
        public async Task DifferentPorts_ReturnDifferentResponses()
        {
            var handler = new TestableMessageHandler();
            var client = new System.Net.Http.HttpClient(handler);

            handler.RespondTo().Get().ForUrl("http://localhost:8080/api")
                .With(HttpStatusCode.OK).AndContent("text/plain", "Port 8080");

            handler.RespondTo().Get().ForUrl("http://localhost:9090/api")
                .With(HttpStatusCode.OK).AndContent("text/plain", "Port 9090");

            var response8080 = await client.GetAsync("http://localhost:8080/api");
            var response9090 = await client.GetAsync("http://localhost:9090/api");

            (await response8080.Content.ReadAsStringAsync()).Should().Be("Port 8080");
            (await response9090.Content.ReadAsStringAsync()).Should().Be("Port 9090");
        }

        [Fact]
        public async Task RelativeUrl_MatchesAnyDomain()
        {
            var handler = new TestableMessageHandler();
            var client = new System.Net.Http.HttpClient(handler);

            handler.RespondTo().Get().ForUrl("/api/universal")
                .With(HttpStatusCode.OK).AndContent("text/plain", "Universal");

            var response1 = await client.GetAsync("http://domain1.com/api/universal");
            var response2 = await client.GetAsync("https://domain2.org/api/universal");

            response1.StatusCode.Should().Be(HttpStatusCode.OK);
            response2.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task RelativeUrls_ActAsWildcards_MatchingAnyDomainAndScheme()
        {
            var handler = new TestableMessageHandler();
            var client = new System.Net.Http.HttpClient(handler);

            // Both use relative URLs which act as wildcards
            handler.RespondTo().Get().ForUrl("/api/data")
                .With(HttpStatusCode.OK).AndContent("text/plain", "Wildcard Data");

            handler.RespondTo().Get().ForUrl("/api/users")
                .With(HttpStatusCode.OK).AndContent("text/plain", "Wildcard Users");

            // Should match any domain/scheme
            var httpResponse = await client.GetAsync("http://some-domain.com/api/data");
            var httpsResponse = await client.GetAsync("https://another-domain.net/api/users");

            (await httpResponse.Content.ReadAsStringAsync()).Should().Be("Wildcard Data");
            (await httpsResponse.Content.ReadAsStringAsync()).Should().Be("Wildcard Users");
        }

        [Fact]
        public async Task SubdomainsAreDistinct()
        {
            var handler = new TestableMessageHandler();
            var client = new System.Net.Http.HttpClient(handler);

            handler.RespondTo().Get().ForUrl("https://api.example.com/users")
                .With(HttpStatusCode.OK).AndContent("application/json", "[\"api-user\"]");

            handler.RespondTo().Get().ForUrl("https://admin.example.com/users")
                .With(HttpStatusCode.OK).AndContent("application/json", "[\"admin-user\"]");

            var apiResponse = await client.GetAsync("https://api.example.com/users");
            var adminResponse = await client.GetAsync("https://admin.example.com/users");

            (await apiResponse.Content.ReadAsStringAsync()).Should().Be("[\"api-user\"]");
            (await adminResponse.Content.ReadAsStringAsync()).Should().Be("[\"admin-user\"]");
        }

        [Fact]
        public async Task SimulatedServer_WorksWithSpecificDomains()
        {
            var handler = new TestableMessageHandler();
            var client = new System.Net.Http.HttpClient(handler);
            var serverA = new DomainAwareServer("Domain A");
            var serverB = new DomainAwareServer("Domain B");

            handler.RespondTo().Get().ForUrl("https://service-a.example.com/api/info")
                .HandledBy(serverA);

            handler.RespondTo().Get().ForUrl("https://service-b.example.com/api/info")
                .HandledBy(serverB);

            var responseA = await client.GetAsync("https://service-a.example.com/api/info");
            var responseB = await client.GetAsync("https://service-b.example.com/api/info");

            (await responseA.Content.ReadAsStringAsync()).Should().Be("Domain A");
            (await responseB.Content.ReadAsStringAsync()).Should().Be("Domain B");
        }

        private class DomainAwareServer : ISimulatedServer
        {
            private readonly string _identifier;

            public DomainAwareServer(string identifier)
            {
                _identifier = identifier;
            }

            public Task<HttpResponseMessage> HandleRequestAsync(HttpRequestMessage request)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_identifier)
                });
            }
        }
    }
}
