namespace Week3_EmployeeManagementAPI.Responses
{
    /// <summary>
    /// Standardized API response wrapper used by every endpoint.
    /// Every response — success or error — has the same outer shape:
    /// {
    ///   "Success": true/false,
    ///   "StatusCode": 200/400/404/500,
    ///   "Message": "...",
    ///   "Data": { ... } or null,
    ///   "Errors": [ ... ] or null
    /// }
    /// This makes client-side handling predictable — the client always knows
    /// exactly where to find the data, the status, and any error details.
    /// </summary>
    /// <typeparam name="T">The payload type (EmployeeReadDto, List&lt;EmployeeReadDto&gt;, etc.)</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>True when the operation succeeded (2xx), false otherwise.</summary>
        public bool Success { get; set; }

        /// <summary>HTTP status code mirrored in the body for client convenience.</summary>
        public int StatusCode { get; set; }

        /// <summary>Human-readable description of the result.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>The response payload. Null on error responses.</summary>
        public T? Data { get; set; }

        /// <summary>Validation or business error details. Null on success responses.</summary>
        public IEnumerable<string>? Errors { get; set; }

        // ── Static factory helpers ──────────────────────────────────────

        /// <summary>Creates a 200 OK success response with a data payload.</summary>
        public static ApiResponse<T> SuccessResponse(T data, string message = "Request completed successfully.")
            => new()
            {
                Success    = true,
                StatusCode = StatusCodes.Status200OK,
                Message    = message,
                Data       = data
            };

        /// <summary>Creates a 201 Created success response with a data payload.</summary>
        public static ApiResponse<T> CreatedResponse(T data, string message = "Resource created successfully.")
            => new()
            {
                Success    = true,
                StatusCode = StatusCodes.Status201Created,
                Message    = message,
                Data       = data
            };

        /// <summary>Creates a 404 Not Found error response.</summary>
        public static ApiResponse<T> NotFoundResponse(string message)
            => new()
            {
                Success    = false,
                StatusCode = StatusCodes.Status404NotFound,
                Message    = message
            };

        /// <summary>Creates a 400 Bad Request error response with optional validation errors.</summary>
        public static ApiResponse<T> BadRequestResponse(string message, IEnumerable<string>? errors = null)
            => new()
            {
                Success    = false,
                StatusCode = StatusCodes.Status400BadRequest,
                Message    = message,
                Errors     = errors
            };

        /// <summary>Creates a 500 Internal Server Error response.</summary>
        public static ApiResponse<T> ServerErrorResponse(string message = "An unexpected error occurred.")
            => new()
            {
                Success    = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message    = message
            };
    }

    /// <summary>
    /// Non-generic version for responses without a data payload (e.g. 204 No Content equivalent,
    /// or error responses where T is not needed).
    /// </summary>
    public class ApiResponse : ApiResponse<object>
    {
        /// <summary>Creates a 204-equivalent success with no payload.</summary>
        public static ApiResponse NoContentResponse(string message = "Resource deleted successfully.")
            => new()
            {
                Success    = true,
                StatusCode = StatusCodes.Status204NoContent,
                Message    = message
            };
    }
}
