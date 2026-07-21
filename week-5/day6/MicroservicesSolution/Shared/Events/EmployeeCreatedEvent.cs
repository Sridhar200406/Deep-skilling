namespace Shared.Events
{
    /// <summary>
    /// Published by EmployeeService when a new employee is created.
    /// Consumed by DepartmentService (to update employee count)
    /// and any other interested microservice.
    /// </summary>
    public class EmployeeCreatedEvent : BaseEvent
    {
        public EmployeeCreatedEvent()
        {
            EventType = nameof(EmployeeCreatedEvent);
        }

        public int    EmployeeId   { get; init; }
        public string FirstName    { get; init; } = string.Empty;
        public string LastName     { get; init; } = string.Empty;
        public string Email        { get; init; } = string.Empty;
        public string Position     { get; init; } = string.Empty;
        public decimal Salary      { get; init; }
        public int    DepartmentId { get; init; }
        public DateTime HireDate   { get; init; }
    }
}
