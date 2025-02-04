using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ILogger = Serilog.ILogger;


namespace FinInstWssServer.Services;

public class WebSocketService
{
    private readonly IRabbitMQSubscriber _rabbitSubscriber;
    private readonly ILogger _logger;
    private readonly Dictionary<WebSocket, List<String>> _clientSubscriptions = new();

    public WebSocketService(IRabbitMQSubscriber rabbitSubscriber, ILogger logger)
    {
        _rabbitSubscriber = rabbitSubscriber;
        _logger = logger;
        _rabbitSubscriber.MessageReceivedAsync += OnRabbitMessageReceivedAsync;
    }

    public async Task HandleWebSocketAsync(WebSocket webSocket, List<String> listOfInstruments)
    {
        _logger.Information("WebSocket connection established (HandleWebSocketAsync)");

        _clientSubscriptions[webSocket] = listOfInstruments;
        await _rabbitSubscriber.SubscribeToRabbitMQAsync();

        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var buffer = new byte[1024];
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "WebSocket error");
        }
        finally
        {
            await CloseWebSocketConnection(webSocket);
        }
    }

    private async Task OnRabbitMessageReceivedAsync(string channelCode, string message)
    {
        foreach (var (webSocket, channels) in _clientSubscriptions)
        {
            if (webSocket.State == WebSocketState.Open && channels.Contains(channelCode))
            {
                var msgBytes = Encoding.UTF8.GetBytes(message);
                await webSocket.SendAsync(new ArraySegment<byte>(msgBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }

    private async Task CloseWebSocketConnection(WebSocket webSocket)
    {
        if (_clientSubscriptions.ContainsKey(webSocket))
        {
            _clientSubscriptions.Remove(webSocket);
        }

        _logger.Information("WebSocket connection closed.");
        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
    }
}
