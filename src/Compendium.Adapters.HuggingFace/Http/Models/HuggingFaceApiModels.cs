// -----------------------------------------------------------------------
// <copyright file="HuggingFaceApiModels.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.HuggingFace.Http.Models;

/// <summary>
/// Hugging Face chat completion request (OpenAI-compatible Messages API shape — used both for
/// dedicated Inference Endpoints with a TGI backend and the serverless inference API behind
/// <c>/v1/chat/completions</c>).
/// </summary>
internal sealed class HuggingFaceChatCompletionRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; set; }

    [JsonPropertyName("messages")]
    public required List<HuggingFaceChatMessage> Messages { get; set; }

    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; }

    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("top_p")]
    public float? TopP { get; set; }

    [JsonPropertyName("frequency_penalty")]
    public float? FrequencyPenalty { get; set; }

    [JsonPropertyName("presence_penalty")]
    public float? PresencePenalty { get; set; }

    [JsonPropertyName("stop")]
    public List<string>? Stop { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    [JsonPropertyName("tools")]
    public List<HuggingFaceToolDefinition>? Tools { get; set; }

    [JsonPropertyName("tool_choice")]
    public object? ToolChoice { get; set; }
}

internal sealed class HuggingFaceChatMessage
{
    [JsonPropertyName("role")]
    public required string Role { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("tool_call_id")]
    public string? ToolCallId { get; set; }

    [JsonPropertyName("tool_calls")]
    public List<HuggingFaceToolCall>? ToolCalls { get; set; }
}

internal sealed class HuggingFaceToolDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public required HuggingFaceFunctionDefinition Function { get; set; }
}

internal sealed class HuggingFaceFunctionDefinition
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("parameters")]
    public JsonElement? Parameters { get; set; }
}

internal sealed class HuggingFaceToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public HuggingFaceToolCallFunction? Function { get; set; }
}

internal sealed class HuggingFaceToolCallFunction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;
}

internal sealed class HuggingFaceChatCompletionResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("choices")]
    public List<HuggingFaceChatChoice> Choices { get; set; } = new();

    [JsonPropertyName("usage")]
    public HuggingFaceUsage? Usage { get; set; }
}

internal sealed class HuggingFaceChatChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public HuggingFaceChatMessage? Message { get; set; }

    [JsonPropertyName("delta")]
    public HuggingFaceChatDelta? Delta { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

internal sealed class HuggingFaceChatDelta
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("tool_calls")]
    public List<HuggingFaceToolCall>? ToolCalls { get; set; }
}

internal sealed class HuggingFaceUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

internal sealed class HuggingFaceStreamChunk
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<HuggingFaceChatChoice> Choices { get; set; } = new();

    [JsonPropertyName("usage")]
    public HuggingFaceUsage? Usage { get; set; }
}

/// <summary>
/// Embedding request for the <em>feature-extraction</em> task. The endpoint accepts either a single
/// string or an array of strings under the <c>inputs</c> key.
/// </summary>
internal sealed class HuggingFaceFeatureExtractionRequest
{
    [JsonPropertyName("inputs")]
    public required List<string> Inputs { get; set; }
}

/// <summary>
/// Hugging Face returns errors in one of two shapes:
/// <list type="bullet">
///   <item><description><c>{ "error": "message string" }</c></description></item>
///   <item><description><c>{ "error": { "message": "…", "type": "…" } }</c></description></item>
/// </list>
/// <see cref="ErrorRaw"/> captures the raw value so the HTTP client can dispatch on the shape.
/// </summary>
internal sealed class HuggingFaceErrorResponse
{
    [JsonPropertyName("error")]
    public JsonElement? ErrorRaw { get; set; }
}
