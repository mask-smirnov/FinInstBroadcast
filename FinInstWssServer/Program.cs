using FinInstUtils;
using FinInstWssServer.Services;
using Serilog;
using ILogger = Serilog.ILogger;

namespace FinInstWssServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog((context, configuration) =>
                configuration.ReadFrom.Configuration(context.Configuration)); //Read Serilog settings form appsettings.json

            builder.Services.AddSingleton<ConfigurationReader>(
                builder => new ConfigurationReader(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ConfigurationReader.FILENAME), 
                    builder.GetRequiredService<ILogger>()));

            builder.Services.AddSingleton<IRabbitMQSubscriber, RabbitMQSubscriber>();
            builder.Services.AddSingleton<WebSocketService>();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseWebSockets();

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws" && context.WebSockets.IsWebSocketRequest)
                {
                    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                    // Example: The client is subscribing to "BTCUSDT", "EURUSDT"
                    List<String> listOfInstruments = new() { "BTCUSDT", "EURUSDT" };

                    var webSocketService = context.RequestServices.GetRequiredService<WebSocketService>();
                    await webSocketService.HandleWebSocketAsync(webSocket, listOfInstruments);
                }
                else
                {
                    await next();
                }
            });

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();

            app.UseSerilogRequestLogging();

            app.MapControllers();

            app.Run();
        }
    }
}
