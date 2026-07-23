namespace EmployeeService.Responses
{
    /// <summary>Standardized API response wrapper.</summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public IEnumerable<string>? Errors { get; set; }

        public static ApiResponse<T> SuccessResponse(T data, string message = "Request completed successfully.")
            => new() { Success = true, StatusCode = StatusCodes.Status200OK, Message = message, Data = data };

        public static ApiResponse<T> CreatedResponse(T data, string message = "Resource created successfully.")
            => new() { Success = true, StatusCode = StatusCodes.Status201Created, Message = message, Data = data };

        public static ApiResponse<T> NotFoundResponse(string message)
            => new() { Success = false, StatusCode = StatusCodes.Status404NotFound, Message = message };

        public static ApiResponse<T> BadRequestResponse(string message, IEnumerable<string>? errors = null)
            => new() { Success = false, StatusCode = StatusCodes.Status400BadRequest, Message = message, Errors = errors };

        public static ApiResponse<T> ServerErrorResponse(string message = "An unexpected error occurred.")
            => new() { Success = false, StatusCode = StatusCodes.Status500InternalServerError, Message = message };
    }

    /// <summary>Non-generic convenience class.</summary>
    public class ApiResponse : ApiResponse<object>
    {
        public static ApiResponse NoContentResponse(string message = "Resource deleted successfully.")
            => new() { Success = true, StatusCode = StatusCodes.Status204NoContent, Message = message };
    }
}
