using Newtonsoft.Json;
using System.Collections.Generic;

namespace Codenizer.HttpClient.Testable.AnthropicServer.Models
{
    public class MessageRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("messages")]
        public List<Message> Messages { get; set; } = new();

        [JsonProperty("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonProperty("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }

        [JsonProperty("stop_sequences")]
        public List<string>? StopSequences { get; set; }

        [JsonProperty("stream")]
        public bool Stream { get; set; }

        [JsonProperty("system")]
        public string? System { get; set; }

        [JsonProperty("temperature")]
        public float? Temperature { get; set; }

        [JsonProperty("tool_choice")]
        public object? ToolChoice { get; set; }

        [JsonProperty("tools")]
        public List<Tool>? Tools { get; set; }

        [JsonProperty("top_k")]
        public int? TopK { get; set; }

        [JsonProperty("top_p")]
        public float? TopP { get; set; }
    }

    public class Message
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public object Content { get; set; } // string or List<ContentBlock>
    }

    public class ContentBlock
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("text")]
        public string? Text { get; set; } // for text blocks

        [JsonProperty("id")]
        public string? Id { get; set; } // for tool_use

        [JsonProperty("name")]
        public string? Name { get; set; } // for tool_use

        [JsonProperty("input")]
        public object? Input { get; set; } // for tool_use

        [JsonProperty("tool_use_id")]
        public string? ToolUseId { get; set; } // for tool_result

        [JsonProperty("content")]
        public object? Content { get; set; } // for tool_result
    }

    public class Tool
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("input_schema")]
        public object InputSchema { get; set; }
    }

    public class MessageResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = "message";

        [JsonProperty("role")]
        public string Role { get; set; } = "assistant";

        [JsonProperty("content")]
        public List<ContentBlock> Content { get; set; } = new();

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("stop_reason")]
        public string? StopReason { get; set; }

        [JsonProperty("stop_sequence")]
        public string? StopSequence { get; set; }

        [JsonProperty("usage")]
        public Usage Usage { get; set; }
    }

    public class Usage
    {
        [JsonProperty("input_tokens")]
        public int InputTokens { get; set; }

        [JsonProperty("output_tokens")]
        public int OutputTokens { get; set; }
    }
    public class CountTokensResponse
    {
        [JsonProperty("input_tokens")]
        public int InputTokens { get; set; }
    }
}
