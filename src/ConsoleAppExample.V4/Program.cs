using ConsoleAppExample.V4;
using DistributedCache.AzureTableStorage.Extensions;
using DistributedCache.AzureTableStorage.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

var services = new ServiceCollection();

// Configure
services.Configure<AzureTableStorageCacheOptions>(options =>
{
    options.TableName = "CacheTest";
    options.PartitionKey = "ConsoleApp";
    options.ConnectionString = "UseDevelopmentStorage=true;";
    options.ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(5);
});

// Add logging & services
services.AddLogging(builder =>
{
    builder.SetMinimumLevel(LogLevel.Trace);
    builder.AddConsole();
    builder.AddDebug();
});
services.AddDistributedAzureTableStorageCache();

// Build ServiceProvider
var serviceProvider = services.BuildServiceProvider();

// Resolve ILoggerFactory and ILogger via DI
var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();
logger.LogInformation("Start...");

// Resolve IDistributedCache via DI
var cache = serviceProvider.GetService<IDistributedCache>();

// Run tests
TestAsync(logger, cache).GetAwaiter().GetResult();


static async Task TestAsync(ILogger logger, IDistributedCache cache)
{
    var test = new TestModel
    {
        Id = 1,
        Name = "Test 1"
    };

    var cacheOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(3) };
    await cache.SetAsync("t1", test, cacheOptions);

    var t2 = await cache.GetAsync<TestModel>("t1");
    logger.LogInformation("t2 : {TestModel}", JsonConvert.SerializeObject(t2));

    await Task.Delay(TimeSpan.FromSeconds(2));

    var t3 = await cache.GetAsync<TestModel>("t1");
    logger.LogInformation("t3 : {TestModel}", JsonConvert.SerializeObject(t3));

    await Task.Delay(TimeSpan.FromSeconds(2));

    var t4 = await cache.GetAsync<TestModel>("t1");
    logger.LogInformation("t4 : {TestModel}", JsonConvert.SerializeObject(t4));

    string stringKey = "string-" + Guid.NewGuid();
    await cache.SetAsync(stringKey, "testabc", cacheOptions);

    var stringResult = await cache.GetAsync<string>(stringKey);
    logger.LogInformation("stringResult : {stringResult}", stringResult);

    string arrayKey = "array-" + Guid.NewGuid();
    cacheOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(4) };
    await cache.SetAsync(arrayKey, new[] { "a", "b" }, cacheOptions);

    var arrayResult = await cache.GetAsync<string[]>(arrayKey);
    logger.LogInformation("arrayResult : {arrayResult}", JsonConvert.SerializeObject(arrayResult));

    await Task.Delay(TimeSpan.FromSeconds(1));
}