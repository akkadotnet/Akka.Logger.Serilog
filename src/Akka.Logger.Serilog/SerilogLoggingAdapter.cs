using System;
using System.Linq;
using System.Collections.Generic;
using Akka.Event;
using Serilog.Core;
using Serilog.Core.Enrichers;

namespace Akka.Logger.Serilog
{
    public class SerilogLoggingAdapter : BusLogging
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

        private SerilogLoggingAdapter(LoggingBus bus, string logSource, Type logClass, ContextNode enricher) : base(bus, logSource, logClass, SerilogLogMessageFormatter.Instance)
        {
            _bus = bus;
            _logSource = logSource;
            _logClass = logClass;
            _enricherNode = enricher;
        }

        /// <summary>
        /// Logs a <see cref="F:Akka.Event.LogLevel.DebugLevel" /> message.
        /// </summary>
        /// <param name="format">The message that is being logged.</param>
        /// <param name="args">An optional list of items used to format the message.</param>
        public new void Debug(string format, params object[] args)
        {
            base.Debug(format, BuildArgs(args));
        }

        /// <summary>
        /// Logs a <see cref="F:Akka.Event.LogLevel.InfoLevel" /> message.
        /// </summary>
        /// <param name="format">The message that is being logged.</param>
        /// <param name="args">An optional list of items used to format the message.</param>
        public new void Info(string format, params object[] args)
        {
            base.Info(format, BuildArgs(args));
        }

        /// <summary>
        /// Obsolete. Use <see cref="M:Akka.Event.ILoggingAdapter.Warning(System.String,System.Object[])" /> instead!
        /// </summary>
        /// <param name="format">N/A</param>
        /// <param name="args">N/A</param>
        public new void Warn(string format, params object[] args)
        {
            base.Warning(format, BuildArgs(args));
        }

        /// <summary>
        /// Logs a <see cref="F:Akka.Event.LogLevel.WarningLevel" /> message.
        /// </summary>
        /// <param name="format">The message that is being logged.</param>
        /// <param name="args">An optional list of items used to format the message.</param>
        public new void Warning(string format, params object[] args)
        {
            base.Warning(format, BuildArgs(args));
        }

        /// <summary>
        /// Logs a <see cref="F:Akka.Event.LogLevel.ErrorLevel" /> message.
        /// </summary>
        /// <param name="format">The message that is being logged.</param>
        /// <param name="args">An optional list of items used to format the message.</param>
        public new void Error(string format, params object[] args)
        {
            base.Error(format, BuildArgs(args));
        }

        /// <summary>
        /// Logs a <see cref="F:Akka.Event.LogLevel.ErrorLevel" /> message and associated exception.
        /// </summary>
        /// <param name="cause">The exception associated with this message.</param>
        /// <param name="format">The message that is being logged.</param>
        /// <param name="args">An optional list of items used to format the message.</param>
        public new void Error(Exception cause, string format, params object[] args)
        {
            base.Error(cause, format, BuildArgs(args));
        }

        /// <summary>Logs a message with a specified level.</summary>
        /// <param name="logLevel">The level used to log the message.</param>
        /// <param name="format">The message that is being logged.</param>
        /// <param name="args">An optional list of items used to format the message.</param>
        public new void Log(LogLevel logLevel, string format, params object[] args)
        {
            base.Log(logLevel, format, BuildArgs(args));
        }

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

        private object[] BuildArgs(IEnumerable<object> args)
        {
            var newArgs = args.ToList();
            if (_enricherNode != null)
            {
                var currentNode = _enricherNode;
                while (currentNode != null)
                {
                    newArgs.Add(currentNode.Enricher);
                    currentNode = currentNode.Next;
                }
            }
            return newArgs.ToArray();
        }
    }
}