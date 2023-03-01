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
        private readonly ILoggingAdapter _log = Logging.GetLogger(Context.System.EventStream, "SerilogLogger");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetFormat(object message)
        {
            return message is LogMessage logMessage 
                ? logMessage.Format 
                : message is string format ? format : "{Message:l}";
        }

        private static object[] GetArgs(object message)
        {
            var logMessage = message as LogMessage;
            return logMessage?.Parameters().Where(a => a is not PropertyEnricher).ToArray() ?? System.Array.Empty<object>();
        }

        private static ILogger GetLogger(LogEvent logEvent) {
			var logger = Log.Logger
				.ForContext(Constants.SourceContextPropertyName, logEvent.LogClass.FullName)
				.ForContext("ActorPath", Context.Sender.Path)
				.ForContext("Timestamp", logEvent.Timestamp)
				.ForContext("LogSource", logEvent.LogSource)
				.ForContext("Thread", logEvent.Thread.ManagedThreadId.ToString("0000"));

            if (logEvent.Message is SerilogPayload logMessage)
            {
                logger = logMessage.Enrichers.OfType<PropertyEnricher>().Aggregate(logger, (current, enricher) => current.ForContext(enricher));
            }

            return logger;
        }

        private static void Handle(Error logEvent) {
            GetLogger(logEvent).Error(logEvent.Cause, GetFormat(logEvent.Message), GetArgs(logEvent.Message));
        }

        private static void Handle(Warning logEvent) {
            GetLogger(logEvent).Warning(logEvent.Cause, GetFormat(logEvent.Message), GetArgs(logEvent.Message));
        }

        private static void Handle(Info logEvent)
        {
            GetLogger(logEvent).Information(logEvent.Cause, GetFormat(logEvent.Message), GetArgs(logEvent.Message));
        }

        private static void Handle(Debug logEvent)
        {
            GetLogger(logEvent).Debug(logEvent.Cause, GetFormat(logEvent.Message), GetArgs(logEvent.Message));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogLogger"/> class.
        /// </summary>
        public SerilogLogger()
        {
            Receive<Error>(Handle);
            Receive<Warning>(Handle);
            Receive<Info>(Handle);
            Receive<Debug>(Handle);
            Receive<InitializeLogger>(_ =>
            {
                _log.Info("SerilogLogger started");
                Sender.Tell(new LoggerInitialized());
            });
        }
    }
}

