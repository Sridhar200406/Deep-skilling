namespace Caching
{
    /// <summary>Contract for the Redis distributed cache service.</summary>
    public interface IRedisCacheService
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiry = null) where T : class;
        Task RemoveAsync(string key);
        Task RemoveManyAsync(IEnumerable<string> keys);
    }
}
