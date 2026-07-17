namespace Messaging.Consumer
{
    /// <summary>Strongly-typed RabbitMQ consumer settings bound from appsettings.json.</summary>
    public class RabbitMQConsumerSettings
    {
        public string Host        { get; set; } = "localhost";
        public int    Port        { get; set; } = 5672;
        public string UserName    { get; set; } = "guest";
        public string Password    { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
    }
}
