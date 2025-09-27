using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using WebApplication2.Areas.Identity.Data;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Services
{
    /// <summary>
    /// Implementation of security-related services
    /// </summary>
    public class SecurityService : ISecurityService
    {
        private readonly WebApplication2Context _context;
        private readonly UserManager<WebApplication2User> _userManager;
        private readonly IMemoryCache _cache;
        private readonly IDataProtector _dataProtector;
        private readonly ILogger<SecurityService> _logger;
        
        // Rate limiting configuration
        private readonly Dictionary<string, RateLimitConfig> _rateLimits = new()
        {
            { "/api/", new RateLimitConfig { RequestsPerMinute = 60, RequestsPerHour = 1000 } },
            { "/auth/", new RateLimitConfig { RequestsPerMinute = 10, RequestsPerHour = 100 } },
            { "default", new RateLimitConfig { RequestsPerMinute = 100, RequestsPerHour = 2000 } }
        };
        
        public SecurityService(
            WebApplication2Context context,
            UserManager<WebApplication2User> userManager,
            IMemoryCache cache,
            IDataProtectionProvider dataProtectionProvider,
            ILogger<SecurityService> logger)
        {
            _context = context;
            _userManager = userManager;
            _cache = cache;
            _dataProtector = dataProtectionProvider.CreateProtector("WebApplication2.Security");
            _logger = logger;
        }
        
        public async Task<bool> CanAccessResourceAsync(string userId, string resourceId, string resourceType)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return false;
                
                var userRoles = await _userManager.GetRolesAsync(user);
                
                // Admin can access everything
                if (userRoles.Contains("Admin")) return true;
                
                switch (resourceType.ToLower())
                {
                    case "product":
                        // Farmers can only access their own products
                        if (userRoles.Contains("Farmer"))
                        {
                            var product = await _context.Products.FindAsync(int.Parse(resourceId));
                            return product?.UserId == userId;
                        }
                        // Support employees can access all products
                        return userRoles.Contains("Support Employee");
                        
                    case "user":
                        // Users can access their own profile
                        if (resourceId == userId) return true;
                        // Support employees and admins can access user data
                        return userRoles.Contains("Support Employee");
                        
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking resource access for user {UserId}, resource {ResourceId}", userId, resourceId);
                return false;
            }
        }
        
        public async Task LogSecurityEventAsync(string eventType, string description, string? userId = null, string? ipAddress = null)
        {
            try
            {
                var securityLog = new SecurityLog
                {
                    EventType = eventType,
                    Description = description,
                    UserId = userId,
                    IpAddress = ipAddress,
                    Timestamp = DateTime.UtcNow
                };
                
                _context.SecurityLogs.Add(securityLog);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Security event logged: {EventType} - {Description}", eventType, description);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log security event: {EventType}", eventType);
            }
        }
        
        public async Task<bool> IsRateLimitedAsync(string ipAddress, string endpoint)
        {
            try
            {
                var config = GetRateLimitConfig(endpoint);
                var minuteKey = $"rate_limit_minute_{ipAddress}_{endpoint}_{DateTime.UtcNow:yyyyMMddHHmm}";
                var hourKey = $"rate_limit_hour_{ipAddress}_{endpoint}_{DateTime.UtcNow:yyyyMMddHH}";
                
                var minuteCount = _cache.Get<int>(minuteKey);
                var hourCount = _cache.Get<int>(hourKey);
                
                if (minuteCount >= config.RequestsPerMinute || hourCount >= config.RequestsPerHour)
                {
                    await LogSecurityEventAsync("RATE_LIMIT_EXCEEDED", $"Rate limit exceeded for {ipAddress} on {endpoint}", null, ipAddress);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking rate limit for {IpAddress}", ipAddress);
                return false; // Allow request if rate limiting fails
            }
        }
        
        public async Task RecordApiRequestAsync(string ipAddress, string endpoint)
        {
            try
            {
                var config = GetRateLimitConfig(endpoint);
                var minuteKey = $"rate_limit_minute_{ipAddress}_{endpoint}_{DateTime.UtcNow:yyyyMMddHHmm}";
                var hourKey = $"rate_limit_hour_{ipAddress}_{endpoint}_{DateTime.UtcNow:yyyyMMddHH}";
                
                var minuteCount = _cache.Get<int>(minuteKey);
                var hourCount = _cache.Get<int>(hourKey);
                
                _cache.Set(minuteKey, minuteCount + 1, TimeSpan.FromMinutes(1));
                _cache.Set(hourKey, hourCount + 1, TimeSpan.FromHours(1));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording API request for {IpAddress}", ipAddress);
            }
        }
        
        public async Task<bool> ValidateApiKeyAsync(string apiKey)
        {
            try
            {
                var hashedKey = HashApiKey(apiKey);
                var keyRecord = await _context.ApiKeys
                    .FirstOrDefaultAsync(k => k.HashedKey == hashedKey && k.IsActive && k.ExpiresAt > DateTime.UtcNow);
                
                if (keyRecord != null)
                {
                    keyRecord.LastUsed = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return true;
                }
                
                await LogSecurityEventAsync("INVALID_API_KEY", $"Invalid API key used: {apiKey[..8]}...");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating API key");
                return false;
            }
        }
        
        public async Task<string> GenerateApiKeyAsync(string userId)
        {
            try
            {
                var apiKey = GenerateSecureToken(32);
                var hashedKey = HashApiKey(apiKey);
                
                var keyRecord = new ApiKey
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    HashedKey = hashedKey,
                    Name = $"API Key - {DateTime.UtcNow:yyyy-MM-dd}",
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddYears(1),
                    IsActive = true,
                    Permissions = "read,write"
                };
                
                _context.ApiKeys.Add(keyRecord);
                await _context.SaveChangesAsync();
                
                await LogSecurityEventAsync("API_KEY_GENERATED", $"New API key generated for user {userId}", userId);
                
                return apiKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating API key for user {UserId}", userId);
                throw;
            }
        }
        
        public async Task<bool> RevokeApiKeyAsync(string apiKey)
        {
            try
            {
                var hashedKey = HashApiKey(apiKey);
                var keyRecord = await _context.ApiKeys.FirstOrDefaultAsync(k => k.HashedKey == hashedKey);
                
                if (keyRecord != null)
                {
                    keyRecord.IsActive = false;
                    await _context.SaveChangesAsync();
                    
                    await LogSecurityEventAsync("API_KEY_REVOKED", $"API key revoked: {keyRecord.Id}", keyRecord.UserId);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking API key");
                return false;
            }
        }
        
        public async Task<List<ApiKeyInfo>> GetUserApiKeysAsync(string userId)
        {
            try
            {
                var keys = await _context.ApiKeys
                    .Where(k => k.UserId == userId)
                    .Select(k => new ApiKeyInfo
                    {
                        KeyId = k.Id,
                        Name = k.Name,
                        CreatedAt = k.CreatedAt,
                        LastUsed = k.LastUsed,
                        IsActive = k.IsActive,
                        Permissions = k.Permissions
                    })
                    .ToListAsync();
                
                return keys;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API keys for user {UserId}", userId);
                return new List<ApiKeyInfo>();
            }
        }
        
        public PasswordStrengthResult ValidatePasswordStrength(string password)
        {
            var result = new PasswordStrengthResult();
            var score = 0;
            var weaknesses = new List<string>();
            var suggestions = new List<string>();
            
            // Length check
            if (password.Length >= 12) score += 25;
            else if (password.Length >= 8) score += 15;
            else weaknesses.Add("Password is too short");
            
            // Character variety
            if (Regex.IsMatch(password, @"[a-z]")) score += 10;
            else weaknesses.Add("Missing lowercase letters");
            
            if (Regex.IsMatch(password, @"[A-Z]")) score += 10;
            else weaknesses.Add("Missing uppercase letters");
            
            if (Regex.IsMatch(password, @"\d")) score += 10;
            else weaknesses.Add("Missing numbers");
            
            if (Regex.IsMatch(password, @"[!@#$%^&*(),.?;:{}|<>\[\]]")) score += 15;
            else weaknesses.Add("Missing special characters");
            
            // Common patterns
            if (Regex.IsMatch(password, @"(.)\1{2,}"))
            {
                score -= 10;
                weaknesses.Add("Contains repeated characters");
            }
            
            if (Regex.IsMatch(password, @"(012|123|234|345|456|567|678|789|890|abc|bcd|cde|def)", RegexOptions.IgnoreCase))
            {
                score -= 15;
                weaknesses.Add("Contains sequential characters");
            }
            
            // Dictionary words (simplified check)
            var commonWords = new[] { "password", "admin", "user", "login", "welcome", "qwerty", "123456" };
            if (commonWords.Any(word => password.ToLower().Contains(word)))
            {
                score -= 20;
                weaknesses.Add("Contains common words");
            }
            
            // Entropy bonus
            var uniqueChars = password.Distinct().Count();
            if (uniqueChars > password.Length * 0.7) score += 10;
            
            result.Score = Math.Max(0, Math.Min(100, score));
            result.IsStrong = result.Score >= 70;
            result.Weaknesses = weaknesses;
            
            if (!result.IsStrong)
            {
                suggestions.Add("Use at least 12 characters");
                suggestions.Add("Include uppercase and lowercase letters");
                suggestions.Add("Add numbers and special characters");
                suggestions.Add("Avoid common words and patterns");
            }
            
            result.Suggestions = suggestions;
            return result;
        }
        
        public async Task<SecurityAssessmentResult> AssessAccountSecurityAsync(string userId)
        {
            var result = new SecurityAssessmentResult
            {
                LastAssessment = DateTime.UtcNow
            };
            
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    result.SecurityIssues.Add("User not found");
                    result.RiskScore = 100;
                    return result;
                }
                
                var riskScore = 0;
                
                // Check recent failed login attempts
                var recentFailedLogins = await _context.SecurityLogs
                    .Where(l => l.UserId == userId && l.EventType == "LOGIN_FAILED" && l.Timestamp > DateTime.UtcNow.AddDays(-7))
                    .CountAsync();
                
                if (recentFailedLogins > 10)
                {
                    riskScore += 30;
                    result.SecurityIssues.Add("High number of failed login attempts");
                }
                
                // Check password age (if we track it)
                // Note: PasswordChangedAt property not implemented in WebApplication2User
                // TODO: Add PasswordChangedAt property to WebApplication2User if password age tracking is needed
                /*
                if (user.PasswordChangedAt.HasValue && user.PasswordChangedAt < DateTime.UtcNow.AddMonths(-6))
                {
                    riskScore += 20;
                    result.SecurityIssues.Add("Password hasn't been changed in 6+ months");
                    result.Recommendations.Add("Consider changing your password");
                }
                */
                
                // Check for suspicious activity
                var suspiciousActivity = await _context.SecurityLogs
                    .Where(l => l.UserId == userId && 
                               (l.EventType == "SUSPICIOUS_LOGIN" || l.EventType == "RATE_LIMIT_EXCEEDED") &&
                               l.Timestamp > DateTime.UtcNow.AddDays(-30))
                    .CountAsync();
                
                if (suspiciousActivity > 0)
                {
                    riskScore += 25;
                    result.SecurityIssues.Add("Suspicious activity detected");
                }
                
                result.RiskScore = Math.Min(100, riskScore);
                result.IsSecure = result.RiskScore < 30;
                
                if (result.RiskScore >= 30)
                {
                    result.Recommendations.Add("Review recent account activity");
                    result.Recommendations.Add("Enable two-factor authentication if available");
                    result.Recommendations.Add("Use a strong, unique password");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assessing account security for user {UserId}", userId);
                result.SecurityIssues.Add("Unable to complete security assessment");
                result.RiskScore = 50; // Medium risk if assessment fails
            }
            
            return result;
        }
        
        public string EncryptSensitiveData(string data)
        {
            try
            {
                return _dataProtector.Protect(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting sensitive data");
                throw;
            }
        }
        
        public string DecryptSensitiveData(string encryptedData)
        {
            try
            {
                return _dataProtector.Unprotect(encryptedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting sensitive data");
                throw;
            }
        }
        
        public string GenerateSecureToken(int length = 32)
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[length];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "")[..length];
        }
        
        public bool ValidateCsrfToken(string token, string expectedToken)
        {
            return !string.IsNullOrEmpty(token) && 
                   !string.IsNullOrEmpty(expectedToken) && 
                   token.Equals(expectedToken, StringComparison.Ordinal);
        }
        
        private RateLimitConfig GetRateLimitConfig(string endpoint)
        {
            foreach (var kvp in _rateLimits)
            {
                if (endpoint.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }
            return _rateLimits["default"];
        }
        
        private string HashApiKey(string apiKey)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
            return Convert.ToBase64String(hashedBytes);
        }
    }
    
    /// <summary>
    /// Rate limiting configuration
    /// </summary>
    public class RateLimitConfig
    {
        public int RequestsPerMinute { get; set; }
        public int RequestsPerHour { get; set; }
    }
}