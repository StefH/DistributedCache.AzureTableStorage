using Azure.Data.Tables;
using DistributedCache.AzureTableStorage.Options;
using Microsoft.Extensions.Options;

namespace DistributedCache.AzureTableStorage.Implementations;

internal class TableStorageClientUsingStorageConnectionString : AzureTableStorageCache
{
    public TableStorageClientUsingStorageConnectionString(IOptions<AzureTableStorageCacheOptions> options) :
        base(options, new TableServiceClient(options.Value.ConnectionString))
    {
    }
}