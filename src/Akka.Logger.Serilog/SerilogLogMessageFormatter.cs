﻿using System.Collections.Generic;
using System.Linq;
using Akka.Event;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;

namespace Akka.Logger.Serilog
{
    /// <inheritdoc />
    /// <summary>
    /// This class contains methods used to convert Serilog templated messages
    /// into normal text messages.
    /// </summary>
    public class SerilogLogMessageFormatter : ILogMessageFormatter
    {
        private readonly MessageTemplateCache _templateCache;

        public static readonly SerilogLogMessageFormatter Instance = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogLogMessageFormatter"/> class.
        /// </summary>
        public SerilogLogMessageFormatter()
        {
            _templateCache = new MessageTemplateCache(new MessageTemplateParser());
        }

        /// <summary>
        /// Converts the specified template string to a text string using the specified
        /// token array to match replacements.
        /// </summary>
        /// <param name="format">The template string used in the conversion.</param>
        /// <param name="args">The array that contains values to replace in the template.</param>
        /// <returns>
        /// A text string where the template placeholders have been replaced with
        /// their corresponding values.
        /// </returns>
        public string Format(string format, params object[] args)
        {
            // We're using serilog built-in function to bind all the object arguments to their proper
            // value types. If this fails, we fell back to the old implementation where we map everything
            // to ScalarValue.
            if (Log.Logger.BindMessageTemplate(format, args, out var boundTemplate, out var boundProps))
                return boundTemplate.Render(boundProps.ToDictionary(p => p.Name, p => p.Value));
            
            var template = _templateCache.Parse(format);
            var propertyTokens = template.Tokens.OfType<PropertyToken>().ToArray();
            var properties = new Dictionary<string, LogEventPropertyValue>();

            for (var i = 0; i < args.Length; i++)
            {
                var propertyToken = propertyTokens.ElementAtOrDefault(i);
                if (propertyToken == null)
                    break;

                if(!properties.ContainsKey(propertyToken.PropertyName))
                    properties.Add(propertyToken.PropertyName, new ScalarValue(args[i]));
            }

            return template.Render(properties);
        }

        /// <summary>
        /// Converts the specified template string to a text string using the specified
        /// token array to match replacements.
        /// </summary>
        /// <param name="format">The template string used in the conversion.</param>
        /// <param name="args">The <see cref="IEnumerable{T}"/> that contains values to replace in the template.</param>
        /// <returns>
        /// A text string where the template placeholders have been replaced with
        /// their corresponding values.
        /// </returns>
        public string Format(string format, IEnumerable<object> args)
            => Format(format, args.ToArray());
    }
}