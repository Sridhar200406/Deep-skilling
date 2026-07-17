using System.ComponentModel.DataAnnotations;

namespace DepartmentService.Models
{
    /// <summary>
    /// Department entity. EmployeeIds are NOT stored here —
    /// the EmployeeService owns that foreign key.
    /// </summary>
    public class Department
    {
        [Key]
        public int DepartmentId { get; set; }

        [Required][MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? Location { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
