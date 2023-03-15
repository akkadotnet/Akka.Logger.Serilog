using System;
using System.Linq;
using System.Collections.Generic;
using Akka.Event;
using Serilog.Core;
using Serilog.Core.Enrichers;

namespace Akka.Logger.Serilog
{
    internal readonly struct SerilogPayload
    {
        public SerilogPayload(object message, IReadOnlyList<ILogEventEnricher> enrichers)
        {
            Message = message;
            Enrichers = enrichers;
        }

        public IReadOnlyList<ILogEventEnricher> Enrichers { get; }
        
        public object Message { get; }

        public override string ToString()
        {
            return Message.ToString();
        }
    }
    
    public class SerilogLoggingAdapter : LoggingAdapterBase
    {
        private readonly LoggingBus _bus;
        private readonly Type _logClass;
        private readonly string _logSource;

        /// <summary>
        /// Helper class that allows context trees.
        /// </summary>
        private class ContextNode
        {
            public ContextNode Next { get; set; }
            public ILogEventEnricher Enricher { get; set; }
        }

        private readonly ContextNode _enricherNode;


        public SerilogLoggingAdapter(LoggingBus bus, string logSource, Type logClass) : this(bus, logSource, logClass, null)
        {
        }

        private SerilogLoggingAdapter(LoggingBus bus, string logSource, Type logClass, ContextNode enricher) : base(SerilogLogMessageFormatter.Instance)
        {
            _bus = bus;
            _logSource = logSource;
            _logClass = logClass;
            _enricherNode = enricher;
            
            IsErrorEnabled = bus.LogLevel <= LogLevel.ErrorLevel;
            IsWarningEnabled = bus.LogLevel <= LogLevel.WarningLevel;
            IsInfoEnabled = bus.LogLevel <= LogLevel.InfoLevel;
            IsDebugEnabled = bus.LogLevel <= LogLevel.DebugLevel;
        }
        
        private LogEvent CreateLogEvent(LogLevel logLevel, object message, Exception cause = null)
            => logLevel switch
            {
                LogLevel.DebugLevel => new Debug(cause, _logSource, _logClass, BuildMessage(message)),
                LogLevel.InfoLevel => new Info(cause, _logSource, _logClass, BuildMessage(message)),
                LogLevel.WarningLevel => new Warning(cause, _logSource, _logClass, BuildMessage(message)),
                LogLevel.ErrorLevel => new Error(cause, _logSource, _logClass, BuildMessage(message)),
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
            };

        protected override void NotifyLog(LogLevel logLevel, object message, Exception cause = null)
            => _bus.Publish(CreateLogEvent(logLevel, message, cause));

        public override bool IsDebugEnabled { get; }
        public override bool IsInfoEnabled { get; }
        public override bool IsWarningEnabled { get; }
        public override bool IsErrorEnabled { get; }

        public ILoggingAdapter SetContextProperty(string name, object value, bool destructureObjects = false)
        {
            var contextProperty = new PropertyEnricher(name, value, destructureObjects);

            var contextNode = new ContextNode
            {
                Enricher = contextProperty,
                Next = _enricherNode
            };

            return new SerilogLoggingAdapter(_bus, _logSource, _logClass, contextNode);
        }
        
        private object BuildMessage(object message)
        {
            return new SerilogPayload(message, BuildArgs());
        }

        private IReadOnlyList<ILogEventEnricher> BuildArgs()
        {
            if (_enricherNode == null)
                return Array.Empty<ILogEventEnricher>();
            
            var newArgs = new List<ILogEventEnricher>();
            var currentNode = _enricherNode;
            while (currentNode != null)
            {
                newArgs.Add(currentNode.Enricher);
                currentNode = currentNode.Next;
            }

            return newArgs;
        }
    }
}