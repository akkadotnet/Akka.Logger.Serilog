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
