// -----------------------------------------------------------------------
// <copyright file="HuggingFaceHttpClientTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.HuggingFace.Http;
using Compendium.Adapters.HuggingFace.Http.Models;
using Compendium.Adapters.HuggingFace.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Compendium.Adapters.HuggingFace.Tests.Http;

/// <summary>
/// Unit tests for <see cref="HuggingFaceHttpClient"/>. HTTP transport is mocked with RichardSzalay.MockHttp.
/// </summary>
public class HuggingFaceHttpClientTests
{
    private static HuggingFaceChatCompletionRequest BuildRequest(string model = TestFactories.DefaultChatModel) =>
        new()
        {
            Model = model,
            Messages = new List<HuggingFaceChatMessage>
            {
                new() { Role = "user", Content = "Hello" }
            }
        };

    // ---------- URL resolution ----------

    [Fact]
    public void ResolveChatUrl_InferenceEndpointMode_AppendsV1ChatCompletions()
    {
        // Arrange
        var (sut, _) = TestFactories.CreateHttpClient();

        // Act
        var url = sut.ResolveChatUrl("ignored-in-endpoint-mode");

        // Assert
        url.AbsoluteUri.Should().Be(TestFactories.DefaultEndpointUrl + "/v1/chat/completions");
    }

    [Fact]
    public void ResolveChatUrl_ServerlessMode_PutsModelInPath()
    {
        // Arrange
        var (sut, _) = TestFactories.CreateHttpClient(o =>
        {
            o.Mode = HuggingFaceMode.ServerlessInference;
            o.EndpointUrl = null;
        });

        // Act
        var url = sut.ResolveChatUrl("meta-llama/Meta-Llama-3.1-8B-Instruct");

        // Assert
        url.AbsoluteUri.Should().Be(
            TestFactories.DefaultServerlessBaseUrl + "/meta-llama/Meta-Llama-3.1-8B-Instruct/v1/chat/completions");
    }

    [Fact]
    public void ResolveChatUrl_InferenceEndpointMode_WithoutEndpoint_Throws()
    {
        // Arrange
        var (sut, _) = TestFactories.CreateHttpClient(o => o.EndpointUrl = null);

        // Act
        var act = () => sut.ResolveChatUrl("any");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*EndpointUrl is required*");
    }

    [Fact]
    public void ResolveChatUrl_StripsTrailingSlashesFromBase()
    {
        // Arrange
        var (sut, _) = TestFactories.CreateHttpClient(o => o.EndpointUrl = TestFactories.DefaultEndpointUrl + "///");

        // Act
        var url = sut.ResolveChatUrl("any");

        // Assert
        url.AbsoluteUri.Should().Be(TestFactories.DefaultEndpointUrl + "/v1/chat/completions");
    }

    [Fact]
    public void ResolveEmbeddingUrl_InferenceEndpointMode_PostsToBareEndpoint()
    {
        // Arrange
        var (sut, _) = TestFactories.CreateHttpClient();

        // Act
        var url = sut.ResolveEmbeddingUrl("ignored");

        // Assert
        // Uri normalisation appends "/" to a URL that has no path component; the bare endpoint
        // is what the server expects (HF treats both forms identically).
        url.AbsoluteUri.TrimEnd('/').Should().Be(TestFactories.DefaultEndpointUrl);
    }

    [Fact]
    public void ResolveEmbeddingUrl_ServerlessMode_AppendsModelId()
    {
        // Arrange
        var (sut, _) = TestFactories.CreateHttpClient(o =>
        {
            o.Mode = HuggingFaceMode.ServerlessInference;
            o.EndpointUrl = null;
        });

        // Act
        var url = sut.ResolveEmbeddingUrl("sentence-transformers/all-MiniLM-L6-v2");

        // Assert
        url.AbsoluteUri.Should().Be(
            TestFactories.DefaultServerlessBaseUrl + "/sentence-transformers/all-MiniLM-L6-v2");
    }

    // ---------- Constructor / headers ----------

    [Fact]
    public void Constructor_ConfiguresBearerAuthorization()
    {
        // Arrange
        var (sut, _) = TestFactories.CreateHttpClient(o => o.HfToken = "hf_my_secret_redacted");

        // Act
        var headers = GetUnderlyingHttpClient(sut).DefaultRequestHeaders;

        // Assert
        headers.Authorization.Should().NotBeNull();
        headers.Authorization!.Scheme.Should().Be("Bearer");
        headers.Authorization!.Parameter.Should().Be("hf_my_secret_redacted");
    }

