using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DistributedCache.AzureTableStorage.Models;
using DistributedCache.AzureTableStorage.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using Stef.Validation;
using WindowsAzure.Table;
using WindowsAzure.Table.Extensions;
#if WINDOWSAZURE
using Microsoft.WindowsAzure.Storage;
#else
using Microsoft.Azure.Cosmos.Table;
#endif


namespace DistributedCache.AzureTableStorage.Implementations
{
    /// <summary>
    /// An <see cref="IDistributedCache"/> implementation to cache data in Azure Table Storage.
    /// </summary>
    /// <seealso cref="IDistributedCache"/>.
    public class AzureTableStorageCache : IDistributedCache
    {
        private static readonly TimeSpan MinimumExpiredItemsDeletionInterval = TimeSpan.FromMinutes(5);
        private readonly ISystemClock _systemClock;
        private readonly TimeSpan? _expiredItemsDeletionInterval;
        private DateTimeOffset _lastExpirationScan;
        private readonly Func<Task> _deleteExpiredCachedItemsDelegate;
        private readonly string _partitionKey;
        private readonly Lazy<ITableSet<CachedItem>> _tableSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureTableStorageCache"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        public AzureTableStorageCache(IOptions<AzureTableStorageCacheOptions> options)
        {
            Guard.NotNull(options);

            var cacheOptions = options.Value;

            Guard.NotNullOrEmpty(cacheOptions.ConnectionString, nameof(AzureTableStorageCacheOptions.ConnectionString));
            Guard.NotNullOrEmpty(cacheOptions.TableName, nameof(AzureTableStorageCacheOptions.TableName));
            Guard.NotNullOrEmpty(cacheOptions.PartitionKey, nameof(AzureTableStorageCacheOptions.PartitionKey));

            if (cacheOptions.ExpiredItemsDeletionInterval.HasValue && cacheOptions.ExpiredItemsDeletionInterval.Value < MinimumExpiredItemsDeletionInterval)
            {
                throw new ArgumentException(
                    $"{nameof(AzureTableStorageCacheOptions.ExpiredItemsDeletionInterval)} cannot be less than the minimum " +
                    $"value of {MinimumExpiredItemsDeletionInterval.TotalMinutes} minutes.");
            }

            _systemClock = cacheOptions.SystemClock ?? new SystemClock();
            _expiredItemsDeletionInterval = cacheOptions.ExpiredItemsDeletionInterval;
            _deleteExpiredCachedItemsDelegate = DeleteExpiredCacheItems;
            _partitionKey = cacheOptions.PartitionKey;

            _tableSet = new Lazy<ITableSet<CachedItem>>(() =>
            {
                // Create CloudTableClient
                var client = CloudStorageAccount.Parse(cacheOptions.ConnectionString).CreateCloudTableClient();

                // Create TableSet
                var tableSet = new TableSet<CachedItem>(client, cacheOptions.TableName);

                if (cacheOptions.CreateTableIfNotExists)
                {
                    tableSet.CreateIfNotExistsAsync().GetAwaiter().GetResult();
                }

                return tableSet;
            });
        }

        /// <inheritdoc cref="IDistributedCache.Get(string)"/>
        public byte[]? Get(string key)
        {
            Guard.NotNullOrEmpty(key, nameof(key));

            var value = GetAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();

            ScanForExpiredItemsIfRequired();

            return value;
        }

        /// <inheritdoc cref="IDistributedCache.GetAsync(string, CancellationToken)"/>
        public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            Guard.NotNullOrEmpty(key, nameof(key));

            await RefreshAsync(key, token).ConfigureAwait(false);

            CachedItem item = await RetrieveAsync(key).ConfigureAwait(false);

            ScanForExpiredItemsIfRequired();

            return item?.Data;
        }

        /// <inheritdoc cref="IDistributedCache.Refresh(string)"/>
        public void Refresh(string key)
        {
            Guard.NotNullOrEmpty(key, nameof(key));

            RefreshAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();

            ScanForExpiredItemsIfRequired();
        }

        /// <inheritdoc cref="IDistributedCache.RefreshAsync(string, CancellationToken)"/>
        public async Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
        {
            Guard.NotNullOrEmpty(key, nameof(key));

            var item = await RetrieveAsync(key).ConfigureAwait(false);
            if (item != null)
            {
                if (ShouldDelete(item))
                {
                    await _tableSet.Value.RemoveAsync(item, token).ConfigureAwait(false);
                    return;
                }

                item.LastAccessTime = _systemClock.UtcNow;

                await _tableSet.Value.UpdateAsync(item, token).ConfigureAwait(false);
            }

            ScanForExpiredItemsIfRequired();
        }

