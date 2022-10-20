using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Internal;

namespace DistributedCache.AzureTableStorage.Options;

/// <summary>
/// Configuration settings for the Azure Table Storage cache.
/// </summary>
public class AzureTableStorageCacheOptions
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
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the StorageAccountUri of the storage account.
    /// </summary>
    public Uri? StorageAccountUri { get; set; }

    /// <summary>
    /// Gets or sets the TenantId.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the ClientId.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the ClientSecret.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the Username.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the Password.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// The name of the table to use.
    /// </summary>
    [Required]
    public string TableName { get; set; } = null!;

    /// <summary>
    /// The Partition Key to use.
    /// </summary>
    [Required]
    public string PartitionKey { get; set; } = null!;

    /// <summary>
    /// Creates the table 'TableName' if it does not already exist. Default is true.
    /// </summary>
    public bool CreateTableIfNotExists { get; set; } = true;
}