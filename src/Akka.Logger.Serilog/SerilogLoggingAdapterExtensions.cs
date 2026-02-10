using System;
using Akka.Actor;
using Akka.Event;

namespace Akka.Logger.Serilog
{
    public static class SerilogLoggingAdapterExtensions
    {
        /// <summary>
        /// Create a logger that enriches log events with the specified property.
        /// </summary>
        /// <param name="adapter">ILoggingAdapter instance</param>
        /// <param name="propertyName">The name of the property. Must be non-empty.</param>
        /// <param name="value">The property value.</param>
        /// <param name="destructureObjects">If true, the value will be serialized as a structured object if possible; if false, the object will be recorded as a scalar or simple array.</param>
        [Obsolete("Use ILoggingAdapter.WithContext() instead. This method will be removed in a future version.")]
        public static ILoggingAdapter ForContext(this ILoggingAdapter adapter, string propertyName, object value, bool destructureObjects = false)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if(adapter is SerilogLoggingAdapter customAdapter)
                return customAdapter.SetContextProperty(propertyName, value, destructureObjects);

            if (adapter is BusLogging defaultAkkaAdapter)
            {
                var enrichedAdapter = new SerilogLoggingAdapter(defaultAkkaAdapter.Bus, defaultAkkaAdapter.LogSource, defaultAkkaAdapter.LogClass);
                return enrichedAdapter.SetContextProperty(propertyName, value, destructureObjects);
            }

            // log a warning if the adapter is not a SerilogLoggingAdapter or BusLogging
            adapter.Warning($"Cannot enrich log event with property {propertyName} because the adapter is not a {typeof(SerilogLoggingAdapter)} or {typeof(BusLogging)}.");
            return adapter;
#pragma warning restore CS0618
        }

        /// <summary>
        /// Creates a new logging adapter using the specified context's event stream.
        /// </summary>
        /// <param name="context">The context used to configure the logging adapter.</param>
        /// <returns>The newly created logging adapter.</returns>
        public static ILoggingAdapter GetLogger<T>(this IActorContext context)
            where T : class, ILoggingAdapter
        {
            var logSource = context.Self.ToString();
            var logClass = context.Props.Type;

#pragma warning disable CS0618 // Type or member is obsolete
            return new SerilogLoggingAdapter(context.System.EventStream, logSource, logClass);
#pragma warning restore CS0618
        }

        public static ILoggingAdapter GetLogger<T>(this ActorSystem system, object logSourceObj)
            where T : class, ILoggingAdapter
        {
            if (logSourceObj is null)
                throw new ArgumentNullException(nameof(logSourceObj));

            var logSource = LogSource.Create(logSourceObj, system);
#pragma warning disable CS0618 // Type or member is obsolete
            return new SerilogLoggingAdapter(system.EventStream, logSource.Source, logSource.Type);
#pragma warning restore CS0618
        }

        public static ILoggingAdapter GetLogger<T>(this ActorSystem system, string logSource, Type logType)
            where T : class, ILoggingAdapter
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return new SerilogLoggingAdapter(system.EventStream, logSource, logType);
#pragma warning restore CS0618
        }
    }
}
