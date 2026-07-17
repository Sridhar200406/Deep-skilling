using DepartmentService.Interfaces;
using Messaging.Consumer;
using Shared.Events;

namespace DepartmentService.EventHandlers
{
    /// <summary>
    /// Reacts to EmployeeUpdatedEvent:
    /// If the department changed, adjusts EmployeeCount on both old and new dept.
    /// </summary>
    public class EmployeeUpdatedEventHandler : IEventHandler<EmployeeUpdatedEvent>
    {
        private readonly IDepartmentRepository _repo;
        private readonly ILogger<EmployeeUpdatedEventHandler> _logger;

        public EmployeeUpdatedEventHandler(IDepartmentRepository repo,
            ILogger<EmployeeUpdatedEventHandler> logger)
        { _repo = repo; _logger = logger; }

        public async Task HandleAsync(EmployeeUpdatedEvent @event, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[EmployeeUpdated] EmployeeId={Id} NewDept={NewDept} OldDept={OldDept}",
                @event.EmployeeId, @event.DepartmentId, @event.OldDepartmentId);

            // Department transfer: decrement old, increment new
            if (@event.OldDepartmentId.HasValue && @event.OldDepartmentId != @event.DepartmentId)
            {
                var oldDept = await _repo.GetByIdAsync(@event.OldDepartmentId.Value);
                if (oldDept != null)
                {
                    oldDept.EmployeeCount = Math.Max(0, oldDept.EmployeeCount - 1);
                    await _repo.UpdateAsync(oldDept);
                    _logger.LogInformation("[EmployeeUpdated] Decremented {Name} to {Count}",
                        oldDept.Name, oldDept.EmployeeCount);
                }

                var newDept = await _repo.GetByIdAsync(@event.DepartmentId);
                if (newDept != null)
                {
                    newDept.EmployeeCount++;
                    await _repo.UpdateAsync(newDept);
                    _logger.LogInformation("[EmployeeUpdated] Incremented {Name} to {Count}",
                        newDept.Name, newDept.EmployeeCount);
                }
            }
            else
            {
                _logger.LogInformation("[EmployeeUpdated] No department change — no count update needed.");
            }
        }
    }
}
