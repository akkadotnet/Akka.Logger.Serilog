#### 1.3.9 August 23 2018 ####
* [Fixed: Regression: ForContext API doesn't apply changes](https://github.com/akkadotnet/Akka.Logger.Serilog/issues/51)
* Upgraded to Akka.NET v1.3.9.
* Upgraded to Serilog v2.7.1.

#### 1.3.6 April 28 2018 ####
* Restored `SerilogLogMessageFormatter` in order to fix [Bug: `LogEvent.ToString()` blows up when using Serilog semantic formatting](https://github.com/akkadotnet/Akka.Logger.Serilog/issues/43). 
* Upgraded to [Akka.NET v1.3.6](https://github.com/akkadotnet/akka.net/releases/tag/v1.3.6).

If you intend on using any of the Serilog semantic logging formats in your logging strings, __you need to use the SerilogLoggingAdapter__ inside your instrumented code or there could be elsewhere inside parts of your `ActorSystem`:

```csharp
var log = Context.GetLogger<SerilogLoggingAdapter>(); // correct
log.Info("My boss makes me use {semantic} logging", "semantic"); // serilog semantic logging format
```

This will allow all logging events to be consumed anywhere inside the `ActorSystem`, including places like the Akka.NET TestKit, without throwing `FormatException`s when they encounter semantic logging syntax outside of the `SerilogLogger`.

#### 1.3.3 January 27 2018 ####

Removed SerilogLogMessageFormatter since its no longer needed
Support for Akka 1.3.3
Update to Serilog 2.6.0

#### 1.2.0 April 18 2017 ####

Support for Akka 1.2.0

#### 1.1.3 Januari 26 2017 ####

Support for Akka 1.1.3

Update to Serilog 2.4.0

#### 1.1.2 September 26 2016 ####

Support for Akka 1.1.2
Update to Serilog 2.2.1

#### 1.1.1 Juli 16 2016 ####

Support for Akka 1.1.1
Updated to Serilog 2.0.0

#### 1.0.8 March 27 2016 ####

Support for Akka 1.0.8

#### 1.0.7 Februari 29 2016 ####

Support for Akka 1.0.7