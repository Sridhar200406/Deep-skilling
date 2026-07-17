using DepartmentService.Interfaces;
using Messaging.Consumer;
using Shared.Events;

namespace DepartmentService.EventHandlers
{
    /// <summary>
    /// Reacts to EmployeeDeletedEvent:
    /// Decrements the EmployeeCount on the relevant department.
    /// </summary>
    public class EmployeeDeletedEventHandler : IEventHandler<EmployeeDeletedEvent>
    {
        private readonly IDepartmentRepository _repo;
        private readonly ILogger<EmployeeDeletedEventHandler> _logger;

        public EmployeeDeletedEventHandler(IDepartmentRepository repo,
            ILogger<EmployeeDeletedEventHandler> logger)
        { _repo = repo; _logger = logger; }

        public async Task HandleAsync(EmployeeDeletedEvent @event, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[EmployeeDeleted] EmployeeId={Id} DeptId={DeptId}",
                @event.EmployeeId, @event.DepartmentId);

            var dept = await _repo.GetByIdAsync(@event.DepartmentId);
            if (dept == null)
            {
                _logger.LogWarning("[EmployeeDeleted] Department {Id} not found — skipping.", @event.DepartmentId);
                return;
            }

            dept.EmployeeCount = Math.Max(0, dept.EmployeeCount - 1);
            await _repo.UpdateAsync(dept);

            _logger.LogInformation("[EmployeeDeleted] Department {Name} EmployeeCount = {Count}",
                dept.Name, dept.EmployeeCount);
        }
    }
}
