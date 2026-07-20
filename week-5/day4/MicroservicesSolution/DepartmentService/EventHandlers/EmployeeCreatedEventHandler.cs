using DepartmentService.Interfaces;
using Messaging.Consumer;
using Shared.Events;

namespace DepartmentService.EventHandlers
{
    /// <summary>
    /// Reacts to EmployeeCreatedEvent:
    /// Increments the EmployeeCount on the relevant department.
    /// </summary>
    public class EmployeeCreatedEventHandler : IEventHandler<EmployeeCreatedEvent>
    {
        private readonly IDepartmentRepository _repo;
        private readonly ILogger<EmployeeCreatedEventHandler> _logger;

        public EmployeeCreatedEventHandler(IDepartmentRepository repo,
            ILogger<EmployeeCreatedEventHandler> logger)
        { _repo = repo; _logger = logger; }

        public async Task HandleAsync(EmployeeCreatedEvent @event, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[EmployeeCreated] EmployeeId={Id} DeptId={DeptId} Email={Email}",
                @event.EmployeeId, @event.DepartmentId, @event.Email);

            var dept = await _repo.GetByIdAsync(@event.DepartmentId);
            if (dept == null)
            {
                _logger.LogWarning("[EmployeeCreated] Department {Id} not found — skipping count update.", @event.DepartmentId);
                return;
            }

            dept.EmployeeCount++;
            await _repo.UpdateAsync(dept);

            _logger.LogInformation(
                "[EmployeeCreated] Department {Name} EmployeeCount updated to {Count}",
                dept.Name, dept.EmployeeCount);
        }
    }
}
