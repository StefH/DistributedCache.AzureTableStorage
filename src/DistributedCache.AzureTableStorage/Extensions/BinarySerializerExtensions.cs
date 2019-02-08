using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System.IO;

namespace Stef.Extensions.Caching.AzureTableStorage.Extensions
{
    internal static class BinarySerializerExtensions
    {
        [CanBeNull]
        public static byte[] ToByteArray(this object obj)
        {
            if (obj == null)
            {
                return null;
            }

            var memoryStream = new MemoryStream();
            using (var writer = new BsonDataWriter(memoryStream))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(writer, obj);

                return memoryStream.ToArray();
            }
        }

        public static T FromByteArray<T>(this byte[] data) where T : class
        {
            if (data == null)
            {
                return default(T);
            }

            var memoryStream = new MemoryStream(data);
            using (var reader = new BsonDataReader(memoryStream))
            {
                var serializer = new JsonSerializer();
                return serializer.Deserialize<T>(reader);
            }
        }
    }
}