// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

// Demonstrates both Hugging Face inference surfaces side-by-side. Set HF_TOKEN, then
// either:
//   - HF_ENDPOINT_URL (your dedicated Inference Endpoint, recommended for production), OR
//   - HF_SERVERLESS_MODEL (a free-tier serverless model id, e.g. meta-llama/Meta-Llama-3.1-8B-Instruct).
//
// Cost trade-offs:
//   Inference Endpoint  : flat per-hour compute, predictable latency, no shared rate limits,
//                          autoscale-to-zero supported. Pay even when idle (unless scaled to zero).
//   Serverless Inference : free tier with aggressive rate limits + cold starts; Pro tier
//                          shares one quota across the org. Great for prototyping; risky in prod.
using Compendium.Abstractions.AI;
using Compendium.Abstractions.AI.Models;
using Compendium.Adapters.HuggingFace.Configuration;
using Compendium.Adapters.HuggingFace.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var hfToken = Environment.GetEnvironmentVariable("HF_TOKEN");
if (string.IsNullOrEmpty(hfToken))
{
    Console.Error.WriteLine("Set HF_TOKEN first (a hf_* user access token).");
    return 1;
}

var endpointUrl = Environment.GetEnvironmentVariable("HF_ENDPOINT_URL");
var serverlessModel = Environment.GetEnvironmentVariable("HF_SERVERLESS_MODEL");

if (string.IsNullOrEmpty(endpointUrl) && string.IsNullOrEmpty(serverlessModel))
{
    Console.Error.WriteLine(
        "Set either HF_ENDPOINT_URL (dedicated endpoint) or HF_SERVERLESS_MODEL (serverless model id).");
    return 1;
}

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddCompendiumHuggingFace(opt =>
{
    opt.HfToken = hfToken;
    if (!string.IsNullOrEmpty(endpointUrl))
    {
        opt.Mode = HuggingFaceMode.InferenceEndpoint;
        opt.EndpointUrl = endpointUrl;
        Console.WriteLine($"Using dedicated Inference Endpoint: {endpointUrl}");
    }
    else
    {
        opt.Mode = HuggingFaceMode.ServerlessInference;
        opt.DefaultChatModel = serverlessModel!;
        opt.DefaultEmbeddingModel = "sentence-transformers/all-MiniLM-L6-v2";
        Console.WriteLine($"Using serverless inference for model: {serverlessModel}");
    }
});

await using var sp = services.BuildServiceProvider();
var provider = sp.GetRequiredService<IAIProvider>();

// 1. Chat completion (OpenAI-compatible Messages API).
var chatRequest = new CompletionRequest
{
    Model = string.Empty, // falls back to DefaultChatModel
    SystemPrompt = "Answer in one short sentence.",
    Messages = new List<Message> { Message.User("What is event sourcing?") },
    MaxTokens = 128
};
var chat = await provider.CompleteAsync(chatRequest);
if (chat.IsFailure)
{
    Console.Error.WriteLine($"Chat error: {chat.Error.Code} - {chat.Error.Message}");
    return 1;
}
Console.WriteLine($"\n[chat]\n  finish={chat.Value.FinishReason}\n  content={chat.Value.Content}");

// 2. Embeddings (feature-extraction task).
var embedding = await provider.EmbedAsync(new EmbeddingRequest
{
    Model = string.Empty, // falls back to DefaultEmbeddingModel
    Inputs = new List<string> { "Hugging Face hosts open models.", "Compendium routes through them." }
});
if (embedding.IsFailure)
{
    Console.Error.WriteLine($"Embedding error: {embedding.Error.Code} - {embedding.Error.Message}");
    return 1;
}
Console.WriteLine($"\n[embeddings] received {embedding.Value.Embeddings.Count} vector(s)");
foreach (var e in embedding.Value.Embeddings)
{
    Console.WriteLine($"  #{e.Index} dim={e.Vector.Length} first4=[{string.Join(",", e.Vector.Take(4))}]");
}

return 0;
