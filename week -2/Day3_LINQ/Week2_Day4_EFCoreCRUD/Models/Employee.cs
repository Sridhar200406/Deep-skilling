using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Week2_Day4_EFCoreCRUD.Models
{
    /// <summary>
    /// Employee entity — maps to the Employees table in the database.
    /// Same model as Day 2. Do NOT modify.
    /// </summary>
    public class Employee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EmployeeId { get; set; }          // Primary Key (auto-increment)

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Department is required.")]
        [MaxLength(50, ErrorMessage = "Department cannot exceed 50 characters.")]
        public string Department { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Salary must be a positive value.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Salary { get; set; }
    }
}
