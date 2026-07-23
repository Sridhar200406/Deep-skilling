namespace Shared.Models;

// ─── Interface ────────────────────────────────────────────────────────────────

/// <summary>
/// Marker interface for all employee domain events.
/// Enables generic handling in publishers and consumers.
/// </summary>
public interface IEmployeeEvent
{
    int EmployeeId { get; }
    string EventType { get; }
    string CorrelationId { get; }
    DateTime OccurredAt { get; }
}

// ─── Service Bus Message Envelope ────────────────────────────────────────────

/// <summary>
/// Wraps any event with routing and tracing metadata.
/// Published as the Service Bus message body.
/// </summary>
public class ServiceBusMessageEnvelope<T> where T : IEmployeeEvent
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = typeof(T).Name;
    public string Source { get; set; } = "EmployeeService";
    public string Version { get; set; } = "1.0";
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public T Payload { get; set; } = default!;
}

// ─── Employee Domain Events ───────────────────────────────────────────────────

/// <summary>
/// Published when a new employee is created.
/// Topic: employee-events
/// Subscriptions: email-subscription, audit-subscription, cache-subscription
/// </summary>
public class EmployeeCreatedEvent : IEmployeeEvent
{
    public int EmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public decimal Salary { get; set; }
    public DateTime HireDate { get; set; }
    public string EventType => "EmployeeCreated";
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Published when an employee profile is updated.
/// Topic: employee-events
/// </summary>
public class EmployeeUpdatedEvent : IEmployeeEvent
{
    public int EmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public decimal Salary { get; set; }
    public bool IsActive { get; set; }
    public string? ChangedBy { get; set; }
    public string EventType => "EmployeeUpdated";
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Published when an employee is soft-deleted (deactivated).
/// Topic: employee-events
/// </summary>
public class EmployeeDeletedEvent : IEmployeeEvent
{
    public int EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DeletedBy { get; set; }
    public string? Reason { get; set; }
    public string EventType => "EmployeeDeleted";
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Published when a document is uploaded for an employee.
/// Topic: employee-events
/// </summary>
public class EmployeeDocumentUploadedEvent : IEmployeeEvent
{
    public int EmployeeId { get; set; }
    public int DocumentId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string BlobName { get; set; } = string.Empty;
    public string EventType => "EmployeeDocumentUploaded";
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

// ─── Service Bus Configuration ────────────────────────────────────────────────

/// <summary>
/// Centralises all Service Bus topic/subscription/queue names.
/// Both publisher and consumer reference the same constants to avoid typos.
/// </summary>
public static class ServiceBusConstants
{
    // Topic — all employee events flow through this single topic
    public const string EmployeeEventsTopic = "employee-events";

    // Subscriptions — each subscriber gets its own copy of every message
    public const string EmailSubscription   = "email-subscription";
    public const string AuditSubscription   = "audit-subscription";
    public const string CacheSubscription   = "cache-subscription";
    public const string CleanupSubscription = "cleanup-subscription";

    // Queue — direct point-to-point (no fan-out needed)
    public const string DeadLetterProcessingQueue = "dead-letter-processing";

    // Message property keys
    public const string EventTypeProperty    = "EventType";
    public const string CorrelationIdProperty = "CorrelationId";
    public const string SourceProperty       = "Source";
    public const string VersionProperty      = "Version";
}
