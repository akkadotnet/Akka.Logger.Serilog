﻿//-----------------------------------------------------------------------
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
            // Unwrap SerilogPayload
            if (message is SerilogPayload payload)
                message = payload.Message;
            
            return message is LogMessage logMessage ? logMessage.Format : "{Message:l}";
        }

        private static object[] GetArgs(object message)
        {
            // Unwrap SerilogPayload
            if (message is SerilogPayload payload)
                message = payload.Message;
            
            return message is LogMessage logMessage 
                ? logMessage.Parameters().Where(a => a is not PropertyEnricher).ToArray() 
                : new[] { message };
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

