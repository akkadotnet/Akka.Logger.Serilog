using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using Akka.Logger.Serilog.Tests.Generator;
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
        private ILoggingAdapter _loggingAdapter;

        public SerilogFormattingSpecs(ITestOutputHelper helper) : base(Config, output: helper)
        {
            _sink = new TestSink(helper);
            
            _serilogLogger = new LoggerConfiguration()
                .WriteTo.Sink(_sink)
                .WriteTo.ColoredConsole()
                .MinimumLevel.Information()
                .CreateLogger();
            
            SerilogLog.Logger = _serilogLogger;

            var logSource = Sys.Name;
            var logClass = typeof(ActorSystem);

            _loggingAdapter = new SerilogLoggingAdapter(Sys.EventStream, logSource, logClass);
        }

        [Theory(DisplayName = "Serilog output must be compatible with previous version")]
        [MemberData(nameof(MessageFormatDataGenerator))]
        public async Task LogOutputRegressionTest(string version, string expected, string messageFormat, object[] args)
        {
            _sink.Clear();
            await AwaitConditionAsync(() => _sink.Writes.Count == 0);
            
            _serilogLogger.Information(messageFormat, args);
            await AwaitConditionAsync(() => _sink.Writes.Count == 1);

            _sink.Writes.TryDequeue(out var logEvent).Should().BeTrue();
            logEvent.RenderMessage().Should().Be(expected);
            
            Sys.EventStream.Subscribe(TestActor, typeof(LogEvent));
            _loggingAdapter.Log(LogLevel.InfoLevel, messageFormat, args);
            var akkaLogEvent = ExpectMsg<LogEvent>();

            akkaLogEvent.ToString().Should().Contain(expected);
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

        public static IEnumerable<object[]> MessageFormatDataGenerator()
        {
            var testDataFolder = new DirectoryInfo(Path.Combine(".", "TestFiles"));
            foreach (var fileInfo in testDataFolder.EnumerateFiles())
            {
                var version = Path.GetFileNameWithoutExtension(fileInfo.Name);
                var logOutputs = File.ReadLines(fileInfo.FullName).ToArray();
                foreach (var i in Enumerable.Range(0, TestData.Args.Length))
                {
                    var expected = logOutputs[i];
                    var messageFormat = TestData.MessageFormats[i];
                    var args = TestData.Args[i];
                    yield return new object[] { version, expected, messageFormat, args };
                }
            }
        }
    }
}
