namespace Shared.Events
{
    /// <summary>
    /// Base class for all domain events.
    /// Every event carries a unique ID and UTC timestamp for tracing and idempotency.
    /// </summary>
    public abstract class BaseEvent
    {
        public Guid   EventId        { get; init; } = Guid.NewGuid();
        public string EventType      { get; init; } = string.Empty;
        public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
        public string CorrelationId  { get; init; } = Guid.NewGuid().ToString();
    }
}
