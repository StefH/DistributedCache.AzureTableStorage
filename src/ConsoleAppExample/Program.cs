using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Threading;
using DistributedCache.AzureTableStorage.Extensions;
using DistributedCache.AzureTableStorage.Implementations;
using DistributedCache.AzureTableStorage.Options;

namespace ConsoleAppExample
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = Options.Create(
                new AzureTableStorageCacheOptions
                {
                    TableName = "CacheTest",
                    PartitionKey = "ConsoleApp",
                    //Separator = ":",
                    ConnectionString = "UseDevelopmentStorage=true;"
                }
            );

            IDistributedCache cache = new AzureTableStorageCache(options);

            var test = new TestModel
            {
                Id = 1,
                Name = "Test 1"
            };

            var cacheOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(3) };
            cache.SetAsync("t1", test, cacheOptions).GetAwaiter().GetResult();

            var t2 = cache.GetAsync<TestModel>("t1").GetAwaiter().GetResult();

            Console.WriteLine("t2" + JsonConvert.SerializeObject(t2));

            int x = 0;

            Thread.Sleep(TimeSpan.FromSeconds(2));

            var t3 = cache.GetAsync<TestModel>("t1").GetAwaiter().GetResult();
            Console.WriteLine("t3" + JsonConvert.SerializeObject(t3));

            Thread.Sleep(TimeSpan.FromSeconds(2));

            var t4 = cache.GetAsync<TestModel>("t1").GetAwaiter().GetResult();
            Console.WriteLine("t4" + JsonConvert.SerializeObject(t4));
        }
    }
}
