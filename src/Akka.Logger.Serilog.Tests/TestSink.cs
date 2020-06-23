using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Akka.Logger.Serilog.Tests
{
    /// <inheritdoc />
    /// <summary>
    /// Basic concurrent sink implementation for testing the final output from Serilog
    /// </summary>
    public sealed class TestSink : ILogEventSink
    {
        public ConcurrentQueue<global::Serilog.Events.LogEvent> Writes { get; private set; } = new ConcurrentQueue<global::Serilog.Events.LogEvent>();

        /// <summary>
        /// Resets the contents of the queue
        /// </summary>
        public void Clear()
        {
            Writes = new ConcurrentQueue<LogEvent>();
        }

        public void Emit(global::Serilog.Events.LogEvent logEvent)
        {
            Writes.Enqueue(logEvent);
        }
    }
}