// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.Huggingface.DependencyInjection;
using Compendium.Adapters.Huggingface.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Adapters.Huggingface.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCompendiumHuggingfaceAdapter_WithConfiguration_RegistersAdapterAndOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Compendium:Adapters:Huggingface:BaseUrl"] = "https://api.example.com",
                ["Compendium:Adapters:Huggingface:ApiKey"] = "k1",
            })
            .Build();

        // Act
        var actual = services.AddCompendiumHuggingfaceAdapter(configuration);
        var sp = actual.BuildServiceProvider();

        // Assert
        actual.Should().BeSameAs(services);
        sp.GetRequiredService<HuggingfaceAdapter>().Should().NotBeNull();
    }

    [Fact]
    public void AddCompendiumHuggingfaceAdapter_WithCallback_RegistersAdapterAndOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCompendiumHuggingfaceAdapter(o =>
        {
            o.BaseUrl = "https://api.example.com";
            o.ApiKey = "k1";
        });
        var sp = services.BuildServiceProvider();

        // Assert
        sp.GetRequiredService<HuggingfaceAdapter>().Should().NotBeNull();
    }

    [Fact]
    public void AddCompendiumHuggingfaceAdapter_NullServices_Throws()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act = () => services!.AddCompendiumHuggingfaceAdapter(_ => { });

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
