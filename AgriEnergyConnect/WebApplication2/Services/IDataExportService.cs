using WebApplication2.Models;
using WebApplication2.Areas.Identity.Data;
using WebApplication2.Models.DTOs;

namespace WebApplication2.Services
{
    public interface IDataExportService
    {
        /// <summary>
        /// Exports products data to CSV format
        /// </summary>
        /// <param name="products">List of products to export</param>
        /// <returns>CSV content as byte array</returns>
        Task<byte[]> ExportProductsToCsvAsync(IEnumerable<Product> products);

        /// <summary>
        /// Exports products data to Excel format
        /// </summary>
        /// <param name="products">List of products to export</param>
        /// <returns>Excel content as byte array</returns>
        Task<byte[]> ExportProductsToExcelAsync(IEnumerable<Product> products);

        /// <summary>
        /// Exports users data to CSV format (Admin only)
        /// </summary>
        /// <param name="users">List of users to export</param>
        /// <returns>CSV content as byte array</returns>
        Task<byte[]> ExportUsersToCsvAsync(IEnumerable<WebApplication2User> users);

        /// <summary>
        /// Exports users data to Excel format (Admin only)
        /// </summary>
        /// <param name="users">List of users to export</param>
        /// <returns>Excel content as byte array</returns>
        Task<byte[]> ExportUsersToExcelAsync(IEnumerable<WebApplication2User> users);

        /// <summary>
        /// Exports analytics data to CSV format
        /// </summary>
        /// <param name="analyticsData">Analytics data to export</param>
        /// <returns>CSV content as byte array</returns>
        Task<byte[]> ExportAnalyticsToCsvAsync(AnalyticsDto analyticsData);

        /// <summary>
        /// Exports analytics data to Excel format
        /// </summary>
        /// <param name="analyticsData">Analytics data to export</param>
        /// <returns>Excel content as byte array</returns>
        Task<byte[]> ExportAnalyticsToExcelAsync(AnalyticsDto analyticsData);

        /// <summary>
        /// Gets the appropriate MIME type for the export format
        /// </summary>
        /// <param name="format">Export format (csv or excel)</param>
        /// <returns>MIME type string</returns>
        string GetMimeType(string format);

        /// <summary>
        /// Gets the appropriate file extension for the export format
        /// </summary>
        /// <param name="format">Export format (csv or excel)</param>
        /// <returns>File extension string</returns>
        string GetFileExtension(string format);
    }
}