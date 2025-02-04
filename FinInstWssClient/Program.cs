using FinInstUtils;
using Serilog;
using Serilog.Sinks.SystemConsole;
using Serilog.Sinks.File;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace FinInstWssClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            String BinanceAPIURL = "wss://stream.binance.com:443/stream?streams=btcusdt/eurusdt/btcjpy";

            ILogger logger = new LoggerConfiguration()
                            .WriteTo.File(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "logs", "wss_client_log.txt"))
                            .WriteTo.Console()
                            .CreateLogger();

            ConfigurationReader confReader = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ConfigurationReader.FILENAME), logger);

            RabbitMQPoster rabbitMQPoster = new(logger, confReader.GetConfig());

            using IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                {
                    services.AddSingleton<WssClient>(provider =>
                        new WssClient(BinanceAPIURL, logger, rabbitMQPoster));
                    services.AddHostedService(provider => provider.GetRequiredService<WssClient>());
                })
                .Build();

            await host.RunAsync();
        }
    }
}
