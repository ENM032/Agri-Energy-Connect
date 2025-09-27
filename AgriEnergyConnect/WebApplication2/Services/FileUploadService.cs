using System.Security.Cryptography;
using System.Text;

namespace WebApplication2.Services
{
    /// <summary>
    /// File upload service implementation
    /// </summary>
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileUploadService> _logger;
        private readonly string _uploadsPath;
        
        // Allowed file extensions for different types
        private readonly string[] _imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
        private readonly string[] _documentExtensions = { ".pdf", ".doc", ".docx", ".txt", ".csv", ".xlsx" };
        
        public FileUploadService(IWebHostEnvironment environment, ILogger<FileUploadService> logger)
        {
            _environment = environment;
            _logger = logger;
            _uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
            
            // Ensure uploads directory exists
            if (!Directory.Exists(_uploadsPath))
            {
                Directory.CreateDirectory(_uploadsPath);
            }
        }
        
        public async Task<string> UploadFileAsync(IFormFile file, string folder = "uploads")
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    throw new ArgumentException("File is null or empty");
                }
                
                // Validate file
                var validation = ValidateFile(file, _imageExtensions.Concat(_documentExtensions).ToArray());
                if (!validation.IsValid)
                {
                    throw new ArgumentException($"File validation failed: {validation.ErrorMessage}");
                }
                
                // Create folder if it doesn't exist
                var folderPath = Path.Combine(_uploadsPath, folder);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                
                // Generate unique filename
                var fileName = GenerateUniqueFileName(file.FileName);
                var filePath = Path.Combine(folderPath, fileName);
                
                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                
                // Return relative path
                var relativePath = Path.Combine("uploads", folder, fileName).Replace("\\", "/");
                
                _logger.LogInformation("File uploaded successfully: {FileName} to {RelativePath}", file.FileName, relativePath);
                
                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file: {FileName}", file?.FileName);
                throw;
            }
        }
        
        public async Task<List<string>> UploadFilesAsync(IList<IFormFile> files, string folder = "uploads")
        {
            var uploadedFiles = new List<string>();
            
            foreach (var file in files)
            {
                try
                {
                    var filePath = await UploadFileAsync(file, folder);
                    uploadedFiles.Add(filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to upload file in batch: {FileName}", file.FileName);
                    // Continue with other files
                }
            }
            
            return uploadedFiles;
        }
        
        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    return false;
                }
                
                var physicalPath = GetPhysicalPath(filePath);
                
                if (File.Exists(physicalPath))
                {
                    await Task.Run(() => File.Delete(physicalPath));
                    _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file: {FilePath}", filePath);
                return false;
            }
        }
        
        public FileValidationResult ValidateFile(IFormFile file, string[] allowedExtensions, int maxSizeInMB = 5)
        {
            var result = new FileValidationResult { IsValid = true };
            var errors = new List<string>();
            
            if (file == null)
            {
                errors.Add("File is required");
            }
            else
            {
                // Check file size
                var maxSizeInBytes = maxSizeInMB * 1024 * 1024;
                if (file.Length > maxSizeInBytes)
                {
                    errors.Add($"File size exceeds {maxSizeInMB}MB limit");
                }
                
                if (file.Length == 0)
                {
                    errors.Add("File is empty");
                }
                
                // Check file extension
                var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
                if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                {
                    errors.Add($"File type not allowed. Allowed types: {string.Join(", ", allowedExtensions)}");
                }
                
                // Check for potentially dangerous filenames
                if (file.FileName.Contains("..") || file.FileName.Contains("/") || file.FileName.Contains("\\"))
                {
                    errors.Add("Invalid filename");
                }
            }
            
            if (errors.Any())
            {
                result.IsValid = false;
                result.Errors = errors;
                result.ErrorMessage = string.Join("; ", errors);
            }
            
            return result;
        }
        
        public string GetPhysicalPath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return string.Empty;
            }
            
            // Normalize path separators
            relativePath = relativePath.Replace("/", "\\");
            
            return Path.Combine(_environment.WebRootPath, relativePath);
        }
        
        /// <summary>
        /// Generate a unique filename to prevent conflicts
        /// </summary>
        /// <param name="originalFileName">Original filename</param>
        /// <returns>Unique filename</returns>
        private string GenerateUniqueFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            
            // Sanitize filename
            nameWithoutExtension = SanitizeFileName(nameWithoutExtension);
            
            // Generate unique identifier
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var randomBytes = new byte[4];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            var randomString = Convert.ToHexString(randomBytes).ToLowerInvariant();
            
            return $"{nameWithoutExtension}_{timestamp}_{randomString}{extension}";
        }
        
        /// <summary>
        /// Sanitize filename by removing invalid characters
        /// </summary>
        /// <param name="fileName">Original filename</param>
        /// <returns>Sanitized filename</returns>
        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new StringBuilder();
            
            foreach (var c in fileName)
            {
                if (!invalidChars.Contains(c) && c != ' ')
                {
                    sanitized.Append(c);
                }
                else if (c == ' ')
                {
                    sanitized.Append('_');
                }
            }
            
            return sanitized.ToString().Trim('_');
        }
    }
}