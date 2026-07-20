namespace Messaging.Producer
{
    /// <summary>Strongly-typed RabbitMQ connection settings bound from appsettings.json.</summary>
    public class RabbitMQSettings
    {
        public string Host        { get; set; } = "localhost";
        public int    Port        { get; set; } = 5672;
        public string UserName    { get; set; } = "guest";
        public string Password    { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
    }
}
