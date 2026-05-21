// -----------------------------------------------------------------------
// <copyright file="HuggingFaceToolCallingExtensions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.AI.Agents.Models;

namespace Compendium.Adapters.HuggingFace.Tools;

/// <summary>
/// Best-effort tool / function calling helpers for Hugging Face Inference Endpoints. Most modern
/// TGI-backed endpoints accept the OpenAI-compatible <c>tools</c> + <c>tool_choice</c> body fields
/// — but not all. Older endpoints, vision/audio endpoints, and non-TGI runtimes will silently
/// ignore the fields. When the upstream does not support tools the call still succeeds and the
/// response simply carries no <see cref="AgentToolInvocation"/> entries.
/// </summary>
public static class HuggingFaceToolCallingExtensions
{
    /// <summary>Key inside <see cref="CompletionRequest.AdditionalParameters"/> carrying the tool list.</summary>
    public const string ToolsKey = "huggingface.tools";

    /// <summary>Key inside <see cref="CompletionRequest.AdditionalParameters"/> carrying the tool_choice value.</summary>
    public const string ToolChoiceKey = "huggingface.tool_choice";

    /// <summary>Key inside <see cref="CompletionResponse.Metadata"/> carrying the assistant's tool_calls.</summary>
    public const string ToolCallsMetadataKey = "huggingface.tool_calls";

    /// <summary>
    /// Attaches a tool catalog to a completion request. The endpoint is expected to surface tool
    /// invocations back in <see cref="CompletionResponse.Metadata"/> under <see cref="ToolCallsMetadataKey"/>;
    /// when the backend doesn't support tools the metadata key is simply absent.
    /// </summary>
    /// <param name="request">The request to clone.</param>
    /// <param name="tools">The tools to expose; ignored when empty.</param>
    /// <param name="toolChoice">Optional choice strategy ("auto", "required", "none", or a tool name).</param>
    public static CompletionRequest WithTools(
        this CompletionRequest request,
        IReadOnlyList<AgentTool> tools,
        string? toolChoice = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(tools);

        var dict = new Dictionary<string, object>(request.AdditionalParameters ?? new Dictionary<string, object>())
        {
            [ToolsKey] = tools
        };
        if (!string.IsNullOrEmpty(toolChoice))
        {
            dict[ToolChoiceKey] = toolChoice;
        }

        return request with { AdditionalParameters = dict };
    }

    /// <summary>
    /// Reads back tool calls the model requested, if any. Returns an empty list when the model
    /// did not call a tool (including when the upstream silently ignored the tools field).
    /// </summary>
    public static IReadOnlyList<AgentToolInvocation> GetToolCalls(this CompletionResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (response.Metadata != null
            && response.Metadata.TryGetValue(ToolCallsMetadataKey, out var raw)
            && raw is IReadOnlyList<AgentToolInvocation> invocations)
        {
            return invocations;
        }
        return Array.Empty<AgentToolInvocation>();
    }
}