        /// <inheritdoc cref="IDistributedCache.Remove(string)"/>
        public void Remove(string key)
        {
            Guard.NotNullOrEmpty(key, nameof(key));

            RemoveAsync(key).Wait();

            ScanForExpiredItemsIfRequired();
        }

        /// <inheritdoc cref="IDistributedCache.RemoveAsync(string, CancellationToken)"/>
        public async Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
        {
            Guard.NotNullOrEmpty(key, nameof(key));

            var item = await RetrieveAsync(key).ConfigureAwait(false);
            if (item != null)
            {
                await _tableSet.Value.RemoveAsync(item, token).ConfigureAwait(false);
            }

            ScanForExpiredItemsIfRequired();
        }

        /// <inheritdoc cref="IDistributedCache.Set(string, byte[], DistributedCacheEntryOptions)"/>
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            Guard.NotNullOrEmpty(key, nameof(key));
            Guard.NotNullOrEmpty(value, nameof(value));
            Guard.NotNull(options, nameof(options));

            SetAsync(key, value, options).ConfigureAwait(false).GetAwaiter().GetResult();

            ScanForExpiredItemsIfRequired();
        }

        /// <inheritdoc cref="IDistributedCache.SetAsync(string, byte[], DistributedCacheEntryOptions, CancellationToken)"/>
        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
        {
            Guard.NotNullOrEmpty(key, nameof(key));
            Guard.NotNullOrEmpty(value, nameof(value));
            Guard.NotNull(options, nameof(options));

            var utcNow = _systemClock.UtcNow;
            var expiresAtTime = GetExpiresAtTime(options, utcNow);

            var item = new CachedItem
            {
                PartitionKey = _partitionKey,
                RowKey = key,
                Data = value,
                LastAccessTime = utcNow,
                AbsoluteExpiration = expiresAtTime
            };

            //if (absoluteExpiration.HasValue)
            //{
            //    item.AbsoluteExpiration = absoluteExpiration;
            //}

            //if (options.SlidingExpiration.HasValue)
            //{
            //    throw new NotSupportedException("SlidingExpiration as TimeSpan is not supported in Azure Table Storage.");
            //    //item.SlidingExpiration = options.SlidingExpiration;
            //}

            await _tableSet.Value.AddOrUpdateAsync(item, token);

            ScanForExpiredItemsIfRequired();
        }

        private DateTimeOffset GetExpiresAtTime(DistributedCacheEntryOptions options, DateTimeOffset currentTime)
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

                return options.AbsoluteExpiration.Value;
            }

            throw new NotSupportedException("Only 'AbsoluteExpirationRelativeToNow' and 'AbsoluteExpiration' are supported in Azure Table Storage.");
        }

        private Task<CachedItem> RetrieveAsync(string key)
        {
            return _tableSet.Value.FirstOrDefaultAsync(e => e.PartitionKey == _partitionKey && e.RowKey == key);
        }

        /// <summary>
        /// Checks whether the cached item should be deleted based on the AbsoluteExpiration value.
        /// </summary>
        /// <param name="item">The <see cref="CachedItem" />.</param>
        /// <returns>
        ///   <c>true</c> if the item should be deleted, <c>false</c> otherwise.
        /// </returns>
        private bool ShouldDelete(CachedItem item)
        {
            var utcNow = _systemClock.UtcNow;

            return item.AbsoluteExpiration <= utcNow;
            //if (item.AbsoluteExpiration != null && item.AbsoluteExpiration.Value <= utcNow)
            //{
            //    return true;
            //}

            //return item.SlidingExpiration.HasValue &&
            //       item.LastAccessTime.HasValue &&
            //       item.LastAccessTime.Value.Add(item.SlidingExpiration.Value) < utcNow;
        }

        // Called by multiple actions to see how long it's been since we last checked for expired items.
        // If sufficient time has elapsed then a scan is initiated on a background task.
        private void ScanForExpiredItemsIfRequired()
        {
            var utcNow = _systemClock.UtcNow;

            // TODO: Multiple threads could trigger this scan which leads to multiple calls to Azure Table Storage.
            if (utcNow - _lastExpirationScan > _expiredItemsDeletionInterval)
            {
                _lastExpirationScan = utcNow;
                Task.Run(_deleteExpiredCachedItemsDelegate);
            }
        }

        private async Task DeleteExpiredCacheItems()
        {
            var utcNow = _systemClock.UtcNow;

            var itemsToDelete = await _tableSet.Value
                .Where(item => item.PartitionKey == _partitionKey && item.AbsoluteExpiration <= utcNow)
                .ToListAsync();

            try
            {
                await _tableSet.Value.RemoveAsync(itemsToDelete);
            }
            catch
            {
                // Just ignore any exceptions
            }
        }
    }
}