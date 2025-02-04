using FinInstWssServer.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace UnitTests
{
    /// <summary>
    /// Receives events fired by RabbitMQSubscriber
    /// For unit testing purposes
    /// </summary>
    public class MockRabbitMQSubscriberReceiver
    {
        private IRabbitMQSubscriber _rabbitMQSubscriber;
        private ILogger _logger;
        private int numberOfMessagesReceived = 0;

        public MockRabbitMQSubscriberReceiver(IRabbitMQSubscriber rabbitMQSubscriber, ILogger logger)
        {
            _rabbitMQSubscriber = rabbitMQSubscriber;
            _logger = logger;
            _rabbitMQSubscriber.MessageReceivedAsync += OnRabbitMessageReceivedAsync;
        }

        public async Task InitAsync()
        {
            await _rabbitMQSubscriber.SubscribeToRabbitMQAsync();
        }

        private async Task OnRabbitMessageReceivedAsync(string channelCode, string message)
        {
            //Counting the number if messages using thread-safe incrementation
            Interlocked.Increment(ref numberOfMessagesReceived);
            _logger.Information($"Message received in MockRabbitMQSubscriberReceiver, total {numberOfMessagesReceived}. ({message})");
        }

        public int getNumberOfMessagesReceived() => numberOfMessagesReceived;
    }
}
