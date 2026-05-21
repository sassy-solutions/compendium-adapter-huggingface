// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.HuggingFace.Configuration;
using Compendium.Adapters.HuggingFace.Http;
using Compendium.Adapters.HuggingFace.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace Compendium.Adapters.HuggingFace.DependencyInjection;

/// <summary>
/// DI registration helpers for the Hugging Face adapter.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Hugging Face <see cref="IAIProvider"/> bound to a configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">A configuration root (the <c>HuggingFace</c> section is bound).</param>
    public static IServiceCollection AddCompendiumHuggingFace(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<HuggingFaceOptions>(
            configuration.GetSection(HuggingFaceOptions.SectionName));

        return services.AddCompendiumHuggingFaceCore();
    }

    /// <summary>
    /// Registers the Hugging Face <see cref="IAIProvider"/> using an inline configuration callback.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Callback to mutate <see cref="HuggingFaceOptions"/>.</param>
    public static IServiceCollection AddCompendiumHuggingFace(
        this IServiceCollection services,
        Action<HuggingFaceOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        return services.AddCompendiumHuggingFaceCore();
    }

    private static IServiceCollection AddCompendiumHuggingFaceCore(this IServiceCollection services)
    {
        services.AddHttpClient<HuggingFaceHttpClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<HuggingFaceOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        })
        .AddStandardResilienceHandler();

        services.AddSingleton<HuggingFaceAIProvider>();
        services.AddSingleton<IAIProvider>(sp => sp.GetRequiredService<HuggingFaceAIProvider>());

        return services;
    }
}
