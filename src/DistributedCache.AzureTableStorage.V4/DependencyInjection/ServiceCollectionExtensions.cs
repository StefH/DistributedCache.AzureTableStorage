using DistributedCache.AzureTableStorage.Implementations;
using DistributedCache.AzureTableStorage.Models;
using DistributedCache.AzureTableStorage.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Stef.Validation;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{

    [PublicAPI]
    public static IServiceCollection AddDistributedAzureTableStorageCache(this IServiceCollection services)
    {
        var options = services.BuildServiceProvider().GetRequiredService<IOptions<AzureTableStorageCacheOptions>>();

        return services.AddDistributedAzureTableStorageCache(options.Value);
    }

    [PublicAPI]
    public static IServiceCollection AddDistributedAzureTableStorageCache(this IServiceCollection services, IConfigurationSection section)
    {
        Guard.NotNull(services);
        Guard.NotNull(section);

        var options = new AzureTableStorageCacheOptions();
        section.Bind(options);

        return services.AddDistributedAzureTableStorageCache(options);
    }

    [PublicAPI]
    public static IServiceCollection AddDistributedAzureTableStorageCache(this IServiceCollection services, Action<AzureTableStorageCacheOptions> configureAction)
    {
        Guard.NotNull(services);
        Guard.NotNull(configureAction);

        var options = new AzureTableStorageCacheOptions();
        configureAction(options);

        return services.AddDistributedAzureTableStorageCache(options);
    }

    [PublicAPI]
    public static IServiceCollection AddDistributedAzureTableStorageCache(this IServiceCollection services, AzureTableStorageCacheOptions options)
    {
        Guard.NotNull(services);
        Guard.NotNull(options);

        var name = typeof(CachedItem).FullName;

        services.AddOptionsWithDataAnnotationValidation(options);

        if (options.StorageAccountUri is { })
        {
            services.AddAzureClients(builder =>
            {
                builder.AddTableServiceClient(options.StorageAccountUri).WithName(name).WithCredential(TokenCredentialBuilder.Build(options));
            });

            services.AddSingleton<IDistributedCache, TableStorageClientUsingIAzureClientFactory>();
        }
        else
        {
            services.AddSingleton<IDistributedCache, TableStorageClientUsingStorageConnectionString>();
        }

        return services;
    }
}