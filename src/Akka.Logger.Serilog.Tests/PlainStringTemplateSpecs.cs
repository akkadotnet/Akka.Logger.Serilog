// -----------------------------------------------------------------------
//  <copyright file="PlainStringTemplateSpecs.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2025 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using FluentAssertions;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using LogEvent = Serilog.Events.LogEvent;

namespace Akka.Logger.Serilog.Tests;

/// <summary>
/// Tests for plain string template handling in Serilog integration.
///
/// Issue: When logging plain strings without placeholders, all messages were
/// collapsed to the same "{Message:l}" template, making them indistinguishable
/// in Seq's @EventType filter.
///
/// Fix: Plain strings are now used as their own template, giving each unique
/// log message its own @EventType.
/// </summary>
public class PlainStringTemplateSpecs : IAsyncLifetime
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

    public PlainStringTemplateSpecs(ITestOutputHelper helper)
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

    /// <summary>
    /// Plain string logs should use the string itself as the message template.
    /// This ensures each unique plain string gets its own @EventType in Seq.
    /// </summary>
    [Fact(DisplayName = "Plain string should be used as message template")]
    public async Task PlainStringShouldBeUsedAsMessageTemplate()
    {
        _sink.Clear();

        await _testKit.AwaitAssertAsync(() =>
        {
            _loggingAdapter.Info("User logged in");
            var logEvent = GetMatchingLogEvent(e => e.RenderMessage().Contains("User logged in"));
            logEvent.Should().NotBeNull();

            // The message template should be the exact string, not "{Message:l}"
            logEvent!.MessageTemplate.Text.Should().Be("User logged in");
        });
    }

    /// <summary>
    /// Different plain strings should have different message templates,
    /// ensuring they can be distinguished by @EventType in Seq.
    /// </summary>
    [Fact(DisplayName = "Different plain strings should have different message templates")]
    public async Task DifferentPlainStringsShouldHaveDifferentMessageTemplates()
    {
        _sink.Clear();

        await _testKit.AwaitAssertAsync(() =>
        {
            _loggingAdapter.Info("User logged in");
            _loggingAdapter.Info("Order completed");
            _loggingAdapter.Info("Payment processed");

            var logEvents = _sink.Writes.ToArray();
            logEvents.Should().HaveCountGreaterOrEqualTo(3);

            var loginEvent = logEvents.FirstOrDefault(e => e.RenderMessage().Contains("User logged in"));
            var orderEvent = logEvents.FirstOrDefault(e => e.RenderMessage().Contains("Order completed"));
            var paymentEvent = logEvents.FirstOrDefault(e => e.RenderMessage().Contains("Payment processed"));

            loginEvent.Should().NotBeNull();
            orderEvent.Should().NotBeNull();
            paymentEvent.Should().NotBeNull();

            // Each should have its own unique template
            loginEvent!.MessageTemplate.Text.Should().Be("User logged in");
            orderEvent!.MessageTemplate.Text.Should().Be("Order completed");
            paymentEvent!.MessageTemplate.Text.Should().Be("Payment processed");

            // Templates should all be different (for @EventType filtering)
            loginEvent.MessageTemplate.Text.Should().NotBe(orderEvent.MessageTemplate.Text);
            loginEvent.MessageTemplate.Text.Should().NotBe(paymentEvent.MessageTemplate.Text);
            orderEvent.MessageTemplate.Text.Should().NotBe(paymentEvent.MessageTemplate.Text);
        });
    }

    /// <summary>
    /// Plain strings should NOT have the Message property added (unlike {Message:l} template).
    /// </summary>
    [Fact(DisplayName = "Plain string should not add Message property")]
    public async Task PlainStringShouldNotAddMessageProperty()
    {
        _sink.Clear();

        await _testKit.AwaitAssertAsync(() =>
        {
            _loggingAdapter.Info("Simple log message");
            var logEvent = GetMatchingLogEvent(e => e.RenderMessage().Contains("Simple log message"));
            logEvent.Should().NotBeNull();

            // Should NOT have a "Message" property since the string IS the template
            logEvent!.Properties.Should().NotContainKey("Message");

            // But should still have Akka metadata
            logEvent.Properties.Should().ContainKey("ActorPath");
            logEvent.Properties.Should().ContainKey("LogSource");
        });
    }

    /// <summary>
    /// Templated messages with placeholders should still work correctly.
    /// </summary>
    [Fact(DisplayName = "Templated messages with placeholders should still work")]
    public async Task TemplatedMessagesWithPlaceholdersShouldWork()
    {
        _sink.Clear();

        await _testKit.AwaitAssertAsync(() =>
        {
            _loggingAdapter.Info("User {UserId} logged in from {IpAddress}", 12345, "192.168.1.1");
            var logEvent = GetMatchingLogEvent(e => e.Properties.ContainsKey("UserId"));
            logEvent.Should().NotBeNull();

            // Template should preserve placeholders
            logEvent!.MessageTemplate.Text.Should().Be("User {UserId} logged in from {IpAddress}");

            // Properties should be extracted
            logEvent.Properties["UserId"].ToString().Should().Be("12345");
            logEvent.Properties["IpAddress"].ToString().Should().Be("\"192.168.1.1\"");
        });
    }

    /// <summary>
    /// Same plain string logged multiple times should have identical templates.
    /// </summary>
    [Fact(DisplayName = "Same plain string should always produce identical template")]
    public async Task SamePlainStringShouldAlwaysProduceIdenticalTemplate()
    {
        _sink.Clear();

        await _testKit.AwaitAssertAsync(() =>
        {
            _loggingAdapter.Info("Repeated message");
            _loggingAdapter.Info("Repeated message");
            _loggingAdapter.Info("Repeated message");

            var logEvents = _sink.Writes.ToArray()
                .Where(e => e.RenderMessage().Contains("Repeated message"))
                .ToArray();

            logEvents.Should().HaveCountGreaterOrEqualTo(3);

            // All should have the same template
            var templates = logEvents.Select(e => e.MessageTemplate.Text).Distinct().ToArray();
            templates.Should().HaveCount(1);
            templates[0].Should().Be("Repeated message");
        });
    }

    /// <summary>
    /// REGRESSION TEST: Verifies fix for https://github.com/akkadotnet/Akka.Logger.Serilog/issues/306
    ///
    /// Issue: Customer reported that plain string log messages all collapsed to the same
    /// @EventType in Seq, making it impossible to filter logs by message type.
    ///
    /// Root cause: GetFormat() returned the string as template, but GetArgs() also
    /// returned the string as an argument, causing inconsistent Serilog behavior.
    ///
    /// Fix: Plain strings are now used as their own template with empty args array.
    /// This gives each unique message its own @EventType hash in Serilog/Seq.
    /// </summary>
    [Fact(DisplayName = "REGRESSION #306: Plain strings should have unique EventType hashes for Seq filtering")]
    public async Task Regression306_PlainStringsShouldHaveUniqueEventTypeHashes()
    {
        _sink.Clear();

        await _testKit.AwaitAssertAsync(() =>
        {
            // Simulate the customer's scenario: multiple different plain string messages
            _loggingAdapter.Info("Application started");
            _loggingAdapter.Info("Database connection established");
            _loggingAdapter.Info("Cache warmed up");
            _loggingAdapter.Info("Application started"); // Duplicate to verify same message = same hash

            var logEvents = _sink.Writes.ToArray();
            logEvents.Should().HaveCountGreaterOrEqualTo(4);

            var appStartedEvents = logEvents.Where(e => e.RenderMessage() == "Application started").ToArray();
            var dbConnectedEvent = logEvents.FirstOrDefault(e => e.RenderMessage() == "Database connection established");
            var cacheWarmedEvent = logEvents.FirstOrDefault(e => e.RenderMessage() == "Cache warmed up");

            appStartedEvents.Should().HaveCountGreaterOrEqualTo(2);
            dbConnectedEvent.Should().NotBeNull();
            cacheWarmedEvent.Should().NotBeNull();

            // CRITICAL: Each unique message must have its own template (for @EventType)
            // Before fix: All would have "{Message:l}" template
            // After fix: Each has its own string as template
            appStartedEvents[0].MessageTemplate.Text.Should().Be("Application started");
            dbConnectedEvent!.MessageTemplate.Text.Should().Be("Database connection established");
            cacheWarmedEvent!.MessageTemplate.Text.Should().Be("Cache warmed up");

            // Verify templates are NOT all the same (the bug)
            appStartedEvents[0].MessageTemplate.Text.Should().NotBe("{Message:l}",
                "plain strings should NOT use generic {Message:l} template");

            // Verify same message produces same template (for consistent @EventType)
            appStartedEvents[0].MessageTemplate.Text.Should().Be(appStartedEvents[1].MessageTemplate.Text,
                "same message should produce identical template for consistent @EventType");

            // Verify different messages produce different templates (for filtering)
            var uniqueTemplates = new[]
            {
                appStartedEvents[0].MessageTemplate.Text,
                dbConnectedEvent.MessageTemplate.Text,
                cacheWarmedEvent.MessageTemplate.Text
            }.Distinct().ToArray();

            uniqueTemplates.Should().HaveCount(3,
                "each unique plain string should have its own template for @EventType filtering in Seq");
        });
    }

    /// <summary>
    /// REGRESSION TEST: https://github.com/akkadotnet/Akka.Logger.Serilog/issues/306
    /// Verifies that the fix doesn't break templated messages.
    /// 
    /// Ensures semantic logging with named placeholders still works correctly.
    /// </summary>
    [Fact(DisplayName = "REGRESSION #306: Templated messages should still extract properties correctly")]
    public async Task Regression306_TemplatedMessagesShouldStillWork()
    {
        _sink.Clear();

        await _testKit.AwaitAssertAsync(() =>
        {
            // Mix of plain strings and templated messages (real-world scenario)
            _loggingAdapter.Info("Service initialized");
            _loggingAdapter.Info("Processing request {RequestId} from {ClientIp}", "REQ-001", "10.0.0.1");
            _loggingAdapter.Info("Request completed");
            _loggingAdapter.Info("User {UserId} performed {Action}", 12345, "Login");

            var logEvents = _sink.Writes.ToArray();
            logEvents.Should().HaveCountGreaterOrEqualTo(4);

            // Plain strings should use themselves as templates
            var initEvent = logEvents.FirstOrDefault(e => e.RenderMessage() == "Service initialized");
            var completedEvent = logEvents.FirstOrDefault(e => e.RenderMessage() == "Request completed");
            initEvent.Should().NotBeNull();
            completedEvent.Should().NotBeNull();
            initEvent!.MessageTemplate.Text.Should().Be("Service initialized");
            completedEvent!.MessageTemplate.Text.Should().Be("Request completed");

            // Templated messages should preserve template and extract properties
            var requestEvent = logEvents.FirstOrDefault(e => e.Properties.ContainsKey("RequestId"));
            var userEvent = logEvents.FirstOrDefault(e => e.Properties.ContainsKey("UserId"));
            requestEvent.Should().NotBeNull();
            userEvent.Should().NotBeNull();

            requestEvent!.MessageTemplate.Text.Should().Be("Processing request {RequestId} from {ClientIp}");
            requestEvent.Properties["RequestId"].ToString().Should().Be("\"REQ-001\"");
            requestEvent.Properties["ClientIp"].ToString().Should().Be("\"10.0.0.1\"");

            userEvent!.MessageTemplate.Text.Should().Be("User {UserId} performed {Action}");
            userEvent.Properties["UserId"].ToString().Should().Be("12345");
            userEvent.Properties["Action"].ToString().Should().Be("\"Login\"");
        });
    }

    private LogEvent? GetMatchingLogEvent(Func<LogEvent, bool> predicate)
    {
        return _sink.Writes.ToArray().FirstOrDefault(predicate);
    }
}