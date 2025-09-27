using Microsoft.AspNetCore.Identity;
using WebApplication2.Areas.Identity.Data;
using WebApplication2.Models;

namespace WebApplication2.Services
{
    /// <summary>
    /// Interface for security-related services
    /// </summary>
    public interface ISecurityService
    {
        /// <summary>
        /// Validates if the current user can access a specific resource
        /// </summary>
        Task<bool> CanAccessResourceAsync(string userId, string resourceId, string resourceType);
        
        /// <summary>
        /// Logs security events for auditing purposes
        /// </summary>
        Task LogSecurityEventAsync(string eventType, string description, string? userId = null, string? ipAddress = null);
        
        /// <summary>
        /// Checks if an IP address is rate limited
        /// </summary>
        Task<bool> IsRateLimitedAsync(string ipAddress, string endpoint);
        
        /// <summary>
        /// Records an API request for rate limiting
        /// </summary>
        Task RecordApiRequestAsync(string ipAddress, string endpoint);
        
        /// <summary>
        /// Validates API key for API authentication
        /// </summary>
        Task<bool> ValidateApiKeyAsync(string apiKey);
        
        /// <summary>
        /// Generates a new API key for a user
        /// </summary>
        Task<string> GenerateApiKeyAsync(string userId);
        
        /// <summary>
        /// Revokes an API key
        /// </summary>
        Task<bool> RevokeApiKeyAsync(string apiKey);
        
        /// <summary>
        /// Gets user's active API keys
        /// </summary>
        Task<List<ApiKeyInfo>> GetUserApiKeysAsync(string userId);
        
        /// <summary>
        /// Validates password strength beyond basic requirements
        /// </summary>
        PasswordStrengthResult ValidatePasswordStrength(string password);
        
        /// <summary>
        /// Checks if user account is compromised based on security indicators
        /// </summary>
        Task<SecurityAssessmentResult> AssessAccountSecurityAsync(string userId);
        
        /// <summary>
        /// Encrypts sensitive data
        /// </summary>
        string EncryptSensitiveData(string data);
        
        /// <summary>
        /// Decrypts sensitive data
        /// </summary>
        string DecryptSensitiveData(string encryptedData);
        
        /// <summary>
        /// Generates secure random tokens
        /// </summary>
        string GenerateSecureToken(int length = 32);
        
        /// <summary>
        /// Validates CSRF token manually when needed
        /// </summary>
        bool ValidateCsrfToken(string token, string expectedToken);
    }
    
    /// <summary>
    /// Information about an API key
    /// </summary>
    public class ApiKeyInfo
    {
        public string KeyId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUsed { get; set; }
        public bool IsActive { get; set; }
        public string Permissions { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Result of password strength validation
    /// </summary>
    public class PasswordStrengthResult
    {
        public bool IsStrong { get; set; }
        public int Score { get; set; } // 0-100
        public List<string> Weaknesses { get; set; } = new List<string>();
        public List<string> Suggestions { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Result of security assessment
    /// </summary>
    public class SecurityAssessmentResult
    {
        public bool IsSecure { get; set; }
        public int RiskScore { get; set; } // 0-100, higher is more risky
        public List<string> SecurityIssues { get; set; } = new List<string>();
        public List<string> Recommendations { get; set; } = new List<string>();
        public DateTime LastAssessment { get; set; }
    }
}