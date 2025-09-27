using System.Diagnostics;

namespace WebApplication2.Services
{
    public interface IPerformanceLoggingService
    {
        /// <summary>
        /// Starts tracking performance for an operation
        /// </summary>
        /// <param name="operationName">Name of the operation being tracked</param>
        /// <param name="context">Additional context information</param>
        /// <returns>Performance tracker instance</returns>
        IPerformanceTracker StartTracking(string operationName, object? context = null);
        
        /// <summary>
        /// Logs a completed operation with its duration
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="duration">Duration of the operation</param>
        /// <param name="success">Whether the operation was successful</param>
        /// <param name="context">Additional context information</param>
        void LogOperation(string operationName, TimeSpan duration, bool success = true, object? context = null);
        
        /// <summary>
        /// Logs system performance metrics
        /// </summary>
        void LogSystemMetrics();
        
        /// <summary>
        /// Logs database query performance
        /// </summary>
        /// <param name="query">SQL query or operation name</param>
        /// <param name="duration">Query execution time</param>
        /// <param name="recordCount">Number of records affected/returned</param>
        void LogDatabaseQuery(string query, TimeSpan duration, int? recordCount = null);
        
        /// <summary>
        /// Logs memory usage information
        /// </summary>
        void LogMemoryUsage();
        
        /// <summary>
        /// Gets current performance metrics
        /// </summary>
        /// <returns>Current system performance metrics</returns>
        Task<PerformanceMetrics> GetCurrentMetricsAsync();
    }
    
    public interface IPerformanceTracker : IDisposable
    {
        /// <summary>
        /// Adds additional context to the performance tracking
        /// </summary>
        /// <param name="key">Context key</param>
        /// <param name="value">Context value</param>
        void AddContext(string key, object value);
        
        /// <summary>
        /// Marks the operation as failed
        /// </summary>
        /// <param name="exception">Optional exception that caused the failure</param>
        void MarkAsFailed(Exception? exception = null);
        
        /// <summary>
        /// Gets the current elapsed time
        /// </summary>
        TimeSpan ElapsedTime { get; }
        
        /// <summary>
        /// Gets the operation name
        /// </summary>
        string OperationName { get; }
        
        /// <summary>
        /// Completes the performance tracking operation
        /// </summary>
        void Complete();
    }
    
    public class PerformanceMetrics
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public long MemoryUsedMB { get; set; }
        public double CpuUsagePercent { get; set; }
        public int ThreadCount { get; set; }
        public TimeSpan Uptime { get; set; }
        public long GcGen0Collections { get; set; }
        public long GcGen1Collections { get; set; }
        public long GcGen2Collections { get; set; }
        public long WorkingSetMB { get; set; }
        public long PrivateMemoryMB { get; set; }
        public int ActiveConnections { get; set; }
        public double AverageResponseTimeMs { get; set; }
        public long TotalRequests { get; set; }
        public double ErrorRate { get; set; }
        public Dictionary<string, object> AdditionalMetrics { get; set; } = new Dictionary<string, object>();
    }
}