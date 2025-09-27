using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    /// <summary>
    /// Model for logging security events
    /// </summary>
    public class SecurityLog
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string EventType { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [MaxLength(450)] // Standard ASP.NET Identity user ID length
        public string? UserId { get; set; }
        
        [MaxLength(45)] // IPv6 max length
        public string? IpAddress { get; set; }
        
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        
        [MaxLength(200)]
        public string? Endpoint { get; set; }
        
        [MaxLength(50)]
        public string? HttpMethod { get; set; }
        
        public int? StatusCode { get; set; }
        
        [MaxLength(1000)]
        public string? AdditionalData { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [MaxLength(50)]
        public string Severity { get; set; } = "Info"; // Info, Warning, Error, Critical
        
        public bool IsResolved { get; set; } = false;
        
        [MaxLength(450)]
        public string? ResolvedBy { get; set; }
        
        public DateTime? ResolvedAt { get; set; }
        
        [MaxLength(500)]
        public string? ResolutionNotes { get; set; }
    }
    
    /// <summary>
    /// Model for API key management
    /// </summary>
    public class ApiKey
    {
        [Key]
        [MaxLength(450)]
        public string Id { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(500)]
        public string HashedKey { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        [MaxLength(200)]
        public string Permissions { get; set; } = string.Empty; // Comma-separated permissions
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastUsed { get; set; }
        
        public DateTime ExpiresAt { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        [MaxLength(45)]
        public string? LastUsedIpAddress { get; set; }
        
        [MaxLength(500)]
        public string? LastUsedUserAgent { get; set; }
        
        public int UsageCount { get; set; } = 0;
        
        [MaxLength(100)]
        public string? Environment { get; set; } // Development, Staging, Production
        
        [MaxLength(200)]
        public string? AllowedIpAddresses { get; set; } // Comma-separated IP addresses
        
        [MaxLength(200)]
        public string? AllowedDomains { get; set; } // Comma-separated domains
        
        public bool RequireHttps { get; set; } = true;
        
        [MaxLength(450)]
        public string? CreatedBy { get; set; }
        
        [MaxLength(450)]
        public string? RevokedBy { get; set; }
        
        public DateTime? RevokedAt { get; set; }
        
        [MaxLength(500)]
        public string? RevokedReason { get; set; }
    }
    
    /// <summary>
    /// Model for tracking user sessions
    /// </summary>
    public class UserSession
    {
        [Key]
        [MaxLength(450)]
        public string Id { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(500)]
        public string SessionToken { get; set; } = string.Empty;
        
        [MaxLength(45)]
        public string? IpAddress { get; set; }
        
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        
        [MaxLength(100)]
        public string? DeviceType { get; set; } // Desktop, Mobile, Tablet
        
        [MaxLength(100)]
        public string? Browser { get; set; }
        
        [MaxLength(100)]
        public string? OperatingSystem { get; set; }
        
        [MaxLength(200)]
        public string? Location { get; set; } // City, Country
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
        
        public DateTime ExpiresAt { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public bool IsRevoked { get; set; } = false;
        
        [MaxLength(450)]
        public string? RevokedBy { get; set; }
        
        public DateTime? RevokedAt { get; set; }
        
        [MaxLength(500)]
        public string? RevokedReason { get; set; }
        
        public bool IsSuspicious { get; set; } = false;
        
        [MaxLength(500)]
        public string? SuspiciousReason { get; set; }
    }
    
    /// <summary>
    /// Model for tracking failed login attempts
    /// </summary>
    public class FailedLoginAttempt
    {
        [Key]
        public int Id { get; set; }
        
        [MaxLength(450)]
        public string? UserId { get; set; }
        
        [MaxLength(256)]
        public string? Email { get; set; }
        
        [MaxLength(256)]
        public string? Username { get; set; }
        
        [Required]
        [MaxLength(45)]
        public string IpAddress { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        
        [MaxLength(100)]
        public string FailureReason { get; set; } = string.Empty; // InvalidPassword, UserNotFound, AccountLocked, etc.
        
        public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime AttemptTime { get; set; } = DateTime.UtcNow;
        
        public bool IsBlocked { get; set; } = false;
        
        [MaxLength(200)]
        public string? Location { get; set; }
        
        [MaxLength(500)]
        public string? AdditionalInfo { get; set; }
    }
    
    /// <summary>
    /// Model for security settings per user
    /// </summary>
    public class UserSecuritySettings
    {
        [Key]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;
        
        public bool TwoFactorEnabled { get; set; } = false;
        
        public bool EmailNotificationsEnabled { get; set; } = true;
        
        public bool SmsNotificationsEnabled { get; set; } = false;
        
        public bool LoginAlertsEnabled { get; set; } = true;
        
        public bool SuspiciousActivityAlertsEnabled { get; set; } = true;
        
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }
        
        public bool PhoneNumberConfirmed { get; set; } = false;
        
        [MaxLength(500)]
        public string? BackupCodes { get; set; } // Encrypted backup codes
        
        public DateTime? LastPasswordChange { get; set; }
        
        public DateTime? LastSecurityReview { get; set; }
        
        public int PasswordChangeFrequencyDays { get; set; } = 90;
        
        public bool RequirePasswordChangeOnNextLogin { get; set; } = false;
        
        [MaxLength(200)]
        public string? TrustedDevices { get; set; } // Comma-separated device IDs
        
        [MaxLength(500)]
        public string? SecurityQuestions { get; set; } // Encrypted security questions and answers
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Model for system-wide security configuration
    /// </summary>
    public class SecurityConfiguration
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string ConfigKey { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(1000)]
        public string ConfigValue { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        [MaxLength(50)]
        public string Category { get; set; } = "General";
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        [MaxLength(450)]
        public string? UpdatedBy { get; set; }
    }
}