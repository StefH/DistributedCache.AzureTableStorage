using Azure.Data.Tables;
using DistributedCache.AzureTableStorage.Models;
using DistributedCache.AzureTableStorage.Options;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

namespace DistributedCache.AzureTableStorage.Implementations;

internal class TableStorageClientUsingIAzureClientFactory : AzureTableStorageCache
{
    public TableStorageClientUsingIAzureClientFactory(IOptions<AzureTableStorageCacheOptions> options, IAzureClientFactory<TableServiceClient> clientFactory) :
        base(options, clientFactory.CreateClient(typeof(CachedItem).FullName))
    {
    }
}