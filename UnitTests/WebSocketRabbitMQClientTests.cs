using System.Threading;
using NUnit.Framework;
using FinInstWssClient;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using UnitTests;
using FinInstWssServer.Services;
using FinInstUtils;
using FinInstUtils.ConfigElements;
using Serilog;

namespace FinInstUnitTests;

[TestFixture]
public class WebSocketRabbitMQClientTests
{
    private Serilog.Core.Logger _logger;

    private ConfigurationReader confReader;

    [SetUp]
    public void Setup()
    {
        _logger = new LoggerConfiguration().CreateLogger();

        confReader = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ConfigurationReader.FILENAME), _logger);
    }

    [TearDown]
    public void TearDown()
    {
        _logger.Dispose();
    }

    [Test]
    public async Task TestWebSocketRabbitMQClient()
    {

        RabbitMQPoster rabbitMQPoster = new(_logger, confReader.GetConfig());
        WssClient client = new("wss://stream.binance.com:443/stream?streams=btcusdt/eurusdt/btcjpy", _logger, rabbitMQPoster);

        await client.ConnectAsync();

        Thread.Sleep(20000); //connect for 20 seconds

        await client.CloseAsync();

        Assert.Pass("Connection maintained for 20 seconds and then closed.");
    }

    [Test]
    public async Task TestRabbitPublishAndConsume()
    {
        //publishes some messages to rabbit and consumes them
        RabbitMQPoster rabbitMQPoster = new(_logger, confReader.GetConfig());
        await rabbitMQPoster.initAsync();

        //subscribes to some queues
        RabbitMQSubscriber rabbitMQSubscriber = new(confReader, _logger);
        MockRabbitMQSubscriberReceiver mockRabbitMQSubscriberReceiver = new(rabbitMQSubscriber, _logger);
        await mockRabbitMQSubscriberReceiver.InitAsync(); //new List<String>() { "BTCUSDT", "EURUSDT" }

        await rabbitMQPoster.publish("""{"result":null,"id":1}""");
        await rabbitMQPoster.publish("""{"stream":"eurusdt@aggTrade","data":{"e":"aggTrade","E":1738534577193,"s":"EURUSDT","a":115491254,"p":"1.02410000","q":"99.00000000","f":132488899,"l":132488899,"T":1738534577193,"m":true,"M":true}}""");
        await rabbitMQPoster.publish("""{"stream":"btcusdt@aggTrade","data":{"e":"aggTrade","E":1738534577231,"s":"BTCUSDT","a":3412663610,"p":"97674.73000000","q":"0.00062000","f":4504797980,"l":4504797980,"T":1738534577231,"m":false,"M":true}}""");
        await rabbitMQPoster.publish("""{"stream":"btcusdt@aggTrade","data":{"e":"aggTrade","E":1738534578551,"s":"BTCUSDT","a":3412663620,"p":"97674.73000000","q":"0.01000000","f":4504797990,"l":4504797990,"T":1738534578551,"m":false,"M":true}}""");
        await rabbitMQPoster.publish("""{"stream":"btcusdt@aggTrade","data":{"e":"aggTrade","E":1738534578573,"s":"BTCUSDT","a":3412663636,"p":"97676.12000000","q":"0.00018000","f":4504798031,"l":4504798033,"T":1738534578573,"m":false,"M":true}}""");

        Thread.Sleep(10000); //wait 10 seconds for Rabbit to pass the messages

        Assert.That(mockRabbitMQSubscriberReceiver.getNumberOfMessagesReceived(), Is.EqualTo(4));
    }
}
