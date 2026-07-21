using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Events;

namespace Messaging.Consumer
{
    /// <summary>
    /// Background service that listens to RabbitMQ and dispatches messages
    /// to the correct IEventHandler implementation.
    ///
    /// Queue bindings (topic exchange: employee.events):
    ///   Queue                    | Routing key pattern
    ///   department.employee.q    | employee.*
    ///
    /// Retry strategy:
    ///   - First delivery: process normally
    ///   - On failure: nack with requeue=false → goes to Dead Letter Queue (DLQ)
    ///   - DLQ: employee.events.dlq (for manual inspection / replay)
    /// </summary>
    public class RabbitMQConsumerService : BackgroundService
    {
        private readonly ILogger<RabbitMQConsumerService> _logger;
        private readonly RabbitMQConsumerSettings _settings;
        private readonly IServiceProvider _serviceProvider;

        private IConnection? _connection;
        private IModel?      _channel;

        private const string ExchangeName    = "employee.events";
        private const string QueueName       = "department.employee.q";
        private const string DlxExchangeName = "employee.events.dlx";
        private const string DlqName         = "employee.events.dlq";

        public RabbitMQConsumerService(
            RabbitMQConsumerSettings settings,
            IServiceProvider serviceProvider,
            ILogger<RabbitMQConsumerService> logger)
        {
            _settings        = settings;
            _serviceProvider = serviceProvider;
            _logger          = logger;
        }

        // ── startup ──────────────────────────────────────────────────────────
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Connect();
            return base.StartAsync(cancellationToken);
        }

        private void Connect()
        {
            var factory = new ConnectionFactory
            {
                HostName                 = _settings.Host,
                Port                     = _settings.Port,
                UserName                 = _settings.UserName,
                Password                 = _settings.Password,
                VirtualHost              = _settings.VirtualHost,
                AutomaticRecoveryEnabled = true,
                RequestedHeartbeat       = TimeSpan.FromSeconds(60)
            };

            _connection = factory.CreateConnection("DepartmentService-Consumer");
            _channel    = _connection.CreateModel();

            // ── Dead Letter Exchange + Queue ──────────────────────────────────
            _channel.ExchangeDeclare(DlxExchangeName, ExchangeType.Fanout, durable: true);
            _channel.QueueDeclare(DlqName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(DlqName, DlxExchangeName, routingKey: string.Empty);

            // ── Main exchange ─────────────────────────────────────────────────
            _channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);

            // ── Main queue with DLX configured ───────────────────────────────
            var args = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", DlxExchangeName }
            };
            _channel.QueueDeclare(QueueName, durable: true, exclusive: false,
                autoDelete: false, arguments: args);

            // Bind to all employee.* events
            _channel.QueueBind(QueueName, ExchangeName, routingKey: "employee.*");

            // Prefetch 1 — fair dispatch, process one at a time
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.LogInformation(
                "RabbitMQConsumer: Connected to {Host}:{Port}, listening on queue [{Queue}]",
                _settings.Host, _settings.Port, QueueName);
        }

        // ── main loop ─────────────────────────────────────────────────────────
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (_, ea) =>
            {
                var routingKey = ea.RoutingKey;
                var body       = Encoding.UTF8.GetString(ea.Body.ToArray());

                _logger.LogInformation(
                    "RabbitMQConsumer: Received message RoutingKey={RoutingKey} MessageId={MessageId}",
                    routingKey, ea.BasicProperties.MessageId);

                try
                {
                    await DispatchAsync(routingKey, body, stoppingToken);
                    _channel!.BasicAck(ea.DeliveryTag, multiple: false);
                    _logger.LogInformation("RabbitMQConsumer: Acked {MessageId}", ea.BasicProperties.MessageId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "RabbitMQConsumer: Handler failed for RoutingKey={RoutingKey}. Sending to DLQ.",
                        routingKey);
                    // nack without requeue → routed to DLQ
                    _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            _channel!.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
            return Task.CompletedTask;
        }

        // ── dispatcher ───────────────────────────────────────────────────────
        private async Task DispatchAsync(string routingKey, string json, CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();

            switch (routingKey)
            {
                case "employee.created":
                    var created = JsonSerializer.Deserialize<EmployeeCreatedEvent>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
                    var createdHandler = scope.ServiceProvider
                        .GetRequiredService<IEventHandler<EmployeeCreatedEvent>>();
                    await createdHandler.HandleAsync(created, ct);
                    break;

                case "employee.updated":
                    var updated = JsonSerializer.Deserialize<EmployeeUpdatedEvent>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
                    var updatedHandler = scope.ServiceProvider
                        .GetRequiredService<IEventHandler<EmployeeUpdatedEvent>>();
                    await updatedHandler.HandleAsync(updated, ct);
                    break;

                case "employee.deleted":
                    var deleted = JsonSerializer.Deserialize<EmployeeDeletedEvent>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
                    var deletedHandler = scope.ServiceProvider
                        .GetRequiredService<IEventHandler<EmployeeDeletedEvent>>();
                    await deletedHandler.HandleAsync(deleted, ct);
                    break;

                default:
                    _logger.LogWarning("RabbitMQConsumer: Unknown routing key [{Key}] — discarding.", routingKey);
                    break;
            }
        }

        // ── cleanup ───────────────────────────────────────────────────────────
        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
