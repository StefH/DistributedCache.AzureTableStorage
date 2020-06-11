using System;
using WindowsAzure.Table.Attributes;

namespace DistributedCache.AzureTableStorage.Models
{
    /// <summary>
    /// Represents an item to be stored in the table.
    /// </summary>
    public class CachedItem
    {
        [PartitionKey]
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public string PartitionKey { get; set; }

        [RowKey]
        public string RowKey { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        public byte[]? Data { get; set; }

        ///// <summary>
        ///// Gets or sets the sliding expiration in ticks.
        ///// </summary>
        //public long? SlidingExpirationTicks { get; set; }

        ///// <summary>
        ///// Gets or sets the sliding expiration as a <see cref="TimeSpan"/>.
        ///// </summary>
        ///// <remarks>
        ///// Ignored as <see cref="TimeSpan"/> is not supported in Azure Table Storage.
        ///// </remarks>
        //[Ignore]
        //public TimeSpan? SlidingExpiration
        //{
        //    get
        //    {
        //        if (SlidingExpirationTicks.HasValue)
        //        {
        //            return TimeSpan.FromTicks(SlidingExpirationTicks.Value);
        //        }

        //        return null;
        //    }

        //    set
        //    {
        //        if (value.HasValue)
        //        {
        //            SlidingExpirationTicks = value.Value.Ticks;
        //        }
        //    }
        //}

        /// <summary>
        /// The absolute expiration date for the cache entry.
        /// This property is named 'AbsoluteExpiration' to be backwards compatible, a better name would be 'ExpiresAtTime'.
        /// </summary>
        public DateTimeOffset AbsoluteExpiration { get; set; }

        /// <summary>
        /// Gets or sets the date and time the item was last accessed.
        /// </summary>
        public DateTimeOffset LastAccessTime { get; set; }
    }
}