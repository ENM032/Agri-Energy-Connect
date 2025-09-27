using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using WebApplication2.Data;
using WebApplication2.Models.ViewModels;
using WebApplication2.Services;

namespace WebApplication2.Controllers
{
    /// <summary>
    /// Controller for admin monitoring dashboard - displays logs, health status, and performance metrics
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class MonitoringController : Controller
    {
        private readonly WebApplication2Context _context;
        private readonly ILogger<MonitoringController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IPerformanceLoggingService _performanceLoggingService;

        public MonitoringController(
            WebApplication2Context context,
            ILogger<MonitoringController> logger,
            IWebHostEnvironment environment,
            IPerformanceLoggingService performanceLoggingService)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
            _performanceLoggingService = performanceLoggingService;
        }

        /// <summary>
        /// Display the main monitoring dashboard
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Admin accessing monitoring dashboard");

                var viewModel = new MonitoringDashboardViewModel
                {
                    RecentLogs = await GetRecentLogsAsync(),
                    HealthChecks = await GetHealthStatusAsync(),
                    PerformanceMetrics = await GetPerformanceMetricsAsync(),
                    SystemInfo = GetSystemInfo()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading monitoring dashboard");
                TempData["ErrorMessage"] = "An error occurred while loading the monitoring dashboard.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Get recent log entries from the log file
        /// </summary>
        private async Task<List<LogEntryViewModel>> GetRecentLogsAsync()
        {
            var logs = new List<LogEntryViewModel>();
            
            try
            {
                var logPath = Path.Combine(_environment.ContentRootPath, "logs", "app-.txt");
                var logFiles = Directory.GetFiles(Path.GetDirectoryName(logPath) ?? "logs", "app-*.txt")
                    .OrderByDescending(f => System.IO.File.GetLastWriteTime(f))
                    .Take(3); // Get last 3 log files

                foreach (var logFile in logFiles)
                {
                    if (System.IO.File.Exists(logFile))
                    {
                        var lines = await System.IO.File.ReadAllLinesAsync(logFile);
                        var recentLines = lines.TakeLast(50).Reverse(); // Get last 50 lines

                        foreach (var line in recentLines)
                        {
                            if (!string.IsNullOrWhiteSpace(line) && line.Contains("["))
                            {
                                var logEntry = ParseLogLine(line);
                                if (logEntry != null)
                                {
                                    logs.Add(logEntry);
                                }
                            }
                        }
                    }
                }

                return logs.OrderByDescending(l => l.Timestamp).Take(100).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading log files");
                logs.Add(new LogEntryViewModel
                {
                    Timestamp = DateTime.Now,
                    Level = "Error",
                    Message = "Unable to read log files",
                    Logger = "MonitoringController"
                });
            }

            return logs;
        }

        /// <summary>
        /// Parse a log line into a LogEntryViewModel
        /// </summary>
        private LogEntryViewModel? ParseLogLine(string line)
        {
            try
            {
                // Basic parsing for Serilog format: [Timestamp] [Level] Message
                var parts = line.Split(new[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    var timestampStr = parts[0].Trim();
                    var level = parts[1].Trim();
                    var message = string.Join(" ", parts.Skip(2)).Trim();

                    if (DateTime.TryParse(timestampStr, out var timestamp))
                    {
                        return new LogEntryViewModel
                        {
                            Timestamp = timestamp,
                            Level = level,
                            Message = message,
                            Logger = ExtractLogger(message)
                        };
                    }
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            return null;
        }

        /// <summary>
        /// Extract logger name from log message
        /// </summary>
        private string ExtractLogger(string message)
        {
            try
            {
                // Look for logger name patterns in the message
                if (message.Contains("Controller"))
                {
                    var parts = message.Split(' ');
                    var controllerPart = parts.FirstOrDefault(p => p.Contains("Controller"));
                    if (controllerPart != null)
                    {
                        return controllerPart;
                    }
                }
            }
            catch
            {
                // Ignore extraction errors
            }

            return "Application";
        }

        /// <summary>
        /// Get health check status
        /// </summary>
        private async Task<List<HealthCheckViewModel>> GetHealthStatusAsync()
        {
            var healthChecks = new List<HealthCheckViewModel>();

            try
            {
                // Database health check
                var dbHealthy = await CheckDatabaseHealthAsync();
                healthChecks.Add(new HealthCheckViewModel
                {
                    Name = "Database",
                    Status = dbHealthy ? "Healthy" : "Unhealthy",
                    Description = dbHealthy ? "Database connection is working" : "Database connection failed",
                    LastChecked = DateTime.Now
                });

                // File system health check
                var fsHealthy = CheckFileSystemHealth();
                healthChecks.Add(new HealthCheckViewModel
                {
                    Name = "File System",
                    Status = fsHealthy ? "Healthy" : "Unhealthy",
                    Description = fsHealthy ? "File system is accessible" : "File system access issues",
                    LastChecked = DateTime.Now
                });

                // Memory health check
                var memoryHealthy = CheckMemoryHealth();
                healthChecks.Add(new HealthCheckViewModel
                {
                    Name = "Memory",
                    Status = memoryHealthy ? "Healthy" : "Warning",
                    Description = memoryHealthy ? "Memory usage is normal" : "High memory usage detected",
                    LastChecked = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing health checks");
                healthChecks.Add(new HealthCheckViewModel
                {
                    Name = "Health Check System",
                    Status = "Error",
                    Description = "Unable to perform health checks",
                    LastChecked = DateTime.Now
                });
            }

            return healthChecks;
        }

        /// <summary>
        /// Check database connectivity
        /// </summary>
        private async Task<bool> CheckDatabaseHealthAsync()
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync("SELECT 1");
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check file system health
        /// </summary>
        private bool CheckFileSystemHealth()
        {
            try
            {
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                var testFile = Path.Combine(uploadsPath, "health_check.tmp");
                System.IO.File.WriteAllText(testFile, "health check");
                System.IO.File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check memory usage
        /// </summary>
        private bool CheckMemoryHealth()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var memoryUsageMB = process.WorkingSet64 / 1024 / 1024;
                return memoryUsageMB < 500; // Consider healthy if under 500MB
            }
            catch
            {
                return true; // Assume healthy if can't check
            }
        }

        /// <summary>
        /// Get performance metrics
        /// </summary>
        private async Task<PerformanceMetricsViewModel> GetPerformanceMetricsAsync()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var memoryUsageMB = process.WorkingSet64 / 1024 / 1024;
                var cpuTime = process.TotalProcessorTime;
                var uptime = DateTime.Now - process.StartTime;

                // Get current performance metrics from the performance logging service
                var currentMetrics = await _performanceLoggingService.GetCurrentMetricsAsync();

                return new PerformanceMetricsViewModel
                {
                    MemoryUsageMB = memoryUsageMB,
                    CpuTimeSeconds = cpuTime.TotalSeconds,
                    UptimeHours = uptime.TotalHours,
                    ThreadCount = process.Threads.Count,
                    LastUpdated = DateTime.Now,
                    // Add metrics from performance logging service
                    AverageResponseTime = currentMetrics?.AverageResponseTimeMs ?? 0,
                    TotalRequests = currentMetrics?.TotalRequests ?? 0,
                    ErrorRate = currentMetrics?.ErrorRate ?? 0,
                    ActiveConnections = currentMetrics?.ActiveConnections ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance metrics");
                return new PerformanceMetricsViewModel
                {
                    MemoryUsageMB = 0,
                    CpuTimeSeconds = 0,
                    UptimeHours = 0,
                    ThreadCount = 0,
                    LastUpdated = DateTime.Now,
                    AverageResponseTime = 0,
                    TotalRequests = 0,
                    ErrorRate = 0,
                    ActiveConnections = 0
                };
            }
        }

        /// <summary>
        /// Get system information
        /// </summary>
        private SystemInfoViewModel GetSystemInfo()
        {
            try
            {
                return new SystemInfoViewModel
                {
                    Environment = _environment.EnvironmentName,
                    MachineName = Environment.MachineName,
                    OSVersion = Environment.OSVersion.ToString(),
                    ProcessorCount = Environment.ProcessorCount,
                    DotNetVersion = Environment.Version.ToString(),
                    ApplicationName = "AgriEnergyConnect",
                    StartTime = Process.GetCurrentProcess().StartTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system information");
                return new SystemInfoViewModel
                {
                    Environment = "Unknown",
                    MachineName = "Unknown",
                    OSVersion = "Unknown",
                    ProcessorCount = 0,
                    DotNetVersion = "Unknown",
                    ApplicationName = "AgriEnergyConnect",
                    StartTime = DateTime.Now
                };
            }
        }

        /// <summary>
        /// API endpoint to get real-time health status
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> HealthStatus()
        {
            try
            {
                var healthChecks = await GetHealthStatusAsync();
                return Json(new { success = true, data = healthChecks });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting health status via API");
                return Json(new { success = false, message = "Error retrieving health status" });
            }
        }

        /// <summary>
        /// API endpoint to get real-time performance metrics
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> PerformanceMetrics()
        {
            try
            {
                var metrics = await GetPerformanceMetricsAsync();
                return Json(new { success = true, data = metrics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance metrics via API");
                return Json(new { success = false, message = "Error retrieving performance metrics" });
            }
        }
    }
}