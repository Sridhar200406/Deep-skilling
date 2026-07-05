using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Week3_EmployeeManagementAPI.Models
{
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
        public Department? Department { get; set; }
    }
}
