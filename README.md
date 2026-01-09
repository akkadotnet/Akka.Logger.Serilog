# Akka.Logger.Serilog

This is the Serilog integration plugin for Akka.NET. Please check out our [documentation](http://getakka.net/articles/utilities/serilog.html) on how to get the most out of this plugin.

Targets [Serilog 2.12.0](https://www.nuget.org/packages/Serilog/2.12.0).

### Semantic Logging Syntax
If you intend on using any of the Serilog semantic logging formats in your logging strings, __you need to use the SerilogLoggingAdapter__ inside your instrumented code or there could be elsewhere inside parts of your `ActorSystem`:

```csharp
var log = Context.GetLogger<SerilogLoggingAdapter>(); // correct
log.Info("My boss makes me use {semantic} logging", "semantic"); // serilog semantic logging format
```

or

```csharp
var log = MyActorSystem.GetLogger<SerilogLoggingAdapter>(myContextObject); // correct
log.Info("My boss makes me use {semantic} logging", "semantic"); // serilog semantic logging format
```

or
```csharp
var log = MyActorSystem.GetLogger<SerilogLoggingAdapter>(contextName, contextType); // correct
log.Info("My boss makes me use {semantic} logging", "semantic"); // serilog semantic logging format
```

This will allow all logging events to be consumed anywhere inside the `ActorSystem`, including places like the Akka.NET TestKit, without throwing `FormatException`s when they encounter semantic logging syntax outside of the `SerilogLogger`.

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
- Filtering on Serilog enricher properties (e.g., `TenantId`, `CorrelationId` from `ForContext`)
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

**Important limitation:** Serilog enrichers added via `ForContext()` or `LogContext.PushProperty()` are applied *after* Akka's LogFilter runs, so they cannot be filtered using Akka's LogFilter. Use Serilog's native `.Filter.ByExcluding()` for enricher-based filtering.

### Adding Property Enricher To Your Logs

#### Default Properties
You can add property enrichers to the logging adapter that will be added to all logging calls to that logging adapter.

```csharp
var log = Context.GetLogger<SerilogLoggingAdapter>()
    .ForContext("Address", "No. 4 Privet Drive")
    .ForContext("Town", "Little Whinging")
    .ForContext("County", "Surrey")
    .ForContext("Country", "England");
log.Info("My boss makes me use {Semantic} logging", "semantic");
```

All logging done using the `log` `ILoggingAdapter` instance will append "Address", "Town", "County", and "Country" properties into the Serilog log.

#### One-off Properties

You can add one-off property to a single log message by appending `PropertyEnricher` instances at the end of your logging calls.

```csharp
var log = Context.GetLogger<SerilogLoggingAdapter>();
log.Info(
    "My boss makes me use {Semantic} logging", "semantic",
    new PropertyEnricher("County", "Surrey"), 
    new PropertyEnricher("Country", "England"));
```

This log entry will have "County" and "Country" properties added to it.

### Automatically Convert `ILoggingAdapter` into `SerilogLoggingAdapter`

As of Akka.Logger.Serilog v1.5.25, you can now do the following:

```csharp
var log = Context.GetLogger()
    .ForContext("Address", "No. 4 Privet Drive")
    .ForContext("Town", "Little Whinging")
    .ForContext("County", "Surrey")
    .ForContext("Country", "England");
log.Info("My boss makes me use {Semantic} logging", "semantic");
```

And it will work without having to explicitly call `Context.GetLogger<SerilogLoggingAdapter>()` first.

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
