using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Codenizer.HttpClient.Testable;

namespace Codenizer.HttpClient.Testable.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for all request matching node types in the matching tree.
    /// Tree structure: Method -> Scheme -> Authority -> Path -> Query -> Headers -> Cookies -> Content
    /// </summary>
    public class MatchingLogicFlowTests
    {
        #region Method Matching

        [Fact]
        public async Task ShouldMatchGetMethod()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/test").With(HttpStatusCode.OK).AndContent("text/plain", "GET response");

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/test");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync()).Should().Be("GET response");
        }

        [Fact]
        public async Task ShouldMatchPostMethod()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Post().ForUrl("/test").With(HttpStatusCode.Created).AndContent("text/plain", "POST response");

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.PostAsync("http://localhost/test", new StringContent(""));

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            (await response.Content.ReadAsStringAsync()).Should().Be("POST response");
        }

        [Fact]
        public async Task ShouldMatchPutMethod()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Put().ForUrl("/test").With(HttpStatusCode.OK).AndContent("text/plain", "PUT response");

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.PutAsync("http://localhost/test", new StringContent(""));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync()).Should().Be("PUT response");
        }

        [Fact]
        public async Task ShouldMatchDeleteMethod()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Delete().ForUrl("/test").With(HttpStatusCode.NoContent);

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.DeleteAsync("http://localhost/test");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task ShouldMatchHeadMethod()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Head().ForUrl("/test").With(HttpStatusCode.OK);

            var client = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Head, "http://localhost/test");
            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ShouldMatchOptionsMethod()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Options().ForUrl("/test").With(HttpStatusCode.OK);

            var client = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Options, "http://localhost/test");
            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ShouldNotMatchWrongMethod()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/test").With(HttpStatusCode.OK);

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.PostAsync("http://localhost/test", new StringContent(""));

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }

        #endregion

        #region Path Matching

        [Fact]
        public async Task ShouldMatchExactPath()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/api/users").With(HttpStatusCode.OK).AndContent("text/plain", "users");

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/api/users");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ShouldMatchPathWithRouteParameter()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/api/users/{id}").With(HttpStatusCode.OK).AndContent("text/plain", "user details");

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/api/users/123");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync()).Should().Be("user details");
        }

        [Fact]
        public async Task ShouldMatchPathWithMultipleRouteParameters()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/api/users/{userId}/posts/{postId}").With(HttpStatusCode.OK).AndContent("text/plain", "post");

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/api/users/1/posts/99");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ShouldNotMatchDifferentPath()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/api/users").With(HttpStatusCode.OK);

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/api/products");

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }

        #endregion

        #region Query Matching

        [Fact]
        public async Task ShouldMatchQueryParameter()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/search?q=test").With(HttpStatusCode.OK).AndContent("text/plain", "search results");

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/search?q=test");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ShouldMatchMultipleQueryParameters()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/search?q=test&page=1").With(HttpStatusCode.OK).AndContent("text/plain", "page 1");

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/search?q=test&page=1");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ShouldMatchQueryParameterWithFluentApi()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/search?q=test")
                   .WithQueryStringParameter("q").HavingValue("test")
                   .With(HttpStatusCode.OK).AndContent("text/plain", "search");

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/search?q=test");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ShouldMatchQueryParameterWithAnyValue()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/search?q=placeholder")
                   .WithQueryStringParameter("q").HavingAnyValue()
                   .With(HttpStatusCode.OK).AndContent("text/plain", "any search");

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/search?q=anything");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Header Matching

        [Fact]
        public async Task ShouldMatchAcceptHeader()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/api/data")
                   .Accepting("application/json")
                   .With(HttpStatusCode.OK).AndContent("application/json", "{}");

            var client = new System.Net.Http.HttpClient(handler);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            var response = await client.GetAsync("http://localhost/api/data");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ShouldMatchCustomHeader()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/api/data")
                   .WithHeader("X-Api-Key", "secret123")
                   .With(HttpStatusCode.OK).AndContent("text/plain", "authorized");

            var client = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/data");
            request.Headers.Add("X-Api-Key", "secret123");
            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync()).Should().Be("authorized");
        }

        [Fact]
        public async Task ShouldMatchHeaderCaseInsensitively()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/api/data")
                   .WithHeader("X-Api-Key", "SECRET")
                   .With(HttpStatusCode.OK).AndContent("text/plain", "matched");

            var client = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/data");
            request.Headers.Add("X-Api-Key", "secret");
            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ShouldNotMatchMissingHeader()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/api/data")
                   .WithHeader("X-Api-Key", "secret123")
                   .With(HttpStatusCode.OK);

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/api/data");

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }

        #endregion

        #region Cookie Matching

        [Fact]
        public async Task ShouldMatchRequestCookie()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/api/data")
                   .WithCookie("session", "abc123")
                   .With(HttpStatusCode.OK).AndContent("text/plain", "session valid");

            var client = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/data");
            request.Headers.Add("Cookie", "session=abc123");
            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync()).Should().Be("session valid");
        }

        [Fact]
        public async Task ShouldMatchMultipleCookies()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/api/data")
                   .WithCookie("session", "abc123")
                   .WithCookie("user", "john")
                   .With(HttpStatusCode.OK).AndContent("text/plain", "both cookies");

            var client = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/data");
            request.Headers.Add("Cookie", "session=abc123; user=john");
            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Content Matching

        [Fact]
        public async Task ShouldMatchRequestContent()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Post().ForUrl("/api/data")
                   .ForContent("{\"name\":\"test\"}")
                   .With(HttpStatusCode.OK).AndContent("text/plain", "content matched");

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.PostAsync("http://localhost/api/data",
                new StringContent("{\"name\":\"test\"}", Encoding.UTF8, "application/json"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ShouldMatchContentWithPredicate()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Post().ForUrl("/api/data")
                   .ForContent(content => content.ReadAsStringAsync().GetAwaiter().GetResult().Contains("important"))
                   .With(HttpStatusCode.OK).AndContent("text/plain", "predicate matched");

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.PostAsync("http://localhost/api/data",
                new StringContent("This is important data", Encoding.UTF8, "text/plain"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync()).Should().Be("predicate matched");
        }

        [Fact]
        public async Task ShouldNotMatchDifferentContent()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Post().ForUrl("/api/data")
                   .ForContent("expected content")
                   .With(HttpStatusCode.OK);

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.PostAsync("http://localhost/api/data",
                new StringContent("different content", Encoding.UTF8, "text/plain"));

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }

        #endregion

        #region Combined Matching (Full Flow)

        [Fact]
        public async Task ShouldMatchCompleteRequestWithAllNodes()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo()
                   .Post()
                   .ForUrl("/api/v1/users/{id}/action?confirm=true")
                   .WithHeader("X-Request-Id", "12345")
                   .WithCookie("auth", "token")
                   .ForContent("{\"action\":\"confirm\"}")
                   .With(HttpStatusCode.OK)
                   .AndContent("text/plain", "Full match success");

            var client = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/v1/users/42/action?confirm=true");
            request.Headers.Add("X-Request-Id", "12345");
            request.Headers.Add("Cookie", "auth=token");
            request.Content = new StringContent("{\"action\":\"confirm\"}", Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync()).Should().Be("Full match success");
        }

        #endregion
    }
}
