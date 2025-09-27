using WebApplication2.Services;

namespace WebApplication2.Services
{
    public class SystemMetricsLoggingService : BackgroundService
    {
        private readonly ILogger<SystemMetricsLoggingService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _interval;
        
        public SystemMetricsLoggingService(
            ILogger<SystemMetricsLoggingService> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            
            // Get interval from configuration, default to 5 minutes
            var intervalMinutes = configuration.GetValue<int>("SystemMetrics:IntervalMinutes", 5);
            _interval = TimeSpan.FromMinutes(intervalMinutes);
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("System Metrics Logging Service started. Interval: {Interval}", _interval);
            
            // Wait a bit before starting to allow the application to fully initialize
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var performanceService = scope.ServiceProvider.GetRequiredService<IPerformanceLoggingService>();
                    
                    // Log system metrics
                    performanceService.LogSystemMetrics();
                    
                    // Log memory usage details
                    performanceService.LogMemoryUsage();
                    
                    // Get and log detailed metrics
                    var metrics = await performanceService.GetCurrentMetricsAsync();
                    
                    _logger.LogInformation(
                        "Periodic System Health Check - " +
                        "Memory: {MemoryMB}MB, " +
                        "CPU: {CpuPercent}%, " +
                        "Threads: {ThreadCount}, " +
                        "Uptime: {Uptime}, " +
                        "Working Set: {WorkingSetMB}MB, " +
                        "Private Memory: {PrivateMemoryMB}MB",
                        metrics.MemoryUsedMB,
                        metrics.CpuUsagePercent,
                        metrics.ThreadCount,
                        metrics.Uptime.ToString(@"dd\.hh\:mm\:ss"),
                        metrics.WorkingSetMB,
                        metrics.PrivateMemoryMB);
                    
                    // Check for memory leaks (increasing memory usage over time)
                    await CheckForMemoryLeaks(metrics);
                    
                    // Force garbage collection if memory usage is high
                    if (metrics.MemoryUsedMB > 500)
                    {
                        _logger.LogWarning("High memory usage detected ({MemoryMB}MB), forcing garbage collection", metrics.MemoryUsedMB);
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while logging system metrics");
                }
                
                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
            }
            
            _logger.LogInformation("System Metrics Logging Service stopped");
        }
        
        private readonly Queue<long> _memoryHistory = new();
        private const int MaxHistorySize = 12; // Keep last 12 readings (1 hour if 5-minute intervals)
        
        private async Task CheckForMemoryLeaks(PerformanceMetrics metrics)
        {
            _memoryHistory.Enqueue(metrics.MemoryUsedMB);
            
            // Keep only the last MaxHistorySize readings
            while (_memoryHistory.Count > MaxHistorySize)
            {
                _memoryHistory.Dequeue();
            }
            
            // Check for memory leak pattern (consistently increasing memory)
            if (_memoryHistory.Count >= MaxHistorySize)
            {
                var memoryArray = _memoryHistory.ToArray();
                var increasingCount = 0;
                
                for (int i = 1; i < memoryArray.Length; i++)
                {
                    if (memoryArray[i] > memoryArray[i - 1])
                    {
                        increasingCount++;
                    }
                }
                
                // If memory has been increasing for more than 75% of the readings
                var increasingPercentage = (double)increasingCount / (memoryArray.Length - 1);
                if (increasingPercentage > 0.75)
                {
                    var memoryIncrease = memoryArray[^1] - memoryArray[0];
                    _logger.LogWarning(
                        "Potential memory leak detected. Memory increased by {MemoryIncreaseMB}MB over {TimeSpan}. " +
                        "Increasing trend: {IncreasingPercentage:P}",
                        memoryIncrease,
                        TimeSpan.FromMinutes(_interval.TotalMinutes * (MaxHistorySize - 1)).ToString(@"hh\:mm"),
                        increasingPercentage);
                }
            }
            
            await Task.CompletedTask;
        }
        
        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("System Metrics Logging Service is stopping");
            await base.StopAsync(stoppingToken);
        }
    }
}