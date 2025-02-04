using Microsoft.Extensions.Hosting;
using Serilog;

namespace FinInstWssClient
{
    public class WssClient : BackgroundService
    {
        private readonly WebSocketSharp.WebSocket _webSocket;
        private readonly ILogger _logger;

        private readonly RabbitMQPoster _rabbitMQPoster;

        public WssClient(string url, ILogger logger, RabbitMQPoster rabbitMQPoster)
        {
            _webSocket = new WebSocketSharp.WebSocket(url);
            _logger = logger;
            _rabbitMQPoster = rabbitMQPoster;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Information("WorkerService started (ExecuteAsync)");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ConnectAsync();
                    await Task.Delay(5000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Handle graceful shutdown
                    _logger.Information("WorkerService is stopping.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error in WorkerService");
                }
            }
        }

        public async Task ConnectAsync()
        {
            try
            {
                await _rabbitMQPoster.initAsync();

                _webSocket.OnMessage += async (sender, e) => await OnMessageReceivedAsync(e.Data);
                _webSocket.OnError += (sender, e) => _logger.Error(e.Message);
                _webSocket.OnClose += async (sender, e) => await CloseAsync();

                _webSocket.Connect();
                _logger.Information("WebSocket connected");

                var subscribeMessage = new
                {
                    method = "SUBSCRIBE",
                    @params = new[] { "btcusdt@aggTrade", "eurusdt@aggTrade", "btcusdt@aggTrade" },
                    id = 1
                };

                _webSocket.Send(Newtonsoft.Json.JsonConvert.SerializeObject(subscribeMessage));
            }
            catch (Exception ex)
            {
                _logger.Error("Error during wss connect attempt (%s)", ex.Message);
                throw new ApplicationException(String.Format("Error during wss connect attempt", ex.Message));
            }
        }


        private async Task OnMessageReceivedAsync(string message)
        {
            await _rabbitMQPoster.publish(message);
            _logger.Information($"Message received: {message}");
        }

        public async Task CloseAsync()
        {
            await _rabbitMQPoster.CloseAsync();
            _logger.Information("WebSocket closed");
        }
    }
}
