using System.Diagnostics;
using System.Security.Claims;
using System.Text;

namespace WebApplication2.Middleware
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString();
            
            // Add request ID to context for correlation
            context.Items["RequestId"] = requestId;
            
            // Log incoming request
            await LogRequestAsync(context, requestId);
            
            // Capture original response body stream
            var originalBodyStream = context.Response.Body;
            
            try
            {
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;
                
                // Execute the next middleware
                await _next(context);
                
                stopwatch.Stop();
                
                // Log response
                await LogResponseAsync(context, requestId, stopwatch.ElapsedMilliseconds);
                
                // Copy response back to original stream
                await responseBody.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                // Log unhandled exception
                _logger.LogError(ex, 
                    "Unhandled exception occurred for request {RequestId}. " +
                    "Method: {Method}, Path: {Path}, User: {User}, IP: {ClientIP}, " +
                    "Duration: {Duration}ms",
                    requestId,
                    context.Request.Method,
                    context.Request.Path,
                    GetUserInfo(context),
                    GetClientIpAddress(context),
                    stopwatch.ElapsedMilliseconds);
                
                // Re-throw the exception
                throw;
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private async Task LogRequestAsync(HttpContext context, string requestId)
        {
            var request = context.Request;
            var userInfo = GetUserInfo(context);
            var clientIp = GetClientIpAddress(context);
            
            var requestBody = string.Empty;
            if (request.ContentLength > 0 && request.ContentType?.Contains("application/json") == true)
            {
                request.EnableBuffering();
                var buffer = new byte[Convert.ToInt32(request.ContentLength)];
                await request.Body.ReadAsync(buffer, 0, buffer.Length);
                requestBody = Encoding.UTF8.GetString(buffer);
                request.Body.Position = 0;
            }
            
            _logger.LogInformation(
                "Incoming request {RequestId}: {Method} {Path} from {User} at {ClientIP}. " +
                "Headers: {Headers}. Body: {Body}",
                requestId,
                request.Method,
                request.Path + request.QueryString,
                userInfo,
                clientIp,
                GetSafeHeaders(request.Headers),
                string.IsNullOrEmpty(requestBody) ? "[Empty]" : requestBody.Length > 1000 ? "[Large Body]" : requestBody);
        }

        private async Task LogResponseAsync(HttpContext context, string requestId, long durationMs)
        {
            var response = context.Response;
            var userInfo = GetUserInfo(context);
            
            var responseBody = string.Empty;
            if (response.Body.CanSeek && response.ContentType?.Contains("application/json") == true)
            {
                response.Body.Seek(0, SeekOrigin.Begin);
                responseBody = await new StreamReader(response.Body).ReadToEndAsync();
                response.Body.Seek(0, SeekOrigin.Begin);
            }
            
            var logLevel = GetLogLevel(response.StatusCode, durationMs);
            
            _logger.Log(logLevel,
                "Response for request {RequestId}: {StatusCode} {StatusText} to {User}. " +
                "Duration: {Duration}ms. Content-Type: {ContentType}. Body: {Body}",
                requestId,
                response.StatusCode,
                GetStatusText(response.StatusCode),
                userInfo,
                durationMs,
                response.ContentType ?? "[Not Set]",
                string.IsNullOrEmpty(responseBody) ? "[Empty]" : responseBody.Length > 1000 ? "[Large Body]" : responseBody);
            
            // Log performance warning for slow requests
            if (durationMs > 5000) // 5 seconds
            {
                _logger.LogWarning(
                    "Slow request detected {RequestId}: {Method} {Path} took {Duration}ms for user {User}",
                    requestId,
                    context.Request.Method,
                    context.Request.Path,
                    durationMs,
                    userInfo);
            }
        }

        private string GetUserInfo(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = context.User.FindFirst(ClaimTypes.Name)?.Value;
                var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
                
                return $"{userName} (ID: {userId}, Role: {userRole})";
            }
            
            return "[Anonymous]";
        }

        private string GetClientIpAddress(HttpContext context)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            
            // Check for forwarded IP addresses (when behind proxy/load balancer)
            if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                var forwardedIp = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedIp))
                {
                    ipAddress = forwardedIp.Split(',')[0].Trim();
                }
            }
            else if (context.Request.Headers.ContainsKey("X-Real-IP"))
            {
                var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
                if (!string.IsNullOrEmpty(realIp))
                {
                    ipAddress = realIp;
                }
            }
            
            return ipAddress ?? "[Unknown]";
        }

        private Dictionary<string, string> GetSafeHeaders(IHeaderDictionary headers)
        {
            var safeHeaders = new Dictionary<string, string>();
            var sensitiveHeaders = new[] { "authorization", "cookie", "x-api-key", "x-auth-token" };
            
            foreach (var header in headers)
            {
                if (sensitiveHeaders.Contains(header.Key.ToLower()))
                {
                    safeHeaders[header.Key] = "[REDACTED]";
                }
                else
                {
                    safeHeaders[header.Key] = string.Join(", ", header.Value);
                }
            }
            
            return safeHeaders;
        }

        private LogLevel GetLogLevel(int statusCode, long durationMs)
        {
            if (statusCode >= 500)
                return LogLevel.Error;
            if (statusCode >= 400)
                return LogLevel.Warning;
            if (durationMs > 3000) // 3 seconds
                return LogLevel.Warning;
            
            return LogLevel.Information;
        }

        private string GetStatusText(int statusCode)
        {
            return statusCode switch
            {
                200 => "OK",
                201 => "Created",
                204 => "No Content",
                400 => "Bad Request",
                401 => "Unauthorized",
                403 => "Forbidden",
                404 => "Not Found",
                409 => "Conflict",
                422 => "Unprocessable Entity",
                500 => "Internal Server Error",
                502 => "Bad Gateway",
                503 => "Service Unavailable",
                _ => "Unknown"
            };
        }
    }

    public static class RequestResponseLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestResponseLoggingMiddleware>();
        }
    }
}