using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Distributed;

namespace Stef.Extensions.Caching.AzureTableStorage.Extensions
{
    /// <summary>
    /// https://dejanstojanovic.net/aspnet/2018/may/using-idistributedcache-in-net-core-just-got-a-lot-easier/
    /// </summary>
    public static class DistributedCachingExtensions
    {
        public static async Task SetAsync<T>([NotNull] this IDistributedCache distributedCache, [NotNull] string key, T value, [NotNull] DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
        {
            await distributedCache.SetAsync(key, value.ToByteArray(), options, token);
        }

        public static async Task<T> GetAsync<T>([NotNull] this IDistributedCache distributedCache, [NotNull] string key, CancellationToken token = default(CancellationToken)) where T : class
        {
            var result = await distributedCache.GetAsync(key, token);

            return result.FromByteArray<T>();
        }
    }
}