using System.Text;
using System.Globalization;
using OfficeOpenXml;
using WebApplication2.Models;
using WebApplication2.Areas.Identity.Data;
using WebApplication2.Models.DTOs;

namespace WebApplication2.Services
{
    public class DataExportService : IDataExportService
    {
        private readonly ILogger<DataExportService> _logger;

        public DataExportService(ILogger<DataExportService> logger)
        {
            _logger = logger;
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<byte[]> ExportProductsToCsvAsync(IEnumerable<Product> products)
        {
            try
            {
                var csv = new StringBuilder();
                
                // Add header
                csv.AppendLine("ID,Name,Category,Product Date,User ID,Image Path,Image File Name,Created Date");
                
                // Add data rows
                foreach (var product in products)
                {
                    csv.AppendLine($"{EscapeCsvField(product.Id.ToString())}," +
                                 $"{EscapeCsvField(product.Name)}," +
                                 $"{EscapeCsvField(product.Category)}," +
                                 $"{EscapeCsvField(product.ProductDate.ToString("yyyy-MM-dd"))}," +
                                 $"{EscapeCsvField(product.UserId ?? "")}," +
                                 $"{EscapeCsvField(product.ImagePath ?? "")}," +
                                 $"{EscapeCsvField(product.ImageFileName ?? "")}," +
                                 $"{EscapeCsvField(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))}");
                }
                
                return Encoding.UTF8.GetBytes(csv.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting products to CSV");
                throw;
            }
        }

        public async Task<byte[]> ExportProductsToExcelAsync(IEnumerable<Product> products)
        {
            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Products");
                
                // Add headers
                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Name";
                worksheet.Cells[1, 3].Value = "Category";
                worksheet.Cells[1, 4].Value = "Product Date";
                worksheet.Cells[1, 5].Value = "User ID";
                worksheet.Cells[1, 6].Value = "Image Path";
                worksheet.Cells[1, 7].Value = "Image File Name";
                worksheet.Cells[1, 8].Value = "Export Date";
                
                // Style headers
                using (var range = worksheet.Cells[1, 1, 1, 8])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }
                
                // Add data
                int row = 2;
                foreach (var product in products)
                {
                    worksheet.Cells[row, 1].Value = product.Id;
                    worksheet.Cells[row, 2].Value = product.Name;
                    worksheet.Cells[row, 3].Value = product.Category;
                    worksheet.Cells[row, 4].Value = product.ProductDate.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 5].Value = product.UserId;
                    worksheet.Cells[row, 6].Value = product.ImagePath;
                    worksheet.Cells[row, 7].Value = product.ImageFileName;
                    worksheet.Cells[row, 8].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    row++;
                }
                
                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();
                
                return await package.GetAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting products to Excel");
                throw;
            }
        }

