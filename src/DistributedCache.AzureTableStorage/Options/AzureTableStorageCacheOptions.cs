using System;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;

namespace DistributedCache.AzureTableStorage.Options
{
    /// <summary>
    /// Configuration settings for the Azure Table Storage cache.
    /// </summary>
    public class AzureTableStorageCacheOptions : IOptions<AzureTableStorageCacheOptions>
    {
        /// <summary>
        /// An abstraction to represent the clock of a machine in order to enable unit testing.
        /// </summary>
        public ISystemClock? SystemClock { get; set; }

        /// <summary>
        /// The periodic interval to scan and delete expired items in the cache. Default this is set to null, which means that this functionality is disabled.
        /// </summary>
        public TimeSpan? ExpiredItemsDeletionInterval { get; set; }

        /// <summary>
        /// Gets or sets the connection string of the storage account.
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public string ConnectionString { get; set; }

        /// <summary>
        /// The name of the table to use.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// The Partition Key to use.
        /// </summary>
        public string PartitionKey { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        /// <summary>
        /// Creates the table 'TableName' if it does not already exist. Default is true.
        /// </summary>
        public bool CreateTableIfNotExists { get; set; } = true;
        
        /// <inheritdoc cref="IOptions{TOptions}.Value"/>
        AzureTableStorageCacheOptions IOptions<AzureTableStorageCacheOptions>.Value => this;
    }
}