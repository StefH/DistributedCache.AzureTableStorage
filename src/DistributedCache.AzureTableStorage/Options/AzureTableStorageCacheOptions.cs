using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Stef.Extensions.Caching.AzureTableStorage.Options
{
    /// <summary>
    /// Configuration settings for the Azure table storage cache.
    /// </summary>
    public class AzureTableStorageCacheOptions
    {
        /// <summary>
        /// Gets or sets the connection string of the storage account.
        /// </summary>
        [Required]
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the name of the table to use.
        /// </summary>
        [Required]
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets the partition key to use.
        /// </summary>
        [Required]
        public string PartitionKey { get; set; }

        //[Required]
        //public string Separator { get; set; } = ":";

        // public IDictionary<Type, string> Prefixes { get; set; } = new Dictionary<Type, string>();

        //public Dictionary<(Type, Type), DistributedCacheEntryOptions> Options { get; }
    }
}