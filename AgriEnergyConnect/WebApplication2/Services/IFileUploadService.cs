namespace WebApplication2.Services
{
    /// <summary>
    /// Interface for file upload functionality
    /// </summary>
    public interface IFileUploadService
    {
        /// <summary>
        /// Upload a file and return the file path
        /// </summary>
        /// <param name="file">The file to upload</param>
        /// <param name="folder">The folder to upload to (e.g., "products", "profiles")</param>
        /// <returns>The relative path to the uploaded file</returns>
        Task<string> UploadFileAsync(IFormFile file, string folder = "uploads");
        
        /// <summary>
        /// Upload multiple files
        /// </summary>
        /// <param name="files">The files to upload</param>
        /// <param name="folder">The folder to upload to</param>
        /// <returns>List of relative paths to the uploaded files</returns>
        Task<List<string>> UploadFilesAsync(IList<IFormFile> files, string folder = "uploads");
        
        /// <summary>
        /// Delete a file
        /// </summary>
        /// <param name="filePath">The relative path to the file</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteFileAsync(string filePath);
        
        /// <summary>
        /// Validate file type and size
        /// </summary>
        /// <param name="file">The file to validate</param>
        /// <param name="allowedExtensions">Allowed file extensions</param>
        /// <param name="maxSizeInMB">Maximum file size in MB</param>
        /// <returns>Validation result</returns>
        FileValidationResult ValidateFile(IFormFile file, string[] allowedExtensions, int maxSizeInMB = 5);
        
        /// <summary>
        /// Get the full physical path for a relative file path
        /// </summary>
        /// <param name="relativePath">The relative file path</param>
        /// <returns>The full physical path</returns>
        string GetPhysicalPath(string relativePath);
    }
    
    /// <summary>
    /// File validation result
    /// </summary>
    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new List<string>();
    }
}