namespace Day6_Migrations.Models
{
    public class Employee
    {
        public int Id { get; set; }                        // Primary Key (auto)
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public DateTime JoinedDate { get; set; }

        // Foreign Key
        public int DepartmentId { get; set; }
        public Department? Dept { get; set; }              // Navigation property
    }
}
