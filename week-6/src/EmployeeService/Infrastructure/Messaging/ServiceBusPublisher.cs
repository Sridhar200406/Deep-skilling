using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Models;
using System.Text.Json;

namespace EmployeeService.Infrastructure.Messaging;

// ─────────────────────────────────────────────────────────────────────────────
// Service Bus Publisher
//
// Publishes employee domain events to the Azure Service Bus topic.
// Called by EmployeeAppService after every CRUD operation.
//
// Pattern: fire-and-forget — the API response is never blocked by messaging.
// Failures are logged but not propagated to the caller.
//
// In production: connection string loaded from Azure Key Vault
// In development: loaded from appsettings.Development.json
//
// Topic: employee-events
//   └── Subscriptions (fan-out): email, audit, cache, cleanup
// ─────────────────────────────────────────────────────────────────────────────

public interface IServiceBusPublisher
{
    Task PublishAsync<T>(T eventMessage) where T : IEmployeeEvent;
    Task PublishBatchAsync<T>(IEnumerable<T> events) where T : IEmployeeEvent;
}

public class ServiceBusPublisher : IServiceBusPublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private readonly ILogger<ServiceBusPublisher> _logger;
    private readonly string _topicName;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public ServiceBusPublisher(IConfiguration configuration, ILogger<ServiceBusPublisher> logger)
    {
        _logger = logger;
        _topicName = configuration["ServiceBus:TopicName"] ?? ServiceBusConstants.EmployeeEventsTopic;

        var connectionString = configuration["ServiceBus:ConnectionString"];

        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogWarning(
                "[ServiceBus] ConnectionString not configured. Events will be logged only. " +
                "Set ServiceBus:ConnectionString in Key Vault or appsettings.");

            // Create a null-safe client that logs instead of sending
            _client = null!;
            _sender = null!;
            return;
        }

        _client = new ServiceBusClient(connectionString, new ServiceBusClientOptions
        {
            RetryOptions = new ServiceBusRetryOptions
            {
                MaxRetries     = 3,
                Delay          = TimeSpan.FromSeconds(2),
                MaxDelay       = TimeSpan.FromSeconds(30),
                Mode           = ServiceBusRetryMode.Exponential
            }
        });

        _sender = _client.CreateSender(_topicName);
        _logger.LogInformation("[ServiceBus] Publisher initialized. Topic: {Topic}", _topicName);
    }

    /// <summary>
    /// Publishes a single employee event to the Service Bus topic.
    /// Message includes custom properties for subscription filtering.
    /// </summary>
    public async Task PublishAsync<T>(T eventMessage) where T : IEmployeeEvent
    {
        if (_sender == null)
        {
            LogEventToConsole(eventMessage);
            return;
        }

        try
        {
            var envelope = CreateEnvelope(eventMessage);
            var messageBody = JsonSerializer.Serialize(envelope, JsonOptions);

            var message = new ServiceBusMessage(messageBody)
            {
                MessageId       = envelope.MessageId,
                CorrelationId   = eventMessage.CorrelationId,
                ContentType     = "application/json",
                Subject         = eventMessage.EventType,

                // Custom properties — used by subscription filters
                ApplicationProperties =
                {
                    [ServiceBusConstants.EventTypeProperty]     = eventMessage.EventType,
                    [ServiceBusConstants.CorrelationIdProperty] = eventMessage.CorrelationId,
                    [ServiceBusConstants.SourceProperty]        = "EmployeeService",
                    [ServiceBusConstants.VersionProperty]       = "1.0",
                    ["EmployeeId"]                              = eventMessage.EmployeeId
                }
            };

            await _sender.SendMessageAsync(message);

            _logger.LogInformation(
                "[ServiceBus] Published {EventType} for Employee {EmployeeId}. " +
                "MessageId={MessageId}, CorrelationId={CorrelationId}",
                eventMessage.EventType,
                eventMessage.EmployeeId,
                envelope.MessageId,
                eventMessage.CorrelationId);
        }
        catch (ServiceBusException ex) when (ex.IsTransient)
        {
            _logger.LogWarning(ex,
                "[ServiceBus] Transient error publishing {EventType} for Employee {EmployeeId}. " +
                "Azure SDK will retry automatically.",
                eventMessage.EventType, eventMessage.EmployeeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ServiceBus] Failed to publish {EventType} for Employee {EmployeeId}. " +
                "The employee operation completed successfully — only the event was not published.",
                eventMessage.EventType, eventMessage.EmployeeId);
            // Do NOT re-throw — Service Bus failure must never fail the API operation
        }
    }

    /// <summary>
    /// Publishes multiple events in a single Service Bus batch (efficient for bulk operations).
    /// </summary>
    public async Task PublishBatchAsync<T>(IEnumerable<T> events) where T : IEmployeeEvent
    {
        if (_sender == null)
        {
            foreach (var evt in events) LogEventToConsole(evt);
            return;
        }

        var eventList = events.ToList();
        if (!eventList.Any()) return;

        try
        {
            using var batch = await _sender.CreateMessageBatchAsync();

            foreach (var eventMessage in eventList)
            {
                var envelope = CreateEnvelope(eventMessage);
                var messageBody = JsonSerializer.Serialize(envelope, JsonOptions);

                var message = new ServiceBusMessage(messageBody)
                {
                    MessageId     = envelope.MessageId,
                    CorrelationId = eventMessage.CorrelationId,
                    ContentType   = "application/json",
                    Subject       = eventMessage.EventType,
                    ApplicationProperties =
                    {
                        [ServiceBusConstants.EventTypeProperty] = eventMessage.EventType,
                        ["EmployeeId"]                          = eventMessage.EmployeeId
                    }
                };

                if (!batch.TryAddMessage(message))
                {
                    // Batch full — send current batch and start a new one
                    await _sender.SendMessagesAsync(batch);
                    _logger.LogDebug("[ServiceBus] Batch sent ({Count} events), starting new batch",
                        batch.Count);
                }
            }

            await _sender.SendMessagesAsync(batch);
            _logger.LogInformation("[ServiceBus] Batch published {Count} events", eventList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ServiceBus] Failed to publish batch of {Count} events", eventList.Count);
        }
    }

    private static ServiceBusMessageEnvelope<T> CreateEnvelope<T>(T eventMessage) where T : IEmployeeEvent
        => new()
        {
            MessageId     = Guid.NewGuid().ToString(),
            CorrelationId = eventMessage.CorrelationId,
            EventType     = eventMessage.EventType,
            Source        = "EmployeeService",
            Version       = "1.0",
            PublishedAt   = DateTime.UtcNow,
            Payload       = eventMessage
        };

    private void LogEventToConsole<T>(T eventMessage) where T : IEmployeeEvent
    {
        _logger.LogInformation(
            "[ServiceBus - DEV MODE] {EventType} for Employee {EmployeeId} " +
            "(Service Bus not configured — event logged only)",
            eventMessage.EventType, eventMessage.EmployeeId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_sender != null) await _sender.DisposeAsync();
        if (_client != null) await _client.DisposeAsync();
    }
}
