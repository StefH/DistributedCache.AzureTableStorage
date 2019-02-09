using DistributedCache.AzureTableStorage.Extensions;
using DistributedCache.AzureTableStorage.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace ConsoleAppExample
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create ServiceCollection
            var services = new ServiceCollection();

            // Configure
            services.Configure<AzureTableStorageCacheOptions>(options =>
            {
                options.TableName = "CacheTest";
                options.PartitionKey = "ConsoleApp";
                options.ConnectionString = "UseDevelopmentStorage=true;";
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
            var logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<Program>();
            logger.LogInformation("Start...");

            // Resolve IDistributedCache via DI
            var cache = serviceProvider.GetService<IDistributedCache>();

            // Run tests
            TestAsync(logger, cache).GetAwaiter().GetResult();
        }

        private static async Task TestAsync(ILogger logger, IDistributedCache cache)
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

            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }
}