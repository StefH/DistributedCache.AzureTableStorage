using DistributedCache.AzureTableStorage.Utils;
using DistributedCache.AzureTableStorage.Validation;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DistributedCache.AzureTableStorage.Extensions
{
    /// <summary>
    /// https://dejanstojanovic.net/aspnet/2018/may/using-idistributedcache-in-net-core-just-got-a-lot-easier/
    /// </summary>
    public static class DistributedCachingExtensions
    {
        /// <summary>
        /// Sets the value with the given key.
        /// </summary>
        /// <typeparam name="T">Generic type.</typeparam>
        /// <param name="distributedCache">The distributed cache.</param>
        /// <param name="key">A string identifying the requested value.</param>
        /// <param name="value">The value to set in the cache.</param>
        /// <param name="options">The cache options for the value.</param>
        /// <param name="token">Optional. The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public static Task SetAsync<T>([NotNull] this IDistributedCache distributedCache, [NotNull] string key, T value, [NotNull] DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
        {
            Guard.NotNull(distributedCache, nameof(distributedCache));
            Guard.NotNullOrEmpty(key, nameof(key));
            Guard.NotNull(options, nameof(options));

            if (typeof(T) == typeof(string))
            {
                return distributedCache.SetStringAsync(key, value as string, options, token);
            }

            return distributedCache.SetAsync(key, BinarySerializer.Serialize(value), options, token);
        }

        /// <summary>
        /// Gets a value with the given key.
        /// </summary>
        /// <typeparam name="T">Generic type.</typeparam>
        /// <param name="distributedCache">The distributed cache.</param>
        /// <param name="key">A string identifying the requested value.</param>
        /// <param name="token">Optional. The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the located value or null.</returns>
        public static async Task<T?> GetAsync<T>([NotNull] this IDistributedCache distributedCache, [NotNull] string key, CancellationToken token = default) where T : class
        {
            Guard.NotNull(distributedCache, nameof(distributedCache));
            Guard.NotNullOrEmpty(key, nameof(key));

            // In case of string, just use the existing DistributedCacheExtensions
            if (typeof(T) == typeof(string))
            {
                string stringValue = await distributedCache.GetStringAsync(key, token).ConfigureAwait(false);

                return (T)Convert.ChangeType(stringValue, typeof(T));
            }

            var result = await distributedCache.GetAsync(key, token).ConfigureAwait(false);

            return BinarySerializer.Deserialize<T>(result);
        }
    }
}