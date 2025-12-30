using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Codenizer.HttpClient.Testable.Tests.Unit
{
    public class WhenMatchingContent
    {
        [Fact]
        public async Task GivenRequestWithMatchingContentAssertion_MatchesConfiguredResponse()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo()
                   .Post()
                   .ForUrl("/api/test")
                   .ForContent(content => content.ReadAsStringAsync().Result.Contains("expected"))
                   .With(HttpStatusCode.Created);

            var client = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/test")
            {
                Content = new StringContent("some expected data")
            };

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task GivenRequestWithFailingContentAssertion_ReturnsInternalServerError()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo()
                   .Post()
                   .ForUrl("/api/test")
                   .ForContent(content => content.ReadAsStringAsync().Result.Contains("expected"))
                   .With(HttpStatusCode.Created);

            var client = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/test")
            {
                Content = new StringContent("some other data")
            };

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task GivenRequestWithExceptionInAssertion_ReturnsInternalServerError()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo()
                   .Post()
                   .ForUrl("/api/test")
                   .ForContent(content => throw new Exception("Boom"))
                   .With(HttpStatusCode.Created);

            var client = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/test")
            {
                Content = new StringContent("data")
            };

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task GivenRequestWithJsonContentMatch_MatchesIgnoringWhitespace()
        {
            // Simulate a JSON match where formatting differs but semantic content is same
            // Note: This relies on the user implementing JSON parsing in the assertion
            var handler = new TestableMessageHandler();
            handler.RespondTo()
                   .Post()
                   .ForUrl("/api/test")
                   .ForContent(content => 
                   {
                       var json = content.ReadAsStringAsync().Result;
                       return json.Replace(" ", "").Contains("\"id\":1");
                   })
                   .With(HttpStatusCode.OK);

            var client = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/test")
            {
                Content = new StringContent("{ \"id\": 1, \"name\": \"test\" }")
            };

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GivenForContentStringAndAssertionConfiguredSeparately_LastOneWins()
        {
            // Verify mutual exclusivity: calling ForContent(string) clears assertion
            var handler = new TestableMessageHandler();
            var builder = handler.RespondTo()
                   .Post()
                   .ForUrl("/api/test")
                   .ForContent(c => true); // Assertion set
            
            // Override with string
            builder.ForContent("specific string"); // Assertion should be null now
            builder.With(HttpStatusCode.OK);

            var client = new System.Net.Http.HttpClient(handler);
            
            // 1. Request matching "specific string" -> Should Match
            var request1 = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/test")
            {
                Content = new StringContent("specific string")
            };
            var response1 = await client.SendAsync(request1);
            response1.StatusCode.Should().Be(HttpStatusCode.OK);

            // 2. Request matching predicate (true) but NOT string -> Should Fail
            var request2 = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/test")
            {
                Content = new StringContent("anything else")
            };
            var response2 = await client.SendAsync(request2);
            response2.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }
    }
}
