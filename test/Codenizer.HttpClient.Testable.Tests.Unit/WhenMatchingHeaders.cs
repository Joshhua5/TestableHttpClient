using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Codenizer.HttpClient.Testable.Tests.Unit
{
    public class WhenMatchingHeaders
    {
        [Fact]
        public async Task GivenRequestWithHeader_MatchesConfiguredResponse()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo()
                   .Get()
                   .ForUrl("/api/test")
                   .WithHeader("X-Api-Key", "secret-key")
                   .With(HttpStatusCode.OK);

            var client = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
            request.Headers.Add("X-Api-Key", "secret-key");

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GivenRequestWithHeaderExpressedInDifferentCase_MatchesConfiguredResponse()
        {
            // Header keys are case-insensitive in HTTP
            var handler = new TestableMessageHandler();
            handler.RespondTo()
                   .Get()
                   .ForUrl("/api/test")
                   .WithHeader("x-api-key", "secret-key")
                   .With(HttpStatusCode.OK);

            var client = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
            request.Headers.Add("X-API-KEY", "secret-key");

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GivenRequestWithMultipleHeaders_MatchesConfiguredResponse()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo()
                   .Get()
                   .ForUrl("/api/test")
                   .WithHeader("X-Header-1", "Value1")
                   .WithHeader("X-Header-2", "Value2")
                   .With(HttpStatusCode.OK);

            var client = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
            request.Headers.Add("X-Header-1", "Value1");
            request.Headers.Add("X-Header-2", "Value2");

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GivenRequestWithMissingHeader_ReturnsInternalServerError()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo()
                   .Get()
                   .ForUrl("/api/test")
                   .WithHeader("X-Required", "true")
                   .With(HttpStatusCode.OK);

            var client = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
            // Header missing

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task GivenRequestWithIncorrectHeaderValue_ReturnsInternalServerError()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo()
                   .Get()
                   .ForUrl("/api/test")
                   .WithHeader("X-Status", "Active")
                   .With(HttpStatusCode.OK);

            var client = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
            request.Headers.Add("X-Status", "Inactive");

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }
    }
}
