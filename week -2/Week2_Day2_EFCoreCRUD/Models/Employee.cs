namespace Week2_Day2_EFCoreCRUD.Models
{
    /// <summary>
    /// Represents an Employee entity mapped to the Employees table in the database.
    /// </summary>
    public class Employee
    {
        // Primary key – EF Core auto-increments this
        public int EmployeeId { get; set; }

        // Full name of the employee
        public string Name { get; set; } = string.Empty;

        // Department the employee belongs to
        public string Department { get; set; } = string.Empty;

        // Monthly or annual salary
        public decimal Salary { get; set; }
    }
}
