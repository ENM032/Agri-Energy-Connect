using Microsoft.Extensions.Options;
using System.IO;
using System.Text.Json;

namespace WebApplication2.Services
{
    /// <summary>
    /// Service for cleaning up old log files based on retention policies
    /// </summary>
    public class LogCleanupService : BackgroundService
    {
        private readonly ILogger<LogCleanupService> _logger;
        private readonly IConfiguration _configuration;
        private readonly LogRetentionOptions _retentionOptions;
        private readonly Timer _cleanupTimer;

        public LogCleanupService(
            ILogger<LogCleanupService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _retentionOptions = new LogRetentionOptions();
            _configuration.GetSection("LogRetentionPolicy").Bind(_retentionOptions);

            // Setup timer for scheduled cleanup
            var cleanupSchedule = _retentionOptions.CleanupSchedule;
            if (cleanupSchedule.Enabled)
            {
                var startTime = DateTime.Today.Add(cleanupSchedule.StartTime);
                if (startTime < DateTime.Now)
                {
                    startTime = startTime.AddDays(1);
                }

                var timeUntilStart = startTime - DateTime.Now;
                var interval = TimeSpan.FromHours(cleanupSchedule.IntervalHours);

                _cleanupTimer = new Timer(_ => 
                {
                    _ = Task.Run(async () => await PerformCleanupAsync());
                }, null, timeUntilStart, interval);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Log cleanup service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Perform initial cleanup
                    await PerformCleanupAsync();

                    // Wait for 24 hours before next cleanup
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Service is stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during log cleanup");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Retry in 1 hour
                }
            }

            _logger.LogInformation("Log cleanup service stopped");
        }

        private async Task PerformCleanupAsync()
        {
            try
            {
                _logger.LogInformation("Starting log cleanup process");

                var logsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                if (!Directory.Exists(logsDirectory))
                {
                    _logger.LogWarning("Logs directory does not exist: {LogsDirectory}", logsDirectory);
                    return;
                }

                // Cleanup general logs
                await CleanupLogDirectory(
                    logsDirectory,
                    "agri-energy-*.log",
                    _retentionOptions.GeneralLogs.RetentionDays,
                    (long)(_retentionOptions.GeneralLogs.MaxTotalSizeGB * 1024 * 1024 * 1024));

                // Cleanup error logs
                var errorsDirectory = Path.Combine(logsDirectory, "errors");
                if (Directory.Exists(errorsDirectory))
                {
                    await CleanupLogDirectory(
                        errorsDirectory,
                        "agri-energy-errors-*.log",
                        _retentionOptions.ErrorLogs.RetentionDays,
                        (long)(_retentionOptions.ErrorLogs.MaxTotalSizeGB * 1024 * 1024 * 1024));
                }

                // Cleanup performance logs
                var performanceDirectory = Path.Combine(logsDirectory, "performance");
                if (Directory.Exists(performanceDirectory))
                {
                    await CleanupLogDirectory(
                        performanceDirectory,
                        "agri-energy-performance-*.log",
                        _retentionOptions.PerformanceLogs.RetentionDays,
                        (long)(_retentionOptions.PerformanceLogs.MaxTotalSizeGB * 1024 * 1024 * 1024));
                }

                _logger.LogInformation("Log cleanup process completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during log cleanup process");
                throw;
            }
        }

