using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using FinInstUtils;
using FinInstUtils.ConfigElements;


namespace FinInstWssService
{

    public class RabbitMQSubscriber : IDisposable
    {
        private readonly List<RabbitMQInstance> _instances;
        private readonly List<IConnection> _connections = new();
        private readonly List<IChannel> _channels = new();
        private readonly ILogger<RabbitMQSubscriber> _logger;

        private readonly string[] _channelCodes = { "channel1", "channel2", "channel3" };

        public event Action<string, string> MessageReceived; // Event: (channelCode, message)

        public RabbitMQSubscriber(List<RabbitMQInstance> instances, ILogger<RabbitMQSubscriber> logger)
        {
            _instances = instances ?? throw new ArgumentNullException(nameof(instances));
            _logger = logger;

            //TODO await SubscribeToRabbitMQ();
        }

        private async Task SubscribeToRabbitMQAsync()
        {
            foreach (var instance in _instances)
            {
                try
                {
                    var factory = new ConnectionFactory
                    {
                        HostName = instance.HostName,
                        Port = instance.Port,
                        UserName = instance.UserName,
                        Password = instance.Password
                    };

                    var connection = await factory.CreateConnectionAsync();
                    _connections.Add(connection);

                    foreach (var channelCode in _channelCodes)
                    {
                        var channel = await connection.CreateChannelAsync();
                        _channels.Add(channel);

                        await channel.QueueDeclareAsync(queue: channelCode, durable: true, exclusive: false, autoDelete: false, arguments: null);

                        var consumer = new AsyncEventingBasicConsumer(channel);
                        consumer.ReceivedAsync += async (model, ea) => 
                        {
                            var body = ea.Body.ToArray();
                            var message = Encoding.UTF8.GetString(body);
                            _logger.LogInformation($"Received message from {channelCode}: {message}");
                            MessageReceived?.Invoke(channelCode, message);
                        };

                        await channel.BasicConsumeAsync(queue: channelCode, autoAck: true, consumer: consumer);
                        _logger.LogInformation($"Subscribed to {channelCode} on {instance.HostName}:{instance.Port}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to subscribe to RabbitMQ at {instance.HostName}:{instance.Port}");
                }
            }
        }

        public async Task UnsubscribeAsync()
        {
            _logger.LogInformation("Unsubscribing from all RabbitMQ instances...");
            foreach (var channel in _channels)
            {
                await channel.CloseAsync();
                await channel.DisposeAsync();
            }

            foreach (var connection in _connections)
            {
                await connection.CloseAsync();
                await connection.DisposeAsync();
            }

            _channels.Clear();
            _connections.Clear();
        }

        public async Task DisposeAsync()
        {
            await UnsubscribeAsync();
        }
    }

}
