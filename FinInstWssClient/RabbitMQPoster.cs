using FinInstUtils;
using FinInstUtils.BinanceMessage;
using FinInstUtils.ConfigElements;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace FinInstWssClient
{
    public class RabbitMQPoster
    {
        private readonly ILogger _logger;
        private readonly Configuration _config;

        //the class works with multiple instances of Rabbit, so we need a pool of connections
        private List<IChannel> rabbitChannelsPool = new();
        private List<IConnection> rabbitConnectionsPool = new();

        private readonly Random random = new();

        public RabbitMQPoster(ILogger logger, Configuration config)
        {
            _logger = logger;
            _config = config;
        }

        public async Task initAsync()
        {
            if (_config.RabbitMQInstances is null || _config.RabbitMQInstances.Count == 0)
            {
                _logger.Error("No RabbitMQ instances are set up in the configuration file");
                throw new InvalidOperationException("No RabbitMQ instances are set up in the configuration file");
            }

            foreach (var rabbitInstance in _config.RabbitMQInstances) //Init channel to each rabbit instance
                await initRabbitChannelAsync(rabbitInstance);
        }

        private async Task initRabbitChannelAsync(RabbitMQInstance instanceData)
        {
            var factory = new ConnectionFactory()
            {
                HostName = instanceData.HostName,
                Port = instanceData.Port,
                UserName = instanceData.UserName,
                Password = instanceData.Password
            };

            var connection = await factory.CreateConnectionAsync();
            rabbitConnectionsPool.Add(connection);

            var channel = await connection.CreateChannelAsync();
    
            foreach (Instrument pair in _config.Instruments) //Create a queue for each trading pair
                await channel.QueueDeclareAsync(queue: pair.Name, exclusive: false, arguments: null);
            
            rabbitChannelsPool.Add(channel);

            _logger.Information($"RabbitMQ channel initialized (host = {instanceData.HostName}, port = {instanceData.Port})");
        }

        /// <summary>
        /// Publishes the message to randomly selected RabbitMQ instance
        /// </summary>
        /// <param name="message"></param>
        public async Task publish(string message)
        {
            if (TradeMessage.TryDeserialize(message, out TradeMessage tradeMessage))
            {
                byte[] body = Encoding.UTF8.GetBytes(message);
                BasicProperties basicProperties = new();
                basicProperties.ContentType = "text/plain";

                int index = random.Next(rabbitChannelsPool.Count); // Randomly select some RabbitMQ instance

                await rabbitChannelsPool[index].BasicPublishAsync(
                    exchange: "",
                    routingKey: tradeMessage.Data.Symbol,
                    mandatory: false,
                    basicProperties,
                    body: body);
                _logger.Information($"Message received and published: {message}");
            }
            else
            {
                _logger.Warning($"Message received but not published: {message}");
            }
        }

        public async Task CloseAsync()
        {
            foreach (var channel in rabbitChannelsPool)
            {
                await channel.CloseAsync();
                await channel.DisposeAsync();
            }

            foreach (var connection in rabbitConnectionsPool)
            {
                await connection.CloseAsync();
                await connection.DisposeAsync();
            }

            _logger.Information("Rabbit channels closed");
        }
    }
}
