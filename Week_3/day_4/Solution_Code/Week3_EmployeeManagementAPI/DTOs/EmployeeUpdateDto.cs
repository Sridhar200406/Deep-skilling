using System.ComponentModel.DataAnnotations;

namespace Week3_EmployeeManagementAPI.DTOs
{
    /// <summary>
    /// DTO used for PUT /api/employees/{id}.
    /// Includes EmployeeId so the service can verify the route ID matches the body.
    /// Same validation rules as EmployeeCreateDto — all fields must be supplied (full update).
    /// </summary>
    public class EmployeeUpdateDto
    {
        // Included so the controller can verify route id == body id
        [Range(1, int.MaxValue, ErrorMessage = "EmployeeId must be a positive integer.")]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 100 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 100 characters.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
        [StringLength(200, ErrorMessage = "Email cannot exceed 200 characters.")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Phone must be a valid phone number.")]
        [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters.")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Position is required.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Position must be between 1 and 100 characters.")]
        public string Position { get; set; } = string.Empty;

        [Range(0, 9999999.99, ErrorMessage = "Salary must be between 0 and 9,999,999.99.")]
        public decimal Salary { get; set; }

        [Required(ErrorMessage = "Hire date is required.")]
        public DateTime HireDate { get; set; }

        public bool IsActive { get; set; } = true;

        [Range(1, int.MaxValue, ErrorMessage = "A valid DepartmentId is required (must be >= 1).")]
        public int DepartmentId { get; set; }
    }
}
