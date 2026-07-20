using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Caching
{
    /// <summary>
    /// Generic Redis distributed cache service.
    /// Wraps IDistributedCache with strongly-typed get/set/remove helpers
    /// and structured logging for cache hits and misses.
    /// </summary>
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisCacheService> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
        {
            _cache  = cache;
            _logger = logger;
        }

        /// <summary>
        /// Try to get a cached value by key.
        /// Returns default(T) on cache miss or deserialization failure.
        /// </summary>
        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                var bytes = await _cache.GetAsync(key);
                if (bytes == null || bytes.Length == 0)
                {
                    _logger.LogDebug("Cache MISS for key={Key}", key);
                    return default;
                }

                var value = JsonSerializer.Deserialize<T>(bytes, _jsonOptions);
                _logger.LogDebug("Cache HIT for key={Key}", key);
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache GET failed for key={Key} — treating as miss.", key);
                return default;
            }
        }

        /// <summary>Set a value in Redis with an absolute expiration.</summary>
        public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiry = null) where T : class
        {
            try
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _jsonOptions);

                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = absoluteExpiry ?? TimeSpan.FromMinutes(10)
                };

                await _cache.SetAsync(key, bytes, options);
                _logger.LogDebug("Cache SET key={Key} expiry={Expiry}", key, absoluteExpiry);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache SET failed for key={Key} — continuing without cache.", key);
            }
        }

        /// <summary>Remove a single key from Redis.</summary>
        public async Task RemoveAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
                _logger.LogDebug("Cache REMOVE key={Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache REMOVE failed for key={Key}.", key);
            }
        }

        /// <summary>Remove multiple keys (used for cache invalidation on write operations).</summary>
        public async Task RemoveManyAsync(IEnumerable<string> keys)
        {
            foreach (var key in keys)
                await RemoveAsync(key);
        }
    }
}
