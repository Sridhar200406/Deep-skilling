using System.ComponentModel.DataAnnotations;

namespace Week3_EmployeeManagementAPI.DTOs
{
    public class EmployeeUpdateDto
    {
        [Range(1, int.MaxValue)]
        public int EmployeeId { get; set; }

        [Required][StringLength(100, MinimumLength = 1)]
        public string FirstName { get; set; } = string.Empty;

        [Required][StringLength(100, MinimumLength = 1)]
        public string LastName { get; set; } = string.Empty;

        [Required][EmailAddress][StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [Phone][StringLength(20)]
        public string? Phone { get; set; }

        [Required][StringLength(100, MinimumLength = 1)]
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
