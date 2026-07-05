using System.ComponentModel.DataAnnotations;

namespace Week3_EmployeeManagementAPI.DTOs
{
    /// <summary>DTO for POST /api/employees — unchanged from Day 4.</summary>
    public class EmployeeCreateDto
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 100 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 100 characters.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Phone must be a valid phone number.")]
        [StringLength(20)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Position is required.")]
        [StringLength(100, MinimumLength = 1)]
        public string Position { get; set; } = string.Empty;

        [Range(0, 9999999.99, ErrorMessage = "Salary must be between 0 and 9,999,999.99.")]
        public decimal Salary { get; set; }

        [Required(ErrorMessage = "Hire date is required.")]
        public DateTime HireDate { get; set; }

        public bool IsActive { get; set; } = true;

        [Range(1, int.MaxValue, ErrorMessage = "A valid DepartmentId is required.")]
        public int DepartmentId { get; set; }
    }
}
