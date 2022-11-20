using Microsoft.Extensions.Configuration;

using RabbitMQ.Client;

namespace DataCaptureService
{
    internal class Program
    {
        private static IModel channel;
        private static Settings settings;

        private static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            IConfiguration config = builder.Build();

            settings = config.GetRequiredSection("Settings").Get<Settings>();

            var factory = new ConnectionFactory
            {
                Uri = new Uri(settings.RabbitMQUri)
            };

            IConnection connection = factory.CreateConnection();
            channel = connection.CreateModel();
            channel.ExchangeDeclare(settings.ExchangeName, ExchangeType.Direct, true, false);

            using var watcher = new FileSystemWatcher(settings.SourceFolderPath);
            watcher.Created += OnCreated;
            watcher.Filter = $"*.{settings.FileType}";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();

            channel.Close();
            connection.Close();
        }

        static async void OnCreated(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Created)
            {
                return;
            }

            var fileName = Path.GetFileName(e.FullPath);
            if (fileName == null)
            {
                return;
            }            

            if(!await IsFileLocked(e.FullPath))
            {
                Console.WriteLine($"The copy operation for file {fileName} to source folder failed.");
                return;
            }

            Console.WriteLine($"{fileName} file is added to source folder, sending message to queue...");

            var props = channel.CreateBasicProperties();
            props.Headers = new Dictionary<string, object>
            {
                { "fileName", fileName }
            };
            props.Persistent = true;

            byte[] content = File.ReadAllBytes(e.FullPath);

            channel.BasicPublish(settings.ExchangeName, settings.RoutingKey, props, content);
        }

        private static async Task<bool> IsFileLocked(string path)
        {
            return await Task.Run(() =>
            {
                int retryCount = 0;
                while (true)
                {
                    try
                    {
                        using (var stream = new StreamReader(path))
                        {
                            return true;
                        }
                    }
                    catch (IOException)
                    {
                        retryCount++;
                        if (retryCount < settings.MaxRetryCount)
                        {
                            Task.Delay(2000);
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            });
        }
    }
}