using System;
using System.Collections.Generic;
using Codenizer.HttpClient.Testable.AnthropicServer.Models;

namespace Codenizer.HttpClient.Testable.AnthropicServer.Handlers
{
    internal class BatchesHandler
    {
        private readonly AnthropicState _state;

        public BatchesHandler(AnthropicState state)
        {
            _state = state;
        }

        public MessageBatch Create(CreateMessageBatchRequest request)
        {
            var batch = new MessageBatch
            {
                Id = $"batch_{Guid.NewGuid()}",
                ProcessingStatus = "in_progress",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                RequestCounts = new BatchRequestCounts
                {
                    Processing = request.Requests.Count,
                    Succeeded = 0,
                    Errored = 0,
                    Canceled = 0,
                    Expired = 0
                }
            };

            _state.Batches.TryAdd(batch.Id, batch);
            return batch;
        }

        public MessageBatch? Retrieve(string batchId)
        {
            if (_state.Batches.TryGetValue(batchId, out var batch))
            {
                return batch;
            }
            return null;
        }

        public MessageBatch? Cancel(string batchId)
        {
            if (_state.Batches.TryGetValue(batchId, out var batch))
            {
                batch.ProcessingStatus = "canceled";
                batch.CancelInitiatedAt = DateTime.UtcNow;
                
                // Move processing to canceled count
                batch.RequestCounts.Canceled += batch.RequestCounts.Processing;
                batch.RequestCounts.Processing = 0;
                
                return batch;
            }
            return null;
        }
    }
}
