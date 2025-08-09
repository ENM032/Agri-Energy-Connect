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
            
            // Configure relationship with User
            entity.HasOne(p => p.User)
                  .WithMany(u => u.Products)
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
    public DbSet<Product> Products { get; set; }
}
