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
    /// Stress test for matching logic with 100+ diverse routes registered.
    /// Each route is tested individually to verify correct matching.
    /// </summary>
    public class LargeRouteSetMatchingTests
    {
        // Helper record to store route configuration and how to make the request
        private record RouteConfig(
            string UniqueId,
            System.Func<System.Net.Http.HttpClient, Task<HttpResponseMessage>> MakeRequest
        );

        [Fact]
        public async Task ShouldCorrectlyMatchEveryRoute_Among100DifferentRoutes()
        {
            var handler = new TestableMessageHandler();
            var routeConfigs = new List<RouteConfig>();
            
            // ============================================================
            // REGISTER 100+ ROUTES WITH DIVERSE MATCHING CONFIGURATIONS
            // Each route returns a unique identifier
            // ============================================================
            
            // Category 1: Simple GET routes (20 routes)
            for (int i = 0; i < 20; i++)
            {
                var uniqueId = $"GET-Resource-{i}";
                var url = $"/api/v1/resource{i}";
                
                handler.RespondTo()
                       .Get()
                       .ForUrl(url)
                       .With(HttpStatusCode.OK)
                       .AndContent("text/plain", uniqueId);
                
                routeConfigs.Add(new RouteConfig(
                    uniqueId,
                    client => client.GetAsync($"http://localhost{url}")
                ));
            }
            
            // Category 2: GET routes with query parameters (15 routes)
            for (int i = 0; i < 15; i++)
            {
                var uniqueId = $"GET-Search-Category-{i}";
                var url = $"/api/v1/search?category={i}";
                
                handler.RespondTo()
                       .Get()
                       .ForUrl(url)
                       .With(HttpStatusCode.OK)
                       .AndContent("text/plain", uniqueId);
                
                routeConfigs.Add(new RouteConfig(
                    uniqueId,
                    client => client.GetAsync($"http://localhost{url}")
                ));
            }
            
            // Category 3: GET routes with route parameters (15 routes)
            for (int i = 0; i < 15; i++)
            {
                var uniqueId = $"GET-UserProfile-{i}";
                var urlPattern = $"/api/v1/users/{{id}}/profile{i}";
                var actualUrl = $"/api/v1/users/12345/profile{i}";
                
                handler.RespondTo()
                       .Get()
                       .ForUrl(urlPattern)
                       .With(HttpStatusCode.OK)
                       .AndContent("text/plain", uniqueId);
                
                routeConfigs.Add(new RouteConfig(
                    uniqueId,
                    client => client.GetAsync($"http://localhost{actualUrl}")
                ));
            }
            
            // Category 4: POST routes with different paths (15 routes)
            for (int i = 0; i < 15; i++)
            {
                var uniqueId = $"POST-Create-{i}";
                var url = $"/api/v1/create{i}";
                
                handler.RespondTo()
                       .Post()
                       .ForUrl(url)
                       .With(HttpStatusCode.Created)
                       .AndContent("text/plain", uniqueId);
                
                routeConfigs.Add(new RouteConfig(
                    uniqueId,
                    client => client.PostAsync($"http://localhost{url}", new StringContent(""))
                ));
            }
            
            // Category 5: POST routes with specific content matching (10 routes)
            for (int i = 0; i < 10; i++)
            {
                var uniqueId = $"POST-Process-Type-{i}";
                var content = $"{{\"type\":\"{i}\"}}";
                
                handler.RespondTo()
                       .Post()
                       .ForUrl("/api/v1/process")
                       .ForContent(content)
                       .With(HttpStatusCode.OK)
                       .AndContent("text/plain", uniqueId);
                
                var capturedContent = content;
                routeConfigs.Add(new RouteConfig(
                    uniqueId,
                    client => client.PostAsync(
                        "http://localhost/api/v1/process",
                        new StringContent(capturedContent, Encoding.UTF8, "application/json"))
                ));
            }
            
            // Category 6: PUT routes (10 routes)
            for (int i = 0; i < 10; i++)
            {
                var uniqueId = $"PUT-Update-{i}";
                var url = $"/api/v1/update{i}";
                
                handler.RespondTo()
                       .Put()
                       .ForUrl(url)
                       .With(HttpStatusCode.OK)
                       .AndContent("text/plain", uniqueId);
                
                routeConfigs.Add(new RouteConfig(
                    uniqueId,
                    client => client.PutAsync($"http://localhost{url}", new StringContent(""))
                ));
            }
            
            // Category 7: DELETE routes (10 routes) - return unique ID in custom header since no content
            for (int i = 0; i < 10; i++)
            {
                var uniqueId = $"DELETE-Resource-{i}";
                var url = $"/api/v1/delete{i}";
                
                handler.RespondTo()
                       .Delete()
                       .ForUrl(url)
                       .With(HttpStatusCode.OK)
                       .AndContent("text/plain", uniqueId);
                
                routeConfigs.Add(new RouteConfig(
                    uniqueId,
                    client => client.DeleteAsync($"http://localhost{url}")
                ));
            }
            
            // Category 8: Routes with header requirements (5 routes)
            for (int i = 0; i < 5; i++)
            {
                var uniqueId = $"GET-Protected-Version-{i}";
                var headerValue = $"v{i}";
                
                handler.RespondTo()
                       .Get()
                       .ForUrl("/api/v1/protected")
                       .WithHeader("X-Api-Version", headerValue)
                       .With(HttpStatusCode.OK)
                       .AndContent("text/plain", uniqueId);
                
                var capturedHeaderValue = headerValue;
                routeConfigs.Add(new RouteConfig(
                    uniqueId,
                    async client =>
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/v1/protected");
                        request.Headers.Add("X-Api-Version", capturedHeaderValue);
                        return await client.SendAsync(request);
                    }
                ));
            }
            
            // Category 9: Routes with cookie requirements (5 routes)
            for (int i = 0; i < 5; i++)
            {
                var uniqueId = $"GET-Session-Type-{i}";
                var cookieValue = $"type{i}";
                
                handler.RespondTo()
                       .Get()
                       .ForUrl("/api/v1/session")
                       .WithCookie("session-type", cookieValue)
                       .With(HttpStatusCode.OK)
                       .AndContent("text/plain", uniqueId);
                
                var capturedCookieValue = cookieValue;
                routeConfigs.Add(new RouteConfig(
                    uniqueId,
                    async client =>
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/v1/session");
                        request.Headers.Add("Cookie", $"session-type={capturedCookieValue}");
                        return await client.SendAsync(request);
                    }
                ));
            }
            
            // Category 10: Combined matchers - header + query (5 routes)
            for (int i = 0; i < 5; i++)
            {
                var uniqueId = $"GET-Data-Filter-Tenant-{i}";
                var url = $"/api/v2/data?filter={i}";
                var tenantId = $"tenant{i}";
                
                handler.RespondTo()
                       .Get()
                       .ForUrl(url)
                       .WithHeader("X-Tenant-Id", tenantId)
                       .With(HttpStatusCode.OK)
                       .AndContent("text/plain", uniqueId);
                
                var capturedUrl = url;
                var capturedTenantId = tenantId;
                routeConfigs.Add(new RouteConfig(
                    uniqueId,
                    async client =>
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, $"http://localhost{capturedUrl}");
                        request.Headers.Add("X-Tenant-Id", capturedTenantId);
                        return await client.SendAsync(request);
                    }
                ));
            }

            // Verify we have 100+ routes configured
            handler.ConfiguredResponses.Count.Should().BeGreaterOrEqualTo(100, 
                "we should have at least 100 routes registered");
            
            routeConfigs.Count.Should().Be(handler.ConfiguredResponses.Count,
                "each route should have a corresponding test configuration");

            // ============================================================
            // TEST EVERY SINGLE ROUTE
            // ============================================================
            var client = new System.Net.Http.HttpClient(handler);
            var failedRoutes = new List<string>();
            
            foreach (var config in routeConfigs)
            {
                var response = await config.MakeRequest(client);
                var content = await response.Content.ReadAsStringAsync();
                
                if (content != config.UniqueId)
                {
                    failedRoutes.Add($"Route '{config.UniqueId}': Expected '{config.UniqueId}', Got '{content}'");
                }
            }
            
            // ============================================================
            // ASSERT ALL ROUTES MATCHED CORRECTLY
            // ============================================================
            failedRoutes.Should().BeEmpty(
                $"all {routeConfigs.Count} routes should match correctly. Failures:\n" + 
                string.Join("\n", failedRoutes));
        }
    }
}
