using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Week3_EmployeeManagementAPI.Models
{
    public class Department
    {
        [Key][DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DepartmentId { get; set; }

        [Required][MaxLength(100)]
        public string DepartmentName { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Description { get; set; }

        [JsonIgnore]
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}
