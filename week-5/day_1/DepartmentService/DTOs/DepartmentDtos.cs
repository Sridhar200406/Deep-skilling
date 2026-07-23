using System.ComponentModel.DataAnnotations;

namespace DepartmentService.DTOs
{
    public class DepartmentCreateDto
    {
        [Required][StringLength(100, MinimumLength = 1)]
        public string DepartmentName { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Description { get; set; }
    }

    public class DepartmentReadDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class DepartmentUpdateDto
    {
        [Range(1, int.MaxValue)]
        public int DepartmentId { get; set; }

        [Required][StringLength(100, MinimumLength = 1)]
        public string DepartmentName { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Description { get; set; }
    }
}
