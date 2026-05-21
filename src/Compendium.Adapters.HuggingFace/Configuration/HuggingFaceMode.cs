// -----------------------------------------------------------------------
// <copyright file="HuggingFaceMode.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.HuggingFace.Configuration;

/// <summary>
/// Selects which Hugging Face inference surface the adapter targets. The two surfaces differ in
/// URL shape, cost model, latency, and which models are reachable — pick deliberately.
/// </summary>
public enum HuggingFaceMode
{
    /// <summary>
    /// Per-tenant dedicated <em>Inference Endpoint</em> (paid, managed compute) hosted under
    /// <c>https://&lt;name&gt;.endpoints.huggingface.cloud</c>. The endpoint is bound to a single
    /// model + task at deploy time, so <see cref="HuggingFaceOptions.DefaultChatModel"/> is informational
    /// (sent as the body's <c>model</c> field but ignored by the server). This is the production
    /// recommendation: predictable latency, no shared rate limits, optional EU residency,
    /// autoscale-to-zero supported.
    /// </summary>
    InferenceEndpoint = 0,

    /// <summary>
    /// Shared <em>Serverless Inference API</em> at <c>https://api-inference.huggingface.co/models/&lt;model-id&gt;</c>.
    /// Free tier + Pro tier; aggressive rate limits; cold-start penalty on first call. Use for
    /// prototyping, not production. Model id is required either on the request or via
    /// <see cref="HuggingFaceOptions.DefaultChatModel"/> / <see cref="HuggingFaceOptions.DefaultEmbeddingModel"/>.
    /// </summary>
    ServerlessInference = 1
}
