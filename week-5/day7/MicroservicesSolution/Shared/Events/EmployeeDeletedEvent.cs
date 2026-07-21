namespace Shared.Events
{
    /// <summary>
    /// Published by EmployeeService when an employee is deleted.
    /// </summary>
    public class EmployeeDeletedEvent : BaseEvent
    {
        public EmployeeDeletedEvent()
        {
            EventType = nameof(EmployeeDeletedEvent);
        }

        public int    EmployeeId   { get; init; }
        public string Email        { get; init; } = string.Empty;
        public int    DepartmentId { get; init; }
        public DateTime DeletedAt  { get; init; } = DateTime.UtcNow;
    }
}
