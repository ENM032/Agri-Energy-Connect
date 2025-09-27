using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace WebApplication2.Services
{
    public class CachingService : ICachingService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CachingService> _logger;
        private readonly ConcurrentDictionary<string, bool> _cacheKeys;
        
        public CachingService(IMemoryCache memoryCache, ILogger<CachingService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
            _cacheKeys = new ConcurrentDictionary<string, bool>();
        }
        
        public T? Get<T>(string key)
        {
            try
            {
                if (_memoryCache.TryGetValue(key, out T? value))
                {
                    _logger.LogDebug("Cache hit for key: {Key}", key);
                    return value;
                }
                
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return default(T);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache value for key: {Key}", key);
                return default(T);
            }
        }
        
        public void Set<T>(string key, T value, TimeSpan expiration)
        {
            try
            {
                var options = new MemoryCacheEntryOptions
                {
                    SlidingExpiration = expiration,
                    Priority = CacheItemPriority.Normal
                };
                
                options.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
                {
                    _cacheKeys.TryRemove(evictedKey.ToString()!, out _);
                    _logger.LogDebug("Cache entry evicted: {Key}, Reason: {Reason}", evictedKey, reason);
                });
                
                _memoryCache.Set(key, value, options);
                _cacheKeys.TryAdd(key, true);
                
                _logger.LogDebug("Cache set for key: {Key}, Expiration: {Expiration}", key, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
            }
        }
        
        public void Set<T>(string key, T value, DateTimeOffset absoluteExpiration)
        {
            try
            {
                var options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = absoluteExpiration,
                    Priority = CacheItemPriority.Normal
                };
                
                options.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
                {
                    _cacheKeys.TryRemove(evictedKey.ToString()!, out _);
                    _logger.LogDebug("Cache entry evicted: {Key}, Reason: {Reason}", evictedKey, reason);
                });
                
                _memoryCache.Set(key, value, options);
                _cacheKeys.TryAdd(key, true);
                
                _logger.LogDebug("Cache set for key: {Key}, Absolute Expiration: {AbsoluteExpiration}", key, absoluteExpiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
            }
        }
        
        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration)
        {
            try
            {
                if (_memoryCache.TryGetValue(key, out T? cachedValue))
                {
                    _logger.LogDebug("Cache hit for key: {Key}", key);
                    return cachedValue!;
                }
                
                _logger.LogDebug("Cache miss for key: {Key}, creating new value", key);
                
                var value = await factory();
                Set(key, value, expiration);
                
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOrCreateAsync for key: {Key}", key);
                return await factory(); // Fallback to factory without caching
            }
        }
        
        public void Remove(string key)
        {
            try
            {
                _memoryCache.Remove(key);
                _cacheKeys.TryRemove(key, out _);
                _logger.LogDebug("Cache entry removed: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache entry for key: {Key}", key);
            }
        }
        
        public void RemoveByPrefix(string prefix)
        {
            try
            {
                var keysToRemove = _cacheKeys.Keys
                    .Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                foreach (var key in keysToRemove)
                {
                    Remove(key);
                }
                
                _logger.LogDebug("Removed {Count} cache entries with prefix: {Prefix}", keysToRemove.Count, prefix);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache entries by prefix: {Prefix}", prefix);
            }
        }
        
        public bool Exists(string key)
        {
            try
            {
                return _memoryCache.TryGetValue(key, out _);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
                return false;
            }
        }
    }
}