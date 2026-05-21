// -----------------------------------------------------------------------
// <copyright file="TestFactories.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.HuggingFace.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Compendium.Adapters.HuggingFace.Tests.TestSupport;

/// <summary>
/// Helpers to construct internal SUTs for unit tests against both Hugging Face modes.
/// </summary>
internal static class TestFactories
{
    public const string DefaultEndpointUrl = "https://abc123.endpoints.huggingface.cloud";
    public const string DefaultServerlessBaseUrl = "https://api-inference.huggingface.co/models";
    public const string DefaultHfToken = "hf_test_token_redacted";
    public const string DefaultChatModel = "meta-llama/Meta-Llama-3.1-8B-Instruct";
    public const string DefaultEmbeddingModel = "sentence-transformers/all-MiniLM-L6-v2";

    public static HuggingFaceOptions DefaultOptions(Action<HuggingFaceOptions>? configure = null)
    {
        var options = new HuggingFaceOptions
        {
            Mode = HuggingFaceMode.InferenceEndpoint,
            HfToken = DefaultHfToken,
            EndpointUrl = DefaultEndpointUrl,
            ServerlessBaseUrl = DefaultServerlessBaseUrl,
            DefaultChatModel = DefaultChatModel,
            DefaultEmbeddingModel = DefaultEmbeddingModel,
            DefaultMaxTokens = 1024,
            TimeoutSeconds = 120,
            EnableLogging = false
        };
        configure?.Invoke(options);
        return options;
    }

    public static HuggingFaceOptions ServerlessOptions(Action<HuggingFaceOptions>? configure = null)
    {
        return DefaultOptions(o =>
        {
            o.Mode = HuggingFaceMode.ServerlessInference;
            o.EndpointUrl = null;
            configure?.Invoke(o);
        });
    }

    public static (HuggingFaceHttpClient Client, MockHttpMessageHandler Handler) CreateHttpClient(
        Action<HuggingFaceOptions>? configure = null)
    {
        var handler = new MockHttpMessageHandler();
        var options = DefaultOptions(configure);
        var httpClient = new HttpClient(handler);
        var sut = new HuggingFaceHttpClient(
            httpClient,
            Options.Create(options),
            NullLogger<HuggingFaceHttpClient>.Instance);
        return (sut, handler);
    }

    public static (HuggingFaceAIProvider Provider, MockHttpMessageHandler Handler) CreateProvider(
        Action<HuggingFaceOptions>? configure = null)
    {
        var handler = new MockHttpMessageHandler();
        var options = DefaultOptions(configure);
        var httpClient = new HttpClient(handler);
        var typedClient = new HuggingFaceHttpClient(
            httpClient,
            Options.Create(options),
            NullLogger<HuggingFaceHttpClient>.Instance);
        var provider = new HuggingFaceAIProvider(
            typedClient,
            Options.Create(options),
            NullLogger<HuggingFaceAIProvider>.Instance);
        return (provider, handler);
    }

    public static CompletionRequest SimpleCompletionRequest(string? model = null)
    {
        return new CompletionRequest
        {
            Model = model ?? DefaultChatModel,
            Messages = new List<Message> { Message.User("Hello") }
        };
    }

    /// <summary>
    /// Recording logger used to verify that log methods were invoked.
    /// </summary>
    public sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception), exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
