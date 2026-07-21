using System.ComponentModel.DataAnnotations;

namespace DepartmentService.DTOs
{
    public class DepartmentCreateDto
    {
        [Required][StringLength(100)] public string  Name        { get; set; } = string.Empty;
        [StringLength(500)]           public string? Description { get; set; }
        [StringLength(100)]           public string? Location    { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
