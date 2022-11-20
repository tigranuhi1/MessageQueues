namespace DataCaptureService
{
    public sealed class Settings
    {
        public string RabbitMQUri { get; set; }
        public string ExchangeName { get; set; }
        public string RoutingKey { get; set; }
        public string SourceFolderPath { get; set; }
        public string FileType { get; set; }
        public int MaxRetryCount { get; set; }
    }
}
