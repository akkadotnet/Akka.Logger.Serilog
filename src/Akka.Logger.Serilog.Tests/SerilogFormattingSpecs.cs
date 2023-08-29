using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using FluentAssertions;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using SerilogLog = Serilog.Log;

namespace Akka.Logger.Serilog.Tests
{
    public class SerilogFormattingSpecs : TestKit.Xunit2.TestKit
    {
        public static readonly Config Config = 
@"
akka.loglevel = DEBUG
# akka.loggers=[""Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog""]
";
        private readonly ILogger _serilogLogger;
        private readonly TestSink _sink;

        private readonly ILoggingAdapter _loggingAdapter;

        public SerilogFormattingSpecs(ITestOutputHelper helper) : base(Config, output: helper)
        {
            _sink = new TestSink(helper);
            
            _serilogLogger = new LoggerConfiguration()
                .WriteTo.ColoredConsole()
                .WriteTo.Sink(_sink)
                .MinimumLevel.Information()
                .CreateLogger();
            
            SerilogLog.Logger = _serilogLogger;

            var logSource = Sys.Name;
            var logClass = typeof(ActorSystem);

            _loggingAdapter = new SerilogLoggingAdapter(Sys.EventStream, logSource, logClass);
        }

        [Fact]
        public void LogOutputRegressionTest()
        {
            const string message = "{IntArray} {DoubleArray} {StringArray} {DoubleList}";
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
            
            _serilogLogger.Information(message, args);
            AwaitCondition(() => _sink.Writes.Count == 1);

            _sink.Writes.TryDequeue(out var logEvent).Should().BeTrue();
            logEvent.RenderMessage().Should().Be(expectedMessage);
            
            Sys.EventStream.Subscribe(TestActor, typeof(LogEvent));
            _loggingAdapter.Log(LogLevel.InfoLevel, message, args);
            var akkaLogEvent = ExpectMsg<LogEvent>();

            akkaLogEvent.ToString().Should().Contain(expectedMessage);
        }
        
        [Theory]
        [InlineData(LogLevel.DebugLevel, "test case {0}", new object[]{ 1 })]
        [InlineData(LogLevel.DebugLevel, "test case {myNum}", new object[] { 1 })]
        [InlineData(LogLevel.DebugLevel, "test case {myNum} {myStr}", new object[] { 1, "foo" })]
        public void ShouldHandleSerilogFormats(LogLevel level, string formatStr, object[] args)
        {
            Sys.EventStream.Subscribe(TestActor, typeof(LogEvent));

            Action logWrite = () =>
            {
                _loggingAdapter.Log(level, formatStr, args);

                var logEvent = ExpectMsg<LogEvent>();
                logEvent.LogLevel().Should().Be(level);
                logEvent.ToString().Should().NotBeEmpty();
            };

            logWrite.Should().NotThrow<FormatException>();
        }
    }
}
