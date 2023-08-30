using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using FluentAssertions;
using Serilog;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;
using LogEvent = Akka.Event.LogEvent;

namespace Akka.Logger.Serilog.Tests
{
    public class LogMessageSpecs : TestKit.Xunit2.TestKit
    {
        public static readonly Config Config = @"akka.loglevel = DEBUG
                                                 akka.loggers=[""Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog""]";

        private readonly ILoggingAdapter _loggingAdapter;
        private readonly TestSink _sink;

        public LogMessageSpecs(ITestOutputHelper helper) : base(Config, output: helper)
        {
            _sink = new TestSink(helper);
            global::Serilog.Log.Logger = new LoggerConfiguration()
                .WriteTo.Sink(_sink)
                .MinimumLevel.Debug()
                .CreateLogger();
            _loggingAdapter = Sys.Log;
        }

        [Fact]
        public void LogOutputRegressionTest()
        {
            const string message = "{0} {1} {2} {3}";
            const string expectedMessage = "[0, 1, 2] [0.1, 0.2, 0.3] [\"One\", \"Two\"] [1, 2, 3]";
            var args = new object[]
            {
                new int[] { 0, 1, 2 },
                new double[] { 0.1, 0.2, 0.3 },
                new string[] { "One", "Two" },
                new List<double> { 1, 2, 3 }
            };
            
            _sink.Clear();
            AwaitCondition(() => _sink.Writes.Count == 0);
            
            global::Serilog.Log.Logger.Information(message, args);
            AwaitCondition(() => _sink.Writes.Count == 1);

            _sink.Writes.TryDequeue(out var logEvent).Should().BeTrue();
            logEvent.RenderMessage().Should().Be(expectedMessage);
            
            Sys.EventStream.Subscribe(TestActor, typeof(LogEvent));
            _loggingAdapter.Info(message, args);
            var akkaLogEvent = ExpectMsg<LogEvent>();
            AwaitCondition(() => _sink.Writes.Count == 1);
            _sink.Writes.TryDequeue(out logEvent).Should().BeTrue();

            logEvent.RenderMessage().Should().Contain(expectedMessage);
            akkaLogEvent.ToString().Should().Contain(expectedMessage);
        }
        
        [Fact]
        public void ShouldLogDebugLevelMessage()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            AwaitCondition(() => _sink.Writes.Count == 0);

            context.Debug("hi");
            AwaitCondition(() => _sink.Writes.Count == 1);

            _sink.Writes.TryDequeue(out var logEvent).Should().BeTrue();
            logEvent.Level.Should().Be(LogEventLevel.Debug);
            logEvent.RenderMessage().Should().Contain("hi");
        }

        [Fact]
        public void ShouldLogDebugLevelMessageWithArgs()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            AwaitCondition(() => _sink.Writes.Count == 0);

            context.Debug("hi {0}", "test");
            AwaitCondition(() => _sink.Writes.Count == 1);

