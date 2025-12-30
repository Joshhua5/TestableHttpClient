using System.Net.Http;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Codenizer.HttpClient.Testable.Tests.Unit
{
    public class Next
    {
        [Fact]
        public void One()
        {
            var builder = new RequestBuilder(
                HttpMethod.Get,
                "https://blog.codenizer.nl/api/v1/some/entity?query=param&query=blah&foo=bar",
                null);

            var configuredRequests = ConfiguredRequests.FromRequestBuilders(new[] { builder });

            configuredRequests
                .Count
                .Should()
                .Be(1);
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenGetRequestForFullyQualifiedUrl_MatchingExactRequestSucceeds()
        {
            var builder = new RequestBuilder(
                HttpMethod.Get,
                "https://blog.codenizer.nl/api/v1/some/entity?query=param&query=blah&foo=bar",
                null);

            var configuredRequests = ConfiguredRequests.FromRequestBuilders(new[] { builder });

            var match = await configuredRequests
                .MatchAsync(new HttpRequestMessage(HttpMethod.Get,
                    "https://blog.codenizer.nl/api/v1/some/entity?query=param&query=blah&foo=bar"));

            match
                .Should()
                .NotBeNull();
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenGetRequestForRelativeUrl_MatchingRelativeRequestSucceeds()
        {
            var builder = new RequestBuilder(
                HttpMethod.Get,
                "/api/v1/some/entity?query=param&query=blah&foo=bar",
                null);

            var configuredRequests = ConfiguredRequests.FromRequestBuilders(new[] { builder });

            var match = await configuredRequests
                .MatchAsync(new HttpRequestMessage(HttpMethod.Get,
                    "/api/v1/some/entity?query=param&query=blah&foo=bar"));

            match
                .Should()
                .NotBeNull();
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenGetRequestForRelativeUrl_MatchingPostRequestFails()
        {
            var builder = new RequestBuilder(
                HttpMethod.Get,
                "/api/v1/some/entity?query=param&query=blah&foo=bar",
                null);

            var configuredRequests = ConfiguredRequests.FromRequestBuilders(new[] { builder });

            var match = await configuredRequests
                .MatchAsync(new HttpRequestMessage(HttpMethod.Post,
                    "/api/v1/some/entity?query=param&query=blah&foo=bar")
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                });

            match
                .Should()
                .BeNull();
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenGetRequestForRelativeUrlWithQueryParams_MatchingRequestWithoutQueryParamsFails()
        {
            var builder = new RequestBuilder(
                HttpMethod.Get,
                "/api/v1/some/entity?query=param&query=blah&foo=bar",
                null);

            var configuredRequests = ConfiguredRequests.FromRequestBuilders(new[] { builder });

            var match = await configuredRequests
                .MatchAsync(new HttpRequestMessage(HttpMethod.Get,
                    "/api/v1/some/entity"));

            match
                .Should()
                .BeNull();
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenGetRequestForRelativeUrlWithQueryParams_MatchingRequestWithDifferentQueryParamValueFails()
        {
            var builder = new RequestBuilder(
                HttpMethod.Get,
                "/api/v1/some/entity?query=param",
                null);

            var configuredRequests = ConfiguredRequests.FromRequestBuilders(new[] { builder });

            var match = await configuredRequests
                .MatchAsync(new HttpRequestMessage(HttpMethod.Get,
                    "/api/v1/some/entity?query=bar"));

            match
                .Should()
                .BeNull();
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenGetRequestWithAcceptHeader_MatchingRequestWithoutAcceptHeaderFails()
        {
            var builder = new RequestBuilder(
                HttpMethod.Get,
                "/api/v1/some/entity?query=param",
                null);
            
            builder.Accepting("text/plain");

            var configuredRequests = ConfiguredRequests.FromRequestBuilders(new[] { builder });

            var match = await configuredRequests
                .MatchAsync(new HttpRequestMessage(HttpMethod.Get,
                    "/api/v1/some/entity?query=param"));

            match
                .Should()
                .BeNull();
        }
    }
}