using System;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using FluentAssertions;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Logger.Serilog.Tests
{
    public class SerilogFormattingSpecs : TestKit.Xunit2.TestKit
    {
        public static readonly Config Config = @"akka.loglevel = DEBUG";
        private ILogger _serilogLogger;

        private ILoggingAdapter _loggingAdapter;

        public SerilogFormattingSpecs(ITestOutputHelper helper) : base(Config, output: helper)
        {
            _serilogLogger = new LoggerConfiguration()
                .WriteTo.ColoredConsole()
                .MinimumLevel.Information()
                .CreateLogger();

            var logSource = Sys.Name;
            var logClass = typeof(ActorSystem);

            _loggingAdapter = new SerilogLoggingAdapter(Sys.EventStream, logSource, logClass);
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
