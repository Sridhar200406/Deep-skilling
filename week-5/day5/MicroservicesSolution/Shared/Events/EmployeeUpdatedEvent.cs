namespace Shared.Events
{
    /// <summary>
    /// Published by EmployeeService when an existing employee is updated.
    /// </summary>
    public class EmployeeUpdatedEvent : BaseEvent
    {
        public EmployeeUpdatedEvent()
        {
            EventType = nameof(EmployeeUpdatedEvent);
        }

        public int    EmployeeId      { get; init; }
        public string FirstName       { get; init; } = string.Empty;
        public string LastName        { get; init; } = string.Empty;
        public string Email           { get; init; } = string.Empty;
        public string Position        { get; init; } = string.Empty;
        public decimal Salary         { get; init; }
        public int    DepartmentId    { get; init; }
        public int?   OldDepartmentId { get; init; }  // previous dept, useful for DepartmentService
        public bool   IsActive        { get; init; }
    }
}
