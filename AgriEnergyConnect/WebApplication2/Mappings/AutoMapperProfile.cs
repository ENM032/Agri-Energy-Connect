using AutoMapper;
using WebApplication2.Models;
using WebApplication2.Models.DTOs;
using WebApplication2.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using WebApplication2.Areas.Identity.Data;

namespace WebApplication2.Mappings
{
    /// <summary>
    /// AutoMapper configuration profile for mapping between entities, DTOs, and ViewModels
    /// </summary>
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            ConfigureProductMappings();
            ConfigureUserMappings();
            ConfigureNotificationMappings();
            ConfigureAnalyticsMappings();
        }
        
        private void ConfigureProductMappings()
        {
            // Product mappings
            CreateMap<Product, ProductViewModel>();
            CreateMap<ProductViewModel, Product>();
            
            // Product Entity to DTO mappings
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.UserName : string.Empty));
            
            // ProductResponseDto doesn't exist, using ProductDto instead
            
            // Product Entity to ViewModel mappings
            CreateMap<Product, ProductViewModel>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.UserName : string.Empty))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.HasImage, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.ImagePath)))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.ImagePath) ? $"/uploads/products/{src.ImagePath}" : null))
                .ForMember(dest => dest.FormattedDate, opt => opt.MapFrom(src => src.ProductDate.ToString("MMM dd, yyyy")))
                .ForMember(dest => dest.IsOwner, opt => opt.Ignore()) // Set in controller based on current user
                .ForMember(dest => dest.CanEdit, opt => opt.Ignore()) // Set in controller based on permissions
                .ForMember(dest => dest.CanDelete, opt => opt.Ignore()); // Set in controller based on permissions
            
            CreateMap<Product, ProductEditViewModel>()
                .ForMember(dest => dest.CurrentImagePath, opt => opt.MapFrom(src => src.ImagePath))
                .ForMember(dest => dest.CurrentImageFileName, opt => opt.MapFrom(src => src.ImageFileName))
                .ForMember(dest => dest.RemoveCurrentImage, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.ProductImage, opt => opt.Ignore())
                .ForMember(dest => dest.Categories, opt => opt.Ignore())
                .ForMember(dest => dest.AvailableCategories, opt => opt.Ignore());
            
            // DTO to Entity mappings
            CreateMap<CreateProductDto, Product>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.ImagePath, opt => opt.Ignore())
                .ForMember(dest => dest.ImageFileName, opt => opt.Ignore());
            
            CreateMap<UpdateProductDto, Product>()
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.ImagePath, opt => opt.Ignore())
                .ForMember(dest => dest.ImageFileName, opt => opt.Ignore());
            
            // ViewModel to Entity mappings
            CreateMap<ProductCreateViewModel, Product>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore()) // Set in controller
                .ForMember(dest => dest.ImagePath, opt => opt.Ignore())
                .ForMember(dest => dest.ImageFileName, opt => opt.Ignore());
            
            CreateMap<ProductEditViewModel, Product>()
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.ImagePath, opt => opt.Ignore())
                .ForMember(dest => dest.ImageFileName, opt => opt.Ignore());
        }
        
        private void ConfigureUserMappings()
        {
            // User Entity to DTO mappings
            CreateMap<WebApplication2User, UserDto>()
                .ForMember(dest => dest.Roles, opt => opt.Ignore()) // Set separately in service
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => !src.LockoutEnd.HasValue || src.LockoutEnd <= DateTimeOffset.Now))
                .ForMember(dest => dest.LastLoginDate, opt => opt.Ignore()) // Set from additional data if available
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore()); // Set from additional data if available
            
            // UserResponseDto doesn't exist, using UserDto instead
            // CreateMap<WebApplication2User, UserResponseDto>() - removed as UserResponseDto doesn't exist
            
            // User Entity to ViewModel mappings
            CreateMap<WebApplication2User, UserProfileViewModel>()
                .ForMember(dest => dest.Roles, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => !src.LockoutEnd.HasValue || src.LockoutEnd <= DateTimeOffset.Now))
                .ForMember(dest => dest.LastLoginDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.ProductCount, opt => opt.Ignore())
                .ForMember(dest => dest.NotificationCount, opt => opt.Ignore())
                .ForMember(dest => dest.FormattedLastLogin, opt => opt.Ignore())
                .ForMember(dest => dest.FormattedCreatedDate, opt => opt.Ignore());
            
            CreateMap<WebApplication2User, EditUserProfileViewModel>();
            
            // DTO to Entity mappings
            CreateMap<CreateUserDto, WebApplication2User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
                .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.NormalizedUserName, opt => opt.Ignore())
                .ForMember(dest => dest.NormalizedEmail, opt => opt.Ignore())
                .ForMember(dest => dest.EmailConfirmed, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnabled, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
                .ForMember(dest => dest.AccessFailedCount, opt => opt.Ignore())
                .ForMember(dest => dest.TwoFactorEnabled, opt => opt.Ignore())
                .ForMember(dest => dest.Displayname, opt => opt.MapFrom(src => src.DisplayName));
            
            CreateMap<UpdateProfileDto, WebApplication2User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
                .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.NormalizedUserName, opt => opt.Ignore())
                .ForMember(dest => dest.NormalizedEmail, opt => opt.Ignore())
                .ForMember(dest => dest.EmailConfirmed, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnabled, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
                .ForMember(dest => dest.AccessFailedCount, opt => opt.Ignore())
                .ForMember(dest => dest.TwoFactorEnabled, opt => opt.Ignore())
                .ForMember(dest => dest.Displayname, opt => opt.MapFrom(src => src.DisplayName));
            
            // ViewModel to Entity mappings
            CreateMap<EditUserProfileViewModel, WebApplication2User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
                .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.NormalizedUserName, opt => opt.Ignore())
                .ForMember(dest => dest.NormalizedEmail, opt => opt.Ignore())
                .ForMember(dest => dest.EmailConfirmed, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnabled, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
                .ForMember(dest => dest.AccessFailedCount, opt => opt.Ignore())
                .ForMember(dest => dest.TwoFactorEnabled, opt => opt.Ignore());
            
            CreateMap<CreateUserViewModel, WebApplication2User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
                .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.NormalizedUserName, opt => opt.Ignore())
                .ForMember(dest => dest.NormalizedEmail, opt => opt.Ignore())
                .ForMember(dest => dest.EmailConfirmed, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnabled, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
                .ForMember(dest => dest.AccessFailedCount, opt => opt.Ignore())
                .ForMember(dest => dest.TwoFactorEnabled, opt => opt.Ignore())
                .ForMember(dest => dest.Displayname, opt => opt.MapFrom(src => src.DisplayName));
        }
        
        private void ConfigureNotificationMappings()
        {
            // Notification Entity to ViewModel mappings
            CreateMap<Notification, NotificationViewModel>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
                .ForMember(dest => dest.TypeIcon, opt => opt.Ignore()) // Calculated property
                .ForMember(dest => dest.TypeClass, opt => opt.Ignore()) // Calculated property
                .ForMember(dest => dest.TimeAgo, opt => opt.Ignore()); // Calculated property
            
            // ViewModel to Entity mappings
            CreateMap<CreateNotificationViewModel, Notification>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsRead, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.UserId, opt => opt.Ignore()) // Set in controller
                .ForMember(dest => dest.ReadAt, opt => opt.Ignore());
        }
        
        private void ConfigureAnalyticsMappings()
        {
            // Analytics DTO mappings (these are typically not mapped from entities)
            // but we can create reverse mappings if needed
            
            // Add mapping from ViewModel to DTO for API responses
            CreateMap<DashboardAnalyticsViewModel, DashboardAnalyticsDto>()
                .ForMember(dest => dest.TotalUsers, opt => opt.MapFrom(src => src.TotalUsers))
                .ForMember(dest => dest.TotalProducts, opt => opt.MapFrom(src => src.TotalProducts))
                .ForMember(dest => dest.TotalFarmers, opt => opt.MapFrom(src => src.TotalFarmers))
                .ForMember(dest => dest.TotalAdmins, opt => opt.MapFrom(src => src.TotalAdmins))
                .ForMember(dest => dest.TotalSupportEmployees, opt => opt.MapFrom(src => src.TotalSupportEmployees));
            
            CreateMap<DashboardAnalyticsDto, DashboardViewModel>()
                .ForMember(dest => dest.TotalUsers, opt => opt.MapFrom(src => src.TotalUsers))
                .ForMember(dest => dest.TotalProducts, opt => opt.MapFrom(src => src.TotalProducts))
                .ForMember(dest => dest.TotalFarmers, opt => opt.MapFrom(src => src.TotalFarmers))
                .ForMember(dest => dest.TotalAdmins, opt => opt.MapFrom(src => src.TotalAdmins))
                .ForMember(dest => dest.TotalSupportEmployees, opt => opt.MapFrom(src => src.TotalSupportEmployees))
                .ForMember(dest => dest.SystemStatus, opt => opt.MapFrom(src => "Healthy"))
                .ForMember(dest => dest.UserGrowthData, opt => opt.Ignore())
                .ForMember(dest => dest.ProductGrowthData, opt => opt.Ignore())
                .ForMember(dest => dest.CategoryDistribution, opt => opt.Ignore())
                .ForMember(dest => dest.RoleDistribution, opt => opt.Ignore())
                .ForMember(dest => dest.SystemUptime, opt => opt.MapFrom(src => 99.9))
                .ForMember(dest => dest.LastSystemCheck, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.RecentActivities, opt => opt.Ignore())
                .ForMember(dest => dest.RecentProducts, opt => opt.Ignore())
                .ForMember(dest => dest.RecentUsers, opt => opt.Ignore())
                .ForMember(dest => dest.RecentNotifications, opt => opt.Ignore())
                .ForMember(dest => dest.AverageResponseTime, opt => opt.MapFrom(src => 150.0))
                .ForMember(dest => dest.TotalRequests, opt => opt.MapFrom(src => 10000))
                .ForMember(dest => dest.ErrorCount, opt => opt.MapFrom(src => 5))
                .ForMember(dest => dest.ErrorRate, opt => opt.MapFrom(src => 0.05))
                .ForMember(dest => dest.TotalFarmers, opt => opt.Ignore())
                .ForMember(dest => dest.TotalAdmins, opt => opt.Ignore())
                .ForMember(dest => dest.TotalSupportEmployees, opt => opt.Ignore())
                .ForMember(dest => dest.ActiveUsersToday, opt => opt.Ignore())
                .ForMember(dest => dest.ProductsToday, opt => opt.Ignore())
                .ForMember(dest => dest.MostPopularCategory, opt => opt.Ignore())
                .ForMember(dest => dest.TotalNotifications, opt => opt.Ignore())
                .ForMember(dest => dest.UnreadNotifications, opt => opt.Ignore())
                .ForMember(dest => dest.NotificationsToday, opt => opt.Ignore());
        }
    }
}