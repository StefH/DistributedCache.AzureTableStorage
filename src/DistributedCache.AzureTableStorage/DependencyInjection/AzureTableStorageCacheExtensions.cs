using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Distributed;
using Stef.Extensions.Caching.AzureTableStorage.Implementations;
using Stef.Extensions.Caching.AzureTableStorage.Validation;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods to add Azure table storage cache.
    /// </summary>
    public static class AzureTableStorageCacheExtensions
    {
        /// <summary>
        /// Add Azure table storage cache as an IDistributedCache to the service container.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <returns>
        /// The updated <see cref="IServiceCollection"/>.
        /// </returns>
        public static IServiceCollection AddDistributedAzureTableStorageCache([NotNull] this IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services));

            services.AddOptions();

            services.AddSingleton<IDistributedCache, AzureTableStorageCache>();

            return services;
        }
    }
}