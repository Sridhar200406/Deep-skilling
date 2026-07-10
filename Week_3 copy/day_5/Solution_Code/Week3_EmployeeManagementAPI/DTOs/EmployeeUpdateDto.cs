using System.ComponentModel.DataAnnotations;

namespace Week3_EmployeeManagementAPI.DTOs
{
    /// <summary>DTO for PUT /api/employees/{id} — unchanged from Day 4.</summary>
    public class EmployeeUpdateDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "EmployeeId must be a positive integer.")]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, MinimumLength = 1)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, MinimumLength = 1)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Position is required.")]
        [StringLength(100, MinimumLength = 1)]
        public string Position { get; set; } = string.Empty;

        [Range(0, 9999999.99)]
        public decimal Salary { get; set; }

        [Required]
        public DateTime HireDate { get; set; }

        public bool IsActive { get; set; } = true;

        [Range(1, int.MaxValue, ErrorMessage = "A valid DepartmentId is required.")]
        public int DepartmentId { get; set; }
    }
}
