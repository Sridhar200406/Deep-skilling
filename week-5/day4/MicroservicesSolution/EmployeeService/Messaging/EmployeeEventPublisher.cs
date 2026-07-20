using Messaging.Producer;
using Microsoft.Extensions.Logging;
using Shared.Events;

namespace EmployeeService.Messaging
{
    /// <summary>
    /// Wraps RabbitMQProducer and provides strongly-typed publish methods
    /// for all three employee domain events.
    /// Injected into EmployeeBusinessService via DI.
    /// </summary>
    public class EmployeeEventPublisher
    {
        private readonly RabbitMQProducer _producer;
        private readonly ILogger<EmployeeEventPublisher> _logger;

        public EmployeeEventPublisher(RabbitMQProducer producer, ILogger<EmployeeEventPublisher> logger)
        {
            _producer = producer;
            _logger   = logger;
        }

        public void PublishEmployeeCreated(EmployeeCreatedEvent @event)
        {
            _logger.LogInformation("Publishing EmployeeCreatedEvent for EmployeeId={Id}", @event.EmployeeId);
            _producer.Publish(@event, routingKey: "employee.created");
        }

        public void PublishEmployeeUpdated(EmployeeUpdatedEvent @event)
        {
            _logger.LogInformation("Publishing EmployeeUpdatedEvent for EmployeeId={Id}", @event.EmployeeId);
            _producer.Publish(@event, routingKey: "employee.updated");
        }

        public void PublishEmployeeDeleted(EmployeeDeletedEvent @event)
        {
            _logger.LogInformation("Publishing EmployeeDeletedEvent for EmployeeId={Id}", @event.EmployeeId);
            _producer.Publish(@event, routingKey: "employee.deleted");
        }
    }
}
