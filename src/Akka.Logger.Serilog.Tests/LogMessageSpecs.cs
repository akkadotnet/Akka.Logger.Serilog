using System;
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
        public void ShouldLogDebugLevelMessage()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            _testKit.AwaitCondition(() => _sink.Writes.Count == 0);

            context.Debug("hi");

            _testKit.AwaitCondition(() =>
                AssertCondition(logEvent => logEvent.Level == LogEventLevel.Debug
                                            && logEvent.RenderMessage() == "hi")
            );
        }

        [Fact]
        public void ShouldLogMessageWithPropertyEnrichers()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            _testKit.AwaitCondition(() => _sink.Writes.Count == 0);

            context.Debug("Hi {0}", "Harry Potter", 
                new PropertyEnricher("Address", "No. 4 Privet Drive"),
                new PropertyEnricher("Town", "Little Whinging"),
                new PropertyEnricher("County", "Surrey"),
                new PropertyEnricher("Country", "England"));
            
            _testKit.AwaitCondition(() =>
                AssertCondition(logEvent =>
                {
                    try
                    {
                        logEvent.Level.Should().Be(LogEventLevel.Debug);
                        logEvent.RenderMessage().Should().Contain("Hi \"Harry Potter\"");
                        logEvent.Properties.Should().ContainKeys("Address", "Town", "County", "Country");
                        logEvent.Properties["Address"].ToString().Should().Be("\"No. 4 Privet Drive\"");
                        logEvent.Properties["Town"].ToString().Should().Be("\"Little Whinging\"");
                        logEvent.Properties["County"].ToString().Should().Be("\"Surrey\"");
                        logEvent.Properties["Country"].ToString().Should().Be("\"England\"");
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                })
            );
        }

        [Fact]
        public void ShouldLogDebugLevelMessageWithArgs()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            _testKit.AwaitCondition(() => _sink.Writes.Count == 0);

            context.Debug("hi {0}", "test");

            _testKit.AwaitCondition(() =>
                AssertCondition(logEvent => logEvent.Level == LogEventLevel.Debug
                                            && logEvent.RenderMessage() == "hi \"test\"")
            );
        }

        [Fact]
        public void ShouldLogDebugLevelMessageWithException()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            _testKit.AwaitCondition(() => _sink.Writes.Count == 0);

            var exception = new Exception("BOOM!!!");
            context.Debug(exception, "hi");

            _testKit.AwaitCondition(() =>
                AssertCondition(logEvent => logEvent.Level == LogEventLevel.Debug
                                            && logEvent.Exception == exception
                                            && logEvent.RenderMessage() == "hi")
            );
        }

        [Fact]
        public void ShouldLogDebugLevelMessageWithArgsAndException()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            _testKit.AwaitCondition(() => _sink.Writes.Count == 0);

            var exception = new Exception("BOOM!!!");
            context.Debug(exception, "hi {0}", "test");

            _testKit.AwaitCondition(() =>
                AssertCondition(logEvent => logEvent.Level == LogEventLevel.Debug
                                            && logEvent.Exception == exception
                                            && logEvent.RenderMessage() == "hi \"test\"")
            );
        }
        
        [Fact]
        public void ShouldLogInfoLevelMessage()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            _testKit.AwaitCondition(() => _sink.Writes.Count == 0);

            context.Info("hi");

            _testKit.AwaitCondition(() =>
                AssertCondition(logEvent => logEvent.Level == LogEventLevel.Information
                                            && logEvent.RenderMessage() == "hi")
            );
        }

        [Fact]
        public void ShouldLogInfoLevelMessageWithArgs()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            _testKit.AwaitCondition(() => _sink.Writes.Count == 0);

            context.Info("hi {0}", "test");

            _testKit.AwaitCondition(() =>
                AssertCondition(logEvent => logEvent.Level == LogEventLevel.Information
                                            && logEvent.RenderMessage() == "hi \"test\"")
            );
        }

        [Fact]
        public void ShouldLogInfoLevelMessageWithException()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            _testKit.AwaitCondition(() => _sink.Writes.Count == 0);

            var exception = new Exception("BOOM!!!");
            context.Info(exception, "hi");

            _testKit.AwaitCondition(() =>
                AssertCondition(logEvent => logEvent.Level == LogEventLevel.Information
                                            && logEvent.Exception == exception
                                            && logEvent.RenderMessage() == "hi")
            );
        }

        [Fact]
        public void ShouldLogInfoLevelMessageWithArgsAndException()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            _testKit.AwaitCondition(() => _sink.Writes.Count == 0);

            var exception = new Exception("BOOM!!!");
            context.Info(exception, "hi {0}", "test");

            _testKit.AwaitCondition(() =>
                AssertCondition(logEvent => logEvent.Level == LogEventLevel.Information
                                            && logEvent.Exception == exception
                                            && logEvent.RenderMessage() == "hi \"test\"")
            );
        }
        
        [Fact]
        public void ShouldLogWarningLevelMessage()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            _testKit.AwaitCondition(() => _sink.Writes.Count == 0);

            context.Warning("hi");

            _testKit.AwaitCondition(() =>
                AssertCondition(logEvent => logEvent.Level == LogEventLevel.Warning
                                            && logEvent.RenderMessage() == "hi")
            );
        }

        [Fact]
        public void ShouldLogWarningLevelMessageWithArgs()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            _testKit.AwaitCondition(() => _sink.Writes.Count == 0);

            context.Warning("hi {0}", "test");

            _testKit.AwaitCondition(() =>
                AssertCondition(logEvent => logEvent.Level == LogEventLevel.Warning
                                            && logEvent.RenderMessage() == "hi \"test\"")
            );
        }

        [Fact]
        public void ShouldLogWarningLevelMessageWithException()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            _testKit.AwaitCondition(() => _sink.Writes.Count == 0);

            var exception = new Exception("BOOM!!!");
            context.Warning(exception, "hi");

            _testKit.AwaitCondition(() =>
                AssertCondition(logEvent => logEvent.Level == LogEventLevel.Warning
                                            && logEvent.Exception == exception
                                            && logEvent.RenderMessage() == "hi")
            );
        }

        [Fact]
        public void ShouldLogWarningLevelMessageWithArgsAndException()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            _testKit.AwaitCondition(() => _sink.Writes.Count == 0);

            var exception = new Exception("BOOM!!!");
            context.Warning(exception, "hi {0}", "test");

            _testKit.AwaitCondition(() =>
                AssertCondition(logEvent => logEvent.Level == LogEventLevel.Warning
                                            && logEvent.Exception == exception
                                            && logEvent.RenderMessage() == "hi \"test\"")
            );
        }
        
        [Fact]
        public void ShouldLogErrorLevelMessage()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            _testKit.AwaitCondition(() => _sink.Writes.Count == 0);

            context.Error("hi");

            _testKit.AwaitCondition(() =>
                AssertCondition(logEvent => logEvent.Level == LogEventLevel.Error
                                            && logEvent.RenderMessage() == "hi")
            );
        }

        [Fact]
        public void ShouldLogErrorLevelMessageWithArgs()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            _testKit.AwaitCondition(() => _sink.Writes.Count == 0);

            context.Error("hi {0}", "test");

            _testKit.AwaitCondition(() =>
                AssertCondition(logEvent => logEvent.Level == LogEventLevel.Error
                                            && logEvent.RenderMessage() == "hi \"test\"")
            );
        }

        [Fact]
        public void ShouldLogErrorLevelMessageWithException()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            _testKit.AwaitCondition(() => _sink.Writes.Count == 0);

            var exception = new Exception("BOOM!!!");
            context.Error(exception, "hi");

            _testKit.AwaitCondition(() =>
                AssertCondition(logEvent => logEvent.Level == LogEventLevel.Error
                                            && logEvent.Exception == exception
                                            && logEvent.RenderMessage() == "hi")
            );
        }

        [Fact]
        public void ShouldLogErrorLevelMessageWithArgsAndException()
        {
            var context = _loggingAdapter;

            _sink.Clear();
            _testKit.AwaitCondition(() => _sink.Writes.Count == 0);

            var exception = new Exception("BOOM!!!");
            context.Error(exception, "hi {0}", "test");

            _testKit.AwaitCondition(() =>
                AssertCondition(logEvent => logEvent.Level == LogEventLevel.Error
                                            && logEvent.Exception == exception
                                            && logEvent.RenderMessage() == "hi \"test\"")
            );
        }

        private bool AssertCondition(Func<LogEvent, bool> condition)
        {
            while (_sink.Writes.TryDequeue(out var logEvent))
            {
                if (condition(logEvent))
                    return true;
            }
            return false;
        }
    }
}