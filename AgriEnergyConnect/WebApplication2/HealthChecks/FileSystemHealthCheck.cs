using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.IO;

namespace WebApplication2.HealthChecks
{
    public class FileSystemHealthCheck : IHealthCheck
    {
        private readonly string _uploadPath;
        private readonly ILogger<FileSystemHealthCheck> _logger;

        public FileSystemHealthCheck(IConfiguration configuration, ILogger<FileSystemHealthCheck> logger)
        {
            _uploadPath = configuration["FileUpload:UploadPath"] ?? "wwwroot/uploads";
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var fullPath = Path.GetFullPath(_uploadPath);
                
                // Check if directory exists
                if (!Directory.Exists(fullPath))
                {
                    _logger.LogWarning("Upload directory does not exist: {Path}", fullPath);
                    return HealthCheckResult.Degraded($"Upload directory does not exist: {fullPath}");
                }

                // Check if we can write to the directory
                var testFile = Path.Combine(fullPath, $"healthcheck_{Guid.NewGuid()}.tmp");
                await File.WriteAllTextAsync(testFile, "health check test", cancellationToken);
                
                // Check if we can read from the directory
                var content = await File.ReadAllTextAsync(testFile, cancellationToken);
                
                // Clean up test file
                File.Delete(testFile);
                
                if (content != "health check test")
                {
                    _logger.LogError("File system read/write test failed");
                    return HealthCheckResult.Unhealthy("File system read/write test failed");
                }

                // Check available disk space
                var driveInfo = new DriveInfo(Path.GetPathRoot(fullPath) ?? "C:\\");
                var availableSpaceGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                
                var data = new Dictionary<string, object>
                {
                    ["uploadPath"] = fullPath,
                    ["availableSpaceGB"] = Math.Round(availableSpaceGB, 2),
                    ["totalSpaceGB"] = Math.Round(driveInfo.TotalSize / (1024.0 * 1024.0 * 1024.0), 2)
                };

                if (availableSpaceGB < 1.0) // Less than 1GB available
                {
                    _logger.LogWarning("Low disk space: {AvailableSpace}GB remaining", availableSpaceGB);
                    return HealthCheckResult.Degraded("Low disk space", data: data);
                }

                _logger.LogDebug("File system health check passed. Available space: {AvailableSpace}GB", availableSpaceGB);
                return HealthCheckResult.Healthy("File system is accessible", data);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied to upload directory: {Path}", _uploadPath);
                return HealthCheckResult.Unhealthy($"Access denied to upload directory: {_uploadPath}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File system health check failed");
                return HealthCheckResult.Unhealthy("File system health check failed", ex);
            }
        }
    }
}