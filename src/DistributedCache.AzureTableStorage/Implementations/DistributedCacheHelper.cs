//using Stef.Extensions.Caching.AzureTableStorage.Options;

//namespace Stef.Extensions.Caching.AzureTableStorage.Implementations
//{
//    internal static class DistributedCacheHelper
//    {
//        private const string DefaultClassName = "Default";

//        public static string GetPartitionKey(AzureTableStorageCacheOptions options, string className = null)
//        {
//            return $"{options.PartitionKeyPrefix}{options.Separator}{className ?? DefaultClassName}";
//        }
//    }
//}