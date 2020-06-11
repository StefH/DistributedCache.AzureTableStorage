using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DistributedCache.AzureTableStorage.Utils
{
    internal static class BinarySerializer
    {
        public static byte[]? Serialize(object? obj)
        {
            if (obj == null)
            {
                return null;
            }

            var memoryStream = new MemoryStream();
            using var writer = new BsonDataWriter(memoryStream);
            var serializer = new JsonSerializer();
            serializer.Serialize(writer, obj);

            return memoryStream.ToArray();
        }

        public static T? Deserialize<T>(byte[]? data) where T : class
        {
            if (data == null)
            {
                return default;
            }

            var memoryStream = new MemoryStream(data);
            using var reader = new BsonDataReader(memoryStream);
            if (typeof(T).GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                reader.ReadRootValueAsArray = true;
            }

            var serializer = new JsonSerializer();
            return serializer.Deserialize<T>(reader);
        }
    }
}