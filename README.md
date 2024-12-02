# ![logo](resources/AzureTableStorage_logo_64x64.png?raw=true) DistributedCache.AzureTableStorage

* Based on [oceanweb/azuretablestoragecache](https://gitlab.com/oceanweb/azuretablestoragecache) but with lower dependencies.
* Extra added logic to use strongly typed objects with IDistributedCache instead of byte arrays.
* Multiple versions are available, see table below for details.

## Info
| Version | Dependencies | Information 
| :--- | :--- | :---
[![V1](https://img.shields.io/badge/nuget-v1.2.0-blue)](https://www.nuget.org/packages/DistributedCache.AzureTableStorage/1.2.0) | [Windows.Azure.Storage](https://www.nuget.org/packages/WindowsAzure.Storage/) | This dependency is declared deprecated by Microsoft.
[![V2](https://img.shields.io/badge/nuget-v2.1.0-blue)](https://www.nuget.org/packages/DistributedCache.AzureTableStorage/2.1.0) | [Microsoft.Azure.Cosmos.Table](https://www.nuget.org/packages/Microsoft.Azure.Cosmos.Table/1.0.7) |
[![V2-preview](https://img.shields.io/badge/nuget-v2.1.0_Preview-blue)](https://www.nuget.org/packages/DistributedCache.AzureTableStorage/2.1.0-preview) | [Microsoft.Azure.Cosmos.Table - Preview](https://www.nuget.org/packages/Microsoft.Azure.Cosmos.Table/2.0.0-preview) | This is still a preview version.
[![V3](https://img.shields.io/badge/nuget-v3.3.0-blue)](https://www.nuget.org/packages/DistributedCache.AzureTableStorage) | [Azure.Data.Tables](https://www.nuget.org/packages/Azure.Data.Tables) |


## Code example

### Configure in code (option 1)
c# code:
``` c#
// Configure in code
services.Configure<AzureTableStorageCacheOptions>(options =>
{
    options.TableName = "CacheTest";
    options.PartitionKey = "ConsoleApp";
    options.ConnectionString = "UseDevelopmentStorage=true;";
    options.ExpiredItemsDeletionInterval = TimeSpan.FromHours(24);
});
```

### Notes

- If the TableName does not exists, it's created. If you want to disable this behaviour, set the `options.CreateTableIfNotExists` to `false`.
- When an item is retrieved or added to to the cache, a periodic interval scan can be done to find and delete expired items in the cache. Default this is set to `null`, which means that this functionality is disabled. If you want to change this, set the `options.ExpiredItemsDeletionInterval` to a different value, like `TimeSpan.FromMinutes(30)`.

### Configure via appsettings.json (option 2)

appsettings.json:
``` js
"AzureTableStorageCacheOptions": {
  "ConnectionString": "UseDevelopmentStorage=true;",
  "TableName": "CacheTest",
  "PartitionKey": "ConsoleApp"
}
```

c# code:
``` c#
// Configure via app.setttings
services.Configure<AzureTableStorageCacheOptions>(Configuration.GetSection("AzureTableStorageCacheOptions"));
```

### Add DistributedAzureTableStorageCache to services
``` c#
services.AddDistributedAzureTableStorageCache();
```

### Usage

``` c#
IDistributedCache cache = ... // injected via DI

var test = new TestModel
{
    Id = 1,
    Name = "Test 1"
};

// Set an item in the cache using an expiration from 30 seconds
var cacheOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) };
await cache.SetAsync("t1", test, cacheOptions);

// Retrieve the item from the cache
var t2 = await cache.GetAsync<TestModel>("t1");
logger.LogInformation("t2 : {TestModel}", JsonConvert.SerializeObject(t2));
```
