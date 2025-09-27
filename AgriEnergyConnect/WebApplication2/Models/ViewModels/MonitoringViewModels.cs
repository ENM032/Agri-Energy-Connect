using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models.ViewModels
{
    /// <summary>
    /// View model for the main monitoring dashboard
    /// </summary>
    public class MonitoringDashboardViewModel
    {
        public List<LogEntryViewModel> RecentLogs { get; set; } = new();
        public List<HealthCheckViewModel> HealthChecks { get; set; } = new();
        public PerformanceMetricsViewModel PerformanceMetrics { get; set; } = new();
        public SystemInfoViewModel SystemInfo { get; set; } = new();
    }

    /// <summary>
    /// View model for log entries
    /// </summary>
    public class LogEntryViewModel
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Logger { get; set; } = string.Empty;
        
        public string LevelCssClass => Level.ToLower() switch
        {
            "error" => "text-danger",
            "warning" => "text-warning",
            "information" => "text-info",
            "debug" => "text-muted",
            _ => "text-secondary"
        };
        
        public string LevelIcon => Level.ToLower() switch
        {
            "error" => "fas fa-exclamation-circle",
            "warning" => "fas fa-exclamation-triangle",
            "information" => "fas fa-info-circle",
            "debug" => "fas fa-bug",
            _ => "fas fa-circle"
        };
    }

    /// <summary>
    /// View model for health check status
    /// </summary>
    public class HealthCheckViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime LastChecked { get; set; }
        
        public string StatusCssClass => Status.ToLower() switch
        {
            "healthy" => "text-success",
            "warning" => "text-warning",
            "unhealthy" => "text-danger",
            "error" => "text-danger",
            _ => "text-secondary"
        };
        
        public string StatusIcon => Status.ToLower() switch
        {
            "healthy" => "fas fa-check-circle",
            "warning" => "fas fa-exclamation-triangle",
            "unhealthy" => "fas fa-times-circle",
            "error" => "fas fa-times-circle",
            _ => "fas fa-question-circle"
        };
        
        public string StatusBadgeClass => Status.ToLower() switch
        {
            "healthy" => "badge-success",
            "warning" => "badge-warning",
            "unhealthy" => "badge-danger",
            "error" => "badge-danger",
            _ => "badge-secondary"
        };
    }

    /// <summary>
    /// View model for performance metrics
    /// </summary>
    public class PerformanceMetricsViewModel
    {
        [Display(Name = "Memory Usage (MB)")]
        public long MemoryUsageMB { get; set; }
        
        [Display(Name = "CPU Time (Seconds)")]
        public double CpuTimeSeconds { get; set; }
        
        [Display(Name = "Uptime (Hours)")]
        public double UptimeHours { get; set; }
        
        [Display(Name = "Thread Count")]
        public int ThreadCount { get; set; }
        
        [Display(Name = "Average Response Time (ms)")]
        public double AverageResponseTime { get; set; }
        
        [Display(Name = "Total Requests")]
        public long TotalRequests { get; set; }
        
        [Display(Name = "Error Rate (%)")]
        public double ErrorRate { get; set; }
        
        [Display(Name = "Active Connections")]
        public int ActiveConnections { get; set; }
        
        public DateTime LastUpdated { get; set; }
        
        public string MemoryUsageFormatted => $"{MemoryUsageMB:N0} MB";
        public string CpuTimeFormatted => $"{CpuTimeSeconds:N1} seconds";
        public string UptimeFormatted => $"{UptimeHours:N1} hours";
        public string AverageResponseTimeFormatted => $"{AverageResponseTime:N1} ms";
        public string ErrorRateFormatted => $"{ErrorRate:N2}%";
        
        public string MemoryUsageClass => MemoryUsageMB switch
        {
            > 1000 => "text-danger",
            > 500 => "text-warning",
            _ => "text-success"
        };
        
        public string ErrorRateClass => ErrorRate switch
        {
            > 5.0 => "text-danger",
            > 1.0 => "text-warning",
            _ => "text-success"
        };
    }

    /// <summary>
    /// View model for system information
    /// </summary>
    public class SystemInfoViewModel
    {
        [Display(Name = "Environment")]
        public string Environment { get; set; } = string.Empty;
        
        [Display(Name = "Machine Name")]
        public string MachineName { get; set; } = string.Empty;
        
        [Display(Name = "OS Version")]
        public string OSVersion { get; set; } = string.Empty;
        
        [Display(Name = "Processor Count")]
        public int ProcessorCount { get; set; }
        
        [Display(Name = ".NET Version")]
        public string DotNetVersion { get; set; } = string.Empty;
        
        [Display(Name = "Application Name")]
        public string ApplicationName { get; set; } = string.Empty;
        
        [Display(Name = "Start Time")]
        public DateTime StartTime { get; set; }
        
        public string EnvironmentBadgeClass => Environment.ToLower() switch
        {
            "production" => "badge-danger",
            "staging" => "badge-warning",
            "development" => "badge-success",
            _ => "badge-secondary"
        };
    }

    /// <summary>
    /// View model for log filtering and pagination
    /// </summary>
    public class LogFilterViewModel
    {
        public string? Level { get; set; }
        public string? Logger { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    /// <summary>
    /// View model for real-time monitoring updates
    /// </summary>
    public class MonitoringUpdateViewModel
    {
        public List<HealthCheckViewModel> HealthChecks { get; set; } = new();
        public PerformanceMetricsViewModel PerformanceMetrics { get; set; } = new();
        public List<LogEntryViewModel> RecentLogs { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}