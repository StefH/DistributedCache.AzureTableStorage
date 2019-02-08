using Microsoft.Extensions.Caching.Distributed;
using System.Threading;
using System.Threading.Tasks;

namespace DistributedCache.AzureTableStorage.Interfaces
{
    public interface IDistributedCache<T> : IDistributedCache where T : class
    {
        Task<T> GetItemAsync(string key, CancellationToken token = default(CancellationToken));

        Task SetItemAsync(string key, T value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken));

        Task RefreshItemAsync(string key, CancellationToken token = default(CancellationToken));

        Task RemoveItemAsync(string key, CancellationToken token = default(CancellationToken));
    }
}