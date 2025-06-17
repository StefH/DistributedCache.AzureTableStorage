## DistributedCache.AzureTableStorage

* Based on [oceanweb/azuretablestoragecache](https://gitlab.com/oceanweb/azuretablestoragecache) but with lower dependencies.
* Extra added logic to use strongly typed objects with IDistributedCache instead of byte arrays.
* Multiple versions are available, see table below for details.


### Code example

#### Configure in code (option 1)
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

#### Notes

- If the TableName does not exists, it's created. If you want to disable this behaviour, set the `options.CreateTableIfNotExists` to `false`.
- When an item is retrieved or added to to the cache, a periodic interval scan can be done to find and delete expired items in the cache. Default this is set to `null`, which means that this functionality is disabled. If you want to change this, set the `options.ExpiredItemsDeletionInterval` to a different value, like `TimeSpan.FromMinutes(30)`.

#### Configure via appsettings.json (option 2)

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

#### Add DistributedAzureTableStorageCache to services
``` c#
services.AddDistributedAzureTableStorageCache();
```

#### Usage

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


### Sponsors

[Entity Framework Extensions](https://entityframework-extensions.net/?utm_source=StefH) and [Dapper Plus](https://dapper-plus.net/?utm_source=StefH) are major sponsors and proud to contribute to the development of **DistributedCache.AzureTableStorage**.

[![Entity Framework Extensions](https://raw.githubusercontent.com/StefH/resources/main/sponsor/entity-framework-extensions-sponsor.png)](https://entityframework-extensions.net/bulk-insert?utm_source=StefH)

[![Dapper Plus](https://raw.githubusercontent.com/StefH/resources/main/sponsor/dapper-plus-sponsor.png)](https://dapper-plus.net/bulk-insert?utm_source=StefH)