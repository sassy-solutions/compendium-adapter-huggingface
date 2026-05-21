// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.HuggingFace.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Compendium.Adapters.HuggingFace.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    private static IServiceCollection BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        return services;
    }

    [Fact]
    public void AddCompendiumHuggingFace_WithConfigureAction_RegistersOptionsAndProvider()
    {
        // Arrange
        var services = BuildServices();

        // Act
        services.AddCompendiumHuggingFace(o =>
        {
            o.HfToken = "hf_abc";
            o.EndpointUrl = "https://abc.endpoints.huggingface.cloud";
        });
        var sp = services.BuildServiceProvider();

        // Assert
        sp.GetService<IOptions<HuggingFaceOptions>>().Should().NotBeNull();
        sp.GetRequiredService<IOptions<HuggingFaceOptions>>().Value.HfToken.Should().Be("hf_abc");
        sp.GetService<IAIProvider>().Should().NotBeNull();
        sp.GetRequiredService<IAIProvider>().ProviderId.Should().Be("huggingface");
    }

    [Fact]
    public void AddCompendiumHuggingFace_WithConfigureAction_RegistersHttpClientFactory()
    {
        // Arrange
        var services = BuildServices();

        // Act
        services.AddCompendiumHuggingFace(o =>
        {
            o.HfToken = "hf";
            o.EndpointUrl = "https://x";
            o.TimeoutSeconds = 42;
        });
        var sp = services.BuildServiceProvider();

        // Assert
        var factory = sp.GetService<IHttpClientFactory>();
        factory.Should().NotBeNull();
        sp.GetRequiredService<IOptions<HuggingFaceOptions>>().Value.TimeoutSeconds.Should().Be(42);
    }

    [Fact]
    public void AddCompendiumHuggingFace_WithConfigureAction_ResolvesIAIProviderAsSingleton()
    {
        // Arrange
        var services = BuildServices();
        services.AddCompendiumHuggingFace(o => { o.HfToken = "hf"; o.EndpointUrl = "https://x"; });
        var sp = services.BuildServiceProvider();

        // Act
        var first = sp.GetRequiredService<IAIProvider>();
        var second = sp.GetRequiredService<IAIProvider>();

        // Assert
        first.Should().BeSameAs(second);
    }

    [Fact]
    public void AddCompendiumHuggingFace_WithConfiguration_BindsHuggingFaceSection()
    {
        // Arrange
        var services = BuildServices();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HuggingFace:HfToken"] = "hf_bound",
                ["HuggingFace:Mode"] = "ServerlessInference",
                ["HuggingFace:DefaultChatModel"] = "Qwen/Qwen2.5-7B-Instruct",
                ["HuggingFace:DefaultMaxTokens"] = "2048",
                ["HuggingFace:TimeoutSeconds"] = "30"
            })
            .Build();

        // Act
        services.AddCompendiumHuggingFace(config);
        var sp = services.BuildServiceProvider();

        // Assert
        var opts = sp.GetRequiredService<IOptions<HuggingFaceOptions>>().Value;
        opts.HfToken.Should().Be("hf_bound");
        opts.Mode.Should().Be(HuggingFaceMode.ServerlessInference);
        opts.DefaultChatModel.Should().Be("Qwen/Qwen2.5-7B-Instruct");
        opts.DefaultMaxTokens.Should().Be(2048);
        opts.TimeoutSeconds.Should().Be(30);
        sp.GetService<IAIProvider>().Should().NotBeNull();
    }

    [Fact]
    public void AddCompendiumHuggingFace_WithConfigureAction_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = BuildServices();

        // Act
        var returned = services.AddCompendiumHuggingFace(o => { o.HfToken = "hf"; o.EndpointUrl = "https://x"; });

        // Assert
        returned.Should().BeSameAs(services);
    }

    [Fact]
    public void AddCompendiumHuggingFace_WithConfiguration_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = BuildServices();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["HuggingFace:HfToken"] = "hf" })
            .Build();

        // Act
        var returned = services.AddCompendiumHuggingFace(config);

        // Assert
        returned.Should().BeSameAs(services);
    }

    [Fact]
    public void AddCompendiumHuggingFace_WithNullServices_Throws()
    {
        IServiceCollection? services = null;
        var configure = (Action<HuggingFaceOptions>)(o => { });
        var act = () => services!.AddCompendiumHuggingFace(configure);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCompendiumHuggingFace_WithNullConfigureAction_Throws()
    {
        var services = BuildServices();
        var act = () => services.AddCompendiumHuggingFace((Action<HuggingFaceOptions>)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCompendiumHuggingFace_WithNullConfiguration_Throws()
    {
        var services = BuildServices();
        var act = () => services.AddCompendiumHuggingFace((IConfiguration)null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
