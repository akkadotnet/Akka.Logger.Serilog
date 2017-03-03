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
        private readonly ILoggingAdapter _log = Context.GetLogger();

        private static string GetFormat(object message)
        {
            var logMessage = message as LogMessage;
            return logMessage != null ? logMessage.Format : "{Message}";
        }

        private static object[] GetArgs(object message)
        {
            var logMessage = message as LogMessage;
            return logMessage?.Args.Where(a => !(a is PropertyEnricher)).ToArray() ?? new[] { message };
        }

        private static ILogger GetLogger(LogEvent logEvent) {
            var logger = Log.Logger.ForContext("SourceContext", Context.Sender.Path);
            logger = logger
                .ForContext("Timestamp", logEvent.Timestamp)
                .ForContext("LogSource", logEvent.LogSource)
                .ForContext("Thread", logEvent.Thread.ManagedThreadId.ToString().PadLeft(4, '0'));

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
              GetLogger(logEvent).Warning(GetFormat(logEvent.Message), GetArgs(logEvent.Message));
        }

        private static void Handle(Info logEvent)
        {
              GetLogger(logEvent).Information(GetFormat(logEvent.Message), GetArgs(logEvent.Message));
        }

        private static void Handle(Debug logEvent)
        {
              GetLogger(logEvent).Debug(GetFormat(logEvent.Message), GetArgs(logEvent.Message));
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

