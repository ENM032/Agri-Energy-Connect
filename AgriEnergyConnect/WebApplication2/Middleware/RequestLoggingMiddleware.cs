using Serilog;
using System.Diagnostics;
using System.Text;

namespace WebApplication2.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString();
            
            // Add request ID to context for tracking
            context.Items["RequestId"] = requestId;
            
            // Log request details
            await LogRequestAsync(context, requestId);
            
            var originalBodyStream = context.Response.Body;
            
            try
            {
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;
                
                await _next(context);
                
                stopwatch.Stop();
                
                // Log response details
                await LogResponseAsync(context, requestId, stopwatch.ElapsedMilliseconds);
                
                await responseBody.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                // Log unhandled exceptions
                _logger.LogError(ex, "Unhandled exception occurred for request {RequestId} to {RequestPath}. Duration: {Duration}ms",
                    requestId, context.Request.Path, stopwatch.ElapsedMilliseconds);
                
                context.Response.Body = originalBodyStream;
                throw;
            }
        }

        private async Task LogRequestAsync(HttpContext context, string requestId)
        {
            var request = context.Request;
            var userName = context.User?.Identity?.Name ?? "Anonymous";
            var userAgent = request.Headers["User-Agent"].ToString();
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            
            _logger.LogInformation("Request {RequestId}: {Method} {Path} from {UserName} ({IPAddress}) - {UserAgent}",
                requestId, request.Method, request.Path, userName, ipAddress, userAgent);
            
            // Log request body for POST/PUT requests (be careful with sensitive data)
            if ((request.Method == "POST" || request.Method == "PUT") && 
                request.ContentLength > 0 && 
                request.ContentLength < 10000) // Limit body logging to reasonable size
            {
                request.EnableBuffering();
                var buffer = new byte[Convert.ToInt32(request.ContentLength)];
                await request.Body.ReadAsync(buffer, 0, buffer.Length);
                var bodyAsText = Encoding.UTF8.GetString(buffer);
                request.Body.Position = 0;
                
                // Don't log sensitive data like passwords
                if (!bodyAsText.Contains("password", StringComparison.OrdinalIgnoreCase) &&
                    !bodyAsText.Contains("token", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Request {RequestId} Body: {RequestBody}", requestId, bodyAsText);
                }
            }
        }

        private async Task LogResponseAsync(HttpContext context, string requestId, long durationMs)
        {
            var response = context.Response;
            
            var logLevel = response.StatusCode >= 500 ? LogLevel.Error :
                          response.StatusCode >= 400 ? LogLevel.Warning :
                          LogLevel.Information;
            
            _logger.Log(logLevel, "Response {RequestId}: {StatusCode} in {Duration}ms - Content-Type: {ContentType}",
                requestId, response.StatusCode, durationMs, response.ContentType);
            
            // Log slow requests
            if (durationMs > 5000) // 5 seconds
            {
                _logger.LogWarning("Slow request detected {RequestId}: {Method} {Path} took {Duration}ms",
                    requestId, context.Request.Method, context.Request.Path, durationMs);
            }
        }
    }

    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}