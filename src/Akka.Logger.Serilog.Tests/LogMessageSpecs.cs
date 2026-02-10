using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using FluentAssertions;
using Serilog;
using Serilog.Core.Enrichers;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;
using LogEvent = Serilog.Events.LogEvent;

namespace Akka.Logger.Serilog.Tests
{
    public class LogMessageSpecs: IAsyncLifetime
    {
        private static readonly Config Config = @"akka.loglevel = DEBUG
                                                 akka.loggers=[""Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog""]";

        private readonly ITestOutputHelper _helper;
        private readonly TestSink _sink;

        private ActorSystem _sys;
        private TestKit.Xunit2.TestKit _testKit;
        private ILoggingAdapter _loggingAdapter;

        public LogMessageSpecs(ITestOutputHelper helper)
        {
            _helper = helper;
            _sink = new TestSink(helper);

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Sink(_sink)
                .MinimumLevel.Debug()
                .CreateLogger();
        }

        public Task InitializeAsync()
        {
            _sys = ActorSystem.Create("TestActorSystem", Config);
            _testKit = new TestKit.Xunit2.TestKit(_sys, _helper);
            _loggingAdapter = _sys.Log;

            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _testKit.Shutdown();
            await _sys.Terminate();
        }

        [Fact]
        public async Task ShouldLogDebugLevelMessage()
        {
            _sink.Clear();

            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Debug("hi");
                var logEvent = GetMatchingLogEvent(e => e.Level == LogEventLevel.Debug
                                                       && e.RenderMessage() == "hi");
                logEvent.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task ShouldLogMessageWithPropertyEnrichers()
        {
            _sink.Clear();

            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Debug("Hi {0}", "Harry Potter",
                    new PropertyEnricher("Address", "No. 4 Privet Drive"),
                    new PropertyEnricher("Town", "Little Whinging"),
                    new PropertyEnricher("County", "Surrey"),
                    new PropertyEnricher("Country", "England"));

                var logEvent = GetMatchingLogEvent(e => e.Level == LogEventLevel.Debug
                                                       && e.Properties.ContainsKey("Address"));
                logEvent.Should().NotBeNull();
                logEvent!.RenderMessage().Should().Contain("Hi \"Harry Potter\"");
                logEvent.Properties.Should().ContainKeys("Address", "Town", "County", "Country");
                logEvent.Properties["Address"].ToString().Should().Be("\"No. 4 Privet Drive\"");
                logEvent.Properties["Town"].ToString().Should().Be("\"Little Whinging\"");
                logEvent.Properties["County"].ToString().Should().Be("\"Surrey\"");
                logEvent.Properties["Country"].ToString().Should().Be("\"England\"");
            });
        }

