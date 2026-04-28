using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using FluentAssertions;
using Serilog;
using Serilog.Core;
using Serilog.Core.Enrichers;
using Serilog.Events;
using Xunit;
using LogEvent = Akka.Event.LogEvent;

namespace Akka.Logger.Serilog.Tests
{
    public class ForContextSpecs : IAsyncLifetime
    {
        public static readonly Config Config =
@"akka.loglevel = DEBUG
akka.loggers=[""Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog""]
akka.logger-formatter=""Akka.Logger.Serilog.SerilogLogMessageFormatter, Akka.Logger.Serilog""";

        private readonly ITestOutputHelper _helper;
        private readonly TestSink _sink = new TestSink();

        private ActorSystem _sys;
        private TestKit.Xunit.TestKit _testKit;
        private ILoggingAdapter _loggingAdapter;

        public ForContextSpecs(ITestOutputHelper helper)
        {
            _helper = helper;

            global::Serilog.Log.Logger = new LoggerConfiguration()
                .WriteTo.Sink(_sink)
                .MinimumLevel.Information()
                .CreateLogger();
        }

        public ValueTask InitializeAsync()
        {
            _sys = ActorSystem.Create("ForContextTestSystem", Config);
            _testKit = new TestKit.Xunit.TestKit(_sys, _helper);
            _loggingAdapter = Logging.GetLogger(_sys, _sys.Name);
            return default;
        }

        public async ValueTask DisposeAsync()
        {
            _testKit.Shutdown();
            await _sys.Terminate();
        }

        [Fact]
        public async Task ShouldLogMessageWithContextPropertyDefaultLogger()
        {
            var context = _loggingAdapter
                .WithContext("Address", "No. 4 Privet Drive")
                .WithContext("Town", "Little Whinging")
                .WithContext("County", "Surrey")
                .WithContext("Country", "England");

            _sink.Clear();

            await _testKit.AwaitAssertAsync(() =>
            {
                context.Info("Hi {Person}", "Harry Potter");
                var logEvent = _sink.Writes.ToArray()
                    .FirstOrDefault(e => e.Properties.ContainsKey("Person"));
                logEvent.Should().NotBeNull();
                logEvent!.Level.Should().Be(LogEventLevel.Information);
                logEvent.RenderMessage().Should().Contain("Hi \"Harry Potter\"");
                logEvent.Properties.Should().ContainKeys("Person", "Address", "Town", "County", "Country");
                logEvent.Properties["Person"].ToString().Should().Be("\"Harry Potter\"");
                logEvent.Properties["Address"].ToString().Should().Be("\"No. 4 Privet Drive\"");
                logEvent.Properties["Town"].ToString().Should().Be("\"Little Whinging\"");
                logEvent.Properties["County"].ToString().Should().Be("\"Surrey\"");
                logEvent.Properties["Country"].ToString().Should().Be("\"England\"");
            });
        }

        [Fact]
        public async Task ShouldLogMessageWithContextProperty()
        {
            var context = _loggingAdapter
                .WithContext("Address", "No. 4 Privet Drive")
                .WithContext("Town", "Little Whinging")
                .WithContext("County", "Surrey")
                .WithContext("Country", "England");

            _sink.Clear();

            await _testKit.AwaitAssertAsync(() =>
            {
                context.Info("Hi {Person}", "Harry Potter");
                var logEvent = _sink.Writes.ToArray()
                    .FirstOrDefault(e => e.Properties.ContainsKey("Person"));
                logEvent.Should().NotBeNull();
                logEvent!.Level.Should().Be(LogEventLevel.Information);
                logEvent.RenderMessage().Should().Contain("Hi \"Harry Potter\"");
                logEvent.Properties.Should().ContainKeys("Person", "Address", "Town", "County", "Country");
                logEvent.Properties["Person"].ToString().Should().Be("\"Harry Potter\"");
                logEvent.Properties["Address"].ToString().Should().Be("\"No. 4 Privet Drive\"");
                logEvent.Properties["Town"].ToString().Should().Be("\"Little Whinging\"");
                logEvent.Properties["County"].ToString().Should().Be("\"Surrey\"");
                logEvent.Properties["Country"].ToString().Should().Be("\"England\"");
            });
        }

        [Fact]
        public async Task ShouldPassAlongAdditionalContext()
        {
            var traceId = Guid.NewGuid();
            var spanId = Guid.NewGuid();
            var context1 = _loggingAdapter.WithContext("traceId", traceId);
            var context2 = context1.WithContext("spanId", spanId);

            _sink.Clear();

            await _testKit.AwaitAssertAsync(() =>
            {
                context1.Info("hi");
                var logEvent = _sink.Writes.ToArray()
                    .FirstOrDefault(e => e.Properties.ContainsKey("traceId") &&
                                        e.RenderMessage().Contains("hi"));
                logEvent.Should().NotBeNull();
                logEvent!.Level.Should().Be(LogEventLevel.Information);
                logEvent.Properties.ContainsKey("traceId").Should().BeTrue();
                logEvent.Properties["traceId"].ToString().Should().BeEquivalentTo(traceId.ToString());
            });

            _sink.Clear();

            await _testKit.AwaitAssertAsync(() =>
            {
                context2.Info("bye");
                var logEvent2 = _sink.Writes.ToArray()
                    .FirstOrDefault(e => e.Properties.ContainsKey("spanId") &&
                                         e.RenderMessage().Contains("bye"));
                logEvent2.Should().NotBeNull();
                logEvent2!.Level.Should().Be(LogEventLevel.Information);

                // needs to still have the context from context1
                logEvent2.Properties.ContainsKey("traceId").Should().BeTrue();
                logEvent2.Properties["traceId"].ToString().Should().BeEquivalentTo(traceId.ToString());

                // and its own context from context2
                logEvent2.Properties.ContainsKey("spanId").Should().BeTrue();
                logEvent2.Properties["spanId"].ToString().Should().BeEquivalentTo(spanId.ToString());
            });
        }

        [Fact]
        public async Task ShouldPassAlongClassNameAsSourceContext()
        {
            _sink.Clear();

            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Info("hi");
                var logEvent = _sink.Writes.ToArray()
                    .FirstOrDefault(e => e.RenderMessage().Contains("hi"));
                logEvent.Should().NotBeNull();
                logEvent!.Level.Should().Be(LogEventLevel.Information);
                logEvent.Properties.ContainsKey(Constants.SourceContextPropertyName).Should().BeTrue();
            });
        }

        [Fact]
        public async Task ShouldPassAlongActorPath()
        {
            _sink.Clear();

            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Info("hi");
                var logEvent = _sink.Writes.ToArray()
                    .FirstOrDefault(e => e.RenderMessage().Contains("hi"));
                logEvent.Should().NotBeNull();
                logEvent!.Level.Should().Be(LogEventLevel.Information);
                logEvent.Properties.ContainsKey("ActorPath").Should().BeTrue();
            });
        }
    }
}
