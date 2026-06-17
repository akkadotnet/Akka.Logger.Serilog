#### 1.5.69 June 17th 2026 ####

* [Update Akka.NET to 1.5.69](https://github.com/akkadotnet/akka.net/releases/tag/1.5.69)
* [Update Akka.Hosting to 1.5.69](https://github.com/akkadotnet/Akka.Hosting/releases/tag/1.5.69)
* [Fix incorrect WithLogging API reference in docs (should be ConfigureLoggers)](https://github.com/akkadotnet/Akka.Logger.Serilog/pull/343)

#### 1.5.67 April 28th 2026 ####

* [Update Akka.NET to 1.5.67](https://github.com/akkadotnet/akka.net/releases/tag/1.5.67)
* [Update Akka.Hosting to 1.5.67](https://github.com/akkadotnet/Akka.Hosting/releases/tag/1.5.67)

#### 1.5.63 March 24th 2026 ####

* [Update Akka.NET to 1.5.63](https://github.com/akkadotnet/akka.net/releases/tag/1.5.63)
* [Update Akka.Hosting to 1.5.63](https://github.com/akkadotnet/Akka.Hosting/releases/tag/1.5.63)

#### 1.5.60 February 10 2026 ####

* [Update Akka.NET to 1.5.60](https://github.com/akkadotnet/akka.net/releases/tag/1.5.60)
* [Add WithContext() support and deprecate Serilog-specific ForContext()](https://github.com/akkadotnet/Akka.Logger.Serilog/pull/310)
* [Convert tests from deprecated APIs to WithContext() and fix flaky net471 tests](https://github.com/akkadotnet/Akka.Logger.Serilog/pull/312)

This release adds support for Akka.NET 1.5.60's built-in `WithContext()` logging context enrichment API. Context properties set via `WithContext()` on any `ILoggingAdapter` now automatically flow through to Serilog as structured properties.

**Deprecations:**
- `SerilogLoggingAdapter` class is now marked `[Obsolete]` - use the standard `ILoggingAdapter` with `WithContext()` instead
- `ForContext()` extension method is now marked `[Obsolete]` - use `WithContext()` instead

These deprecated APIs still work and will continue to work — they just produce compiler warnings. They will not be removed until a future major version.

**What's NOT Changing:**

Your existing Serilog configuration is completely unaffected. This deprecation only affects how you create logging adapters inside Akka.NET actor code — it does not change anything about the Serilog pipeline itself.

Specifically, all of the following continue to work exactly as before:

- **Your Serilog `LoggerConfiguration`** — whether configured via C# code, `appsettings.json`, or any other Serilog configuration provider
- **Global enrichers** — `FromLogContext()`, `WithMachineName()`, `WithProcessId()`, `WithThreadId()`, and any custom `ILogEventEnricher` implementations
- **Global properties** — static properties added via `Enrich.WithProperty()` or the `"Properties"` section in JSON config
- **Output templates** — `{ActorPath}`, `{LogSource}`, `{Thread}`, and all other Akka metadata properties are still emitted exactly as before
- **Sinks** — Console, Seq, Elasticsearch, file sinks, and all other Serilog sinks are unaffected
- **`LogContext.PushProperty()`** — Serilog's ambient context API works the same as always
- **Serilog's native `ILogger.ForContext()`** — this is Serilog's own API and is NOT deprecated; only the Akka-specific `ForContext()` extension method on `ILoggingAdapter` is deprecated
- **`PropertyEnricher` parameters** — passing `PropertyEnricher` objects as log message parameters still works
- **Level switches and minimum level overrides** — no changes to log level filtering behavior

**Migration:**
```csharp
// Old (deprecated - produces compiler warning, but still works)
var log = Context.GetLogger<SerilogLoggingAdapter>()
    .ForContext("TenantId", "TENANT-001");

// New (recommended)
var log = Context.GetLogger()
    .WithContext("TenantId", "TENANT-001");
```

The new `WithContext()` API is part of core Akka.NET and works identically across all logging backends (Serilog, NLog, Microsoft.Extensions.Logging).

#### 1.5.59 January 26 2026 ####

* [Update Akka.NET to 1.5.59](https://github.com/akkadotnet/akka.net/releases/tag/1.5.59)
* [Update Akka.Hosting to 1.5.59](https://github.com/akkadotnet/Akka.Hosting/releases/tag/1.5.59)

#### 1.5.58 January 9 2026 ####

* [Update Akka.NET to 1.5.58](https://github.com/akkadotnet/akka.net/releases/tag/1.5.58)
* [Update Akka.Hosting to 1.5.58](https://github.com/akkadotnet/Akka.Hosting/releases/tag/1.5.58)
* [Add Akka.Hosting extensions for Serilog](https://github.com/akkadotnet/Akka.Logger.Serilog/pull/283)
* [Add LogFilter integration support](https://github.com/akkadotnet/Akka.Logger.Serilog/pull/289)

This release adds Akka.Hosting integration and LogFilter support for Serilog.

**New Features:**

- **Akka.Hosting Extensions**: New `AddSerilogLogging()` extension method for `LoggerConfigBuilder` that simplifies Serilog setup with Akka.Hosting. Automatically configures `SerilogLogger` and enables `SerilogLogMessageFormatter` for semantic logging.
- **LogFilter Integration**: Serilog now properly integrates with Akka.NET's LogFilter API for source-based log filtering, enabling pre-pipeline filtering for improved performance.

**Usage Example:**

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAkka("MyActorSystem", configurationBuilder =>
{
    configurationBuilder.ConfigureLoggers(loggerConfigBuilder =>
    {
        loggerConfigBuilder.AddSerilogLogging();
    });
});
```

#### 1.5.57 January 8 2026 ####

* [Update Akka.NET to 1.5.57](https://github.com/akkadotnet/akka.net/releases/tag/1.5.57)
* [Add semantic logging support for Akka.NET 1.5.56+](https://github.com/akkadotnet/Akka.Logger.Serilog/pull/294)

This release adds full semantic logging support, enabling Serilog to receive properly structured message templates and parameters instead of pre-formatted strings. This enhancement leverages Akka.NET's semantic logging APIs introduced in version 1.5.56, enabling richer structured logging capabilities.

**New Features:**
- **Semantic Logging**: Serilog now receives message templates with named and positional parameters for true structured logging
- **Enhanced Template Support**: Full support for Serilog destructuring (`@`), stringification (`$`), and format specifiers (e.g., `:N2`)
- **ForContext Integration**: Semantic logging works seamlessly with `ForContext()` for enriched log contexts
- **Akka Metadata Preservation**: All Akka.NET metadata (timestamp, log level, thread, logger name) is preserved in structured logs
- **Backwards Compatible**: Fully compatible with older Akka.NET versions through `LogMessage` type checking

#### 1.5.25 June 17 2024 ####

* [Update Akka.Hosting to 1.5.25](https://github.com/akkadotnet/akka.net/releases/tag/1.5.25)
* [implicitly convert regular `BusLogger` to `SerilogLoggingAdapter` when `ForContext` is called](https://github.com/akkadotnet/Akka.Logger.Serilog/pull/285)

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

#### 1.5.12.1 August 31 2023 ####

* [Update Akka.Hosting to 1.5.12.1](https://github.com/akkadotnet/Akka.Hosting/releases/tag/1.5.12.1)

#### 1.5.12 August 31 2023 ####

* [Update Akka.NET to 1.5.12](https://github.com/akkadotnet/akka.net/releases/tag/1.5.12)
* [Fix Serilog message output bug](https://github.com/akkadotnet/Akka.Logger.Serilog/pull/255)

#### 1.5.7 May 19 2023 ####

* [Update Akka.NET to 1.5.7](https://github.com/akkadotnet/akka.net/releases/tag/1.5.7)
* [Fix SerilogLogMessageFormatter duplicate key exception bug](https://github.com/akkadotnet/Akka.Logger.Serilog/pull/228)
* [Add log event property enricher support](https://github.com/akkadotnet/Akka.Logger.Serilog/pull/229)
* [Fix backward compatibility issue](https://github.com/akkadotnet/Akka.Logger.Serilog/pull/230)

#### 1.5.0.1 March 15 2023 ####

* [Fixed: `SerilogMessageFormatter` CTOR is `private`, can't be instantiated as the default `LogMessageFormatter`](https://github.com/akkadotnet/Akka.Logger.Serilog/issues/217)
