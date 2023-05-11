using Akka.Hosting;
using Akka.Actor;
using Akka.Actor.Dsl;
using Akka.Cluster.Hosting;
using Akka.Event;
using Akka.Hosting.Logging;
using Akka.Hosting.LoggingDemo;
using Akka.Logger.Serilog;
using Akka.Remote.Hosting;
using Serilog;
using Serilog.Core.Enrichers;
using Serilog.Formatting.Json;
using LogLevel = Akka.Event.LogLevel;

Serilog.Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate:"[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(new JsonFormatter(), "output.json")
    .MinimumLevel.Debug()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAkka("MyActorSystem", (configurationBuilder, serviceProvider) =>
{
    configurationBuilder
        .ConfigureLoggers(setup =>
        {
            // This sets the minimum log level
            setup.LogLevel = LogLevel.InfoLevel;
            
            // Clear all loggers
            setup.ClearLoggers();
            
            // Add serilog logger
            setup.AddLogger<SerilogLogger>();
            setup.LogMessageFormatter = typeof(SerilogLogMessageFormatter);
        })
        .WithRemoting("localhost", 8110)
        .WithClustering(new ClusterOptions(){ Roles = new[]{ "myRole" }, 
            SeedNodes = new[]{ "akka.tcp://MyActorSystem@localhost:8110" }})
        .WithActors((system, registry) =>
        {
            var echo = system.ActorOf(act =>
            {
                var counter = 0;
                act.ReceiveAny((o, context) =>
                {
                    counter++;
                    context.GetLogger().Info("Actor received {ID}", o, new PropertyEnricher(
                        "custom-property", 
                        new
                        {
                            name = "Custom", 
                            data = counter
                        }));
                    context.Sender.Tell($"{context.Self} rcv {o}");
                });
            }, "echo");
            registry.TryRegister<Echo>(echo); // register for DI
        });
});

var app = builder.Build();

app.MapGet("/", async (context) =>
{
    var echo = context.RequestServices.GetRequiredService<ActorRegistry>().Get<Echo>();
    var body = await echo.Ask<string>(context.TraceIdentifier, context.RequestAborted).ConfigureAwait(false);
    await context.Response.WriteAsync(body);
});

app.Run();