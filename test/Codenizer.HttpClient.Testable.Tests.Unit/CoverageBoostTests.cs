using System;
using System.IO;
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
            var info = new System.Runtime.Serialization.SerializationInfo(typeof(MultipleResponsesConfiguredException), new System.Runtime.Serialization.FormatterConverter());
            var context = new System.Runtime.Serialization.StreamingContext();

            // Act
            #pragma warning disable CS0618 // Type or member is obsolete
            originalException.GetObjectData(info, context);
            #pragma warning restore CS0618

            // Assert
            info.GetInt32("numberOfResponses").Should().Be(2);
            info.GetString("pathAndQuery").Should().Be("/api/test");
        }

        [Fact]
        public void MultipleResponsesConfiguredException_Deserialization_Constructor()
        {
            // Arrange
            var info = new System.Runtime.Serialization.SerializationInfo(typeof(MultipleResponsesConfiguredException), new System.Runtime.Serialization.FormatterConverter());
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
            var exception = new TestableMultipleResponsesConfiguredException(info, context);

            // Assert
            exception.NumberOfResponses.Should().Be(5);
            exception.PathAndQuery.Should().Be("/api/foo");
        }

        private class TestableMultipleResponsesConfiguredException : MultipleResponsesConfiguredException
        {
            public TestableMultipleResponsesConfiguredException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
            {
            }
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
    }
}
