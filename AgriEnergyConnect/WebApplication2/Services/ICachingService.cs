using Microsoft.Extensions.Caching.Memory;

namespace WebApplication2.Services
{
    public interface ICachingService
    {
        /// <summary>
        /// Gets a cached item by key
        /// </summary>
        /// <typeparam name="T">Type of cached item</typeparam>
        /// <param name="key">Cache key</param>
        /// <returns>Cached item or default value</returns>
        T? Get<T>(string key);
        
        /// <summary>
        /// Sets a cached item with expiration
        /// </summary>
        /// <typeparam name="T">Type of item to cache</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="value">Value to cache</param>
        /// <param name="expiration">Cache expiration time</param>
        void Set<T>(string key, T value, TimeSpan expiration);
        
        /// <summary>
        /// Sets a cached item with absolute expiration
        /// </summary>
        /// <typeparam name="T">Type of item to cache</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="value">Value to cache</param>
        /// <param name="absoluteExpiration">Absolute expiration time</param>
        void Set<T>(string key, T value, DateTimeOffset absoluteExpiration);
        
        /// <summary>
        /// Gets or creates a cached item
        /// </summary>
        /// <typeparam name="T">Type of cached item</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="factory">Factory function to create the item if not cached</param>
        /// <param name="expiration">Cache expiration time</param>
        /// <returns>Cached or newly created item</returns>
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiration);
        
        /// <summary>
        /// Removes a cached item
        /// </summary>
        /// <param name="key">Cache key</param>
        void Remove(string key);
        
        /// <summary>
        /// Removes all cached items with keys starting with the specified prefix
        /// </summary>
        /// <param name="prefix">Key prefix</param>
        void RemoveByPrefix(string prefix);
        
        /// <summary>
        /// Checks if a key exists in cache
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <returns>True if key exists</returns>
        bool Exists(string key);
    }
    
    public static class CacheKeys
    {
        public const string ProductsList = "products_list";
        public const string ProductsListByCategory = "products_list_category_{0}";
        public const string ProductById = "product_{0}";
        public const string CategoriesList = "categories_list";
        public const string UserRoles = "user_roles_{0}";
        public const string DashboardStats = "dashboard_stats";
        public const string RecentProducts = "recent_products_{0}";
        public const string ProductCount = "product_count";
        public const string UserCount = "user_count";
        public const string AnalyticsData = "analytics_data_{0}";
        public const string UserStatistics = "user_statistics";
        public const string ProductAnalytics = "product_analytics_{0}";
        public const string ProductTrends = "product_trends";
        public const string DashboardAnalytics = "dashboard_analytics";
    }
}