using System.ComponentModel.DataAnnotations;

namespace DepartmentService.Models
{
    public class Department
    {
        [Key] public int DepartmentId { get; set; }
        [Required][MaxLength(100)] public string Name { get; set; } = string.Empty;
        [MaxLength(500)] public string? Description { get; set; }
        [MaxLength(100)] public string? Location    { get; set; }
        public bool     IsActive      { get; set; } = true;
        public DateTime CreatedAt     { get; set; } = DateTime.UtcNow;
        public int      EmployeeCount { get; set; } = 0;  // maintained by consumer
    }
}
