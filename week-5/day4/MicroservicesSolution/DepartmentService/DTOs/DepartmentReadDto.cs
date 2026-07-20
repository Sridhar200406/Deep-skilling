namespace DepartmentService.DTOs
{
    public class DepartmentReadDto
    {
        public int     DepartmentId  { get; set; }
        public string  Name          { get; set; } = string.Empty;
        public string? Description   { get; set; }
        public string? Location      { get; set; }
        public bool    IsActive      { get; set; }
        public DateTime CreatedAt    { get; set; }
        public int     EmployeeCount { get; set; }
    }
}
