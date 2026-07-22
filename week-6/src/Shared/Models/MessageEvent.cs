namespace Shared.Models;

public class EmployeeCreatedEvent
{
    public int EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class EmployeeUpdatedEvent
{
    public int EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class EmployeeDeletedEvent
{
    public int EmployeeId { get; set; }
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
}