    [Fact]
    public void Constructor_WithEmptyToken_DoesNotAddAuthorization()
    {
        // Arrange
        var (sut, _) = TestFactories.CreateHttpClient(o => o.HfToken = "");

        // Act
        var headers = GetUnderlyingHttpClient(sut).DefaultRequestHeaders;

        // Assert
        headers.Authorization.Should().BeNull();
    }

    [Fact]
    public void Constructor_DoesNotDoubleAddAuthorization_IfAlreadyPresent()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer external");
        var options = TestFactories.DefaultOptions();
        var sut = new HuggingFaceHttpClient(httpClient, Options.Create(options), NullLogger<HuggingFaceHttpClient>.Instance);

        // Act
        var headers = GetUnderlyingHttpClient(sut).DefaultRequestHeaders;

        // Assert
        headers.GetValues("Authorization").Should().ContainSingle().Which.Should().Be("Bearer external");
    }

    // ---------- CreateChatCompletionAsync ----------

    [Fact]
    public async Task CreateChatCompletionAsync_OnSuccess_ReturnsParsedResponse()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        var json = """
        {
          "id": "hf-gen-1",
          "model": "meta-llama/Meta-Llama-3.1-8B-Instruct",
          "created": 1730000000,
          "choices": [
            { "index": 0, "message": { "role": "assistant", "content": "Hi!" }, "finish_reason": "stop" }
          ],
          "usage": { "prompt_tokens": 10, "completion_tokens": 4, "total_tokens": 14 }
        }
        """;
        handler.When(HttpMethod.Post, TestFactories.DefaultEndpointUrl + "/v1/chat/completions")
            .Respond("application/json", json);

        // Act
        var result = await sut.CreateChatCompletionAsync(BuildRequest(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("hf-gen-1");
        result.Value.Choices.Should().ContainSingle();
        result.Value.Choices[0].Message!.Content.Should().Be("Hi!");
        result.Value.Usage!.PromptTokens.Should().Be(10);
    }

    [Fact]
    public async Task CreateChatCompletionAsync_ServerlessMode_PostsToModelPath()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient(o =>
        {
            o.Mode = HuggingFaceMode.ServerlessInference;
            o.EndpointUrl = null;
        });
        var expected = handler
            .Expect(HttpMethod.Post, TestFactories.DefaultServerlessBaseUrl + "/meta-llama/Meta-Llama-3.1-8B-Instruct/v1/chat/completions")
            .Respond("application/json", """{"id":"x","model":"m","created":0,"choices":[]}""");

        // Act
        var result = await sut.CreateChatCompletionAsync(BuildRequest(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        handler.GetMatchCount(expected).Should().Be(1);
    }

    [Fact]
    public async Task CreateChatCompletionAsync_WhenLoggingEnabled_LogsRequestAndResponse()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Post, "*/v1/chat/completions")
            .Respond("application/json", """{"id":"x","model":"m","created":0,"choices":[]}""");
        var options = TestFactories.DefaultOptions(o => o.EnableLogging = true);
        var httpClient = new HttpClient(handler);
        var logger = new TestFactories.RecordingLogger<HuggingFaceHttpClient>();
        var sut = new HuggingFaceHttpClient(httpClient, Options.Create(options), logger);

        // Act
        var result = await sut.CreateChatCompletionAsync(BuildRequest(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        logger.Entries.Should().Contain(e => e.Message.Contains("HuggingFace chat request"));
        logger.Entries.Should().Contain(e => e.Message.Contains("HuggingFace response"));
    }

    [Fact]
    public async Task CreateChatCompletionAsync_WhenResponseIsNullJson_ReturnsProviderError()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/v1/chat/completions")
            .Respond("application/json", "null");

        // Act
        var result = await sut.CreateChatCompletionAsync(BuildRequest(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderError");
        result.Error.Message.Should().Contain("Empty response");
    }

    [Fact]
    public async Task CreateChatCompletionAsync_WhenResponseIsInvalidJson_ReturnsProviderError()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/v1/chat/completions")
            .Respond("application/json", "this is not json");

        // Act
        var result = await sut.CreateChatCompletionAsync(BuildRequest(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderError");
        result.Error.Message.Should().Contain("Invalid response format");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "AI.InvalidApiKey")]
    [InlineData(HttpStatusCode.TooManyRequests, "AI.RateLimitExceeded")]
    [InlineData(HttpStatusCode.PaymentRequired, "AI.InsufficientCredits")]
    [InlineData(HttpStatusCode.NotFound, "AI.ModelNotFound")]
    [InlineData(HttpStatusCode.InternalServerError, "AI.ProviderError")]
    public async Task CreateChatCompletionAsync_OnErrorStatus_MapsToTypedError(HttpStatusCode status, string expectedCode)
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        // Use the "error as object" shape.
        handler.When(HttpMethod.Post, "*/v1/chat/completions")
            .Respond(status, "application/json", """{"error":{"code":"E1","message":"boom","type":"x"}}""");

        // Act
        var result = await sut.CreateChatCompletionAsync(BuildRequest(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(expectedCode);
    }

    [Fact]
    public async Task CreateChatCompletionAsync_OnErrorStatus_WithStringErrorShape_UsesStringAsMessage()
    {
        // Arrange — HF's other error shape: `{ "error": "model is currently loading" }`
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/v1/chat/completions")
            .Respond(HttpStatusCode.ServiceUnavailable, "application/json", "{\"error\":\"model is currently loading\"}");

        // Act
        var result = await sut.CreateChatCompletionAsync(BuildRequest(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderError");
        result.Error.Message.Should().Contain("model is currently loading");
    }

    [Fact]
    public async Task CreateChatCompletionAsync_OnErrorStatus_WithUnparseableBody_ReturnsProviderErrorWithRawBody()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/v1/chat/completions")
            .Respond(HttpStatusCode.BadGateway, "text/plain", "raw body text");

        // Act
        var result = await sut.CreateChatCompletionAsync(BuildRequest(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderError");
        result.Error.Message.Should().Contain("raw body text");
    }

    [Fact]
    public async Task CreateChatCompletionAsync_OnErrorStatus_WithEmptyBody_FallsBackToStatusName()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/v1/chat/completions")
            .Respond(HttpStatusCode.GatewayTimeout, "application/json", "");

        // Act
        var result = await sut.CreateChatCompletionAsync(BuildRequest(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderError");
        result.Error.Message.Should().Contain("GatewayTimeout");
    }

    [Fact]
    public async Task CreateChatCompletionAsync_OnHttpRequestException_ReturnsProviderError()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/v1/chat/completions")
            .Throw(new HttpRequestException("connection refused"));

        // Act
        var result = await sut.CreateChatCompletionAsync(BuildRequest(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderError");
        result.Error.Message.Should().Contain("connection refused");
    }

    [Fact]
    public async Task CreateChatCompletionAsync_OnTimeout_ReturnsTimeoutError()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient(o => o.TimeoutSeconds = 7);
        handler.When(HttpMethod.Post, "*/v1/chat/completions")
            .Throw(new TaskCanceledException("timeout"));

        // Act
        var result = await sut.CreateChatCompletionAsync(BuildRequest(), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.Timeout");
        result.Error.Message.Should().Contain("7");
    }

    [Fact]
    public async Task CreateChatCompletionAsync_WhenCallerCancels_PropagatesTaskCanceledException()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        handler.When(HttpMethod.Post, "*/v1/chat/completions")
            .Throw(new TaskCanceledException("cancelled"));

        // Act
        var act = async () => await sut.CreateChatCompletionAsync(BuildRequest(), cts.Token);

        // Assert
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    // ---------- StreamChatCompletionAsync ----------

    [Fact]
    public async Task StreamChatCompletionAsync_OnSuccess_YieldsParsedChunks()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        var stream = string.Join("\n",
            "data: {\"id\":\"c1\",\"model\":\"m\",\"choices\":[{\"delta\":{\"content\":\"Hel\"}}]}",
            "",
            "data: {\"id\":\"c1\",\"model\":\"m\",\"choices\":[{\"delta\":{\"content\":\"lo\"}}]}",
            "data: {\"id\":\"c1\",\"model\":\"m\",\"choices\":[{\"delta\":{},\"finish_reason\":\"stop\"}]}",
            "data: [DONE]",
            "");
        handler.When(HttpMethod.Post, "*/v1/chat/completions")
            .Respond("text/event-stream", stream);

        // Act
        var chunks = new List<HuggingFaceStreamChunk>();
        await foreach (var c in sut.StreamChatCompletionAsync(BuildRequest(), CancellationToken.None))
        {
            c.IsSuccess.Should().BeTrue();
            chunks.Add(c.Value);
        }

        // Assert
        chunks.Should().HaveCount(3);
        chunks[0].Choices[0].Delta!.Content.Should().Be("Hel");
        chunks[1].Choices[0].Delta!.Content.Should().Be("lo");
        chunks[2].Choices[0].FinishReason.Should().Be("stop");
    }

    [Fact]
    public async Task StreamChatCompletionAsync_OnNon2xxStatus_YieldsSingleFailureAndStops()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/v1/chat/completions")
            .Respond(HttpStatusCode.TooManyRequests, "application/json", "{\"error\":\"slow down\"}");

        // Act
        var results = new List<Result<HuggingFaceStreamChunk>>();
        await foreach (var r in sut.StreamChatCompletionAsync(BuildRequest(), CancellationToken.None))
        {
            results.Add(r);
        }

        // Assert
        results.Should().ContainSingle();
        results[0].IsFailure.Should().BeTrue();
        results[0].Error.Code.Should().Be("AI.RateLimitExceeded");
    }

    [Fact]
    public async Task StreamChatCompletionAsync_SkipsBlankLines_NonDataLines_AndUnparseableData()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        var stream = string.Join("\n",
            "",
            ": comment line",
            "event: message",
            "data: not-json",
            "data: {\"id\":\"c1\",\"model\":\"m\",\"choices\":[{\"delta\":{\"content\":\"ok\"}}]}",
            "data: [DONE]",
            "");
        handler.When(HttpMethod.Post, "*/v1/chat/completions")
            .Respond("text/event-stream", stream);

        // Act
        var chunks = new List<HuggingFaceStreamChunk>();
        await foreach (var c in sut.StreamChatCompletionAsync(BuildRequest(), CancellationToken.None))
        {
            c.IsSuccess.Should().BeTrue();
            chunks.Add(c.Value);
        }

        // Assert
        chunks.Should().ContainSingle();
        chunks[0].Choices[0].Delta!.Content.Should().Be("ok");
    }

    [Fact]
    public async Task StreamChatCompletionAsync_StopsWhenCancellationRequested()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        var stream = string.Join("\n",
            "data: {\"id\":\"c1\",\"model\":\"m\",\"choices\":[{\"delta\":{\"content\":\"a\"}}]}",
            "data: {\"id\":\"c1\",\"model\":\"m\",\"choices\":[{\"delta\":{\"content\":\"b\"}}]}",
            "data: {\"id\":\"c1\",\"model\":\"m\",\"choices\":[{\"delta\":{\"content\":\"c\"}}]}",
            "data: [DONE]",
            "");
        handler.When(HttpMethod.Post, "*/v1/chat/completions")
            .Respond("text/event-stream", stream);

        using var cts = new CancellationTokenSource();
        var chunks = new List<HuggingFaceStreamChunk>();

        // Act
        await foreach (var c in sut.StreamChatCompletionAsync(BuildRequest(), cts.Token))
        {
            if (c.IsSuccess)
            {
                chunks.Add(c.Value);
            }
            cts.Cancel();
        }

        // Assert
        chunks.Should().HaveCountGreaterThan(0);
        chunks.Should().HaveCountLessThan(4);
    }

    [Fact]
    public async Task StreamChatCompletionAsync_NullChunkAfterDeserialization_IsSkipped()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        var stream = string.Join("\n",
            "data: null",
            "data: {\"id\":\"c1\",\"model\":\"m\",\"choices\":[{\"delta\":{\"content\":\"x\"}}]}",
            "data: [DONE]",
            "");
        handler.When(HttpMethod.Post, "*/v1/chat/completions")
            .Respond("text/event-stream", stream);

        // Act
        var chunks = new List<HuggingFaceStreamChunk>();
        await foreach (var c in sut.StreamChatCompletionAsync(BuildRequest(), CancellationToken.None))
        {
            chunks.Add(c.Value);
        }

        // Assert
        chunks.Should().ContainSingle();
        chunks[0].Choices[0].Delta!.Content.Should().Be("x");
    }

    // ---------- CreateFeatureExtractionAsync ----------

    [Fact]
    public async Task CreateFeatureExtractionAsync_InferenceEndpointMode_PostsToBareEndpoint()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        var expected = handler.Expect(HttpMethod.Post, TestFactories.DefaultEndpointUrl)
            .Respond("application/json", "[0.1, 0.2, 0.3]");
        var payload = new HuggingFaceFeatureExtractionRequest { Inputs = new List<string> { "hi" } };

        // Act
        var result = await sut.CreateFeatureExtractionAsync(TestFactories.DefaultEmbeddingModel, payload, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("[0.1, 0.2, 0.3]");
        handler.GetMatchCount(expected).Should().Be(1);
    }

    [Fact]
    public async Task CreateFeatureExtractionAsync_ServerlessMode_PostsToModelPath()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient(o =>
        {
            o.Mode = HuggingFaceMode.ServerlessInference;
            o.EndpointUrl = null;
        });
        var expected = handler.Expect(HttpMethod.Post, TestFactories.DefaultServerlessBaseUrl + "/sentence-transformers/all-MiniLM-L6-v2")
            .Respond("application/json", "[[0.1, 0.2]]");
        var payload = new HuggingFaceFeatureExtractionRequest { Inputs = new List<string> { "hi" } };

        // Act
        var result = await sut.CreateFeatureExtractionAsync("sentence-transformers/all-MiniLM-L6-v2", payload, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        handler.GetMatchCount(expected).Should().Be(1);
    }

    [Fact]
    public async Task CreateFeatureExtractionAsync_WhenLoggingEnabled_LogsRequestAndResponse()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        handler.When(HttpMethod.Post, TestFactories.DefaultEndpointUrl)
            .Respond("application/json", "[0.1, 0.2]");
        var options = TestFactories.DefaultOptions(o => o.EnableLogging = true);
        var httpClient = new HttpClient(handler);
        var logger = new TestFactories.RecordingLogger<HuggingFaceHttpClient>();
        var sut = new HuggingFaceHttpClient(httpClient, Options.Create(options), logger);
        var payload = new HuggingFaceFeatureExtractionRequest { Inputs = new List<string> { "hi" } };

        // Act
        var result = await sut.CreateFeatureExtractionAsync(TestFactories.DefaultEmbeddingModel, payload, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        logger.Entries.Should().Contain(e => e.Message.Contains("HuggingFace embedding request"));
        logger.Entries.Should().Contain(e => e.Message.Contains("HuggingFace embedding response"));
    }

    [Fact]
    public async Task CreateFeatureExtractionAsync_OnErrorStatus_MapsError()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, TestFactories.DefaultEndpointUrl)
            .Respond(HttpStatusCode.Unauthorized, "application/json", "{\"error\":\"bad token\"}");
        var payload = new HuggingFaceFeatureExtractionRequest { Inputs = new List<string> { "hi" } };

        // Act
        var result = await sut.CreateFeatureExtractionAsync(TestFactories.DefaultEmbeddingModel, payload, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.InvalidApiKey");
    }

    [Fact]
    public async Task CreateFeatureExtractionAsync_OnTimeout_ReturnsTimeoutError()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient(o => o.TimeoutSeconds = 9);
        handler.When(HttpMethod.Post, TestFactories.DefaultEndpointUrl)
            .Throw(new TaskCanceledException("timeout"));
        var payload = new HuggingFaceFeatureExtractionRequest { Inputs = new List<string> { "hi" } };

        // Act
        var result = await sut.CreateFeatureExtractionAsync(TestFactories.DefaultEmbeddingModel, payload, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.Timeout");
        result.Error.Message.Should().Contain("9");
    }

    [Fact]
    public async Task CreateFeatureExtractionAsync_OnHttpRequestException_ReturnsProviderError()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, TestFactories.DefaultEndpointUrl)
            .Throw(new HttpRequestException("dns failed"));
        var payload = new HuggingFaceFeatureExtractionRequest { Inputs = new List<string> { "hi" } };

        // Act
        var result = await sut.CreateFeatureExtractionAsync(TestFactories.DefaultEmbeddingModel, payload, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderError");
        result.Error.Message.Should().Contain("dns failed");
    }

    [Fact]
    public async Task CreateFeatureExtractionAsync_WhenCallerCancels_PropagatesTaskCanceledException()
    {
        // Arrange
        var (sut, handler) = TestFactories.CreateHttpClient();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        handler.When(HttpMethod.Post, TestFactories.DefaultEndpointUrl)
            .Throw(new TaskCanceledException("cancelled"));
        var payload = new HuggingFaceFeatureExtractionRequest { Inputs = new List<string> { "hi" } };

        // Act
        var act = async () => await sut.CreateFeatureExtractionAsync(TestFactories.DefaultEmbeddingModel, payload, cts.Token);

        // Assert
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    /// <summary>
    /// Reflection helper to extract the underlying <see cref="HttpClient"/> for header inspection.
    /// </summary>
    private static HttpClient GetUnderlyingHttpClient(HuggingFaceHttpClient sut)
    {
        var field = typeof(HuggingFaceHttpClient)
            .GetField("_httpClient", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field.Should().NotBeNull();
        return (HttpClient)field!.GetValue(sut)!;
    }
}
