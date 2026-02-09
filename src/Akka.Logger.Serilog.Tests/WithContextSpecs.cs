using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using FluentAssertions;
using Serilog;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;
using LogEvent = Serilog.Events.LogEvent;

namespace Akka.Logger.Serilog.Tests
{
    /// <summary>
    /// Tests for core Akka.NET WithContext() logging context enrichment
    /// flowing through to Serilog log events.
    /// </summary>
    public class WithContextSpecs : IAsyncLifetime
    {
        public static readonly Config Config =
@"akka.loglevel = DEBUG
akka.loggers=[""Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog""]
akka.logger-formatter=""Akka.Logger.Serilog.SerilogLogMessageFormatter, Akka.Logger.Serilog""";

        private readonly ITestOutputHelper _helper;
        private readonly TestSink _sink;

        private ActorSystem _sys;
        private TestKit.Xunit2.TestKit _testKit;
        private ILoggingAdapter _loggingAdapter;

        public WithContextSpecs(ITestOutputHelper helper)
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
            _sys = ActorSystem.Create("WithContextTestSystem", Config);
            _testKit = new TestKit.Xunit2.TestKit(_sys, _helper);

            // Use ActorSystem overload to get BusLogging with the configured SerilogLogMessageFormatter.
            // The LoggingBus overload defaults to DefaultLogMessageFormatter which can't handle named templates.
            // WithContext() also requires BusLogging (ContextLoggingAdapter delegates to BusLogging.LogWithContext).
            _loggingAdapter = Logging.GetLogger(_sys, _sys.Name);

            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _testKit.Shutdown();
            await _sys.Terminate();
        }

        [Fact(DisplayName = "WithContext single property should appear in Serilog properties")]
        public async Task WithContext_SingleProperty_AppearsInSerilogProperties()
        {
            _sink.Clear();

            var contextLogger = _loggingAdapter.WithContext("TenantId", "TENANT-001");

            await _testKit.AwaitAssertAsync(() =>
            {
                contextLogger.Info("Processing request");
                var logEvent = GetMatchingLogEvent(e => e.Properties.ContainsKey("TenantId"));
                logEvent.Should().NotBeNull("TenantId property should be present in Serilog log event");
                logEvent!.Properties["TenantId"].ToString().Should().Be("\"TENANT-001\"");
            });
        }

        [Fact(DisplayName = "WithContext multiple properties should all appear in Serilog properties")]
        public async Task WithContext_MultipleProperties_AllAppear()
        {
            _sink.Clear();

            var contextLogger = _loggingAdapter
                .WithContext("TenantId", "TENANT-002")
                .WithContext("CorrelationId", "CORR-123")
                .WithContext("Region", "us-east-1");

            await _testKit.AwaitAssertAsync(() =>
            {
                contextLogger.Info("Multi-context request");
                var logEvent = GetMatchingLogEvent(e =>
                    e.Properties.ContainsKey("TenantId") &&
                    e.Properties.ContainsKey("CorrelationId") &&
                    e.Properties.ContainsKey("Region"));
                logEvent.Should().NotBeNull("all three context properties should be present");
                logEvent!.Properties["TenantId"].ToString().Should().Be("\"TENANT-002\"");
                logEvent.Properties["CorrelationId"].ToString().Should().Be("\"CORR-123\"");
                logEvent.Properties["Region"].ToString().Should().Be("\"us-east-1\"");
            });
        }

        [Fact(DisplayName = "WithContext combined with semantic template should have both context and template properties")]
        public async Task WithContext_CombinedWithSemanticTemplate_BothAppear()
        {
            _sink.Clear();

            var contextLogger = _loggingAdapter.WithContext("TenantId", "TENANT-003");

            await _testKit.AwaitAssertAsync(() =>
            {
                contextLogger.Info("User {UserId} performed {Action}", 42, "login");
                var logEvent = GetMatchingLogEvent(e =>
                    e.Properties.ContainsKey("TenantId") &&
                    e.Properties.ContainsKey("UserId"));
                logEvent.Should().NotBeNull("both context and template properties should be present");
                logEvent!.Properties["TenantId"].ToString().Should().Be("\"TENANT-003\"");
                logEvent.Properties["UserId"].ToString().Should().Be("42");
                logEvent.Properties["Action"].ToString().Should().Be("\"login\"");
            });
        }

        [Fact(DisplayName = "WithContext should not pollute unrelated log events")]
        public async Task WithContext_DoesNotPolluteUnrelatedLogs()
        {
            _sink.Clear();

            var contextLogger = _loggingAdapter.WithContext("SecretContext", "should-not-leak");
            var plainLogger = _loggingAdapter;

            await _testKit.AwaitAssertAsync(() =>
            {
                contextLogger.Info("Context message with marker {Marker}", "CTX");
                plainLogger.Info("Plain message with marker {Marker}", "PLAIN");

                var contextEvent = GetMatchingLogEvent(e =>
                    e.Properties.ContainsKey("Marker") &&
                    e.Properties["Marker"].ToString() == "\"CTX\"");
                var plainEvent = GetMatchingLogEvent(e =>
                    e.Properties.ContainsKey("Marker") &&
                    e.Properties["Marker"].ToString() == "\"PLAIN\"");

                contextEvent.Should().NotBeNull();
                plainEvent.Should().NotBeNull();

                contextEvent!.Properties.Should().ContainKey("SecretContext");
                plainEvent!.Properties.Should().NotContainKey("SecretContext",
                    "context properties should not leak to loggers without that context");
            });
        }

        private LogEvent? GetMatchingLogEvent(Func<LogEvent, bool> predicate)
        {
            return _sink.Writes.ToArray().FirstOrDefault(predicate);
        }
    }
}
