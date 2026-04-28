// -----------------------------------------------------------------------
//  <copyright file="PropertyEnricherSpec.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2023 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Akka.Configuration;
using Akka.Event;
using FluentAssertions;
using Serilog;
using Serilog.Core.Enrichers;
using Serilog.Events;
using Xunit;

namespace Akka.Logger.Serilog.Tests;

public class PropertyEnricherSpec : TestKit.Xunit.TestKit
{
    public static readonly Config Config = $@"
akka.loglevel = DEBUG
akka.loggers = [""{typeof(SerilogLogger).AssemblyQualifiedName}""]
akka.logger-formatter = ""{typeof(SerilogLogMessageFormatter).AssemblyQualifiedName}""
";

    private readonly ILoggingAdapter _loggingAdapter;
    private readonly TestSink _sink = new TestSink();

    public PropertyEnricherSpec(ITestOutputHelper helper) : base(Config, output: helper)
    {
        global::Serilog.Log.Logger = new LoggerConfiguration()
            .WriteTo.Sink(_sink)
            .MinimumLevel.Debug()
            .CreateLogger();
        _loggingAdapter = Sys.Log;
    }

    [Fact]
    public void ShouldLogMessageWithPropertyEnrichers()
    {
        var context = _loggingAdapter;

        _sink.Clear();
        AwaitCondition(() => _sink.Writes.Count == 0);

        context.Debug("Hi {Person}", "Harry Potter", 
            new PropertyEnricher("Address", "No. 4 Privet Drive"),
            new PropertyEnricher("Town", "Little Whinging"),
            new PropertyEnricher("County", "Surrey"),
            new PropertyEnricher("Country", "England"));

        AwaitCondition(() =>
        {
            while (_sink.Writes.TryDequeue(out var logEvent))
            {
                try
                {
                    logEvent.Level.Should().Be(LogEventLevel.Debug);
                    logEvent.RenderMessage().Should().Contain("Hi \"Harry Potter\"");
                    logEvent.Properties.Should().ContainKeys("Person", "Address", "Town", "County", "Country");
                    logEvent.Properties["Person"].ToString().Should().Be("\"Harry Potter\"");
                    logEvent.Properties["Address"].ToString().Should().Be("\"No. 4 Privet Drive\"");
                    logEvent.Properties["Town"].ToString().Should().Be("\"Little Whinging\"");
                    logEvent.Properties["County"].ToString().Should().Be("\"Surrey\"");
                    logEvent.Properties["Country"].ToString().Should().Be("\"England\"");
                    return true;
                }
                catch
                {
                    // no-op
                }
            }
            return false;
        });
    }

}