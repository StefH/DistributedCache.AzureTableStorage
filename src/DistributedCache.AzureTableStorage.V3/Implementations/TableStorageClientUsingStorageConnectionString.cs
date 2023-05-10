using Azure.Data.Tables;
using DistributedCache.AzureTableStorage.Options;
using Microsoft.Extensions.Options;
using Stef.Validation;

namespace DistributedCache.AzureTableStorage.Implementations;

internal class TableStorageClientUsingStorageConnectionString : AzureTableStorageCache
{
    public TableStorageClientUsingStorageConnectionString(IOptions<AzureTableStorageCacheOptions> options) :
        base(options, new TableServiceClient(Guard.NotNullOrWhiteSpace(options.Value.ConnectionString), options.Value.ServiceVersionId.HasValue ? new TableClientOptions((TableClientOptions.ServiceVersion)options.Value.ServiceVersionId) : null))
    {
    }
}