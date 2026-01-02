using System;
using System.Collections.Generic;
using Codenizer.HttpClient.Testable.AnthropicServer.Models;

namespace Codenizer.HttpClient.Testable.AnthropicServer.Handlers
{
    internal class MessagesHandler
    {
        public MessageResponse Create(MessageRequest request)
        {
            return new MessageResponse
            {
                Id = $"msg_{Guid.NewGuid()}",
                Model = request.Model,
                Role = "assistant",
                Content = new List<ContentBlock>
                {
                    new ContentBlock
                    {
                        Type = "text",
                        Text = "This is a simulated response from the Anthropic API."
                    }
                },
                StopReason = "end_turn",
                StopSequence = null,
                Usage = new Usage
                {
                    InputTokens = 10,
                    OutputTokens = 20
                }
            };
        }
    }
}
