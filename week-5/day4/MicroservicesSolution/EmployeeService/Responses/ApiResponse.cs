namespace EmployeeService.Responses
{
    public class ApiResponse<T>
    {
        public bool    Success    { get; set; }
        public int     StatusCode { get; set; }
        public string  Message    { get; set; } = string.Empty;
        public T?      Data       { get; set; }
        public IEnumerable<string>? Errors { get; set; }

        public static ApiResponse<T> SuccessResponse(T data, string message = "OK")
            => new() { Success = true,  StatusCode = 200, Message = message, Data = data };
        public static ApiResponse<T> CreatedResponse(T data, string message = "Created")
            => new() { Success = true,  StatusCode = 201, Message = message, Data = data };
        public static ApiResponse<T> NotFoundResponse(string message)
            => new() { Success = false, StatusCode = 404, Message = message };
        public static ApiResponse<T> BadRequestResponse(string message, IEnumerable<string>? errors = null)
            => new() { Success = false, StatusCode = 400, Message = message, Errors = errors };
    }
}
