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
    /// Tests for semantic logging functionality added in Akka.NET 1.5.56.
    /// Verifies that structured properties from log message templates are
    /// accessible in Serilog's log events.
    /// </summary>
    public class SemanticLoggingSpecs : IAsyncLifetime
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

        public SemanticLoggingSpecs(ITestOutputHelper helper)
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
            
            var logSource = _sys.Name;
            var logClass = typeof(ActorSystem);

            _loggingAdapter = new SerilogLoggingAdapter(_sys.EventStream, logSource, logClass);
            
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _testKit.Shutdown();
            await _sys.Terminate();
        }
        
        [Fact(DisplayName = "Should extract named template properties for Serilog")]
        public async Task NamedTemplatePropertiesTest()
        {
            _sink.Clear();

            // Log inside AwaitAssertAsync so retries send fresh messages
            // (handles race where logger isn't subscribed to EventStream yet)
            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Info("User {UserId} with email {Email} logged in", 12345, "user@example.com");
                var logEvent = GetMatchingLogEvent(e => e.Properties.ContainsKey("UserId"));
                logEvent.Should().NotBeNull();
                logEvent!.Properties["UserId"].ToString().Should().Be("12345");
                logEvent.Properties["Email"].ToString().Should().Be("\"user@example.com\"");
            });
        }

        [Fact(DisplayName = "Should extract positional template properties for Serilog")]
        public async Task PositionalTemplatePropertiesTest()
        {
            _sink.Clear();

            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Info("User {0} logged in from {1}", "Bob", "192.168.1.1");
                var logEvent = GetMatchingLogEvent(e => e.Properties.ContainsKey("0"));
                logEvent.Should().NotBeNull();
                logEvent!.Properties["0"].ToString().Should().Be("\"Bob\"");
                logEvent.Properties["1"].ToString().Should().Be("\"192.168.1.1\"");
            });
        }

        [Fact(DisplayName = "Should handle multiple named properties in template")]
        public async Task MultipleNamedPropertiesTest()
        {
            _sink.Clear();

            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Info("Order {OrderId} for customer {CustomerId}: {Amount} {Currency}",
                    "ORD-001", "CUST-456", 99.99, "USD");
                var logEvent = GetMatchingLogEvent(e => e.Properties.ContainsKey("OrderId"));
                logEvent.Should().NotBeNull();
                logEvent!.Properties["OrderId"].ToString().Should().Be("\"ORD-001\"");
                logEvent.Properties["CustomerId"].ToString().Should().Be("\"CUST-456\"");
                logEvent.Properties["Amount"].ToString().Should().Be("99.99");
                logEvent.Properties["Currency"].ToString().Should().Be("\"USD\"");
            });
        }

        [Fact(DisplayName = "Should preserve Akka metadata properties alongside semantic logging properties")]
        public async Task AkkaMetadataAndSemanticPropertiesTest()
        {
            _sink.Clear();

            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Info("User {UserId} action", 999);
                var logEvent = GetMatchingLogEvent(e => e.Properties.ContainsKey("UserId"));
                logEvent.Should().NotBeNull();

                // Semantic property
                logEvent!.Properties["UserId"].ToString().Should().Be("999");

                // Akka metadata properties
                logEvent.Properties.Should().ContainKey("ActorPath");
                logEvent.Properties.Should().ContainKey("LogSource");
                logEvent.Properties.Should().ContainKey("Thread");
            });
        }

        [Fact(DisplayName = "Should handle Serilog destructuring operator")]
        public async Task DestructuringOperatorTest()
        {
            _sink.Clear();

            var user = new { Name = "Alice", Age = 30, Role = "Admin" };
            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Info("Processing user {@User}", user);
                var logEvent = GetMatchingLogEvent(e => e.Properties.ContainsKey("User"));
                logEvent.Should().NotBeNull();

                // Serilog should have destructured the object
                var userProperty = logEvent!.Properties["User"];
                userProperty.Should().BeOfType<StructureValue>();

                var structure = (StructureValue)userProperty;
                structure.Properties.Should().Contain(p => p.Name == "Name");
                structure.Properties.Should().Contain(p => p.Name == "Age");
                structure.Properties.Should().Contain(p => p.Name == "Role");
            });
        }

        [Fact(DisplayName = "Should handle Serilog stringification operator")]
        public async Task StringificationOperatorTest()
        {
            _sink.Clear();

            var exception = new InvalidOperationException("Test error");
            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Info("Error occurred: {$Exception}", exception);
                var logEvent = GetMatchingLogEvent(e => e.Properties.ContainsKey("Exception"));
                logEvent.Should().NotBeNull();

                // Serilog should have used ToString() instead of destructuring
                var exceptionProperty = logEvent!.Properties["Exception"];
                exceptionProperty.Should().BeOfType<ScalarValue>();
            });
        }

        [Fact(DisplayName = "Should handle format specifiers in named templates")]
        public async Task FormatSpecifiersInTemplatesTest()
        {
            _sink.Clear();

            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Info("Total amount: {Amount:N2}", 1234.5678);
                var logEvent = GetMatchingLogEvent(e => e.Properties.ContainsKey("Amount"));
                logEvent.Should().NotBeNull();

                // The rendered message should apply the format
                logEvent!.RenderMessage().Should().Contain("1,234.57");
            });
        }

        [Fact(DisplayName = "Should handle empty/no properties gracefully")]
        public async Task NoPropertiesTest()
        {
            _sink.Clear();

            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Info("No template properties here");
                var logEvent = GetMatchingLogEvent(e => e.RenderMessage().Contains("No template properties here"));
                logEvent.Should().NotBeNull();

                // Should still have Akka metadata properties
                logEvent!.Properties.Should().ContainKey("ActorPath");
                logEvent.Properties.Should().ContainKey("LogSource");
                logEvent.Properties.Should().ContainKey("Thread");
            });
        }

        [Fact(DisplayName = "Should work with ForContext enrichment")]
        public async Task ForContextWithSemanticLoggingTest()
        {
            _sink.Clear();

            var contextLogger = _loggingAdapter.ForContext("TenantId", "TENANT-123");
            await _testKit.AwaitAssertAsync(() =>
            {
                contextLogger.Info("User {UserId} performed action", 456);
                var logEvent = GetMatchingLogEvent(e =>
                    e.Properties.ContainsKey("UserId") && e.Properties.ContainsKey("TenantId"));
                logEvent.Should().NotBeNull();

                // Should have both semantic property and context enrichment
                logEvent!.Properties["UserId"].ToString().Should().Be("456");
                logEvent.Properties["TenantId"].ToString().Should().Be("\"TENANT-123\"");
            });
        }

        /// <summary>
        /// REGRESSION TEST: https://github.com/akkadotnet/Akka.Hosting/issues/701
        /// Verifies that named placeholders are substituted in the rendered message.
        ///
        /// This is the equivalent of the bug fixed in Akka.Hosting's LoggerFactoryLogger where
        /// FormatMessage() used string.Format() which only supports positional {0} placeholders,
        /// causing named placeholders like {UserId} to appear raw in the output.
        ///
        /// Expected: "User 12345 with email user@example.com logged in"
        /// Bug output: "User {UserId} with email {Email} logged in"
        /// </summary>
        [Fact(DisplayName = "Named placeholders should be substituted in rendered message")]
        public async Task NamedPlaceholdersShouldBeSubstitutedInRenderedMessage()
        {
            _sink.Clear();

            await _testKit.AwaitAssertAsync(() =>
            {
                _loggingAdapter.Info("User {UserId} with email {Email} logged in", 12345, "user@example.com");
                var logEvent = GetMatchingLogEvent(e => e.Properties.ContainsKey("UserId"));
                logEvent.Should().NotBeNull();

                var renderedMessage = logEvent!.RenderMessage();

                // The rendered message should contain substituted values, NOT raw placeholders
                renderedMessage.Should().Contain("12345",
                    "the rendered message should contain the substituted UserId value");
                renderedMessage.Should().Contain("user@example.com",
                    "the rendered message should contain the substituted Email value");

                // Should NOT contain raw placeholders
                renderedMessage.Should().NotContain("{UserId}",
                    "the rendered message should NOT contain the raw {UserId} placeholder");
                renderedMessage.Should().NotContain("{Email}",
                    "the rendered message should NOT contain the raw {Email} placeholder");
            });
        }

        /// <summary>
        /// REGRESSION TEST: https://github.com/akkadotnet/Akka.Hosting/issues/701
        /// Verifies multiple named placeholders are all substituted correctly.
        /// </summary>
        [Fact(DisplayName = "Multiple named placeholders should all be substituted")]
        public async Task MultipleNamedPlaceholdersShouldAllBeSubstituted()
        {
            _sink.Clear();

            await _testKit.AwaitAssertAsync(() =>
            {
                // Matches the Discord user's log format:
                // _logger.Info("Published callback event: {Event} | ActorId: {ActorId}", eventName, actorId)
                _loggingAdapter.Info("Published callback event: {Event} | ActorId: {ActorId}", "UserLoggedIn", "actor-123");
                var logEvent = GetMatchingLogEvent(e => e.Properties.ContainsKey("Event"));
                logEvent.Should().NotBeNull();

                var renderedMessage = logEvent!.RenderMessage();

                // Values should be substituted
                renderedMessage.Should().Contain("UserLoggedIn",
                    "the Event value should be substituted");
                renderedMessage.Should().Contain("actor-123",
                    "the ActorId value should be substituted");

                // Raw placeholders should NOT appear
                renderedMessage.Should().NotContain("{Event}",
                    "should NOT contain raw {Event} placeholder");
                renderedMessage.Should().NotContain("{ActorId}",
                    "should NOT contain raw {ActorId} placeholder");
            });
        }

        private LogEvent? GetMatchingLogEvent(Func<LogEvent, bool> predicate)
        {
            return _sink.Writes.ToArray().FirstOrDefault(predicate);
        }
    }
}
