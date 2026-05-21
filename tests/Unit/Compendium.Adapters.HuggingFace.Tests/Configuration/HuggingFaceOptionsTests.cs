// -----------------------------------------------------------------------
// <copyright file="HuggingFaceOptionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.HuggingFace.Tests.Configuration;

public class HuggingFaceOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldBeSensible()
    {
        // Arrange & Act
        var options = new HuggingFaceOptions();

        // Assert
        options.Mode.Should().Be(HuggingFaceMode.InferenceEndpoint);
        options.HfToken.Should().BeEmpty();
        options.EndpointUrl.Should().BeNull();
        options.ServerlessBaseUrl.Should().Be(HuggingFaceOptions.DefaultServerlessBaseUrl);
        options.DefaultChatModel.Should().Be("meta-llama/Meta-Llama-3.1-8B-Instruct");
        options.DefaultEmbeddingModel.Should().Be("sentence-transformers/all-MiniLM-L6-v2");
        options.DefaultTemperature.Should().Be(0.7f);
        options.DefaultMaxTokens.Should().Be(1024);
        options.TimeoutSeconds.Should().Be(120);
        options.RetryAttempts.Should().Be(3);
        options.EnableLogging.Should().BeFalse();
    }

    [Fact]
    public void IsValid_InferenceEndpoint_WithTokenAndEndpoint_ReturnsTrue()
    {
        // Arrange
        var options = new HuggingFaceOptions
        {
            Mode = HuggingFaceMode.InferenceEndpoint,
            HfToken = "hf_x",
            EndpointUrl = "https://abc.endpoints.huggingface.cloud"
        };

        // Act & Assert
        options.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_InferenceEndpoint_WithoutEndpointUrl_ReturnsFalse()
    {
        // Arrange
        var options = new HuggingFaceOptions
        {
            Mode = HuggingFaceMode.InferenceEndpoint,
            HfToken = "hf_x"
        };

        // Act & Assert
        options.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_Serverless_WithTokenAndBase_ReturnsTrue()
    {
        // Arrange
        var options = new HuggingFaceOptions
        {
            Mode = HuggingFaceMode.ServerlessInference,
            HfToken = "hf_x"
            // ServerlessBaseUrl defaulted
        };

        // Act & Assert
        options.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_Serverless_WithEmptyBaseUrl_ReturnsFalse()
    {
        // Arrange
        var options = new HuggingFaceOptions
        {
            Mode = HuggingFaceMode.ServerlessInference,
            HfToken = "hf_x",
            ServerlessBaseUrl = ""
        };

        // Act & Assert
        options.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_EmptyToken_AlwaysReturnsFalse()
    {
        // Arrange
        var options = new HuggingFaceOptions
        {
            Mode = HuggingFaceMode.InferenceEndpoint,
            HfToken = "",
            EndpointUrl = "https://abc.endpoints.huggingface.cloud"
        };

        // Act & Assert
        options.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhitespaceToken_ReturnsFalse()
    {
        // Arrange
        var options = new HuggingFaceOptions
        {
            Mode = HuggingFaceMode.InferenceEndpoint,
            HfToken = "   ",
            EndpointUrl = "https://abc.endpoints.huggingface.cloud"
        };

        // Act & Assert
        options.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithUnknownMode_ReturnsFalse()
    {
        // Arrange
        var options = new HuggingFaceOptions
        {
            Mode = (HuggingFaceMode)999,
            HfToken = "hf_x"
        };

        // Act & Assert
        options.IsValid().Should().BeFalse();
    }

    [Fact]
    public void SectionName_IsHuggingFace()
    {
        HuggingFaceOptions.SectionName.Should().Be("HuggingFace");
    }
}
