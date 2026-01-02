using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Codenizer.HttpClient.Testable.AirtableServer;
using Codenizer.HttpClient.Testable.AirtableServer.Models;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Codenizer.HttpClient.Testable.Tests.Unit
{
    public class AirtableServerTests
    {
        [Fact]
        public async Task CanCreateAndListRecords()
        {
            var handler = new TestableMessageHandler();
            var server = new AirtableSimulatedServer();
            // Seed a base
            server.State.Bases.Add("appTes1base", new AirtableBase { Id = "appTes1base", Name = "Test Base" });
            // Seed a table
            server.State.CreateTable("appTes1base", "Tasks", null, new List<AirtableField> { new AirtableField { Name = "Name", Type = AirtableFieldTypes.SingleLineText } });
            
            server.State.CreateRecord("appTes1base", "Tasks", new Dictionary<string, object?> { { "Name", "Task A" } });
            
            handler.RespondTo().Post().ForUrl("https://api.airtable.com/v0/appTes1base/Tasks").HandledBy(server);
            handler.RespondTo().Get().ForUrl("https://api.airtable.com/v0/appTes1base/Tasks").HandledBy(server);

            var client = new System.Net.Http.HttpClient(handler);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "pat123");

            // Create
            var createRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.airtable.com/v0/appTes1base/Tasks");
            createRequest.Content = new StringContent("{\"fields\": {\"Name\": \"Task B\"}}", System.Text.Encoding.UTF8, "application/json");
            var createResponse = await client.SendAsync(createRequest);
            createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // List
            var listResponse = await client.GetAsync("https://api.airtable.com/v0/appTes1base/Tasks");
            listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await listResponse.Content.ReadAsStringAsync();
            var result = JObject.Parse(content);
            var records = result["records"]?.ToObject<List<AirtableRecord>>();
            records.Should().NotBeNull();
            
            records.Should().HaveCount(2);
            records.Should().Contain(r => r.Fields["Name"].ToString() == "Task A");
            records.Should().Contain(r => r.Fields["Name"].ToString() == "Task B");
        }

        [Fact]
        public async Task CanFilterUsingSimpleFormula()
        {
            var handler = new TestableMessageHandler();
            var server = new AirtableSimulatedServer();
            server.State.Bases.Add("appSimple", new AirtableBase { Id = "appSimple", Name = "Test Base" });
            server.State.CreateTable("appSimple", "Tasks", null, new List<AirtableField> 
            { 
                new AirtableField { Name = "Name", Type = AirtableFieldTypes.SingleLineText },
                new AirtableField { Name = "Status", Type = AirtableFieldTypes.SingleSelect }
            });
            
            // Seed records directly
            server.State.CreateRecord("appSimple", "Tasks", new Dictionary<string, object?> { { "Status", "Todo" }, { "Name", "Task 1" } });
            server.State.CreateRecord("appSimple", "Tasks", new Dictionary<string, object?> { { "Status", "Done" }, { "Name", "Task 2" } });
            
            var formula = "{Status}='Done'";
            var encodedFormula = Uri.EscapeDataString(formula);
            var url = $"https://api.airtable.com/v0/appSimple/Tasks?filterByFormula={encodedFormula}";

            handler.RespondTo().Get().ForUrl(url).HandledBy(server);
            var client = new System.Net.Http.HttpClient(handler);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "pat123");

            // Filter
            var response = await client.GetAsync(url);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JObject.Parse(content);
            var records = result["records"]?.ToObject<List<AirtableRecord>>();
            records.Should().NotBeNull();

            records.Should().HaveCount(1);
            records[0].Fields["Name"].ToString().Should().Be("Task 2");
        }

        [Fact]
        public async Task CanFilterUsingComplexFormula()
        {
            var handler = new TestableMessageHandler();
            var server = new AirtableSimulatedServer();
            server.State.Bases.Add("appComplex", new AirtableBase { Id = "appComplex", Name = "Test Base" });
            server.State.CreateTable("appComplex", "Tasks", null, new List<AirtableField> 
            { 
                new AirtableField { Name = "Name", Type = AirtableFieldTypes.SingleLineText },
                new AirtableField { Name = "Status", Type = AirtableFieldTypes.SingleSelect },
                new AirtableField { Name = "Priority", Type = AirtableFieldTypes.SingleSelect }
            });

            server.State.CreateRecord("appComplex", "Tasks", new Dictionary<string, object?> { { "Status", "Todo" }, { "Priority", "High" }, { "Name", "Include Me" } });
            server.State.CreateRecord("appComplex", "Tasks", new Dictionary<string, object?> { { "Status", "Done" }, { "Priority", "High" }, { "Name", "Exclude Me" } });
            server.State.CreateRecord("appComplex", "Tasks", new Dictionary<string, object?> { { "Status", "Todo" }, { "Priority", "Low" }, { "Name", "Exclude Me Too" } });
            
            var formula = "AND({Status}='Todo', {Priority}='High')";
            var encodedFormula = Uri.EscapeDataString(formula);
            var url = $"https://api.airtable.com/v0/appComplex/Tasks?filterByFormula={encodedFormula}";

            handler.RespondTo().Get().ForUrl(url).HandledBy(server);
            var client = new System.Net.Http.HttpClient(handler);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "pat123");

            var response = await client.GetAsync(url);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JObject.Parse(content);
            var records = result["records"]?.ToObject<List<AirtableRecord>>();
            records.Should().NotBeNull();

            records.Should().HaveCount(1);
            records[0].Fields["Name"].ToString().Should().Be("Include Me");
        }

        [Fact]
        public async Task CanFilterUsingOrFindFormula()
        {
            var handler = new TestableMessageHandler();
            var server = new AirtableSimulatedServer();
            server.State.Bases.Add("appOr", new AirtableBase { Id = "appOr", Name = "Test Base" });
            server.State.CreateTable("appOr", "Items", null, new List<AirtableField> { new AirtableField { Name = "Name", Type = AirtableFieldTypes.SingleLineText } });

            server.State.CreateRecord("appOr", "Items", new Dictionary<string, object?> { { "Name", "Apple" } });
            server.State.CreateRecord("appOr", "Items", new Dictionary<string, object?> { { "Name", "Banana" } });
            server.State.CreateRecord("appOr", "Items", new Dictionary<string, object?> { { "Name", "Cherry" } });
            
            var formula = "OR(FIND('App', {Name}), FIND('Cher', {Name}))";
            var encodedFormula = Uri.EscapeDataString(formula);
            var url = $"https://api.airtable.com/v0/appOr/Items?filterByFormula={encodedFormula}";

            handler.RespondTo().Get().ForUrl(url).HandledBy(server);
            var client = new System.Net.Http.HttpClient(handler);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "pat123");

            var response = await client.GetAsync(url);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JObject.Parse(content);
            var records = result["records"]?.ToObject<List<AirtableRecord>>();
            records.Should().NotBeNull();

            records.Should().HaveCount(2);
            records.Should().Contain(r => r.Fields["Name"].ToString() == "Apple");
            records.Should().Contain(r => r.Fields["Name"].ToString() == "Cherry");
        }

        [Fact]
        public async Task CanFilterUsingView()
        {
            var handler = new TestableMessageHandler();
            var server = new AirtableSimulatedServer();
            server.State.Bases.Add("appView", new AirtableBase { Id = "appView", Name = "Test Base" });
            server.State.CreateTable("appView", "Tasks", null, new List<AirtableField> 
            { 
                new AirtableField { Name = "Name", Type = AirtableFieldTypes.SingleLineText },
                new AirtableField { Name = "Status", Type = AirtableFieldTypes.SingleSelect }
            });

            server.State.CreateRecord("appView", "Tasks", new Dictionary<string, object?> { { "Status", "Todo" }, { "Name", "Keep" } });
            server.State.CreateRecord("appView", "Tasks", new Dictionary<string, object?> { { "Status", "Done" }, { "Name", "Hide" } });

            var table = server.State.GetTable("appView", "Tasks");
            table.Should().NotBeNull();
            table!.Views.Add(new AirtableView 
            { 
                Id = "viw123", 
                Name = "Todo View", 
                FilterFormula = "{Status}='Todo'",
                VisibleFields = new List<string> { "Name" }
            });

            var url = "https://api.airtable.com/v0/appView/Tasks?view=Todo%20View";
            handler.RespondTo().Get().ForUrl(url).HandledBy(server);

            var client = new System.Net.Http.HttpClient(handler);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "pat123");

            // Use view
            var response = await client.GetAsync(url);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var result = JObject.Parse(content);
            var records = result["records"]?.ToObject<List<AirtableRecord>>();
            records.Should().NotBeNull();

            records.Should().HaveCount(1);
            var rec = records[0];
            rec.Fields["Name"]!.ToString().Should().Be("Keep");
            // VisibleFields should filter out "Status"
            rec.Fields.ContainsKey("Status").Should().BeFalse();
        }

        [Fact]
        public async Task CanGenerateAndRetrieveWebhooks()
        {
            var handler = new TestableMessageHandler();
            var server = new AirtableSimulatedServer();
            server.State.Bases.Add("appHook", new AirtableBase { Id = "appHook", Name = "Test Base" });
            server.State.CreateTable("appHook", "Tasks", null, new List<AirtableField> { new AirtableField { Name = "Name", Type = AirtableFieldTypes.SingleLineText } });

            // Setup webhooks endpoints
            handler.RespondTo().Post().ForUrl("https://api.airtable.com/v0/bases/appHook/webhooks").HandledBy(server);
            handler.RespondTo().Get().ForUrl("/v0/bases/appHook/webhooks/{webhookId}/payloads").HandledBy(server);
            handler.RespondTo().Post().ForUrl("https://api.airtable.com/v0/appHook/Tasks").HandledBy(server);

            var client = new System.Net.Http.HttpClient(handler);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "pat123");

            // 1. Create Webhook
            var createHookReq = new HttpRequestMessage(HttpMethod.Post, "https://api.airtable.com/v0/bases/appHook/webhooks");
            createHookReq.Content = new StringContent("{\"notificationUrl\": \"https://example.com/cb\", \"specification\": {\"options\": {\"filters\": {\"dataTypes\": [\"tableData\"]}}}}", System.Text.Encoding.UTF8, "application/json");
            var hookResp = await client.SendAsync(createHookReq);
            hookResp.StatusCode.Should().Be(HttpStatusCode.OK);
            var hookJson = JObject.Parse(await hookResp.Content.ReadAsStringAsync());
            var webhookId = hookJson["id"]?.ToString();
            webhookId.Should().NotBeNull();

            // 2. Perform Action (Create Record)
            var createRecReq = new HttpRequestMessage(HttpMethod.Post, "https://api.airtable.com/v0/appHook/Tasks");
            createRecReq.Content = new StringContent("{\"fields\": {\"Name\": \"Trigger Event\"}}", System.Text.Encoding.UTF8, "application/json");
            var createRecResp = await client.SendAsync(createRecReq);
            createRecResp.StatusCode.Should().Be(HttpStatusCode.OK); // Ensure record creation succeeds

            // 3. Retrieve Payloads
            var payloadsResp = await client.GetAsync($"https://api.airtable.com/v0/bases/appHook/webhooks/{webhookId}/payloads");
            payloadsResp.StatusCode.Should().Be(HttpStatusCode.OK);
            var payloadsJson = JObject.Parse(await payloadsResp.Content.ReadAsStringAsync());
            var payloads = payloadsJson["payloads"]?.ToObject<JArray>();
            payloads.Should().NotBeNull();

            payloads.Should().HaveCount(1);
            payloads![0]["changedTablesById"].Should().NotBeNull();
        }
    }
}
