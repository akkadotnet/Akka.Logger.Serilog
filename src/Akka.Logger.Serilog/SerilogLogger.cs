//-----------------------------------------------------------------------
// <copyright file="SerilogLogger.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.Linq;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.Dispatch;
using Akka.Event;
using Serilog;
using Serilog.Core;
using Serilog.Core.Enrichers;

namespace Akka.Logger.Serilog
{
    /// <summary>
    /// This class is used to receive log events and sends them to
    /// the configured Serilog logger. The following log events are
    /// recognized: <see cref="Debug"/>, <see cref="Info"/>,
    /// <see cref="Warning"/> and <see cref="Error"/>.
    /// </summary>
    public class SerilogLogger : ReceiveActor, IRequiresMessageQueue<ILoggerMessageQueueSemantics>
    {
        /// <summary>
        /// Log filter. See https://getakka.net/articles/utilities/logging.html#filtering-log-messages for details.
        /// </summary>
        public LogFilterEvaluator Filter { get; }
        private readonly ILoggingAdapter _log = Logging.GetLogger(Context.System.EventStream, "SerilogLogger");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetFormat(LogEvent logEvent)
        {
            // Unwrap SerilogPayload if present (Serilog-specific wrapper for context enrichers)
            var message = logEvent.Message;
            if (message is SerilogPayload payload)
                message = payload.Message;

            // Use semantic logging pattern: check if LogMessage and extract template
            if (message is LogMessage logMessage)
                return logMessage.Format;

            // For plain strings, use the string itself as the template.
            // This gives each unique log message its own @EventType in Serilog/Seq,
            // making logs filterable and distinguishable.
            if (message is string str)
                return str;

            // For other objects, use generic template with literal formatting
            return "{Message:l}";
        }

        private static object[] GetArgs(LogEvent logEvent)
        {
            // Unwrap SerilogPayload if present (Serilog-specific wrapper for context enrichers)
            var message = logEvent.Message;
            if (message is SerilogPayload payload)
                message = payload.Message;

            // Use semantic logging pattern: extract parameters and filter PropertyEnricher objects
            if (message is LogMessage logMessage)
                return logMessage.Parameters().Where(a => a is not PropertyEnricher).ToArray();

            // For plain strings, return empty array since the string IS the template (no placeholders)
            if (message is string)
                return [];

            // For other objects, pass the object as the single argument to {Message:l}
            return [message];
        }

        private static ILogger GetLogger(LogEvent logEvent) {
			var logger = Log.Logger
				.ForContext(Constants.SourceContextPropertyName, logEvent.LogClass.FullName)
				.ForContext("ActorPath", Context.Sender.Path)
				.ForContext("Timestamp", logEvent.Timestamp)
				.ForContext("LogSource", logEvent.LogSource)
				.ForContext("Thread", logEvent.Thread.ManagedThreadId.ToString("0000"));

            if (logEvent.Message is SerilogPayload serilogPayload)
            {
                var enrichers = serilogPayload.Enrichers.ToList();
                if (serilogPayload.Message is LogMessage logMessage)
                    enrichers.AddRange(logMessage.Parameters().OfType<ILogEventEnricher>());
                if (enrichers.Count > 0)
                    logger = logger.ForContext(enrichers);
            }

            if (logEvent.Message is LogMessage message)
            {
                var enrichers = message.Parameters().OfType<ILogEventEnricher>().ToList();
                if (enrichers.Count > 0)
                    logger = logger.ForContext(enrichers);
            }

            return logger;
        }

        private static void Handle(Error logEvent) {
            GetLogger(logEvent).Error(logEvent.Cause, GetFormat(logEvent), GetArgs(logEvent));
        }

        private static void Handle(Warning logEvent) {
            GetLogger(logEvent).Warning(logEvent.Cause, GetFormat(logEvent), GetArgs(logEvent));
        }

        private static void Handle(Info logEvent)
        {
            GetLogger(logEvent).Information(logEvent.Cause, GetFormat(logEvent), GetArgs(logEvent));
        }

        private static void Handle(Debug logEvent)
        {
            GetLogger(logEvent).Debug(logEvent.Cause, GetFormat(logEvent), GetArgs(logEvent));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogLogger"/> class.
        /// </summary>
        public SerilogLogger()
        {
            Filter = Context.System.Settings.LogFilter;
            Receive<Error>(e =>
            {
                if(Filter.ShouldTryKeepMessage(e, out _))
                    Handle(e);
            });
            Receive<Warning>(w =>
            {
                if(Filter.ShouldTryKeepMessage(w, out _))
                    Handle(w);
            });
            Receive<Info>(i =>
            {
                if(Filter.ShouldTryKeepMessage(i, out _))
                    Handle(i);
            });
            Receive<Debug>(d =>
            {
                if(Filter.ShouldTryKeepMessage(d, out _))
                    Handle(d);
            });
            Receive<InitializeLogger>(_ =>
            {
                _log.Info("SerilogLogger started");
                Sender.Tell(new LoggerInitialized());
            });
        }
    }
}

