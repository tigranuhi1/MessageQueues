namespace DataCaptureService
{
    public sealed class Settings
    {
        public string RabbitMQUri { get; set; }
        public string ExchangeName { get; set; }
        public string QueueName { get; set; }
        public string RoutingKey { get; set; }
        public string DestinationFolderPath { get; set; }
    }
}
