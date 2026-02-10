# Akka.Logger.Serilog

This is the [Serilog](https://serilog.net/) integration plugin for [Akka.NET](https://getakka.net/). It routes Akka.NET's internal log events through the Serilog pipeline, giving you structured logging with full access to Serilog's enrichment, filtering, and sink ecosystem.

Targets [Serilog 2.12.0](https://www.nuget.org/packages/Serilog/2.12.0).

## How It Works

When you configure Akka.NET to use the Serilog logger, all log events from Akka.NET (actor lifecycle messages, dead letters, your own `ILoggingAdapter` calls, etc.) flow through a `SerilogLogger` actor that writes them to Serilog's global `Log.Logger`.

This means your **entire Serilog configuration** — enrichers, sinks, filters, output templates, minimum levels — applies to Akka.NET log events just like any other log event in your application. Akka.Logger.Serilog doesn't replace or interfere with your Serilog pipeline; it plugs into it.

The following Akka-specific properties are automatically added to every log event:

| Property | Description |
|----------|-------------|
| `SourceContext` | The full type name of the logging class |
| `ActorPath` | The path of the actor that produced the log message |
| `LogSource` | The Akka.NET log source string |
| `Thread` | The managed thread ID (zero-padded) |
| `Timestamp` | When the log event was created |

These properties are available in output templates (e.g., `{ActorPath}`) and in structured log sinks (e.g., Seq, Elasticsearch).

## Setup

### Option 1: Akka.Hosting (Recommended)

```csharp
using Akka.Hosting;
using Akka.Logger.Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog as usual
builder.Host.UseSerilog((context, config) =>
{
    config
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

// Register Akka.NET with Serilog logging
builder.Services.AddAkka("MySystem", configurationBuilder =>
{
    configurationBuilder.WithLogging(loggerConfigBuilder =>
    {
        loggerConfigBuilder.AddSerilogLogging();
    });
});
```

`AddSerilogLogging()` automatically configures both `SerilogLogger` and `SerilogLogMessageFormatter`, enabling semantic logging out of the box.

### Option 2: HOCON Configuration

```hocon
akka {
    loggers = ["Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog"]
    loglevel = DEBUG

    # Required for semantic/structured logging (named template properties)
    logger-formatter = "Akka.Logger.Serilog.SerilogLogMessageFormatter, Akka.Logger.Serilog"
}
```

Make sure you configure the global `Serilog.Log.Logger` before creating your `ActorSystem`:

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "{Timestamp:HH:mm:ss} [{Level:u3}] {ActorPath} - {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var system = ActorSystem.Create("MySystem", hoconConfig);
```

## Semantic (Structured) Logging

When `SerilogLogMessageFormatter` is configured (automatic with `AddSerilogLogging()`, manual with HOCON), named template properties in your log messages are preserved as structured Serilog properties:

```csharp
var log = Context.GetLogger();

// Named properties - UserId and Action become structured Serilog properties
log.Info("User {UserId} performed {Action}", userId, "login");

// Serilog destructuring operator - User is destructured into its component properties
log.Info("Processing {@User}", userObject);

// Format specifiers work too
log.Info("Total: {Amount:C2}", 1234.56);
```

These properties appear in your Serilog sinks exactly as they would if you logged directly with Serilog's `ILogger`.

## Adding Context Properties To Your Logs

As of Akka.NET 1.5.60, you can use the built-in `WithContext()` method on any `ILoggingAdapter` to add persistent properties to all log messages produced by that adapter. These properties automatically flow through to Serilog as structured properties.

```csharp
public class OrderProcessorActor : ReceiveActor
{
    private readonly ILoggingAdapter _log;

    public OrderProcessorActor(string tenantId, string region)
    {
        // Context properties persist for all messages logged with this adapter
        _log = Context.GetLogger()
            .WithContext("TenantId", tenantId)
            .WithContext("Region", region);

        Receive<ProcessOrder>(order =>
        {
            // This log event will include TenantId, Region, AND OrderId
            _log.Info("Processing order {OrderId} for {Amount:C2}", order.Id, order.Amount);
        });
    }
}
```

`WithContext()` is part of core Akka.NET and works with all logging backends (Serilog, NLog, Microsoft.Extensions.Logging), not just Serilog.

## Your Serilog Configuration

Your existing Serilog configuration works exactly as before. Akka.Logger.Serilog writes into the Serilog pipeline — it doesn't replace or modify it.

### Global Enrichers

All of your global enrichers apply to Akka.NET log events:

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()        // ambient async-local context
    .Enrich.WithMachineName()       // machine name on every event
    .Enrich.WithProcessId()         // PID on every event
    .Enrich.WithThreadId()          // thread ID on every event
    .Enrich.WithProperty("Application", "OrderService")  // static property
    .WriteTo.Console()
    .CreateLogger();
```

These enrichers fire on every log event that passes through the Serilog pipeline, including all events from Akka.NET.

### JSON / appsettings.json Configuration

JSON-based Serilog configuration is fully supported. All enrichers, sinks, properties, and level overrides configured in your `appsettings.json` work with Akka.NET log events:

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Enrichers.Environment"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "System": "Error",
        "Microsoft": "Error"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3} {ActorPath} - {Message:lj} {Exception}{NewLine}"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithProcessId", "WithThreadId"],
    "Properties": {
      "Application": "MyService"
    }
  }
}
```

Note that `{ActorPath}` and other Akka metadata properties can be used directly in output templates.

### Serilog's LogContext vs. Akka's WithContext

Serilog's `LogContext.PushProperty()` uses async-local (thread-local on .NET Framework) ambient context. Because Akka.NET processes messages on its own dispatcher threads, `LogContext` properties pushed by the *caller* won't automatically flow into an *actor's* log messages — this is inherent to the Akka threading model.

For enriching log messages inside actors, use `WithContext()` — it travels with the Akka `LogEvent` across thread boundaries:

```csharp
// This works - WithContext travels through the Akka pipeline
var log = Context.GetLogger().WithContext("CorrelationId", correlationId);
log.Info("Processing request");  // CorrelationId is present

