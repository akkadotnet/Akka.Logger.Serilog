using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Akka.Logger.Serilog.Tests
{
    /// <inheritdoc />
    /// <summary>
    /// Basic concurrent sink implementation for testing the final output from Serilog
    /// </summary>
    public sealed class TestSink : ILogEventSink
    {
        public ConcurrentQueue<LogEvent> Writes { get; } = new ();

        private readonly ITestOutputHelper _output;
        private int _count;

        public TestSink(): this(null)
        { }
        
        public TestSink(ITestOutputHelper output)
        {
            _output = output;
        }


        /// <summary>
        /// Resets the contents of the queue
        /// </summary>
        public void Clear()
        {
            while (Writes.TryDequeue(out _))
            { }
        }

        public void Emit(LogEvent logEvent)
        {
            _count++;
            _output?.WriteLine($"[{nameof(TestSink)}][{_count}]: {logEvent.RenderMessage()}");
            Writes.Enqueue(logEvent);
        }
    }
}