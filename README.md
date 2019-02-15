# DistributedCache.AzureTableStorage

* Based on [oceanweb/azuretablestoragecache](https://gitlab.com/oceanweb/azuretablestoragecache) but with lower dependencies.
* Extra added logic to use strongly typed objects with IDistributedCache instead of byte arrays.

## Info
| | |
| --- | --- |
| **NuGet** | [![NuGet Badge](https://buildstats.info/nuget/DistributedCache.AzureTableStorage)](https://www.nuget.org/packages/DistributedCache.AzureTableStorage) |

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
});
```

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