// This does NOT work inside actors - LogContext is thread-local
using (LogContext.PushProperty("CorrelationId", correlationId))
{
    actorRef.Tell(message);  // actor processes on a different thread
}
```

`LogContext.PushProperty()` is still useful for non-actor code (ASP.NET middleware, background services, etc.) where you control the async context.

## Log Filtering

Akka.Logger.Serilog supports [Akka.NET's built-in log filtering](https://getakka.net/articles/utilities/logging.html#filtering-log-messages) to reduce log noise before messages reach Serilog. This is especially useful on high-volume systems.

### When to Use Akka LogFilter vs Serilog Native Filtering

**Use Akka's LogFilter when:**
- Filtering on `LogSource` (actor paths, class names) — this Akka-specific metadata isn't available in Serilog's filter pipeline
- You want to filter messages *before* they enter the Serilog pipeline (better performance on high-volume systems)
- Using source-only filters (no string allocations required)

```csharp
var filters = new LogFilterBuilder()
    .ExcludeSourceStartingWith("Akka.Remote.EndpointWriter")
    .ExcludeSourceContaining("Heartbeat")
    .Build();

var bootstrap = BootstrapSetup.Create()
    .WithSetup(filters);
```

**Use Serilog's native filtering when:**
- Filtering on Serilog enricher properties (e.g., `TenantId`, `CorrelationId` from `WithContext()`)
- Using complex predicate logic
- Filtering on properties added via `LogContext.PushProperty()`

```csharp
Log.Logger = new LoggerConfiguration()
    .Filter.ByExcluding(evt =>
        evt.Properties.TryGetValue("TenantId", out var val) &&
        val.ToString() == "\"internal\"")
    .WriteTo.Console()
    .CreateLogger();
```

**Important limitation:** Context properties added via `WithContext()` or `LogContext.PushProperty()` are applied *after* Akka's LogFilter runs, so they cannot be filtered using Akka's LogFilter. Use Serilog's native `.Filter.ByExcluding()` for enricher-based filtering.

## Deprecated: SerilogLoggingAdapter and ForContext()

> **Note:** `SerilogLoggingAdapter`, the `ForContext()` extension method, and `Context.GetLogger<SerilogLoggingAdapter>()` are deprecated as of Akka.Logger.Serilog 1.5.60. Use the standard `ILoggingAdapter` with `WithContext()` instead.

These deprecated APIs **still work and will continue to work** — they produce compiler warnings but will not be removed until a future major version. Your existing code will compile and run correctly.

If you are migrating from `ForContext()`:

```csharp
// Old (deprecated - still works, produces compiler warning)
var log = Context.GetLogger<SerilogLoggingAdapter>()
    .ForContext("TenantId", "TENANT-001");

// New (recommended)
var log = Context.GetLogger()
    .WithContext("TenantId", "TENANT-001");
```

**Why the change?** `WithContext()` is built into core Akka.NET and works identically across all logging backends. This means you can write logging code once and switch between Serilog, NLog, or Microsoft.Extensions.Logging without changing your actor code.

## Building this solution
To run the build script associated with this solution, execute the following:

**Windows**
```
c:\> build.cmd all
```

**Linux / OS X**
```
c:\> build.sh all
```

If you need any information on the supported commands, please execute the `build.[cmd|sh] help` command.

This build script is powered by [FAKE](https://fake.build/); please see their API documentation should you need to make any changes to the [`build.fsx`](build.fsx) file.
