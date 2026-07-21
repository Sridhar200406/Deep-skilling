using System.ComponentModel.DataAnnotations;

namespace EmployeeService.DTOs
{
    public class EmployeeUpdateDto
    {
        [Range(1,int.MaxValue)]       public int     EmployeeId { get; set; }
        [Required][StringLength(100)] public string  FirstName  { get; set; } = string.Empty;
        [Required][StringLength(100)] public string  LastName   { get; set; } = string.Empty;
        [Required][EmailAddress]      public string  Email      { get; set; } = string.Empty;
        [Phone][StringLength(20)]     public string? Phone      { get; set; }
        [Required][StringLength(100)] public string  Position   { get; set; } = string.Empty;
        [Range(0,9999999.99)]         public decimal Salary     { get; set; }
        [Required]                    public DateTime HireDate  { get; set; }
        public bool IsActive    { get; set; } = true;
        [Range(1,int.MaxValue)]       public int  DepartmentId { get; set; }
    }
}
