using System;
using System.Collections.Generic;
using Codenizer.HttpClient.Testable.AnthropicServer.Models;

namespace Codenizer.HttpClient.Testable.AnthropicServer.Handlers
{
    internal class ModelsHandler
    {
        public ModelListResponse List()
        {
            return new ModelListResponse
            {
                Data = new List<ModelData>
                {
                    new ModelData { Id = "claude-3-opus-20240229", DisplayName = "Claude 3 Opus", CreatedAt = DateTime.UtcNow },
                    new ModelData { Id = "claude-3-sonnet-20240229", DisplayName = "Claude 3 Sonnet", CreatedAt = DateTime.UtcNow },
                    new ModelData { Id = "claude-3-haiku-20240307", DisplayName = "Claude 3 Haiku", CreatedAt = DateTime.UtcNow }
                },
                HasMore = false,
                FirstId = "claude-3-opus-20240229",
                LastId = "claude-3-haiku-20240307"
            };
        }
    }
}
