namespace Week3_EmployeeManagementAPI.DTOs
{
    /// <summary>
    /// DTO returned by GET /api/employees and GET /api/employees/{id}.
    /// This is what the client sees — never the raw Employee entity.
    /// 
    /// Differences from Employee entity:
    ///  - No EF Core navigation collection (no circular reference risk).
    ///  - DepartmentName flattened directly onto the object (no nested Department object).
    ///  - FullName computed property added as a convenience.
    ///  - No data annotation attributes (read-only, never validated as input).
    /// </summary>
    public class EmployeeReadDto
    {
        public int EmployeeId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        /// <summary>Computed: FirstName + " " + LastName — mapped in EmployeeProfile.</summary>
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Position { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public DateTime HireDate { get; set; }
        public bool IsActive { get; set; }

        // Flattened from Department navigation property
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
    }
}