        public async Task<byte[]> ExportUsersToCsvAsync(IEnumerable<WebApplication2User> users)
        {
            try
            {
                var csv = new StringBuilder();
                
                // Add header
                csv.AppendLine("ID,User Name,Email,Email Confirmed,Phone Number,Lockout Enabled,Access Failed Count,Created Date");
                
                // Add data rows
                foreach (var user in users)
                {
                    csv.AppendLine($"{EscapeCsvField(user.Id)}," +
                                 $"{EscapeCsvField(user.UserName ?? "")}," +
                                 $"{EscapeCsvField(user.Email ?? "")}," +
                                 $"{EscapeCsvField(user.EmailConfirmed.ToString())}," +
                                 $"{EscapeCsvField(user.PhoneNumber ?? "")}," +
                                 $"{EscapeCsvField(user.LockoutEnabled.ToString())}," +
                                 $"{EscapeCsvField(user.AccessFailedCount.ToString())}," +
                                 $"{EscapeCsvField(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))}");
                }
                
                return Encoding.UTF8.GetBytes(csv.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting users to CSV");
                throw;
            }
        }

        public async Task<byte[]> ExportUsersToExcelAsync(IEnumerable<WebApplication2User> users)
        {
            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Users");
                
                // Add headers
                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "User Name";
                worksheet.Cells[1, 3].Value = "Email";
                worksheet.Cells[1, 4].Value = "Email Confirmed";
                worksheet.Cells[1, 5].Value = "Phone Number";
                worksheet.Cells[1, 6].Value = "Lockout Enabled";
                worksheet.Cells[1, 7].Value = "Access Failed Count";
                worksheet.Cells[1, 8].Value = "Export Date";
                
                // Style headers
                using (var range = worksheet.Cells[1, 1, 1, 8])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }
                
                // Add data
                int row = 2;
                foreach (var user in users)
                {
                    worksheet.Cells[row, 1].Value = user.Id;
                    worksheet.Cells[row, 2].Value = user.UserName;
                    worksheet.Cells[row, 3].Value = user.Email;
                    worksheet.Cells[row, 4].Value = user.EmailConfirmed;
                    worksheet.Cells[row, 5].Value = user.PhoneNumber;
                    worksheet.Cells[row, 6].Value = user.LockoutEnabled;
                    worksheet.Cells[row, 7].Value = user.AccessFailedCount;
                    worksheet.Cells[row, 8].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    row++;
                }
                
                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();
                
                return await package.GetAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting users to Excel");
                throw;
            }
        }

        public async Task<byte[]> ExportAnalyticsToCsvAsync(AnalyticsDto analyticsData)
        {
            try
            {
                var csv = new StringBuilder();
                
                // Add summary section
                csv.AppendLine("Analytics Summary");
                csv.AppendLine($"Total Users,{analyticsData.TotalUsers}");
                csv.AppendLine($"Total Products,{analyticsData.TotalProducts}");
                csv.AppendLine($"Active Users,{analyticsData.ActiveUsers}");
                csv.AppendLine($"Products This Month,{analyticsData.ProductsThisMonth}");
                csv.AppendLine($"Export Date,{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                csv.AppendLine();
                
                // Add monthly data if available
                if (analyticsData.MonthlyData?.Any() == true)
                {
                    csv.AppendLine("Monthly Data");
                    csv.AppendLine("Month,Users,Products");
                    foreach (var monthData in analyticsData.MonthlyData)
                    {
                        csv.AppendLine($"{monthData.Month},{monthData.Users},{monthData.Products}");
                    }
                }
                
                return Encoding.UTF8.GetBytes(csv.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting analytics to CSV");
                throw;
            }
        }

        public async Task<byte[]> ExportAnalyticsToExcelAsync(AnalyticsDto analyticsData)
        {
            try
            {
                using var package = new ExcelPackage();
                
                // Summary worksheet
                var summarySheet = package.Workbook.Worksheets.Add("Summary");
                summarySheet.Cells[1, 1].Value = "Metric";
                summarySheet.Cells[1, 2].Value = "Value";
                summarySheet.Cells[2, 1].Value = "Total Users";
                summarySheet.Cells[2, 2].Value = analyticsData.TotalUsers;
                summarySheet.Cells[3, 1].Value = "Total Products";
                summarySheet.Cells[3, 2].Value = analyticsData.TotalProducts;
                summarySheet.Cells[4, 1].Value = "Active Users";
                summarySheet.Cells[4, 2].Value = analyticsData.ActiveUsers;
                summarySheet.Cells[5, 1].Value = "Products This Month";
                summarySheet.Cells[5, 2].Value = analyticsData.ProductsThisMonth;
                summarySheet.Cells[6, 1].Value = "Export Date";
                summarySheet.Cells[6, 2].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                
                // Style summary headers
                using (var range = summarySheet.Cells[1, 1, 1, 2])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }
                
                summarySheet.Cells.AutoFitColumns();
                
                // Monthly data worksheet if available
                if (analyticsData.MonthlyData?.Any() == true)
                {
                    var monthlySheet = package.Workbook.Worksheets.Add("Monthly Data");
                    monthlySheet.Cells[1, 1].Value = "Month";
                    monthlySheet.Cells[1, 2].Value = "Users";
                    monthlySheet.Cells[1, 3].Value = "Products";
                    
                    int row = 2;
                    foreach (var monthData in analyticsData.MonthlyData)
                    {
                        monthlySheet.Cells[row, 1].Value = monthData.Month;
                        monthlySheet.Cells[row, 2].Value = monthData.Users;
                        monthlySheet.Cells[row, 3].Value = monthData.Products;
                        row++;
                    }
                    
                    // Style monthly headers
                    using (var range = monthlySheet.Cells[1, 1, 1, 3])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                    }
                    
                    monthlySheet.Cells.AutoFitColumns();
                }
                
                return await package.GetAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting analytics to Excel");
                throw;
            }
        }

        public string GetMimeType(string format)
        {
            return format.ToLower() switch
            {
                "csv" => "text/csv",
                "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
        }

        public string GetFileExtension(string format)
        {
            return format.ToLower() switch
            {
                "csv" => ".csv",
                "excel" => ".xlsx",
                _ => ".bin"
            };
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";
            
            // If field contains comma, quote, or newline, wrap in quotes and escape quotes
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            {
                return $"\"{field.Replace("\"", "\"\"")}\";";
            }
            
            return field;
        }
    }
}