using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Areas.Identity.Data;
using WebApplication2.Models;

namespace WebApplication2.Data;

public class WebApplication2Context : IdentityDbContext<WebApplication2User>
{
    public WebApplication2Context(DbContextOptions<WebApplication2Context> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Configure Product entity for better performance
        builder.Entity<Product>(entity =>
        {
            // Add index on UserId for faster user-specific queries
            entity.HasIndex(p => p.UserId)
                  .HasDatabaseName("IX_Products_UserId");
            
            // Add index on ProductDate for faster date-based filtering
            entity.HasIndex(p => p.ProductDate)
                  .HasDatabaseName("IX_Products_ProductDate");
            
            // Add composite index for common query patterns
            entity.HasIndex(p => new { p.UserId, p.ProductDate })
                  .HasDatabaseName("IX_Products_UserId_ProductDate");
            
            // Add index on Category for filtering
            entity.HasIndex(p => p.Category)
                  .HasDatabaseName("IX_Products_Category");
            
            // Add composite indexes for analytics queries
            entity.HasIndex(p => new { p.ProductDate, p.Category })
                  .HasDatabaseName("IX_Products_ProductDate_Category");
            
            entity.HasIndex(p => new { p.Category, p.ProductDate })
                  .HasDatabaseName("IX_Products_Category_ProductDate");
            
            // Configure string length constraints for better performance
            entity.Property(p => p.Name)
                  .HasMaxLength(100)
                  .IsRequired();
            
            entity.Property(p => p.Category)
                  .HasMaxLength(50)
                  .IsRequired();
            
            entity.Property(p => p.ImagePath)
                  .HasMaxLength(500);
            
            entity.Property(p => p.ImageFileName)
                  .HasMaxLength(100);
            
            // Configure relationship with User
            entity.HasOne(p => p.User)
                  .WithMany(u => u.Products)
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configure Notification entity
        builder.Entity<Notification>(entity =>
        {
            // Add index on UserId for faster user-specific queries
            entity.HasIndex(n => n.UserId)
                  .HasDatabaseName("IX_Notifications_UserId");
            
            // Add index on IsRead for filtering unread notifications
            entity.HasIndex(n => n.IsRead)
                  .HasDatabaseName("IX_Notifications_IsRead");
            
            // Add composite index for common query patterns
            entity.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt })
                  .HasDatabaseName("IX_Notifications_UserId_IsRead_CreatedAt");
            
            // Add index on CreatedAt for ordering
            entity.HasIndex(n => n.CreatedAt)
                  .HasDatabaseName("IX_Notifications_CreatedAt");
            
            // Configure relationship with User
            entity.HasOne(n => n.User)
                  .WithMany()
                  .HasForeignKey(n => n.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configure SecurityLog entity for better performance
        builder.Entity<SecurityLog>(entity =>
        {
            entity.HasIndex(s => s.Timestamp)
                  .HasDatabaseName("IX_SecurityLogs_Timestamp");
            
            entity.HasIndex(s => new { s.Timestamp, s.Action })
                  .HasDatabaseName("IX_SecurityLogs_Timestamp_Action");
            
            entity.HasIndex(s => new { s.UserId, s.Timestamp })
                  .HasDatabaseName("IX_SecurityLogs_UserId_Timestamp");
            
            entity.Property(s => s.Action)
                  .HasMaxLength(100)
                  .IsRequired();
            
            entity.Property(s => s.IpAddress)
                  .HasMaxLength(45); // IPv6 max length
        });
        
        // Configure FailedLoginAttempt entity for better performance
        builder.Entity<FailedLoginAttempt>(entity =>
        {
            entity.HasIndex(f => f.AttemptTime)
                  .HasDatabaseName("IX_FailedLoginAttempts_AttemptTime");
            
            entity.HasIndex(f => new { f.IpAddress, f.AttemptTime })
                  .HasDatabaseName("IX_FailedLoginAttempts_IpAddress_AttemptTime");
            
            entity.Property(f => f.IpAddress)
                  .HasMaxLength(45)
                  .IsRequired();
            
            entity.Property(f => f.UserAgent)
                  .HasMaxLength(500);
        });
        
        // Configure UserSession entity for better performance
        builder.Entity<UserSession>(entity =>
        {
            entity.HasIndex(u => u.UserId)
                  .HasDatabaseName("IX_UserSessions_UserId");
            
            entity.HasIndex(u => u.CreatedAt)
                  .HasDatabaseName("IX_UserSessions_CreatedAt");
            
            entity.HasIndex(u => new { u.UserId, u.IsActive })
                  .HasDatabaseName("IX_UserSessions_UserId_IsActive");
            
            entity.Property(u => u.IpAddress)
                  .HasMaxLength(45);
            
            entity.Property(u => u.UserAgent)
                  .HasMaxLength(500);
        });
        
        // Configure ApiKey entity for better performance
        builder.Entity<ApiKey>(entity =>
        {
            entity.HasIndex(a => a.IsActive)
                  .HasDatabaseName("IX_ApiKeys_IsActive");
            
            entity.HasIndex(a => a.CreatedAt)
                  .HasDatabaseName("IX_ApiKeys_CreatedAt");
            
            entity.Property(a => a.Name)
                  .HasMaxLength(100)
                  .IsRequired();
            
            entity.Property(a => a.AllowedDomains)
                  .HasMaxLength(1000);
        });
        
        // Configure WebApplication2User for analytics
        builder.Entity<WebApplication2User>(entity =>
        {
            entity.HasIndex(u => u.RegistrationDate)
                  .HasDatabaseName("IX_AspNetUsers_RegistrationDate");
        });
    }
    public DbSet<Product> Products { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    
    // Security-related DbSets
    public DbSet<SecurityLog> SecurityLogs { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<FailedLoginAttempt> FailedLoginAttempts { get; set; }
    public DbSet<UserSecuritySettings> UserSecuritySettings { get; set; }
    public DbSet<SecurityConfiguration> SecurityConfigurations { get; set; }
}