        private async Task CleanupLogDirectory(string directory, string pattern, int retentionDays, long maxTotalSize)
        {
            try
            {
                var files = Directory.GetFiles(directory, pattern)
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToList();

                if (!files.Any())
                {
                    return;
                }

                var cutoffDate = DateTime.Now.AddDays(-retentionDays);
                var deletedCount = 0;
                var totalSize = files.Sum(f => f.Length);

                _logger.LogInformation("Cleaning up directory: {Directory}, Files: {FileCount}, Total Size: {TotalSizeMB} MB",
                    directory, files.Count, totalSize / (1024 * 1024));

                // Delete files older than retention period
                var filesToDelete = files.Where(f => f.LastWriteTime < cutoffDate).ToList();
                foreach (var file in filesToDelete)
                {
                    try
                    {
                        file.Delete();
                        deletedCount++;
                        totalSize -= file.Length;
                        _logger.LogDebug("Deleted old log file: {FileName}", file.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete log file: {FileName}", file.FullName);
                    }
                }

                // If total size still exceeds limit, delete oldest files
                if (totalSize > maxTotalSize)
                {
                    var remainingFiles = files.Except(filesToDelete)
                        .OrderBy(f => f.LastWriteTime)
                        .ToList();

                    foreach (var file in remainingFiles)
                    {
                        if (totalSize <= maxTotalSize)
                            break;

                        try
                        {
                            totalSize -= file.Length;
                            file.Delete();
                            deletedCount++;
                            _logger.LogDebug("Deleted log file due to size limit: {FileName}", file.Name);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete log file: {FileName}", file.FullName);
                        }
                    }
                }

                if (deletedCount > 0)
                {
                    _logger.LogInformation("Deleted {DeletedCount} log files from {Directory}", deletedCount, directory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up log directory: {Directory}", directory);
                throw;
            }
        }

        public async Task<LogCleanupStatus> GetCleanupStatusAsync()
        {
            try
            {
                var status = new LogCleanupStatus
                {
                    LastCleanup = DateTime.Now, // This would be stored and retrieved from a persistent store
                    NextCleanup = DateTime.Today.Add(_retentionOptions.CleanupSchedule.StartTime).AddDays(1),
                    IsEnabled = _retentionOptions.CleanupSchedule.Enabled
                };

                var logsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                if (Directory.Exists(logsDirectory))
                {
                    status.LogDirectories = new List<LogDirectoryInfo>();

                    // General logs
                    var generalLogs = GetDirectoryInfo(logsDirectory, "agri-energy-*.log");
                    if (generalLogs != null)
                    {
                        generalLogs.Type = "General";
                        generalLogs.RetentionDays = _retentionOptions.GeneralLogs.RetentionDays;
                        status.LogDirectories.Add(generalLogs);
                    }

                    // Error logs
                    var errorsDirectory = Path.Combine(logsDirectory, "errors");
                    if (Directory.Exists(errorsDirectory))
                    {
                        var errorLogs = GetDirectoryInfo(errorsDirectory, "agri-energy-errors-*.log");
                        if (errorLogs != null)
                        {
                            errorLogs.Type = "Errors";
                            errorLogs.RetentionDays = _retentionOptions.ErrorLogs.RetentionDays;
                            status.LogDirectories.Add(errorLogs);
                        }
                    }

                    // Performance logs
                    var performanceDirectory = Path.Combine(logsDirectory, "performance");
                    if (Directory.Exists(performanceDirectory))
                    {
                        var performanceLogs = GetDirectoryInfo(performanceDirectory, "agri-energy-performance-*.log");
                        if (performanceLogs != null)
                        {
                            performanceLogs.Type = "Performance";
                            performanceLogs.RetentionDays = _retentionOptions.PerformanceLogs.RetentionDays;
                            status.LogDirectories.Add(performanceLogs);
                        }
                    }
                }

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cleanup status");
                throw;
            }
        }

        private LogDirectoryInfo? GetDirectoryInfo(string directory, string pattern)
        {
            try
            {
                var files = Directory.GetFiles(directory, pattern)
                    .Select(f => new FileInfo(f))
                    .ToList();

                if (!files.Any())
                    return null;

                return new LogDirectoryInfo
                {
                    Directory = directory,
                    FileCount = files.Count,
                    TotalSizeMB = files.Sum(f => f.Length) / (1024 * 1024),
                    OldestFile = files.Min(f => f.LastWriteTime),
                    NewestFile = files.Max(f => f.LastWriteTime)
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting directory info for: {Directory}", directory);
                return null;
            }
        }

        public override void Dispose()
        {
            _cleanupTimer?.Dispose();
            base.Dispose();
        }
    }

    /// <summary>
    /// Configuration options for log retention policies
    /// </summary>
    public class LogRetentionOptions
    {
        public LogTypeOptions GeneralLogs { get; set; } = new();
        public LogTypeOptions ErrorLogs { get; set; } = new();
        public LogTypeOptions PerformanceLogs { get; set; } = new();
        public CleanupScheduleOptions CleanupSchedule { get; set; } = new();
    }

    public class LogTypeOptions
    {
        public int RetentionDays { get; set; } = 30;
        public int MaxFileSizeMB { get; set; } = 10;
        public double MaxTotalSizeGB { get; set; } = 1.0;
    }

    public class CleanupScheduleOptions
    {
        public bool Enabled { get; set; } = true;
        public int IntervalHours { get; set; } = 24;
        public TimeSpan StartTime { get; set; } = new TimeSpan(2, 0, 0); // 2:00 AM
    }

    /// <summary>
    /// Status information for log cleanup operations
    /// </summary>
    public class LogCleanupStatus
    {
        public DateTime LastCleanup { get; set; }
        public DateTime NextCleanup { get; set; }
        public bool IsEnabled { get; set; }
        public List<LogDirectoryInfo> LogDirectories { get; set; } = new();
    }

    public class LogDirectoryInfo
    {
        public string Type { get; set; } = string.Empty;
        public string Directory { get; set; } = string.Empty;
        public int FileCount { get; set; }
        public long TotalSizeMB { get; set; }
        public DateTime OldestFile { get; set; }
        public DateTime NewestFile { get; set; }
        public int RetentionDays { get; set; }
    }
}