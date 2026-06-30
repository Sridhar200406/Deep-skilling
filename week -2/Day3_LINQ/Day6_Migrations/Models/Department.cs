namespace Day6_Migrations.Models
{
    public class Department
    {
        public int Id { get; set; }                        // Primary Key
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;

        // Navigation property - one dept has many employees
        public List<Employee> Employees { get; set; } = new();
    }
}
