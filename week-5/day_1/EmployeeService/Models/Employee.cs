using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeService.Models
{
    /// <summary>
    /// Employee entity — owns employee data in the EmployeeService microservice.
    /// DepartmentId is stored as a simple integer (no navigation property since
    /// departments live in a separate microservice).
    /// </summary>
    public class Employee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EmployeeId { get; set; }

        [Required][MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required][MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required][EmailAddress][MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Phone][MaxLength(20)]
        public string? Phone { get; set; }

        [Required][MaxLength(100)]
        public string Position { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Salary { get; set; }

        public DateTime HireDate { get; set; }
        public bool IsActive { get; set; } = true;
        public int DepartmentId { get; set; }
    }
}
