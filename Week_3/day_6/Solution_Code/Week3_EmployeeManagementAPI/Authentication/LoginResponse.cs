namespace Week3_EmployeeManagementAPI.Authentication
{
    /// <summary>
    /// Response body returned by POST /api/auth/login on successful authentication.
    /// The client must include the Token in the Authorization header of subsequent requests:
    ///   Authorization: Bearer {Token}
    /// </summary>
    public class LoginResponse
    {
        /// <summary>The signed JWT token string.</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>Token type — always "Bearer" for JWT.</summary>
        public string TokenType { get; set; } = "Bearer";

        /// <summary>UTC date/time when the token expires.</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>The authenticated username (for UI display).</summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>The role assigned to this user (e.g. "Admin", "Viewer").</summary>
        public string Role { get; set; } = string.Empty;
    }
}
