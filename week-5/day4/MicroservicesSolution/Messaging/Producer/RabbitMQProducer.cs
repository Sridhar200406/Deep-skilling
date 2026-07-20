using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Shared.Events;

namespace Messaging.Producer
{
    /// <summary>
    /// Generic RabbitMQ message producer.
    /// Publishes any BaseEvent to the configured exchange using a topic routing key.
    ///
    /// Pattern:
    ///   Exchange type : topic
    ///   Exchange name : employee.events
    ///   Routing keys  : employee.created | employee.updated | employee.deleted
    /// </summary>
    public class RabbitMQProducer : IDisposable
    {
        private readonly IConnection  _connection;
        private readonly IModel       _channel;
        private readonly ILogger<RabbitMQProducer> _logger;
        private const string ExchangeName = "employee.events";

        public RabbitMQProducer(RabbitMQSettings settings, ILogger<RabbitMQProducer> logger)
        {
            _logger = logger;

            var factory = new ConnectionFactory
            {
                HostName             = settings.Host,
                Port                 = settings.Port,
                UserName             = settings.UserName,
                Password             = settings.Password,
                VirtualHost          = settings.VirtualHost,
                RequestedHeartbeat   = TimeSpan.FromSeconds(60),
                AutomaticRecoveryEnabled = true          // auto-reconnect
            };

            _connection = factory.CreateConnection("EmployeeService-Producer");
            _channel    = _connection.CreateModel();

            // Declare a durable topic exchange — survives RabbitMQ restart
            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type:     ExchangeType.Topic,
                durable:  true,
                autoDelete: false);

            _logger.LogInformation("RabbitMQProducer: Connected to {Host}:{Port}", settings.Host, settings.Port);
        }

        /// <summary>Publish an event to RabbitMQ with retry logic (3 attempts).</summary>
        public void Publish<T>(T @event, string routingKey) where T : BaseEvent
        {
            const int maxRetries = 3;
            var attempt = 0;

            while (attempt < maxRetries)
            {
                try
                {
                    attempt++;
                    var json    = JsonSerializer.Serialize(@event);
                    var body    = Encoding.UTF8.GetBytes(json);

                    var props = _channel.CreateBasicProperties();
                    props.Persistent    = true;                    // survive broker restart
                    props.ContentType   = "application/json";
                    props.MessageId     = @event.EventId.ToString();
                    props.CorrelationId = @event.CorrelationId;
                    props.Timestamp     = new AmqpTimestamp(
                        new DateTimeOffset(@event.OccurredOnUtc).ToUnixTimeSeconds());

                    _channel.BasicPublish(
                        exchange:   ExchangeName,
                        routingKey: routingKey,
                        basicProperties: props,
                        body:       body);

                    _logger.LogInformation(
                        "RabbitMQProducer: Published [{EventType}] EventId={EventId} RoutingKey={RoutingKey}",
                        @event.EventType, @event.EventId, routingKey);
                    return;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    _logger.LogWarning(ex,
                        "RabbitMQProducer: Publish attempt {Attempt} failed. Retrying in 2s...", attempt);
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                }
            }

            _logger.LogError("RabbitMQProducer: All {MaxRetries} publish attempts failed.", maxRetries);
            throw new InvalidOperationException($"Failed to publish {typeof(T).Name} after {maxRetries} attempts.");
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
}
