using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Stef.Extensions.Caching.AzureTableStorage.Models;
using Stef.Extensions.Caching.AzureTableStorage.Options;
using Stef.Extensions.Caching.AzureTableStorage.Validation;
using System;
using System.Threading;
using System.Threading.Tasks;
using WindowsAzure.Table;
using WindowsAzure.Table.Extensions;

namespace Stef.Extensions.Caching.AzureTableStorage.Implementations
{
    /// <summary>
    /// An <see cref="IDistributedCache"/> implementation to cache data in Azure table storage.
    /// </summary>
    /// <seealso cref="IDistributedCache"/>.
    public class AzureTableStorageCache : IDistributedCache
    {
        private readonly AzureTableStorageCacheOptions _options;

        private readonly ITableSet<CachedItem> _table;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureTableStorageCache"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        public AzureTableStorageCache([NotNull] IOptions<AzureTableStorageCacheOptions> options)
        {
            Guard.NotNull(options, nameof(options));

            _options = options.Value;

            // Create CloudTableClient
            var client = CloudStorageAccount.Parse(options.Value.ConnectionString).CreateCloudTableClient();

            // Create table sets
            _table = new TableSet<CachedItem>(client, options.Value.TableName);
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

            CachedItem item = await RetrieveAsync(key);
            if (item != null)
            {
                if (ShouldDelete(item))
                {
                    await RemoveAsync(item, token);
                    return;
                }

                item.LastAccessTime = DateTimeOffset.UtcNow;

                await _table.UpdateAsync(item, token);
            }
        }

        /// <inheritdoc cref="IDistributedCache.Remove(string)"/>
        public void Remove(string key)
        {
            Guard.NotNullOrEmpty(key, nameof(key));

            RemoveAsync(key).Wait();
        }

        /// <inheritdoc cref="IDistributedCache.RemoveAsync(string, CancellationToken)"/>
        public Task RemoveAsync([NotNull] string key, CancellationToken token = default(CancellationToken))
        {
            Guard.NotNullOrEmpty(key, nameof(key));

            CachedItem item = RetrieveAsync(key).Result;
            return item != null ? _table.RemoveAsync(item, token) : Task.CompletedTask;
        }

        /// <inheritdoc cref="IDistributedCache.Set(string, byte[], DistributedCacheEntryOptions)"/>
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            Guard.NotNullOrEmpty(key, nameof(key));
            Guard.NotNullOrEmpty(value, nameof(value));

            SetAsync(key, value, options).Wait();
        }

        /// <inheritdoc cref="IDistributedCache.SetAsync(string, byte[], DistributedCacheEntryOptions, CancellationToken)"/>
        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
        {
            Guard.NotNullOrEmpty(key, nameof(key));
            Guard.NotNullOrEmpty(value, nameof(value));

            DateTimeOffset? absoluteExpiration = null;
            DateTimeOffset currentTime = DateTimeOffset.UtcNow;

            if (options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                absoluteExpiration = currentTime.Add(options.AbsoluteExpirationRelativeToNow.Value);
            }
            else if (options.AbsoluteExpiration.HasValue)
            {
                if (options.AbsoluteExpiration.Value <= currentTime)
                {
                    throw new ArgumentOutOfRangeException(
                       nameof(options.AbsoluteExpiration),
                       options.AbsoluteExpiration.Value,
                       "The absolute expiration value must be in the future.");
                }

                absoluteExpiration = options.AbsoluteExpiration;
            }

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

            await _table.AddOrUpdateAsync(item, token);
        }

        private Task RemoveAsync([NotNull] CachedItem item, CancellationToken token = default(CancellationToken))
        {
            Guard.NotNull(item, nameof(item));

            return _table.RemoveAsync(item, token);
        }

        private Task<CachedItem> RetrieveAsync(string key)
        {
            return _table.FirstOrDefaultAsync(e => e.PartitionKey == _options.PartitionKey && e.RowKey == key);
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
            DateTimeOffset currentTime = DateTimeOffset.UtcNow;
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