using Shared.Events;

namespace Messaging.Consumer
{
    /// <summary>
    /// Contract for handling a specific event type.
    /// Each handler is registered in DI and invoked by the RabbitMQConsumer
    /// when a matching message arrives.
    /// </summary>
    public interface IEventHandler<in T> where T : BaseEvent
    {
        Task HandleAsync(T @event, CancellationToken cancellationToken = default);
    }
}
