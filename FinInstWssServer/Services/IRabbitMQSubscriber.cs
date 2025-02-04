namespace FinInstWssServer.Services
{
    public interface IRabbitMQSubscriber
    {
        event Func<string, string, Task> MessageReceivedAsync;

        /// <summary>
        /// Subcribes client to the messages. Wss service singleton subscribes to all instruments
        /// It sorts the messages by sockets inside itself
        /// </summary>
        Task SubscribeToRabbitMQAsync();
        ValueTask DisposeAsync();

    }
}
