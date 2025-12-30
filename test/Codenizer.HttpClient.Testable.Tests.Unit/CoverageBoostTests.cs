using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Codenizer.HttpClient.Testable.Tests.Unit
{
    public class CoverageBoostTests
    {
        [Fact]
        public void MultipleResponsesConfiguredException_Serialization_CallsGetObjectData()
        {
            // Arrange
            var originalException = new MultipleResponsesConfiguredException(2, "/api/test");
            #pragma warning disable SYSLIB0050 // Formatter-based serialization is obsolete
            var info = new System.Runtime.Serialization.SerializationInfo(typeof(MultipleResponsesConfiguredException), new System.Runtime.Serialization.FormatterConverter());
            #pragma warning restore SYSLIB0050
            var context = new System.Runtime.Serialization.StreamingContext();

            // Act
            #pragma warning disable CS0618, SYSLIB0051 // Type or member is obsolete
            originalException.GetObjectData(info, context);
            #pragma warning restore CS0618, SYSLIB0051

            // Assert
            info.GetInt32("numberOfResponses").Should().Be(2);
            info.GetString("pathAndQuery").Should().Be("/api/test");
        }

        [Fact]
        public void MultipleResponsesConfiguredException_Deserialization_Constructor()
        {
            // Arrange
            #pragma warning disable SYSLIB0050 // Formatter-based serialization is obsolete
            var info = new System.Runtime.Serialization.SerializationInfo(typeof(MultipleResponsesConfiguredException), new System.Runtime.Serialization.FormatterConverter());
            #pragma warning restore SYSLIB0050
            info.AddValue("numberOfResponses", 5);
            info.AddValue("pathAndQuery", "/api/foo");
            info.AddValue("ClassName", "MultipleResponsesConfiguredException");
            info.AddValue("Message", "Multiple responses configured");
            info.AddValue("InnerException", null);
            info.AddValue("HelpURL", null);
            info.AddValue("StackTraceString", null);
            info.AddValue("RemoteStackTraceString", null);
            info.AddValue("RemoteStackIndex", 0);
            info.AddValue("ExceptionMethod", null);
            info.AddValue("HResult", -2146233088);
            info.AddValue("Source", null);

            var context = new System.Runtime.Serialization.StreamingContext();

            // Act
            #pragma warning disable CS0618 // Type or member is obsolete
            var exception = new TestableMultipleResponsesConfiguredException(info, context);
            #pragma warning restore CS0618

            // Assert
            exception.NumberOfResponses.Should().Be(5);
            exception.PathAndQuery.Should().Be("/api/foo");
        }

        private class TestableMultipleResponsesConfiguredException : MultipleResponsesConfiguredException
        {
            #pragma warning disable CS0618 // Type or member is obsolete
            public TestableMultipleResponsesConfiguredException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
            {
            }
            #pragma warning restore CS0618
        }

        [Fact]
        public void ConfigurationDumpVisitor_GeneratesCorrectOutput()
        {
            var visitor = new ConfigurationDumpVisitor();
            visitor.Method(HttpMethod.Get);
            visitor.Scheme("https");
            visitor.Authority("example.org");
            visitor.Path("/api/test");
            visitor.QueryParameter("foo", "bar");
            visitor.QueryParameter("baz", "qux");
            
            var requestBuilder = new RequestBuilder(HttpMethod.Get, "https://example.org/api/test?foo=bar&baz=qux", null);
            requestBuilder.With(HttpStatusCode.OK).AndCookie("Auth", "123").AndHeaders(new System.Collections.Generic.Dictionary<string, string> { { "X-Test", "Value" } });

            visitor.Response(requestBuilder);

            var output = visitor.Output;

            output.Should().Contain("GET");
            output.Should().Contain("https://");
            output.Should().Contain("example.org");
            output.Should().Contain("/api/test");
            output.Should().Contain("?foo=bar");
            output.Should().Contain("&baz=qux"); // Check for ampersand
            output.Should().Contain("Response:");
            output.Should().Contain("HTTP 200 OK");
            output.Should().Contain("X-Test: Value");
            output.Should().Contain("Set-Cookie: Auth=123");
        }

        [Fact]
        public void ConfigurationDumpVisitor_ContentOutput()
        {
            var visitor = new ConfigurationDumpVisitor();
            visitor.Content("Expected Content");
            
            visitor.Output.Should().Contain("Content: Expected Content");
        }

        [Fact]
        public void ConfigurationDumpVisitor_HeaderOutput()
        {
            var visitor = new ConfigurationDumpVisitor();
            visitor.Header("Accept", "application/json");

            visitor.Output.Should().Contain("Accept: application/json");
        }

        [Fact]
        public void HttpRequestMessageExtensions_GetData_ByteArrayContent()
        {
            var bytes = Encoding.UTF8.GetBytes("Hello World");
            var request = new HttpRequestMessage(HttpMethod.Get, "/");
            request.Content = new ByteArrayContent(bytes);

            var data = request.GetData();

            data.Should().BeEquivalentTo(bytes);
        }

        [Fact]
        public void HttpRequestMessageExtensions_GetData_UnknownContent_ReturnsNull()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/");
            request.Content = new MultipartContent(); // Not String or ByteArrayContent

            var data = request.GetData();

            data.Should().BeNull();
        }
        
        [Fact]
        public void HttpRequestMessageExtensions_GetData_NullContent_ReturnsNull()
        {
             var request = new HttpRequestMessage(HttpMethod.Get, "/");
             request.Content = null;

             var data = request.GetData();

             data.Should().BeNull();
        }

        [Fact]
        public void TestableMessageHandler_GetCurrentConfiguration_ReturnsConfigurationString()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/api/test").With(HttpStatusCode.OK);
            handler.RespondTo().Post().ForUrl("/api/submit").With(HttpStatusCode.Created);

            var config = handler.GetCurrentConfiguration();

            config.Should().NotBeNullOrEmpty();
            config.Should().Contain("/api/test");
            config.Should().Contain("/api/submit");
        }

        [Fact]
        public void MultipleResponsesConfiguredException_NumberOfResponses_ReturnsConfiguredCount()
        {
            var exception = new MultipleResponsesConfiguredException(5, "/api/endpoint");

            exception.NumberOfResponses.Should().Be(5);
        }

        [Fact]
        public void MultipleResponsesConfiguredException_PathAndQuery_ReturnsConfiguredPath()
        {
            var exception = new MultipleResponsesConfiguredException(3, "/api/test?foo=bar");

            exception.PathAndQuery.Should().Be("/api/test?foo=bar");
        }

        [Fact]
        public async void TestableMessageHandler_ClonesRequestWithMultipartContent()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Post().ForUrl("/api/upload").With(HttpStatusCode.OK);

            var client = new System.Net.Http.HttpClient(handler);
            var multipartContent = new MultipartContent();
            multipartContent.Add(new StringContent("part1"));
            multipartContent.Add(new StringContent("part2"));

            await client.PostAsync("http://localhost/api/upload", multipartContent);

            handler.Requests.Should().HaveCount(1);
            var capturedRequest = handler.Requests.First();
            capturedRequest.Content.Should().BeOfType<MultipartContent>();
        }

        [Fact]
        public async void TestableMessageHandler_ClonesRequestWithStreamContent()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Post().ForUrl("/api/stream").With(HttpStatusCode.OK);

            var client = new System.Net.Http.HttpClient(handler);
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("stream data"));
            var streamContent = new StreamContent(stream);

            await client.PostAsync("http://localhost/api/stream", streamContent);

            handler.Requests.Should().HaveCount(1);
            var capturedRequest = handler.Requests.First();
            capturedRequest.Content.Should().BeOfType<StreamContent>();
            var capturedData = await capturedRequest.Content!.ReadAsStringAsync();
            capturedData.Should().Be("stream data");
        }

        [Fact]
        public async void TestableMessageHandler_ClonesRequestHeaders()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/api/headers").With(HttpStatusCode.OK);

            var client = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/headers");
            request.Headers.Add("X-Custom-Header", "custom-value");
            request.Headers.Add("Authorization", "Bearer token123");

            await client.SendAsync(request);

            var capturedRequest = handler.Requests.First();
            capturedRequest.Headers.GetValues("X-Custom-Header").First().Should().Be("custom-value");
            capturedRequest.Headers.GetValues("Authorization").First().Should().Be("Bearer token123");
        }

        [Fact]
        public async void RequestBuilder_AndCookie_WithAllParameters()
        {
            var handler = new TestableMessageHandler();
            var expiryDate = DateTime.UtcNow.AddDays(1);

            handler.RespondTo().Get().ForUrl("/api/test")
                .With(HttpStatusCode.OK)
                .AndCookie("session", "abc123",
                    expiresAt: expiryDate,
                    sameSite: "Strict",
                    secure: true,
                    path: "/api",
                    domain: "example.com",
                    maxAge: 3600);

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/api/test");

            var setCookieHeader = response.Headers.GetValues("Set-Cookie").First();
            setCookieHeader.Should().Contain("session=abc123");
            setCookieHeader.Should().Contain("Expires=");
            setCookieHeader.Should().Contain("SameSite=Strict");
            setCookieHeader.Should().Contain("Secure");
            setCookieHeader.Should().Contain("Path=/api");
            setCookieHeader.Should().Contain("Domain=example.com");
            setCookieHeader.Should().Contain("MaxAge=3600");
        }

        [Fact]
        public async void RequestBuilder_AndHeaders_WithDuplicateKeys()
        {
            var handler = new TestableMessageHandler();

            var headers = new Dictionary<string, string>
            {
                { "X-Custom", "value1" }
            };

            handler.RespondTo().Get().ForUrl("/api/test")
                .With(HttpStatusCode.OK)
                .AndHeaders(headers)
                .AndHeaders(new Dictionary<string, string> { { "X-Custom", "value2" } });

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/api/test");

            var customHeader = response.Headers.GetValues("X-Custom").First();
            customHeader.Should().Be("value1,value2");
        }

        [Fact]
        public void RequestBuilder_WithSequence_ThrowsWhenPathNotSet()
        {
            var handler = new TestableMessageHandler();
            var builder = handler.RespondTo().Get();

            Action action = () => builder.WithSequence(b => b.With(HttpStatusCode.OK));

            action.Should().Throw<ArgumentNullException>()
                .WithMessage("*Request path must be configured*");
        }

        [Fact]
        public async void RequestBuilder_WithSequence_CreatesSequenceFromRoot()
        {
            var handler = new TestableMessageHandler();

            handler.RespondTo().Get().ForUrl("/api/seq")
                .WithSequence(b => b.With(HttpStatusCode.OK).AndContent("text/plain", "first"))
                .WithSequence(b => b.With(HttpStatusCode.OK).AndContent("text/plain", "second"));

            var client = new System.Net.Http.HttpClient(handler);

            var response1 = await client.GetAsync("http://localhost/api/seq");
            var content1 = await response1.Content!.ReadAsStringAsync();
            content1.Should().Be("first");

            var response2 = await client.GetAsync("http://localhost/api/seq");
            var content2 = await response2.Content!.ReadAsStringAsync();
            content2.Should().Be("second");
        }
    }
}
