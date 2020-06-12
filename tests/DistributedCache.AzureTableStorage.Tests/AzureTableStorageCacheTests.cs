using DistributedCache.AzureTableStorage.Implementations;
using DistributedCache.AzureTableStorage.Options;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DistributedCache.AzureTableStorage.Tests
{
    public class AzureTableStorageCacheTests
    {
        private readonly IDistributedCache _cache;

        public AzureTableStorageCacheTests()
        {
            var options = Microsoft.Extensions.Options.Options.Create(
                new AzureTableStorageCacheOptions
                {
                    TableName = "AzureTableStorageCacheTests",
                    PartitionKey = "IntegrationTests",
                    ConnectionString = "UseDevelopmentStorage=true;"
                }
            );

            _cache = new AzureTableStorageCache(options);
            _cache.Remove("key1");
        }

        [Fact]
        public void Set_KeyIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            string key = null;
            byte[] value = Encoding.UTF32.GetBytes("value1");

            // Act
            Exception ex = Assert.Throws<ArgumentNullException>(() => _cache.Set(key, value));

            // Assert
            Assert.StartsWith("Value cannot be null.", ex.Message);
            Assert.Contains("Parameter name: key", ex.Message);
        }

        [Fact]
        public async Task SetAsync_KeyIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            string key = null;
            byte[] value = Encoding.UTF32.GetBytes("value1");

            // Act
            Exception ex = await Assert.ThrowsAsync<ArgumentNullException>(async () => await _cache.SetAsync(key, value));

            // Assert
            Assert.StartsWith("Value cannot be null.", ex.Message);
            Assert.Contains("Parameter name: key", ex.Message);
        }

        [Fact]
        public void Set_ValueIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            string key = "key1";
            byte[] value = null;

            // Act
            Exception ex = Assert.Throws<ArgumentNullException>(() => _cache.Set(key, value));

            // Assert
            Assert.StartsWith("Value cannot be null.", ex.Message);
            Assert.Contains("Parameter name: value", ex.Message);
        }

        [Fact]
        public async Task SetAsync_ValueIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            string key = "key1";
            byte[] value = null;

            // Act
            Exception ex = await Assert.ThrowsAsync<ArgumentNullException>(async () => await _cache.SetAsync(key, value));

            // Assert
            Assert.StartsWith("Value cannot be null.", ex.Message);
            Assert.Contains("Parameter name: value", ex.Message);
        }

        [Fact]
        public void Set_AbsoluteExpirationIsBeforeNow_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            string key = "key1";
            byte[] value = Encoding.UTF32.GetBytes("value1");

            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTime.Today.AddDays(-1)
            };

            // Act
            Exception ex = Assert.Throws<ArgumentOutOfRangeException>(() => _cache.Set(key, value, options));

            // Assert
            Assert.StartsWith("The absolute expiration value must be in the future.", ex.Message);
            Assert.Contains("Parameter name: AbsoluteExpiration", ex.Message);
        }

        [Fact]
        public async Task SetAsync_AbsoluteExpirationIsBeforeNow_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            string key = "key1";
            byte[] value = Encoding.UTF32.GetBytes("value1");

            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTime.Today.AddDays(-1)
            };

            // Act
            Exception ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await _cache.SetAsync(key, value, options));

            // Assert
            Assert.StartsWith("The absolute expiration value must be in the future.", ex.Message);
            Assert.Contains("Parameter name: AbsoluteExpiration", ex.Message);
        }

        [Fact(Skip = "SlidingExpiration is not supported")]
        public void Set_KeyIsNotNull_ValueIsStored()
        {
            // Arrange
            string key = "key1";
            byte[] value = Encoding.UTF32.GetBytes("value1");

            // Act
            _cache.Set(key, value);

            // Assert
            byte[] cachedValue = _cache.Get(key);
            Assert.Equal(value, cachedValue);
        }

        [Fact(Skip = "SlidingExpiration is not supported")]
        public async Task SetAsync_KeyIsNotNull_ValueIsStored()
        {
            // Arrange
            string key = "key1";
            byte[] value = Encoding.UTF32.GetBytes("value1");

            // Act
            await _cache.SetAsync(key, value);

            // Assert
            byte[] cachedValue = await _cache.GetAsync(key);
            Assert.Equal(value, cachedValue);
        }

        [Fact]
        public void Get_KeyIsNull_ThrowsArgumentNullException()
        {
            // Arrange/Act
            Exception ex = Assert.Throws<ArgumentNullException>(() => _cache.Get(null));

            // Assert
            Assert.StartsWith("Value cannot be null.", ex.Message);
            Assert.Contains("Parameter name: key", ex.Message);
        }

        [Fact]
        public async Task GetAsync_KeyIsNull_ThrowsArgumentNullException()
        {
            // Arrange/Act
            Exception ex = await Assert.ThrowsAsync<ArgumentNullException>(async () => await _cache.GetAsync(null));

            // Assert
            Assert.StartsWith("Value cannot be null.", ex.Message);
            Assert.Contains("Parameter name: key", ex.Message);
        }

        [Fact]
        public void Get_ItemDoesNotExist_ValueIsNull()
        {
            // Arrange
            string key = "key1";

            // Act
            byte[] cachedValue = _cache.Get(key);

            // Assert
            Assert.Null(cachedValue);
        }

        [Fact]
        public async Task GetAsync_ItemDoesNotExist_ValueIsNull()
        {
            // Arrange
            string key = "key1";

            // Act
            byte[] cachedValue = await _cache.GetAsync(key);

            // Assert
            Assert.Null(cachedValue);
        }

        [Fact(Skip = "SlidingExpiration is not supported")]
        public void Get_ItemExists_ValueIsRetrieved()
        {
            // Arrange
            string key = "key1";
            byte[] value = Encoding.UTF32.GetBytes("value1");
            _cache.Set(key, value);

            // Act
            byte[] cachedValue = _cache.Get(key);

            // Assert
            Assert.Equal(value, cachedValue);
        }

        [Fact(Skip = "SlidingExpiration is not supported")]
        public async Task GetAsync_ItemExists_ValueIsRetrieved()
        {
            // Arrange
            string key = "key1";
            byte[] value = Encoding.UTF32.GetBytes("value1");
            await _cache.SetAsync(key, value);

            // Act
            byte[] cachedValue = await _cache.GetAsync(key);

            // Assert
            Assert.Equal(value, cachedValue);
        }

        [Fact]
        public void Get_ItemExists_AbsoluteExpirationHasExpired_ValueIsNull()
        {
            // Arrange
            string key = "key1";
            byte[] value = Encoding.UTF32.GetBytes("value1");

            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTime.UtcNow.AddMilliseconds(1000)
            };

            _cache.Set(key, value, options);

            Thread.Sleep(2000);

            // Act
            byte[] cachedValue = _cache.Get(key);

            // Assert
            Assert.Null(cachedValue);
        }

        [Fact]
        public async Task GetAsync_ItemExists_AbsoluteExpirationHasExpired_ValueIsNull()
        {
            // Arrange
            string key = "key1";
            byte[] value = Encoding.UTF32.GetBytes("value1");

            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTime.UtcNow.AddMilliseconds(1000)
            };

            await _cache.SetAsync(key, value, options);

            Thread.Sleep(2000);

            // Act
            byte[] cachedValue = await _cache.GetAsync(key);

            // Assert
            Assert.Null(cachedValue);
        }

        [Fact]
        public void Get_ItemExists_AbsoluteExpirationAbsoluteExpirationRelativeToNowHasExpired_ValueIsNull()
        {
            // Arrange
            string key = "key1";
            byte[] value = Encoding.UTF32.GetBytes("value1");

            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(1000)
            };

            _cache.Set(key, value, options);

            Thread.Sleep(2000);

            // Act
            byte[] cachedValue = _cache.Get(key);

            // Assert
            Assert.Null(cachedValue);
        }

        [Fact]
        public async Task GetAsync_ItemExists_AbsoluteExpirationAbsoluteExpirationRelativeToNowHasExpired_ValueIsNull()
        {
            // Arrange
            string key = "key1";
            byte[] value = Encoding.UTF32.GetBytes("value1");

            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(1000)
            };

            await _cache.SetAsync(key, value, options);

            Thread.Sleep(2000);

            // Act
            byte[] cachedValue = await _cache.GetAsync(key);

            // Assert
            Assert.Null(cachedValue);
        }

        [Fact(Skip = "SlidingExpiration is not supported")]
        public void Get_ItemExists_SlidingExpirationHasExpired_ValueIsNull()
        {
            // Arrange
            string key = "key1";
            byte[] value = Encoding.UTF32.GetBytes("value1");

            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMilliseconds(1000)
            };

            _cache.Set(key, value, options);

            Thread.Sleep(2000);

            // Act
            byte[] cachedValue = _cache.Get(key);

            // Assert
            Assert.Null(cachedValue);
        }

        [Fact(Skip = "SlidingExpiration is not supported")]
        public async Task GetAsync_ItemExists_SlidingExpirationHasExpired_ValueIsNull()
        {
            // Arrange
            string key = "key1";
            byte[] value = Encoding.UTF32.GetBytes("value1");

            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMilliseconds(1000)
            };

            await _cache.SetAsync(key, value, options);

            Thread.Sleep(2000);

            // Act
            byte[] cachedValue = await _cache.GetAsync(key);

            // Assert
            Assert.Null(cachedValue);
        }

        [Fact(Skip = "SlidingExpiration is not supported")]
        public void Set_ItemWithKeyExists_ValueIsUpdated()
        {
            // Arrange
            string key = "key1";
            byte[] value = Encoding.UTF32.GetBytes("value1");
            _cache.Set(key, value);
            value = Encoding.UTF32.GetBytes("value2");

            // Act
            _cache.Set(key, value);

            // Assert
            byte[] cachedValue = _cache.Get(key);
            Assert.Equal(value, cachedValue);
        }

        [Fact(Skip = "SlidingExpiration is not supported")]
        public async Task SetAsync_ItemWithKeyExists_ValueIsUpdated()
        {
            // Arrange
            string key = "key1";
            byte[] value = Encoding.UTF32.GetBytes("value1");
            _cache.Set(key, value);
            value = Encoding.UTF32.GetBytes("value2");

            // Act
            await _cache.SetAsync(key, value);

            // Assert
            byte[] cachedValue = await _cache.GetAsync(key);
            Assert.Equal(value, cachedValue);
        }

        [Fact]
        public void Refresh_KeyIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            string key = null;

            // Act
            Exception ex = Assert.Throws<ArgumentNullException>(() => _cache.Refresh(key));

            // Assert
            Assert.StartsWith("Value cannot be null.", ex.Message);
            Assert.Contains("Parameter name: key", ex.Message);
        }

        [Fact]
        public async Task RefreshAsync_KeyIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            string key = null;

            // Act
            Exception ex = await Assert.ThrowsAsync<ArgumentNullException>(async () => await _cache.RefreshAsync(key));

            // Assert
            Assert.StartsWith("Value cannot be null.", ex.Message);
            Assert.Contains("Parameter name: key", ex.Message);
        }

        [Fact]
        public void Refresh_ValueIsRetrievedAfterAbsoluteExpirationHasExpired()
        {
            // Arrange
            string key = "key1";
            byte[] value = Encoding.UTF32.GetBytes("value1");

            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTime.UtcNow.AddMilliseconds(1000)
            };

            _cache.Set(key, value, options);

            Thread.Sleep(750);

            // Act
            _cache.Refresh(key);

            // Assert
            Thread.Sleep(750);
            byte[] cachedValue = _cache.Get(key);
            Assert.Null(cachedValue);
        }

        [Fact]
        public async Task RefreshAsync_ValueIsRetrievedAfterAbsoluteExpirationHasExpired()
        {
            // Arrange
            string key = "key1";
            byte[] value = Encoding.UTF32.GetBytes("value1");

            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTime.UtcNow.AddMilliseconds(1000)
            };

            _cache.Set(key, value, options);

            Thread.Sleep(750);

            // Act
            await _cache.RefreshAsync(key);

            // Assert
            Thread.Sleep(750);
            byte[] cachedValue = await _cache.GetAsync(key);
            Assert.Null(cachedValue);
        }

        [Fact(Skip = "SlidingExpiration is not supported")]
        public void Refresh_ValueIsRetrievedAfterOriginalSlidingExpirationHasExpired()
        {
            // Arrange
            string key = "key1";
            byte[] value = Encoding.UTF32.GetBytes("value1");

            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMilliseconds(1000)
            };

            _cache.Set(key, value, options);

            Thread.Sleep(750);

            // Act
            _cache.Refresh(key);

            // Assert
            Thread.Sleep(750);
            byte[] cachedValue = _cache.Get(key);
            Assert.Equal(value, cachedValue);
        }

        [Fact(Skip = "SlidingExpiration is not supported")]
        public async Task RefreshAsync_ValueIsRetrievedAfterOriginalSlidingExpirationHasExpired()
        {
            // Arrange
            string key = "key1";
            byte[] value = Encoding.UTF32.GetBytes("value1");

            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMilliseconds(1000)
            };

            _cache.Set(key, value, options);

            Thread.Sleep(750);

            // Act
            await _cache.RefreshAsync(key);

            // Assert
            Thread.Sleep(750);
            byte[] cachedValue = await _cache.GetAsync(key);
            Assert.Equal(value, cachedValue);
        }

        [Fact]
        public void Remove_KeyIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            string key = null;

            // Act
            Exception ex = Assert.Throws<ArgumentNullException>(() => _cache.Remove(key));

            // Assert
            Assert.StartsWith("Value cannot be null.", ex.Message);
            Assert.Contains("Parameter name: key", ex.Message);
        }

        [Fact]
        public async Task RemoveAsync_KeyIsNull_ThrowsArgumentNullException()
        {
            // Arrange
            string key = null;

            // Act
            Exception ex = await Assert.ThrowsAsync<ArgumentNullException>(async () => await _cache.RemoveAsync(key));

            // Assert
            Assert.StartsWith("Value cannot be null.", ex.Message);
            Assert.Contains("Parameter name: key", ex.Message);
        }

        [Fact(Skip = "SlidingExpiration is not supported")]
        public void Remove_ValueIsNull()
        {
            // Arrange
            string key = "key1";
            byte[] value = Encoding.UTF32.GetBytes("value1");
            _cache.Set(key, value);

            // Act
            _cache.Remove(key);

            // Assert
            byte[] cachedValue = _cache.Get(key);
            Assert.Null(cachedValue);
        }

        [Fact(Skip = "SlidingExpiration is not supported")]
        public async Task RemoveAsync_ValueIsNull()
        {
            // Arrange
            string key = "key1";
            byte[] value = Encoding.UTF32.GetBytes("value1");

            _cache.Set(key, value);

            // Act
            await _cache.RemoveAsync(key);

            // Assert
            byte[] cachedValue = await _cache.GetAsync(key);
            Assert.Null(cachedValue);
        }
    }
}