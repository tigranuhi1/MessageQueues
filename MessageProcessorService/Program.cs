using Microsoft.Extensions.Configuration;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace DataCaptureService
{
    internal class Program
    {
        static IModel channel;
        static IConfiguration config;
        static Settings settings;
        
        private static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            config = builder.Build();

            settings = config.GetRequiredSection("Settings").Get<Settings>();

            var factory = new ConnectionFactory();
            factory.Uri = new Uri(settings.RabbitMQUri);
            IConnection connection = factory.CreateConnection();
            channel = connection.CreateModel();

            channel.QueueDeclare(settings.QueueName, true, false, false);
            channel.QueueBind(settings.QueueName, settings.ExchangeName, settings.RoutingKey);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += OnReceived;
            
            channel.BasicConsume(settings.QueueName, true, consumer);

            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();

            channel.Close();
            connection.Close();
        }

        static void OnReceived(object sender, BasicDeliverEventArgs e)
        {
            byte[] content = e.Body.ToArray();
            string fileName = GetFileName(e.BasicProperties.Headers);
            Console.WriteLine($"Received new message. Creating new file {fileName}.");

            File.WriteAllBytes(fileName, content);            
        }

        private static string GetFileName(IDictionary<string, object> headers)
        {
            string fileName = Encoding.UTF8.GetString(headers["fileName"] as byte[]);
            return Path.Combine(settings.DestinationFolderPath, fileName);
        }
    }
}