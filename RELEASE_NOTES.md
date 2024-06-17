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
