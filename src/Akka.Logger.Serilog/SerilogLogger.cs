//-----------------------------------------------------------------------
// <copyright file="SerilogLogger.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.Linq;
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

        private static string GetFormat(object message)
        {
            return message is LogMessage logMessage ? logMessage.Format : "{Message:l}";
        }

        private static object[] GetArgs(object message)
        {
            var logMessage = message as LogMessage;
            return logMessage?.Args.Where(a => !(a is PropertyEnricher)).ToArray() ?? new[] { message };
        }

        private static ILogger GetLogger(LogEvent logEvent) {
			var logger = Log.Logger
				.ForContext(Constants.SourceContextPropertyName, logEvent.LogClass.FullName)
				.ForContext("ActorPath", Context.Sender.Path)
				.ForContext("Timestamp", logEvent.Timestamp)
				.ForContext("LogSource", logEvent.LogSource)
				.ForContext("Thread", logEvent.Thread.ManagedThreadId.ToString().PadLeft( 4, '0' ));

            var logMessage = logEvent.Message as LogMessage;
            if (logMessage != null)
            {
                logger = logMessage.Args.OfType<PropertyEnricher>().Aggregate(logger, (current, enricher) => current.ForContext(enricher));
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
            Receive<Error>(m => Handle(m));
            Receive<Warning>(m => Handle(m));
            Receive<Info>(m => Handle(m));
            Receive<Debug>(m => Handle(m));
            Receive<InitializeLogger>(m =>
            {
                _log.Info("SerilogLogger started");
                Sender.Tell(new LoggerInitialized());
            });
        }
    }
}