            _sink.Writes.TryDequeue(out var logEvent).Should().BeTrue();
            logEvent.Level.Should().Be(LogEventLevel.Debug);
            logEvent.RenderMessage().Should().Contain("hi \"test\"");
        }

        [Fact]
        public void ShouldLogDebugLevelMessageWithException()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            AwaitCondition(() => _sink.Writes.Count == 0);

            var exception = new Exception("BOOM!!!");
            context.Debug(exception, "hi");
            AwaitCondition(() => _sink.Writes.Count == 1);

            _sink.Writes.TryDequeue(out var logEvent).Should().BeTrue();
            logEvent.Level.Should().Be(LogEventLevel.Debug);
            logEvent.Exception.Should().Be(exception);
        }

        [Fact]
        public void ShouldLogDebugLevelMessageWithArgsAndException()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            AwaitCondition(() => _sink.Writes.Count == 0);

            var exception = new Exception("BOOM!!!");
            context.Debug(exception, "hi {0}", "test");
            AwaitCondition(() => _sink.Writes.Count == 1);

            _sink.Writes.TryDequeue(out var logEvent).Should().BeTrue();
            logEvent.Level.Should().Be(LogEventLevel.Debug);
            logEvent.Exception.Should().Be(exception);
        }
        
        [Fact]
        public void ShouldLogInfoLevelMessage()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            AwaitCondition(() => _sink.Writes.Count == 0);

            context.Info("hi");
            AwaitCondition(() => _sink.Writes.Count == 1);

            _sink.Writes.TryDequeue(out var logEvent).Should().BeTrue();
            logEvent.Level.Should().Be(LogEventLevel.Information);
            logEvent.RenderMessage().Should().Contain("hi");
        }

        [Fact]
        public void ShouldLogInfoLevelMessageWithArgs()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            AwaitCondition(() => _sink.Writes.Count == 0);

            context.Info("hi {0}", "test");
            AwaitCondition(() => _sink.Writes.Count == 1);

            _sink.Writes.TryDequeue(out var logEvent).Should().BeTrue();
            logEvent.Level.Should().Be(LogEventLevel.Information);
            logEvent.RenderMessage().Should().Contain("hi \"test\"");
        }

        [Fact]
        public void ShouldLogInfoLevelMessageWithException()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            AwaitCondition(() => _sink.Writes.Count == 0);

            var exception = new Exception("BOOM!!!");
            context.Info(exception, "hi");
            AwaitCondition(() => _sink.Writes.Count == 1);

            _sink.Writes.TryDequeue(out var logEvent).Should().BeTrue();
            logEvent.Level.Should().Be(LogEventLevel.Information);
            logEvent.Exception.Should().Be(exception);
        }

        [Fact]
        public void ShouldLogInfoLevelMessageWithArgsAndException()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            AwaitCondition(() => _sink.Writes.Count == 0);

            var exception = new Exception("BOOM!!!");
            context.Info(exception, "hi {0}", "test");
            AwaitCondition(() => _sink.Writes.Count == 1);

            _sink.Writes.TryDequeue(out var logEvent).Should().BeTrue();
            logEvent.Level.Should().Be(LogEventLevel.Information);
            logEvent.Exception.Should().Be(exception);
        }
        
        [Fact]
        public void ShouldLogWarningLevelMessage()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            AwaitCondition(() => _sink.Writes.Count == 0);

            context.Warning("hi");
            AwaitCondition(() => _sink.Writes.Count == 1);

            _sink.Writes.TryDequeue(out var logEvent).Should().BeTrue();
            logEvent.Level.Should().Be(LogEventLevel.Warning);
            logEvent.RenderMessage().Should().Contain("hi");
        }

        [Fact]
        public void ShouldLogWarningLevelMessageWithArgs()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            AwaitCondition(() => _sink.Writes.Count == 0);

            context.Warning("hi {0}", "test");
            AwaitCondition(() => _sink.Writes.Count == 1);

            _sink.Writes.TryDequeue(out var logEvent).Should().BeTrue();
            logEvent.Level.Should().Be(LogEventLevel.Warning);
            logEvent.RenderMessage().Should().Contain("hi \"test\"");
        }

        [Fact]
        public void ShouldLogWarningLevelMessageWithException()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            AwaitCondition(() => _sink.Writes.Count == 0);

            var exception = new Exception("BOOM!!!");
            context.Warning(exception, "hi");
            AwaitCondition(() => _sink.Writes.Count == 1);

            _sink.Writes.TryDequeue(out var logEvent).Should().BeTrue();
            logEvent.Level.Should().Be(LogEventLevel.Warning);
            logEvent.Exception.Should().Be(exception);
        }

        [Fact]
        public void ShouldLogWarningLevelMessageWithArgsAndException()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            AwaitCondition(() => _sink.Writes.Count == 0);

            var exception = new Exception("BOOM!!!");
            context.Warning(exception, "hi {0}", "test");
            AwaitCondition(() => _sink.Writes.Count == 1);

            _sink.Writes.TryDequeue(out var logEvent).Should().BeTrue();
            logEvent.Level.Should().Be(LogEventLevel.Warning);
            logEvent.Exception.Should().Be(exception);
        }
        
        [Fact]
        public void ShouldLogErrorLevelMessage()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            AwaitCondition(() => _sink.Writes.Count == 0);

            context.Error("hi");
            AwaitCondition(() => _sink.Writes.Count == 1);

            _sink.Writes.TryDequeue(out var logEvent).Should().BeTrue();
            logEvent.Level.Should().Be(LogEventLevel.Error);
            logEvent.RenderMessage().Should().Contain("hi");
        }

        [Fact]
        public void ShouldLogErrorLevelMessageWithArgs()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            AwaitCondition(() => _sink.Writes.Count == 0);

            context.Error("hi {0}", "test");
            AwaitCondition(() => _sink.Writes.Count == 1);

            _sink.Writes.TryDequeue(out var logEvent).Should().BeTrue();
            logEvent.Level.Should().Be(LogEventLevel.Error);
            logEvent.RenderMessage().Should().Contain("hi \"test\"");
        }

        [Fact]
        public void ShouldLogErrorLevelMessageWithException()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            AwaitCondition(() => _sink.Writes.Count == 0);

            var exception = new Exception("BOOM!!!");
            context.Error(exception, "hi");
            AwaitCondition(() => _sink.Writes.Count == 1);

            _sink.Writes.TryDequeue(out var logEvent).Should().BeTrue();
            logEvent.Level.Should().Be(LogEventLevel.Error);
            logEvent.Exception.Should().Be(exception);
        }

        [Fact]
        public void ShouldLogErrorLevelMessageWithArgsAndException()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            AwaitCondition(() => _sink.Writes.Count == 0);

            var exception = new Exception("BOOM!!!");
            context.Error(exception, "hi {0}", "test");
            AwaitCondition(() => _sink.Writes.Count == 1);

            _sink.Writes.TryDequeue(out var logEvent).Should().BeTrue();
            logEvent.Level.Should().Be(LogEventLevel.Error);
            logEvent.Exception.Should().Be(exception);
        }
    }
}