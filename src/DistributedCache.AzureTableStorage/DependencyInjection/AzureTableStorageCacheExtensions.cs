﻿using System;
using DistributedCache.AzureTableStorage.Implementations;
using DistributedCache.AzureTableStorage.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Distributed;
using Stef.Validation;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to add Azure Table Storage cache.
/// </summary>
public static class AzureTableStorageCacheExtensions
{
    /// <summary>
    /// Add Azure Table Storage cache as an IDistributedCache to the service container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    [PublicAPI]
    public static IServiceCollection AddDistributedAzureTableStorageCache(this IServiceCollection services)
    {
        Guard.NotNull(services);

        services.AddOptions();
        services.AddSingleton<IDistributedCache, AzureTableStorageCache>();

        return services;
    }

    /// <summary>
    /// Add Azure Table Storage cache as an IDistributedCache to the service container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configureAction">The action used to configure the options.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    [PublicAPI]
    public static IServiceCollection AddDistributedAzureTableStorageCache(this IServiceCollection services, Action<AzureTableStorageCacheOptions> configureAction)
    {
        Guard.NotNull(services);
        Guard.NotNull(configureAction);

        var options = new AzureTableStorageCacheOptions();
        configureAction(options);

        services.AddSingleton(Options.Options.Create(options));
        services.AddSingleton<IDistributedCache, AzureTableStorageCache>();

        return services;
    }
}