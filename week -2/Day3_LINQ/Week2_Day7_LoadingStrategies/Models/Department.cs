namespace Week2_Day7_LoadingStrategies.Models
{
    /// <summary>
    /// Department entity — maps to the Departments table.
    /// Unchanged from Day 6. Virtual keyword added to Employees
    /// collection to support Lazy Loading via EF Core Proxies.
    /// </summary>
    public class Department
    {
        public int    Id       { get; set; }
        public string Name     { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;

        // virtual → required for Lazy Loading (EF Core Proxies)
        // Without virtual, the proxy cannot intercept the property access.
        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}
