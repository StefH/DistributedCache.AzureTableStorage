using Azure;
using Azure.Data.Tables;
using DistributedCache.AzureTableStorage.Models;
using DistributedCache.AzureTableStorage.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using Stef.Validation;

namespace DistributedCache.AzureTableStorage.Implementations;

/// <summary>
/// An <see cref="IDistributedCache"/> implementation to cache data in Azure Table Storage.
/// </summary>
internal abstract class AzureTableStorageCache : IDistributedCache
{
    private static readonly TimeSpan MinimumExpiredItemsDeletionInterval = TimeSpan.FromMinutes(5);
    private readonly ISystemClock _systemClock;
    private readonly TimeSpan? _expiredItemsDeletionInterval;
    private DateTimeOffset _lastExpirationScan;
    private readonly Func<Task> _deleteExpiredCachedItemsDelegate;
    private readonly string _partitionKey;
    private readonly TableClient _tableClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureTableStorageCache"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="tableServiceClient">The TableServiceClient.</param>
    public AzureTableStorageCache(IOptions<AzureTableStorageCacheOptions> options, TableServiceClient tableServiceClient)
    {
        Guard.NotNull(tableServiceClient);

        var cacheOptions = Guard.NotNull(options.Value);
        Guard.NotNullOrEmpty(cacheOptions.TableName);
        Guard.NotNullOrEmpty(cacheOptions.PartitionKey);

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

        if (options.Value.CreateTableIfNotExists)
        {
            tableServiceClient.CreateTableIfNotExists(options.Value.TableName);
        }

        _tableClient = tableServiceClient.GetTableClient(options.Value.TableName);
    }

    /// <inheritdoc cref="IDistributedCache.Get(string)"/>
    public byte[]? Get(string key)
    {
        Guard.NotNullOrEmpty(key);

        var value = GetAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();

        ScanForExpiredItemsIfRequired();

        return value;
    }

    /// <inheritdoc cref="IDistributedCache.GetAsync(string, CancellationToken)"/>
    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        Guard.NotNullOrEmpty(key);

        await RefreshAsync(key, token).ConfigureAwait(false);

        CachedItem? item = await Retrieve(key).ConfigureAwait(false);

        ScanForExpiredItemsIfRequired();

        return item?.Data;
    }

    /// <inheritdoc cref="IDistributedCache.Refresh(string)"/>
    public void Refresh(string key)
    {
        Guard.NotNullOrEmpty(key);

        RefreshAsync(key).ConfigureAwait(false).GetAwaiter().GetResult();

        ScanForExpiredItemsIfRequired();
    }

    /// <inheritdoc cref="IDistributedCache.RefreshAsync(string, CancellationToken)"/>
    public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        Guard.NotNullOrEmpty(key);

        var item = await Retrieve(key).ConfigureAwait(false);
        if (item != null)
        {
            if (ShouldDelete(item))
            {
                await _tableClient
                    .DeleteEntityAsync(item.PartitionKey, item.RowKey, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            item.LastAccessTime = _systemClock.UtcNow;

            await _tableClient
                .UpdateEntityAsync(item, ETag.All, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        ScanForExpiredItemsIfRequired();
    }

    /// <inheritdoc cref="IDistributedCache.Remove(string)"/>
    public void Remove(string key)
    {
        Guard.NotNullOrEmpty(key);

        RemoveAsync(key).Wait();

        ScanForExpiredItemsIfRequired();
    }

    /// <inheritdoc cref="IDistributedCache.RemoveAsync(string, CancellationToken)"/>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        Guard.NotNullOrEmpty(key);

        var item = await Retrieve(key).ConfigureAwait(false);
        if (item != null)
        {
            await _tableClient
                .DeleteEntityAsync(item.PartitionKey, item.RowKey, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        ScanForExpiredItemsIfRequired();
    }

    /// <inheritdoc cref="IDistributedCache.Set(string, byte[], DistributedCacheEntryOptions)"/>
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        Guard.NotNullOrEmpty(key);
        Guard.NotNullOrEmpty(value);
        Guard.NotNull(options);

        SetAsync(key, value, options).ConfigureAwait(false).GetAwaiter().GetResult();

        ScanForExpiredItemsIfRequired();
    }

    /// <inheritdoc cref="IDistributedCache.SetAsync(string, byte[], DistributedCacheEntryOptions, CancellationToken)"/>
    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        Guard.NotNullOrEmpty(key);
        Guard.NotNullOrEmpty(value);
        Guard.NotNull(options);

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

        await _tableClient.UpsertEntityAsync(item, TableUpdateMode.Replace, token).ConfigureAwait(false);

        ScanForExpiredItemsIfRequired();
    }

    private static DateTimeOffset GetExpiresAtTime(DistributedCacheEntryOptions options, DateTimeOffset currentTime)
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

    private ValueTask<CachedItem?> Retrieve(string key)
    {
        return _tableClient
            .QueryAsync<CachedItem>(e => e.PartitionKey == _partitionKey && e.RowKey == key)
            .FirstOrDefaultAsync();
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

        var itemsToDelete = await _tableClient.QueryAsync<CachedItem>()
            .Where(item => item.PartitionKey == _partitionKey && item.AbsoluteExpiration <= utcNow)
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (var itemToDelete in itemsToDelete)
        {
            try
            {
                await _tableClient
                    .DeleteEntityAsync(itemToDelete.PartitionKey, itemToDelete.RowKey)
                    .ConfigureAwait(false);
            }
            catch
            {
                // Just ignore any exceptions
            }
        }
    }
}