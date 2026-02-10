# Akka.Logger.Serilog

This is the Serilog integration plugin for Akka.NET. Please check out our [documentation](http://getakka.net/articles/utilities/serilog.html) on how to get the most out of this plugin.

Targets [Serilog 2.12.0](https://www.nuget.org/packages/Serilog/2.12.0).

### Semantic Logging Syntax
When using [Akka.Hosting](https://github.com/akkadotnet/Akka.Hosting) with `AddSerilogLogging()`, the `SerilogLogMessageFormatter` is automatically configured and semantic logging works with the standard `ILoggingAdapter`:

```csharp
var log = Context.GetLogger(); // standard ILoggingAdapter
log.Info("User {UserId} performed {Action}", userId, "login"); // semantic logging works
```

If you are configuring Akka.NET without Akka.Hosting, you need to set the `SerilogLogMessageFormatter` in your HOCON config:

```hocon
akka.logger-formatter = "Akka.Logger.Serilog.SerilogLogMessageFormatter, Akka.Logger.Serilog"
```

### Adding Context Properties To Your Logs

As of Akka.NET 1.5.60, you can use the built-in `WithContext()` method on any `ILoggingAdapter` to add persistent properties to all log messages produced by that adapter. These properties automatically flow through to Serilog as structured properties.

```csharp
var log = Context.GetLogger()
    .WithContext("TenantId", "TENANT-001")
    .WithContext("CorrelationId", correlationId)
    .WithContext("Region", "us-east-1");
log.Info("Processing request for {UserId}", userId);
```

All logging done using the `log` `ILoggingAdapter` instance will include "TenantId", "CorrelationId", and "Region" as Serilog properties, in addition to the "UserId" semantic template property.

`WithContext()` is part of core Akka.NET and works with all logging backends (Serilog, NLog, Microsoft.Extensions.Logging), not just Serilog.

### Log Filtering

Akka.Logger.Serilog supports [Akka.NET's built-in log filtering](https://getakka.net/articles/utilities/logging.html#filtering-log-messages) to reduce log noise before messages reach Serilog. This is especially useful on high-volume systems.

#### When to Use Akka LogFilter vs Serilog Native Filtering

**Use Akka's LogFilter when:**
- Filtering on `LogSource` (actor paths, class names) - this Akka-specific metadata isn't available in Serilog
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
- Filtering on Serilog enricher properties (e.g., `TenantId`, `CorrelationId` from `WithContext`)
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

### Deprecated: SerilogLoggingAdapter and ForContext()

> **Note:** `SerilogLoggingAdapter`, the `ForContext()` extension method, and `Context.GetLogger<SerilogLoggingAdapter>()` are deprecated as of Akka.Logger.Serilog 1.5.60. Use the standard `ILoggingAdapter` with `WithContext()` instead.

If you are migrating from `ForContext()`:

```csharp
// Old (deprecated)
var log = Context.GetLogger<SerilogLoggingAdapter>()
    .ForContext("TenantId", "TENANT-001");

// New (recommended)
var log = Context.GetLogger()
    .WithContext("TenantId", "TENANT-001");
```

The `ForContext()` and `SerilogLoggingAdapter` APIs will continue to work but will be removed in a future major version.

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