        [Fact]
        public async Task ShouldLogDebugLevelMessageWithArgs()
        {
            _sink.Clear();

            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Debug("hi {0}", "test");
                var logEvent = GetMatchingLogEvent(e => e.Level == LogEventLevel.Debug
                                                       && e.RenderMessage() == "hi \"test\"");
                logEvent.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task ShouldLogDebugLevelMessageWithException()
        {
            _sink.Clear();

            var exception = new Exception("BOOM!!!");
            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Debug(exception, "hi");
                var logEvent = GetMatchingLogEvent(e => e.Level == LogEventLevel.Debug
                                                       && e.Exception == exception
                                                       && e.RenderMessage() == "hi");
                logEvent.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task ShouldLogDebugLevelMessageWithArgsAndException()
        {
            _sink.Clear();

            var exception = new Exception("BOOM!!!");
            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Debug(exception, "hi {0}", "test");
                var logEvent = GetMatchingLogEvent(e => e.Level == LogEventLevel.Debug
                                                       && e.Exception == exception
                                                       && e.RenderMessage() == "hi \"test\"");
                logEvent.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task ShouldLogInfoLevelMessage()
        {
            _sink.Clear();

            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Info("hi");
                var logEvent = GetMatchingLogEvent(e => e.Level == LogEventLevel.Information
                                                       && e.RenderMessage() == "hi");
                logEvent.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task ShouldLogInfoLevelMessageWithArgs()
        {
            _sink.Clear();

            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Info("hi {0}", "test");
                var logEvent = GetMatchingLogEvent(e => e.Level == LogEventLevel.Information
                                                       && e.RenderMessage() == "hi \"test\"");
                logEvent.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task ShouldLogInfoLevelMessageWithException()
        {
            _sink.Clear();

            var exception = new Exception("BOOM!!!");
            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Info(exception, "hi");
                var logEvent = GetMatchingLogEvent(e => e.Level == LogEventLevel.Information
                                                       && e.Exception == exception
                                                       && e.RenderMessage() == "hi");
                logEvent.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task ShouldLogInfoLevelMessageWithArgsAndException()
        {
            _sink.Clear();

            var exception = new Exception("BOOM!!!");
            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Info(exception, "hi {0}", "test");
                var logEvent = GetMatchingLogEvent(e => e.Level == LogEventLevel.Information
                                                       && e.Exception == exception
                                                       && e.RenderMessage() == "hi \"test\"");
                logEvent.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task ShouldLogWarningLevelMessage()
        {
            _sink.Clear();

            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Warning("hi");
                var logEvent = GetMatchingLogEvent(e => e.Level == LogEventLevel.Warning
                                                       && e.RenderMessage() == "hi");
                logEvent.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task ShouldLogWarningLevelMessageWithArgs()
        {
            _sink.Clear();

            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Warning("hi {0}", "test");
                var logEvent = GetMatchingLogEvent(e => e.Level == LogEventLevel.Warning
                                                       && e.RenderMessage() == "hi \"test\"");
                logEvent.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task ShouldLogWarningLevelMessageWithException()
        {
            _sink.Clear();

            var exception = new Exception("BOOM!!!");
            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Warning(exception, "hi");
                var logEvent = GetMatchingLogEvent(e => e.Level == LogEventLevel.Warning
                                                       && e.Exception == exception
                                                       && e.RenderMessage() == "hi");
                logEvent.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task ShouldLogWarningLevelMessageWithArgsAndException()
        {
            _sink.Clear();

            var exception = new Exception("BOOM!!!");
            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Warning(exception, "hi {0}", "test");
                var logEvent = GetMatchingLogEvent(e => e.Level == LogEventLevel.Warning
                                                       && e.Exception == exception
                                                       && e.RenderMessage() == "hi \"test\"");
                logEvent.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task ShouldLogErrorLevelMessage()
        {
            _sink.Clear();

            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Error("hi");
                var logEvent = GetMatchingLogEvent(e => e.Level == LogEventLevel.Error
                                                       && e.RenderMessage() == "hi");
                logEvent.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task ShouldLogErrorLevelMessageWithArgs()
        {
            _sink.Clear();

            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Error("hi {0}", "test");
                var logEvent = GetMatchingLogEvent(e => e.Level == LogEventLevel.Error
                                                       && e.RenderMessage() == "hi \"test\"");
                logEvent.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task ShouldLogErrorLevelMessageWithException()
        {
            _sink.Clear();

            var exception = new Exception("BOOM!!!");
            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Error(exception, "hi");
                var logEvent = GetMatchingLogEvent(e => e.Level == LogEventLevel.Error
                                                       && e.Exception == exception
                                                       && e.RenderMessage() == "hi");
                logEvent.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task ShouldLogErrorLevelMessageWithArgsAndException()
        {
            _sink.Clear();

            var exception = new Exception("BOOM!!!");
            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Error(exception, "hi {0}", "test");
                var logEvent = GetMatchingLogEvent(e => e.Level == LogEventLevel.Error
                                                       && e.Exception == exception
                                                       && e.RenderMessage() == "hi \"test\"");
                logEvent.Should().NotBeNull();
            });
        }

        private LogEvent? GetMatchingLogEvent(Func<LogEvent, bool> predicate)
        {
            return _sink.Writes.ToArray().FirstOrDefault(predicate);
        }
    }
}
