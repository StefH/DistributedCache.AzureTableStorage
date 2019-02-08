using System.ComponentModel.DataAnnotations;

namespace DistributedCache.AzureTableStorage.Options
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
    }
}