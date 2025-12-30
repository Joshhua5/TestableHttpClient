using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Codenizer.HttpClient.Testable.Tests.Unit
{
    public class WhenMatchingCookies
    {
        [Fact]
        public async Task GivenRequestWithCookie_MatchesConfiguredResponse()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo()
                   .Get()
                   .ForUrl("/api/test")
                   .WithCookie("session_id", "abc-123")
                   .With(HttpStatusCode.OK);

            var client = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
            request.Headers.Add("Cookie", "session_id=abc-123");

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GivenRequestWithMultipleCookies_MatchesConfiguredResponse_RegardlessOfOrder()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo()
                   .Get()
                   .ForUrl("/api/test")
                   .WithCookie("a", "1")
                   .WithCookie("b", "2")
                   .With(HttpStatusCode.OK);

            var client = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
            // Reverse order
            request.Headers.Add("Cookie", "b=2; a=1");

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GivenRequestWithExtraCookies_MatchesConfiguredResponse()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo()
                   .Get()
                   .ForUrl("/api/test")
                   .WithCookie("required", "true")
                   .With(HttpStatusCode.OK);

            var client = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
            request.Headers.Add("Cookie", "required=true; tracking=ignored");

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GivenRequestWithMissingCookie_ReturnsInternalServerError()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo()
                   .Get()
                   .ForUrl("/api/test")
                   .WithCookie("session_id", "abc-123")
                   .With(HttpStatusCode.OK);

            var client = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
            // No cookie

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task GivenRequestWithWrongCookieValue_ReturnsInternalServerError()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo()
                   .Get()
                   .ForUrl("/api/test")
                   .WithCookie("session_id", "abc-123")
                   .With(HttpStatusCode.OK);

            var client = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
            request.Headers.Add("Cookie", "session_id=wrong");

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }
    }
}
