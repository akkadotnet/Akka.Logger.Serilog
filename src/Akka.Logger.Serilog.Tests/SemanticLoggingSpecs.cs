using System;
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
            await _testKit.AwaitConditionAsync(() => _sink.Writes.Count == 0);

            _loggingAdapter.Info("User {UserId} with email {Email} logged in", 12345, "user@example.com");

            await _testKit.AwaitConditionAsync(() => 
                AssertCondition(logEvent =>
                {
                    logEvent.Properties.Should().ContainKey("UserId");
                    logEvent.Properties.Should().ContainKey("Email");
                    logEvent.Properties["UserId"].ToString().Should().Be("12345");
                    logEvent.Properties["Email"].ToString().Should().Be("\"user@example.com\"");
                }));
        }

        [Fact(DisplayName = "Should extract positional template properties for Serilog")]
        public async Task PositionalTemplatePropertiesTest()
        {
            _sink.Clear();
            await _testKit.AwaitConditionAsync(() => _sink.Writes.Count == 0);

            _loggingAdapter.Info("User {0} logged in from {1}", "Bob", "192.168.1.1");

            await _testKit.AwaitConditionAsync(() => 
                AssertCondition(logEvent =>
                {
                    logEvent.Properties.Should().ContainKey("0");
                    logEvent.Properties.Should().ContainKey("1");
                    logEvent.Properties["0"].ToString().Should().Be("\"Bob\"");
                    logEvent.Properties["1"].ToString().Should().Be("\"192.168.1.1\"");
                }));
        }

        [Fact(DisplayName = "Should handle multiple named properties in template")]
        public async Task MultipleNamedPropertiesTest()
        {
            _sink.Clear();
            await _testKit.AwaitConditionAsync(() => _sink.Writes.Count == 0);

            _loggingAdapter.Info("Order {OrderId} for customer {CustomerId}: {Amount} {Currency}",
                "ORD-001", "CUST-456", 99.99, "USD");

            await _testKit.AwaitConditionAsync(() => 
                AssertCondition(logEvent =>
                {
                    logEvent.Properties.Should().ContainKeys("OrderId", "CustomerId", "Amount", "Currency");
                    logEvent.Properties["OrderId"].ToString().Should().Be("\"ORD-001\"");
                    logEvent.Properties["CustomerId"].ToString().Should().Be("\"CUST-456\"");
                    logEvent.Properties["Amount"].ToString().Should().Be("99.99");
                    logEvent.Properties["Currency"].ToString().Should().Be("\"USD\"");
                }));
        }

        [Fact(DisplayName = "Should preserve Akka metadata properties alongside semantic logging properties")]
        public async Task AkkaMetadataAndSemanticPropertiesTest()
        {
            _sink.Clear();
            await _testKit.AwaitConditionAsync(() => _sink.Writes.Count == 0);

            _loggingAdapter.Info("User {UserId} action", 999);

            await _testKit.AwaitConditionAsync(() => 
                AssertCondition(logEvent =>
                {
                    // Semantic property
                    logEvent.Properties.Should().ContainKey("UserId");
                    logEvent.Properties["UserId"].ToString().Should().Be("999");

                    // Akka metadata properties
                    logEvent.Properties.Should().ContainKey("ActorPath");
                    logEvent.Properties.Should().ContainKey("LogSource");
                    logEvent.Properties.Should().ContainKey("Thread");
                }));
        }

        [Fact(DisplayName = "Should handle Serilog destructuring operator")]
        public async Task DestructuringOperatorTest()
        {
            _sink.Clear();
            await _testKit.AwaitConditionAsync(() => _sink.Writes.Count == 0);

            var user = new { Name = "Alice", Age = 30, Role = "Admin" };
            _loggingAdapter.Info("Processing user {@User}", user);

            await _testKit.AwaitConditionAsync(() => 
                AssertCondition(logEvent =>
                {
                    // Property name is "User" (@ operator removed by Akka's parser)
                    logEvent.Properties.Should().ContainKey("User");

                    // Serilog should have destructured the object
                    var userProperty = logEvent.Properties["User"];
                    userProperty.Should().BeOfType<StructureValue>();

                    var structure = (StructureValue)userProperty;
                    structure.Properties.Should().Contain(p => p.Name == "Name");
                    structure.Properties.Should().Contain(p => p.Name == "Age");
                    structure.Properties.Should().Contain(p => p.Name == "Role");
                }));
        }

        [Fact(DisplayName = "Should handle Serilog stringification operator")]
        public async Task StringificationOperatorTest()
        {
            _sink.Clear();
            await _testKit.AwaitConditionAsync(() => _sink.Writes.Count == 0);

            var exception = new InvalidOperationException("Test error");
            _loggingAdapter.Info("Error occurred: {$Exception}", exception);

            await _testKit.AwaitConditionAsync(() => 
                AssertCondition(logEvent =>
                {
                    // Property name is "Exception" ($ operator removed by Akka's parser)
                    logEvent.Properties.Should().ContainKey("Exception");

                    // Serilog should have used ToString() instead of destructuring
                    var exceptionProperty = logEvent.Properties["Exception"];
                    exceptionProperty.Should().BeOfType<ScalarValue>();
                }));
        }

        [Fact(DisplayName = "Should handle format specifiers in named templates")]
        public async Task FormatSpecifiersInTemplatesTest()
        {
            _sink.Clear();
            await _testKit.AwaitConditionAsync(() => _sink.Writes.Count == 0);

            _loggingAdapter.Info("Total amount: {Amount:N2}", 1234.5678);

            await _testKit.AwaitConditionAsync(() => 
                AssertCondition(logEvent =>
                {
                    // Property name is "Amount" (format specifier removed by Akka's parser)
                    logEvent.Properties.Should().ContainKey("Amount");

                    // The rendered message should apply the format
                    logEvent.RenderMessage().Should().Contain("1,234.57");
                }));
        }

        [Fact(DisplayName = "Should handle empty/no properties gracefully")]
        public async Task NoPropertiesTest()
        {
            _sink.Clear();
            await _testKit.AwaitConditionAsync(() => _sink.Writes.Count == 0);

            _loggingAdapter.Info("No template properties here");

            await _testKit.AwaitConditionAsync(() => 
                AssertCondition(logEvent =>
                {
                    // Should still have Akka metadata properties
                    logEvent.Properties.Should().ContainKey("ActorPath");
                    logEvent.Properties.Should().ContainKey("LogSource");
                    logEvent.Properties.Should().ContainKey("Thread");

                    // Message content should be preserved
                    logEvent.RenderMessage().Should().Contain("No template properties here");
                }));
        }

        [Fact(DisplayName = "Should work with ForContext enrichment")]
        public async Task ForContextWithSemanticLoggingTest()
        {
            _sink.Clear();
            await _testKit.AwaitConditionAsync(() => _sink.Writes.Count == 0);

            var contextLogger = _loggingAdapter.ForContext("TenantId", "TENANT-123");
            contextLogger.Info("User {UserId} performed action", 456);
            
            await _testKit.AwaitConditionAsync(() => 
                AssertCondition(logEvent =>
                {
                    // Should have both semantic property and context enrichment
                    logEvent.Properties.Should().ContainKey("UserId");
                    logEvent.Properties["UserId"].ToString().Should().Be("456");

                    logEvent.Properties.Should().ContainKey("TenantId");
                    logEvent.Properties["TenantId"].ToString().Should().Be("\"TENANT-123\"");
                }));
        }

        private bool AssertCondition(Action<LogEvent> assertion)
        {
            while (_sink.Writes.TryDequeue(out var logEvent))
            {
                try
                {
                    assertion(logEvent);
                    return true;
                }
                catch
                {
                    // no-op
                }
            }
            return false;
        }
    }
}
