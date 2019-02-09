using DistributedCache.AzureTableStorage.Models;
using DistributedCache.AzureTableStorage.Options;
using DistributedCache.AzureTableStorage.Validation;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Threading;
using System.Threading.Tasks;
using WindowsAzure.Table;
using WindowsAzure.Table.Extensions;

namespace DistributedCache.AzureTableStorage.Implementations
{
    /// <summary>
    /// An <see cref="IDistributedCache"/> implementation to cache data in Azure table storage.
    /// </summary>
    /// <seealso cref="IDistributedCache"/>.
    public class AzureTableStorageCache : IDistributedCache
    {
        private readonly AzureTableStorageCacheOptions _options;

        private readonly ITableSet<CachedItem> _tableSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureTableStorageCache"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        public AzureTableStorageCache([NotNull] IOptions<AzureTableStorageCacheOptions> options)
        {
            Guard.NotNull(options, nameof(options));

            _options = options.Value;

            // Create CloudTableClient
            var client = CloudStorageAccount.Parse(_options.ConnectionString).CreateCloudTableClient();

            // Create TableSet
            _tableSet = new TableSet<CachedItem>(client, _options.TableName);
        }

        /// <inheritdoc cref="IDistributedCache.Get(string)"/>
        public byte[] Get(string key)
        {
            Guard.NotNullOrEmpty(key, nameof(key));

            return GetAsync(key).Result;
        }

        /// <inheritdoc cref="IDistributedCache.GetAsync(string, CancellationToken)"/>
        public async Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken))
        {
            Guard.NotNullOrEmpty(key, nameof(key));

            await RefreshAsync(key, token);

            CachedItem item = await RetrieveAsync(key);
            return item?.Data;
        }

        /// <inheritdoc cref="IDistributedCache.Refresh(string)"/>
        public void Refresh(string key)
        {
            Guard.NotNullOrEmpty(key, nameof(key));

            RefreshAsync(key).Wait();
        }

        /// <inheritdoc cref="IDistributedCache.RefreshAsync(string, CancellationToken)"/>
        public async Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
        {
            Guard.NotNullOrEmpty(key, nameof(key));

            var item = await RetrieveAsync(key);
            if (item != null)
            {
                if (ShouldDelete(item))
                {
                    await _tableSet.RemoveAsync(item, token);
                    return;
                }

                item.LastAccessTime = DateTimeOffset.UtcNow;

                await _tableSet.UpdateAsync(item, token);
            }
        }

        /// <inheritdoc cref="IDistributedCache.Remove(string)"/>
        public void Remove(string key)
        {
            Guard.NotNullOrEmpty(key, nameof(key));

            RemoveAsync(key).Wait();
        }

        /// <inheritdoc cref="IDistributedCache.RemoveAsync(string, CancellationToken)"/>
        public async Task RemoveAsync([NotNull] string key, CancellationToken token = default(CancellationToken))
        {
            Guard.NotNullOrEmpty(key, nameof(key));

            var item = await RetrieveAsync(key);
            if (item != null)
            {
                await _tableSet.RemoveAsync(item, token);
            }
        }

        /// <inheritdoc cref="IDistributedCache.Set(string, byte[], DistributedCacheEntryOptions)"/>
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            Guard.NotNullOrEmpty(key, nameof(key));
            Guard.NotNullOrEmpty(value, nameof(value));
            Guard.NotNull(options, nameof(options));

            SetAsync(key, value, options).Wait();
        }

        /// <inheritdoc cref="IDistributedCache.SetAsync(string, byte[], DistributedCacheEntryOptions, CancellationToken)"/>
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
        {
            Guard.NotNullOrEmpty(key, nameof(key));
            Guard.NotNullOrEmpty(value, nameof(value));
            Guard.NotNull(options, nameof(options));

            var currentTime = DateTimeOffset.UtcNow;
            var absoluteExpiration = ParseOptions(options, currentTime);

            var item = new CachedItem
            {
                PartitionKey = _options.PartitionKey,
                RowKey = key,
                Data = value,
                LastAccessTime = currentTime
            };

            if (absoluteExpiration.HasValue)
            {
                item.AbsoluteExpiration = absoluteExpiration;
            }

            if (options.SlidingExpiration.HasValue)
            {
                item.SlidingExpiration = options.SlidingExpiration;
            }

            return _tableSet.AddOrUpdateAsync(item, token);
        }

        private DateTimeOffset? ParseOptions(DistributedCacheEntryOptions options, DateTimeOffset currentTime)
        {
            if (options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                return currentTime.Add(options.AbsoluteExpirationRelativeToNow.Value);
            }

            if (options.AbsoluteExpiration.HasValue)
            {
                if (options.AbsoluteExpiration.Value <= currentTime)
                {
                    throw new ArgumentOutOfRangeException(nameof(options.AbsoluteExpiration), options.AbsoluteExpiration.Value, "The absolute expiration value must be in the future.");
                }

                return options.AbsoluteExpiration;
            }

            return null;
        }

        private Task<CachedItem> RetrieveAsync(string key)
        {
            return _tableSet.FirstOrDefaultAsync(e => e.PartitionKey == _options.PartitionKey && e.RowKey == key);
        }

        /// <summary>
        /// Checks whether the cached item should be deleted based on the absolute or sliding expiration values.
        /// </summary>
        /// <param name="item">The <see cref="CachedItem" />.</param>
        /// <returns>
        ///   <c>true</c> if the item should be deleted, <c>false</c> otherwise.
        /// </returns>
        private bool ShouldDelete(CachedItem item)
        {
            var currentTime = DateTimeOffset.UtcNow;
            if (item.AbsoluteExpiration != null && item.AbsoluteExpiration.Value <= currentTime)
            {
                return true;
            }

            if (item.SlidingExpiration.HasValue &&
                item.LastAccessTime.HasValue &&
                item.LastAccessTime.Value.Add(item.SlidingExpiration.Value) < currentTime)
            {
                return true;
            }

            return false;
        }
    }
}