using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Codenizer.HttpClient.Testable.Tests.Unit
{
    public class ConcurrencyStressTests
    {
        [Fact]
        public async Task ShouldHandle1000ParallelRegistrationsAndRequests()
        {
            var handler = new TestableMessageHandler();
            var client = new System.Net.Http.HttpClient(handler);
            var iterations = 1000;
            var addTasks = new Task[iterations];
            var requestTasks = new Task<HttpResponseMessage>[iterations];
            
            // We want to interleave adding and requesting to stress the dirty flag / lock logic
            
            // 1. Start tasks to add routes
            for (var i = 0; i < iterations; i++)
            {
                var id = i;
                addTasks[i] = Task.Run(() =>
                {
                    handler.RespondTo()
                           .Get()
                           .ForUrl($"/api/stress/{id}")
                           .With(HttpStatusCode.OK)
                           .AndContent("text/plain", $"Response-{id}");
                });
            }

            // 2. Start tasks to execute requests
            // Some might fail initially if the route isn't added yet, so we'll retry a few times
            // to simulate a real-world scenario where clients might hit a handler while it's being configured
            for (var i = 0; i < iterations; i++)
            {
                var id = i;
                requestTasks[i] = Task.Run(async () =>
                {
                    HttpResponseMessage? response = null;
                    var attempts = 0;
                    
                    while (attempts < 10)
                    {
                        response = await client.GetAsync($"http://localhost/api/stress/{id}");
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            break;
                        }

                        attempts++;
                        await Task.Delay(5); // Wait a bit for the registration task to catch up
                    }

                    return response!;
                });
            }

            // 3. Wait for all registrations and requests to complete
            await Task.WhenAll(addTasks);
            var responses = await Task.WhenAll(requestTasks);

            // 4. Verify results
            var successes = 0;
            var failureDetails = new ConcurrentBag<string>();

            for (var i = 0; i < iterations; i++)
            {
                var response = responses[i];
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (content == $"Response-{i}")
                    {
                        successes++;
                    }
                    else
                    {
                        failureDetails.Add($"Request {i}: Content mismatch. Expected 'Response-{i}', Got '{content}'");
                    }
                }
                else
                {
                    failureDetails.Add($"Request {i}: Failed after retries. Status code: {response.StatusCode}");
                }
            }

            successes.Should().Be(iterations, 
                $"all {iterations} parallel requests should succeed. Failures:\n" + string.Join("\n", failureDetails.Take(10)));
        }

        [Fact]
        public async Task ShouldNotDeadlockWhenRebuildingTreeUnderHeavyLoad()
        {
            var handler = new TestableMessageHandler();
            var client = new System.Net.Http.HttpClient(handler);
            var iterations = 500;
            
            // Pre-register some routes
            for (var i = 0; i < 100; i++)
            {
                handler.RespondTo().Get().ForUrl($"/pre/{i}").With(HttpStatusCode.OK);
            }

            var tasks = new List<Task>();
            var stopRunning = false;

            // Start a task that constantly adds/removes routes to trigger tree rebuilds
            var modifyTask = Task.Run(async () =>
            {
                var counter = 0;
                while (!stopRunning)
                {
                    handler.RespondTo().Get().ForUrl($"/dynamic/{counter++}").With(HttpStatusCode.OK);
                    if (counter % 10 == 0)
                    {
                        // Clear occasionally to stress the Clear -> Rebuild path
                        handler.ClearConfiguredResponses();
                        // Re-add essentials
                        handler.RespondTo().Get().ForUrl("/essential").With(HttpStatusCode.OK);
                    }
                    await Task.Delay(1);
                }
            });

            // Start many tasks hitting the "essential" endpoint
            for (var i = 0; i < iterations; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    while (!stopRunning)
                    {
                        try
                        {
                            var response = await client.GetAsync("http://localhost/essential");
                            // We don't assert OK here because the Dynamic task might have cleared it
                            // the goal is to ensure no Deadlocks or Exceptions occur in the library
                        }
                        catch (ResponseConfigurationException)
                        {
                            // Expected - route might have been cleared/not yet re-added in the race
                            // This is acceptable for this stress test
                        }
                        catch (Exception ex)
                        {
                            // Unexpected exceptions indicate a real problem
                            throw new Exception("Exception during parallel execution", ex);
                        }
                    }
                }));
            }

            // Let it run for a bit
            await Task.Delay(500);
            stopRunning = true;
            
            await Task.WhenAll(tasks);
            await modifyTask;

            // Verification: The fact that we finished without a deadlock or unhandled exception is a success
        }
    }
}
