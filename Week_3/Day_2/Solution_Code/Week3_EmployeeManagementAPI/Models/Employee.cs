using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Week3_EmployeeManagementAPI.Models
{
    /// <summary>
    /// Represents an employee in the organization.
    /// Validation attributes drive both EF Core schema and automatic [ApiController] model validation.
    /// </summary>
    public class Employee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Phone must be a valid phone number.")]
        [MaxLength(20)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Position is required.")]
        [MaxLength(100)]
        public string Position { get; set; } = string.Empty;

        [Range(0, 9999999.99, ErrorMessage = "Salary must be between 0 and 9,999,999.99.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Salary { get; set; }

        public DateTime HireDate { get; set; }

        public bool IsActive { get; set; } = true;

        // Foreign key — must reference a valid Department
        [Range(1, int.MaxValue, ErrorMessage = "A valid DepartmentId is required.")]
        public int DepartmentId { get; set; }

        // Navigation property
        public Department? Department { get; set; }
    }
}
