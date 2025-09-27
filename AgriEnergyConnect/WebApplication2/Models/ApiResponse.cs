namespace WebApplication2.Models
{
    /// <summary>
    /// Standard API response wrapper for consistent JSON responses
    /// </summary>
    /// <typeparam name="T">Type of data being returned</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Indicates if the operation was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// The actual data payload
        /// </summary>
        public T? Data { get; set; }
        
        /// <summary>
        /// Human-readable message about the operation
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// List of validation or other errors
        /// </summary>
        public List<string>? Errors { get; set; }
        
        /// <summary>
        /// Timestamp of the response
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Paginated result wrapper for API responses
    /// </summary>
    /// <typeparam name="T">Type of items in the collection</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// The items for the current page
        /// </summary>
        public List<T> Items { get; set; } = new List<T>();
        
        /// <summary>
        /// Total number of items across all pages
        /// </summary>
        public int TotalCount { get; set; }
        
        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        public int Page { get; set; }
        
        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; set; }
        
        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages { get; set; }
        
        /// <summary>
        /// Whether there is a previous page
        /// </summary>
        public bool HasPreviousPage => Page > 1;
        
        /// <summary>
        /// Whether there is a next page
        /// </summary>
        public bool HasNextPage => Page < TotalPages;
    }
    
    /// <summary>
    /// Standard error response for API endpoints
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Error code or identifier
        /// </summary>
        public string Code { get; set; } = string.Empty;
        
        /// <summary>
        /// Human-readable error message
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Additional error details
        /// </summary>
        public object? Details { get; set; }
        
        /// <summary>
        /// Timestamp when the error occurred
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}