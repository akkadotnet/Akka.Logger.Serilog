using System;
using System.Linq;
using System.Collections.Generic;
using Akka.Event;
using Serilog.Core;
using Serilog.Core.Enrichers;

namespace Akka.Logger.Serilog
{
    public class SerilogLoggingAdapter : ILoggingAdapter
    {
        /// <summary>
        /// Helper class that allows context trees.
        /// </summary>
        private class ContextNode
        {
            public ContextNode Next { get; set; }
            public ILogEventEnricher Enricher { get; set; }
        }

        private readonly ILoggingAdapter adapter;
        private readonly ContextNode enricherNode;

        public SerilogLoggingAdapter(ILoggingAdapter adapter)
            : this(adapter, null)
        {
        }

        private SerilogLoggingAdapter(ILoggingAdapter adapter, ContextNode enricherNode)
        {
            this.adapter = adapter;
            this.enricherNode = enricherNode;
        }

        /// <summary>
        /// Check to determine whether the <see cref="F:Akka.Event.LogLevel.DebugLevel" /> is enabled.
        /// </summary>
        public bool IsDebugEnabled => adapter.IsDebugEnabled;

        /// <summary>
        /// Check to determine whether the <see cref="F:Akka.Event.LogLevel.InfoLevel" /> is enabled.
        /// </summary>
        public bool IsInfoEnabled => adapter.IsInfoEnabled;

        /// <summary>
        /// Check to determine whether the <see cref="F:Akka.Event.LogLevel.WarningLevel" /> is enabled.
        /// </summary>
        public bool IsWarningEnabled => adapter.IsWarningEnabled;

        /// <summary>
        /// Check to determine whether the <see cref="F:Akka.Event.LogLevel.ErrorLevel" /> is enabled.
        /// </summary>
        public bool IsErrorEnabled => adapter.IsErrorEnabled;

        /// <summary>Determines whether a specific log level is enabled.</summary>
        /// <param name="logLevel">The log level that is being checked.</param>
        /// <returns><c>true</c> if the specified level is enabled; otherwise <c>false</c>.</returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            return adapter.IsEnabled(logLevel);
        }

        /// <summary>
        /// Logs a <see cref="F:Akka.Event.LogLevel.DebugLevel" /> message.
        /// </summary>
        /// <param name="format">The message that is being logged.</param>
        /// <param name="args">An optional list of items used to format the message.</param>
        public virtual void Debug(string format, params object[] args)
        {
            adapter.Debug(format, BuildArgs(args));
        }

        /// <summary>
        /// Logs a <see cref="F:Akka.Event.LogLevel.InfoLevel" /> message.
        /// </summary>
        /// <param name="format">The message that is being logged.</param>
        /// <param name="args">An optional list of items used to format the message.</param>
        public virtual void Info(string format, params object[] args)
        {
            adapter.Info(format, BuildArgs(args));
        }

        /// <summary>
        /// Obsolete. Use <see cref="M:Akka.Event.ILoggingAdapter.Warning(System.String,System.Object[])" /> instead!
        /// </summary>
        /// <param name="format">N/A</param>
        /// <param name="args">N/A</param>
        public virtual void Warn(string format, params object[] args)
        {
            adapter.Warning(format, BuildArgs(args));
        }

        /// <summary>
        /// Logs a <see cref="F:Akka.Event.LogLevel.WarningLevel" /> message.
        /// </summary>
        /// <param name="format">The message that is being logged.</param>
        /// <param name="args">An optional list of items used to format the message.</param>
        public virtual void Warning(string format, params object[] args)
        {
            adapter.Warning(format, BuildArgs(args));
        }

        /// <summary>
        /// Logs a <see cref="F:Akka.Event.LogLevel.ErrorLevel" /> message.
        /// </summary>
        /// <param name="format">The message that is being logged.</param>
        /// <param name="args">An optional list of items used to format the message.</param>
        public virtual void Error(string format, params object[] args)
        {
            adapter.Error(format, BuildArgs(args));
        }

        /// <summary>
        /// Logs a <see cref="F:Akka.Event.LogLevel.ErrorLevel" /> message and associated exception.
        /// </summary>
        /// <param name="cause">The exception associated with this message.</param>
        /// <param name="format">The message that is being logged.</param>
        /// <param name="args">An optional list of items used to format the message.</param>
        public virtual void Error(Exception cause, string format, params object[] args)
        {
            adapter.Error(cause, format, BuildArgs(args));
        }

        /// <summary>Logs a message with a specified level.</summary>
        /// <param name="logLevel">The level used to log the message.</param>
        /// <param name="format">The message that is being logged.</param>
        /// <param name="args">An optional list of items used to format the message.</param>
        public virtual void Log(LogLevel logLevel, string format, params object[] args)
        {
            adapter.Log(logLevel, format, BuildArgs(args));
        }

        public ILoggingAdapter SetContextProperty(string name, object value, bool destructureObjects = false)
        {
            var contextProperty = new PropertyEnricher(name, value, destructureObjects);

            var contextNode = new ContextNode
            {
                Enricher = contextProperty,
                Next = enricherNode
            };

            return new SerilogLoggingAdapter(adapter, contextNode);
        }

        private object[] BuildArgs(IEnumerable<object> args)
        {
            var newArgs = args.ToList();
            if (enricherNode != null)
            {
                var currentNode = enricherNode;
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