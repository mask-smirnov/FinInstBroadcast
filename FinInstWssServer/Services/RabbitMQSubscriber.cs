using FinInstUtils;
using FinInstUtils.ConfigElements;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using ILogger = Serilog.ILogger;


namespace FinInstWssServer.Services
{

    public class RabbitMQSubscriber : IRabbitMQSubscriber, IAsyncDisposable
    {
        private readonly Configuration _config; //conf file data
        private readonly List<IConnection> _connections = new(); //rabbit connections pool
        private readonly List<IChannel> _channels = new(); //rabbit channels pool
        private readonly ILogger _logger;
        //private readonly List<String> _listOfInstruments = new(); //list of pairs that clien subscribes to

        public event Func<string, string, Task> MessageReceivedAsync; // Async event (channelCode, message)

        public RabbitMQSubscriber(ConfigurationReader configReader, ILogger logger)
        {
            _config = configReader.GetConfig() ?? throw new ArgumentNullException(nameof(configReader));
            _logger = logger;
            //_listOfInstruments = listOfInstruments;
        }

        public async Task SubscribeToRabbitMQAsync()
        {
            foreach (var instance in _config.RabbitMQInstances)
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

                    var channel = await connection.CreateChannelAsync();
                    _channels.Add(channel);

                    foreach (Instrument instrument in _config.Instruments)
                    {
                        //await channel.QueueDeclareAsync(queue: pair.Name, exclusive: false, arguments: null);

                        var consumer = new AsyncEventingBasicConsumer(channel);
                        consumer.ReceivedAsync += async (model, ea) => 
                        {
                            var body = ea.Body.ToArray();
                            var message = Encoding.UTF8.GetString(body);
                            _logger.Information($"Message received (in lambda) from {instrument.Name}: {message}");
                            
                            if (MessageReceivedAsync != null)
                            {
                                await Task.Run(() => MessageReceivedAsync.Invoke(instrument.Name, message));
                            }
                        };

                        await channel.BasicConsumeAsync(queue: instrument.Name, autoAck: true, consumer: consumer); //autoAck for brevity, this is not a production code

                        _logger.Information($"Subscribed to {instrument.Name} on {instance.HostName}:{instance.Port}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Failed to subscribe to RabbitMQ at {instance.HostName}:{instance.Port}");
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await UnsubscribeAsync();
        }

        public async Task UnsubscribeAsync()
        {
            _logger.Information("Unsubscribing from all RabbitMQ instances...");
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
        }
    }
}
