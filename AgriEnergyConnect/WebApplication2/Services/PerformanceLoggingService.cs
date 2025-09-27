using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace WebApplication2.Services
{
    public class PerformanceLoggingService : IPerformanceLoggingService
    {
        private readonly ILogger<PerformanceLoggingService> _logger;
        private readonly Process _currentProcess;
        private readonly DateTime _startTime;
        private PerformanceCounter? _cpuCounter;
        
        public PerformanceLoggingService(ILogger<PerformanceLoggingService> logger)
        {
            _logger = logger;
            _currentProcess = Process.GetCurrentProcess();
            _startTime = DateTime.UtcNow;
            
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                    _cpuCounter.NextValue(); // First call returns 0
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize CPU performance counter");
            }
        }
        
        public IPerformanceTracker StartTracking(string operationName, object? context = null)
        {
            return new PerformanceTracker(operationName, _logger, context);
        }
        
        public void LogOperation(string operationName, TimeSpan duration, bool success = true, object? context = null)
        {
            var logLevel = GetLogLevel(duration, success);
            
            _logger.Log(logLevel,
                "Performance: Operation {OperationName} completed in {Duration}ms. Success: {Success}. Context: {Context}",
                operationName,
                duration.TotalMilliseconds,
                success,
                context ?? "None");
            
            // Log warning for slow operations
            if (duration.TotalMilliseconds > 5000)
            {
                _logger.LogWarning(
                    "Slow operation detected: {OperationName} took {Duration}ms",
                    operationName,
                    duration.TotalMilliseconds);
            }
        }
        
        public void LogSystemMetrics()
        {
            try
            {
                var metrics = GetCurrentMetricsAsync().Result;
                
                _logger.LogInformation(
                    "System Metrics - Memory: {MemoryMB}MB, CPU: {CpuPercent}%, Threads: {ThreadCount}, " +
                    "Uptime: {Uptime}, GC Gen0: {GcGen0}, GC Gen1: {GcGen1}, GC Gen2: {GcGen2}",
                    metrics.MemoryUsedMB,
                    metrics.CpuUsagePercent,
                    metrics.ThreadCount,
                    metrics.Uptime.ToString(@"dd\.hh\:mm\:ss"),
                    metrics.GcGen0Collections,
                    metrics.GcGen1Collections,
                    metrics.GcGen2Collections);
                
                // Log warnings for high resource usage
                if (metrics.MemoryUsedMB > 1000)
                {
                    _logger.LogWarning("High memory usage detected: {MemoryMB}MB", metrics.MemoryUsedMB);
                }
                
                if (metrics.CpuUsagePercent > 80)
                {
                    _logger.LogWarning("High CPU usage detected: {CpuPercent}%", metrics.CpuUsagePercent);
                }
                
                if (metrics.ThreadCount > 100)
                {
                    _logger.LogWarning("High thread count detected: {ThreadCount}", metrics.ThreadCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log system metrics");
            }
        }
        
        public void LogDatabaseQuery(string query, TimeSpan duration, int? recordCount = null)
        {
            var logLevel = duration.TotalMilliseconds > 1000 ? LogLevel.Warning : LogLevel.Information;
            
            _logger.Log(logLevel,
                "Database Query Performance: {Query} executed in {Duration}ms. Records: {RecordCount}",
                query.Length > 100 ? query.Substring(0, 100) + "..." : query,
                duration.TotalMilliseconds,
                recordCount?.ToString() ?? "Unknown");
        }
        
        public void LogMemoryUsage()
        {
            try
            {
                var workingSet = _currentProcess.WorkingSet64 / 1024 / 1024;
                var privateMemory = _currentProcess.PrivateMemorySize64 / 1024 / 1024;
                var managedMemory = GC.GetTotalMemory(false) / 1024 / 1024;
                
                _logger.LogInformation(
                    "Memory Usage - Working Set: {WorkingSetMB}MB, Private: {PrivateMemoryMB}MB, Managed: {ManagedMemoryMB}MB",
                    workingSet,
                    privateMemory,
                    managedMemory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log memory usage");
            }
        }
        
        public async Task<PerformanceMetrics> GetCurrentMetricsAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    _currentProcess.Refresh();
                    
                    var metrics = new PerformanceMetrics
                    {
                        MemoryUsedMB = GC.GetTotalMemory(false) / 1024 / 1024,
                        ThreadCount = _currentProcess.Threads.Count,
                        Uptime = DateTime.UtcNow - _startTime,
                        GcGen0Collections = GC.CollectionCount(0),
                        GcGen1Collections = GC.CollectionCount(1),
                        GcGen2Collections = GC.CollectionCount(2),
                        WorkingSetMB = _currentProcess.WorkingSet64 / 1024 / 1024,
                        PrivateMemoryMB = _currentProcess.PrivateMemorySize64 / 1024 / 1024
                    };
                    
                    // Get CPU usage
                    if (_cpuCounter != null)
                    {
                        try
                        {
                            metrics.CpuUsagePercent = Math.Round(_cpuCounter.NextValue(), 2);
                        }
                        catch
                        {
                            metrics.CpuUsagePercent = 0;
                        }
                    }
                    
                    return metrics;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get current performance metrics");
                    return new PerformanceMetrics();
                }
            });
        }
        
        private LogLevel GetLogLevel(TimeSpan duration, bool success)
        {
            if (!success)
                return LogLevel.Error;
            if (duration.TotalMilliseconds > 3000)
                return LogLevel.Warning;
            return LogLevel.Information;
        }
        
        public void Dispose()
        {
            _cpuCounter?.Dispose();
            _currentProcess?.Dispose();
        }
    }
    
    public class PerformanceTracker : IPerformanceTracker
    {
        private readonly Stopwatch _stopwatch;
        private readonly ILogger _logger;
        private readonly Dictionary<string, object> _context;
        private bool _failed;
        private Exception? _exception;
        private bool _disposed;
        
        public string OperationName { get; }
        public TimeSpan ElapsedTime => _stopwatch.Elapsed;
        
        public PerformanceTracker(string operationName, ILogger logger, object? initialContext = null)
        {
            OperationName = operationName;
            _logger = logger;
            _context = new Dictionary<string, object>();
            _stopwatch = Stopwatch.StartNew();
            
            if (initialContext != null)
            {
                _context["InitialContext"] = initialContext;
            }
            
            _logger.LogDebug("Started tracking performance for operation: {OperationName}", operationName);
        }
        
        public void AddContext(string key, object value)
        {
            _context[key] = value;
        }
        
        public void MarkAsFailed(Exception? exception = null)
        {
            _failed = true;
            _exception = exception;
        }
        
        public void Complete()
        {
            if (_disposed)
                return;
                
            _stopwatch.Stop();
            
            var logLevel = GetLogLevel();
            
            _logger.Log(logLevel,
                "Performance: Operation {OperationName} completed in {Duration}ms. Success: {Success}. Context: {Context}",
                OperationName,
                _stopwatch.ElapsedMilliseconds,
                !_failed,
                _context.Count > 0 ? _context : null);
        }
        
        public void Dispose()
        {
            if (_disposed)
                return;
                
            _disposed = true;
            _stopwatch.Stop();
            
            var logLevel = GetLogLevel();
            
            if (_failed && _exception != null)
            {
                _logger.Log(logLevel,
                    _exception,
                    "Performance: Operation {OperationName} failed after {Duration}ms. Context: {Context}",
                    OperationName,
                    _stopwatch.ElapsedMilliseconds,
                    _context.Count > 0 ? _context : null);
            }
            else
            {
                _logger.Log(logLevel,
                    "Performance: Operation {OperationName} completed in {Duration}ms. Success: {Success}. Context: {Context}",
                    OperationName,
                    _stopwatch.ElapsedMilliseconds,
                    !_failed,
                    _context.Count > 0 ? _context : null);
            }
            
            // Log warning for slow operations
            if (_stopwatch.ElapsedMilliseconds > 5000)
            {
                _logger.LogWarning(
                    "Slow operation detected: {OperationName} took {Duration}ms",
                    OperationName,
                    _stopwatch.ElapsedMilliseconds);
            }
        }
        
        private LogLevel GetLogLevel()
        {
            if (_failed)
                return LogLevel.Error;
            if (_stopwatch.ElapsedMilliseconds > 3000)
                return LogLevel.Warning;
            return LogLevel.Information;
        }
    }
}