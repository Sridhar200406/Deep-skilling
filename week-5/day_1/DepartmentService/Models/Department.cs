using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DepartmentService.Models
{
    public class Department
    {
        [Key][DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DepartmentId { get; set; }

        [Required][MaxLength(100)]
        public string DepartmentName { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Description { get; set; }
    }
}
