// -----------------------------------------------------------------------
// <copyright file="HuggingFaceOptions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.HuggingFace.Configuration;

/// <summary>
/// Configuration options for the Hugging Face AI provider.
/// </summary>
/// <remarks>
/// Two modes are supported; see <see cref="HuggingFaceMode"/>. Required fields differ by mode:
/// <list type="bullet">
///   <item><description><see cref="HuggingFaceMode.InferenceEndpoint"/> — <see cref="EndpointUrl"/> is required.</description></item>
///   <item><description><see cref="HuggingFaceMode.ServerlessInference"/> — <see cref="DefaultChatModel"/> and/or <see cref="DefaultEmbeddingModel"/> should be set unless every request carries an explicit <c>Model</c>.</description></item>
/// </list>
/// </remarks>
public sealed class HuggingFaceOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "HuggingFace";

    /// <summary>
    /// Default base URL for the Hugging Face <em>Serverless Inference API</em> (model id is appended
    /// at request time as <c>{ServerlessBaseUrl}/{model-id}</c>).
    /// </summary>
    public const string DefaultServerlessBaseUrl = "https://api-inference.huggingface.co/models";

    /// <summary>
    /// Inference surface to target. Default <see cref="HuggingFaceMode.InferenceEndpoint"/>.
    /// </summary>
    public HuggingFaceMode Mode { get; set; } = HuggingFaceMode.InferenceEndpoint;

    /// <summary>
    /// Hugging Face user access token (a <c>hf_*</c> token). Required.
    /// Sent as the <c>Authorization: Bearer &lt;token&gt;</c> header.
    /// </summary>
    public string HfToken { get; set; } = string.Empty;

    /// <summary>
    /// Full URL of a dedicated Inference Endpoint, e.g. <c>https://abc123.endpoints.huggingface.cloud</c>.
    /// Required when <see cref="Mode"/> is <see cref="HuggingFaceMode.InferenceEndpoint"/>;
    /// ignored otherwise.
    /// </summary>
    public string? EndpointUrl { get; set; }

    /// <summary>
    /// Base URL of the serverless inference API. Only used when <see cref="Mode"/> is
    /// <see cref="HuggingFaceMode.ServerlessInference"/>. Defaults to
    /// <see cref="DefaultServerlessBaseUrl"/>.
    /// </summary>
    public string ServerlessBaseUrl { get; set; } = DefaultServerlessBaseUrl;

    /// <summary>
    /// Default chat-completion model id (e.g. <c>meta-llama/Meta-Llama-3.1-8B-Instruct</c>). Only
    /// used in <see cref="HuggingFaceMode.ServerlessInference"/>; in
    /// <see cref="HuggingFaceMode.InferenceEndpoint"/> the endpoint is already bound to one model
    /// at deploy time and this value is sent informationally on the wire.
    /// </summary>
    public string DefaultChatModel { get; set; } = "meta-llama/Meta-Llama-3.1-8B-Instruct";

    /// <summary>
    /// Default embedding model id (e.g. <c>sentence-transformers/all-MiniLM-L6-v2</c>). Same caveat
    /// as <see cref="DefaultChatModel"/> regarding mode.
    /// </summary>
    public string DefaultEmbeddingModel { get; set; } = "sentence-transformers/all-MiniLM-L6-v2";

    /// <summary>
    /// Gets or sets the default sampling temperature.
    /// </summary>
    public float DefaultTemperature { get; set; } = 0.7f;

    /// <summary>
    /// Gets or sets the default maximum tokens for chat completions.
    /// </summary>
    public int DefaultMaxTokens { get; set; } = 1024;

    /// <summary>
    /// Per-request HTTP timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Number of retry attempts for transient failures (applied by <c>Microsoft.Extensions.Http.Resilience</c>).
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// When <c>true</c>, logs the request and response bodies at <see cref="LogLevel.Debug"/>.
    /// </summary>
    public bool EnableLogging { get; set; }

    /// <summary>
    /// Validates required fields for the configured mode. Returns <c>true</c> when sufficient
    /// for the chosen <see cref="Mode"/>.
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(HfToken))
        {
            return false;
        }

        return Mode switch
        {
            HuggingFaceMode.InferenceEndpoint => !string.IsNullOrWhiteSpace(EndpointUrl),
            HuggingFaceMode.ServerlessInference => !string.IsNullOrWhiteSpace(ServerlessBaseUrl),
            _ => false
        };
    }
}
