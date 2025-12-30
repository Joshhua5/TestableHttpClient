using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;
using Codenizer.HttpClient.Testable;

namespace Codenizer.HttpClient.Testable.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for all response return types.
    /// Return types: String, ByteArray, JSON Object, Sync Callback, Async Callback
    /// Each return type is tested with different content configurations.
    /// </summary>
    public class ResponseReturnTypeTests
    {
        #region String Return Type

        [Fact]
        public async Task ShouldReturnStringContent_PlainText()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/text")
                   .With(HttpStatusCode.OK)
                   .AndContent("text/plain", "Hello, World!");

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/text");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType.MediaType.Should().Be("text/plain");
            (await response.Content.ReadAsStringAsync()).Should().Be("Hello, World!");
        }

        [Fact]
        public async Task ShouldReturnStringContent_Html()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/page")
                   .With(HttpStatusCode.OK)
                   .AndContent("text/html", "<html><body>Page</body></html>");

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/page");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType.MediaType.Should().Be("text/html");
            (await response.Content.ReadAsStringAsync()).Should().Contain("<html>");
        }

        [Fact]
        public async Task ShouldReturnStringContent_Xml()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/xml")
                   .With(HttpStatusCode.OK)
                   .AndContent("application/xml", "<root><item>Value</item></root>");

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/xml");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType.MediaType.Should().Be("application/xml");
        }

        #endregion

        #region ByteArray Return Type

        [Fact]
        public async Task ShouldReturnByteArrayContent()
        {
            var handler = new TestableMessageHandler();
            var binaryData = new byte[] { 0x00, 0x01, 0x02, 0xFF };
            handler.RespondTo().Get().ForUrl("/binary")
                   .With(HttpStatusCode.OK)
                   .AndContent("application/octet-stream", binaryData);

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/binary");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsByteArrayAsync();
            content.Should().Equal(0x00, 0x01, 0x02, 0xFF);
        }

        [Fact]
        public async Task ShouldReturnByteArrayContent_Image()
        {
            var handler = new TestableMessageHandler();
            // PNG header bytes
            var pngHeader = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            handler.RespondTo().Get().ForUrl("/image.png")
                   .With(HttpStatusCode.OK)
                   .AndContent("image/png", pngHeader);

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/image.png");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsByteArrayAsync();
            content.Should().StartWith(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        }

        #endregion

        #region JSON Object Return Type

        [Fact]
        public async Task ShouldReturnJsonContent_AnonymousObject()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/api/user")
                   .With(HttpStatusCode.OK)
                   .AndContent("application/json", new { Id = 1, Name = "John", Email = "john@example.com" });

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/api/user");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType.MediaType.Should().Be("application/json");
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("\"Id\":1");
            content.Should().Contain("\"Name\":\"John\"");
        }

        [Fact]
        public async Task ShouldReturnJsonContent_List()
        {
            var handler = new TestableMessageHandler();
            var users = new[] 
            { 
                new { Id = 1, Name = "Alice" },
                new { Id = 2, Name = "Bob" }
            };
            handler.RespondTo().Get().ForUrl("/api/users")
                   .With(HttpStatusCode.OK)
                   .AndContent("application/json", users);

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/api/users");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().StartWith("[");
            content.Should().Contain("Alice");
            content.Should().Contain("Bob");
        }

        [Fact]
        public async Task ShouldReturnJsonContent_UsingAndJsonContent()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/api/data")
                   .With(HttpStatusCode.OK)
                   .AndJsonContent(new { Status = "OK", Count = 42 });

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/api/data");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("\"Status\":\"OK\"");
            content.Should().Contain("\"Count\":42");
        }

        [Fact]
        public async Task ShouldReturnJsonContent_WithCustomSerializerSettings()
        {
            var handler = new TestableMessageHandler();
            var settings = new JsonSerializerSettings { Formatting = Formatting.Indented };
            handler.RespondTo().Get().ForUrl("/api/formatted")
                   .With(HttpStatusCode.OK)
                   .AndJsonContent(new { Name = "Test" }, settings);

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/api/formatted");

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("\n"); // Indented formatting includes newlines
        }

        #endregion

        #region Sync Callback Return Type

        [Fact]
        public async Task ShouldReturnContentFromSyncCallback_String()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/dynamic")
                   .With(HttpStatusCode.OK)
                   .AndContent("text/plain", req => $"Requested: {req.RequestUri.PathAndQuery}");

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/dynamic");

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Be("Requested: /dynamic");
        }

        [Fact]
        public async Task ShouldReturnContentFromSyncCallback_ByteArray()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/dynamic-bytes")
                   .With(HttpStatusCode.OK)
                   .AndContent("application/octet-stream", req => new byte[] { 0xAB, 0xCD });

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/dynamic-bytes");

            var content = await response.Content.ReadAsByteArrayAsync();
            content.Should().Equal(0xAB, 0xCD);
        }

        [Fact]
        public async Task ShouldReturnContentFromSyncCallback_JsonObject()
        {
            var handler = new TestableMessageHandler();
            var counter = 0;
            handler.RespondTo().Get().ForUrl("/counter")
                   .With(HttpStatusCode.OK)
                   .AndContent("application/json", req => new { CallNumber = ++counter });

            var client = new System.Net.Http.HttpClient(handler);

            var response1 = await client.GetAsync("http://localhost/counter");
            var content1 = await response1.Content.ReadAsStringAsync();
            content1.Should().Contain("\"CallNumber\":1");

            var response2 = await client.GetAsync("http://localhost/counter");
            var content2 = await response2.Content.ReadAsStringAsync();
            content2.Should().Contain("\"CallNumber\":2");
        }

        [Fact]
        public async Task ShouldReturnContentFromSyncCallback_BasedOnRequestContent()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Post().ForUrl("/echo")
                   .With(HttpStatusCode.OK)
                   .AndContent("text/plain", req => 
                   {
                       var body = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "";
                       return $"Echo: {body}";
                   });

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.PostAsync("http://localhost/echo", 
                new StringContent("Hello", Encoding.UTF8, "text/plain"));

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Be("Echo: Hello");
        }

        #endregion

        #region Async Callback Return Type

        [Fact]
        public async Task ShouldReturnContentFromAsyncCallback_String()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/async")
                   .With(HttpStatusCode.OK)
                   .AndContent("text/plain", async req => 
                   {
                       await Task.Delay(1); // Simulate async work
                       return "Async Response";
                   });

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/async");

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Be("Async Response");
        }

        [Fact]
        public async Task ShouldReturnContentFromAsyncCallback_JsonObject()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/async-json")
                   .With(HttpStatusCode.OK)
                   .AndContent("application/json", async req => 
                   {
                       await Task.Delay(1);
                       return new { Async = true, Message = "From Async" };
                   });

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/async-json");

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("\"Async\":true");
            content.Should().Contain("\"Message\":\"From Async\"");
        }

        #endregion

        #region Response Headers

        [Fact]
        public async Task ShouldReturnResponseWithCustomHeaders()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/with-headers")
                   .With(HttpStatusCode.OK)
                   .AndHeaders(new System.Collections.Generic.Dictionary<string, string>
                   {
                       { "X-Custom-Header", "CustomValue" },
                       { "X-Request-Id", "12345" }
                   })
                   .AndContent("text/plain", "Response with headers");

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/with-headers");

            response.Headers.GetValues("X-Custom-Header").Should().Contain("CustomValue");
            response.Headers.GetValues("X-Request-Id").Should().Contain("12345");
        }

        #endregion

        #region Response Cookies

        [Fact]
        public async Task ShouldReturnResponseWithCookie()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/login")
                   .With(HttpStatusCode.OK)
                   .AndCookie("session", "abc123")
                   .AndContent("text/plain", "Logged in");

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/login");

            response.Headers.GetValues("Set-Cookie").Should().Contain(c => c.StartsWith("session=abc123"));
        }

        [Fact]
        public async Task ShouldReturnResponseWithCookieAndOptions()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Get().ForUrl("/secure-login")
                   .With(HttpStatusCode.OK)
                   .AndCookie("session", "secure-token", secure: true, path: "/", sameSite: "Strict")
                   .AndContent("text/plain", "Secure login");

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.GetAsync("http://localhost/secure-login");

            var setCookie = string.Join(", ", response.Headers.GetValues("Set-Cookie"));
            setCookie.Should().Contain("session=secure-token");
            setCookie.Should().Contain("Secure");
            setCookie.Should().Contain("SameSite=Strict");
        }

        #endregion

        #region Empty/Null Content

        [Fact]
        public async Task ShouldReturnNoContent()
        {
            var handler = new TestableMessageHandler();
            handler.RespondTo().Delete().ForUrl("/api/resource/1")
                   .With(HttpStatusCode.NoContent);

            var client = new System.Net.Http.HttpClient(handler);
            var response = await client.DeleteAsync("http://localhost/api/resource/1");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        #endregion
    }
}
