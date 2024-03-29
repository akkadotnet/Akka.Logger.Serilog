﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using FluentAssertions;
using Serilog;
using Serilog.Core;
using Serilog.Core.Enrichers;
using Serilog.Events;
using Xunit;
using Xunit.Abstractions;
using LogEvent = Akka.Event.LogEvent;

namespace Akka.Logger.Serilog.Tests
{
    public class ForContextSpecs : TestKit.Xunit2.TestKit
    {
        public static readonly Config Config = @"akka.loglevel = DEBUG
                                                 akka.loggers=[""Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog""]";
        private readonly ILoggingAdapter _loggingAdapter;
        private readonly TestSink _sink = new TestSink();

        public ForContextSpecs(ITestOutputHelper helper) : base(Config, output: helper)
        {
            global::Serilog.Log.Logger = new LoggerConfiguration()
				.WriteTo.Sink(_sink)
				.MinimumLevel.Information()
				.CreateLogger();

			var logSource = Sys.Name;
            var logClass = typeof(ActorSystem);

            _loggingAdapter = Sys.GetLogger<SerilogLoggingAdapter>(logSource, logClass);
        }

        [Fact]
        public void ShouldLogMessageWithContextProperty()
        {
	        var context = _loggingAdapter
		        .ForContext("Address", "No. 4 Privet Drive")
		        .ForContext("Town", "Little Whinging")
		        .ForContext("County", "Surrey")
		        .ForContext("Country", "England");

	        _sink.Clear();
	        AwaitCondition(() => _sink.Writes.Count == 0);

	        context.Info("Hi {Person}", "Harry Potter");
	        AwaitCondition(() => _sink.Writes.Count == 1);

	        _sink.Writes.TryDequeue(out var logEvent).Should().BeTrue();
	        logEvent.Level.Should().Be(LogEventLevel.Information);
	        logEvent.RenderMessage().Should().Contain("Hi \"Harry Potter\"");
	        logEvent.Properties.Should().ContainKeys("Person", "Address", "Town", "County", "Country");
	        logEvent.Properties["Person"].ToString().Should().Be("\"Harry Potter\"");
	        logEvent.Properties["Address"].ToString().Should().Be("\"No. 4 Privet Drive\"");
	        logEvent.Properties["Town"].ToString().Should().Be("\"Little Whinging\"");
	        logEvent.Properties["County"].ToString().Should().Be("\"Surrey\"");
	        logEvent.Properties["Country"].ToString().Should().Be("\"England\"");
        }

        [Fact]
        public void ShouldLogMessageWithContextPropertyAndPropertyEnricher()
        {
	        var context = _loggingAdapter
		        .ForContext("Address", "No. 4 Privet Drive")
		        .ForContext("Town", "Little Whinging");

	        _sink.Clear();
	        AwaitCondition(() => _sink.Writes.Count == 0);

	        context.Info("Hi {Person}", "Harry Potter", new PropertyEnricher("County", "Surrey"), new PropertyEnricher("Country", "England"));
	        AwaitCondition(() => _sink.Writes.Count == 1);

	        _sink.Writes.TryDequeue(out var logEvent).Should().BeTrue();
	        logEvent.Level.Should().Be(LogEventLevel.Information);
	        logEvent.RenderMessage().Should().Contain("Hi \"Harry Potter\"");
	        logEvent.Properties.Should().ContainKeys("Person", "Address", "Town", "County", "Country");
	        logEvent.Properties["Person"].ToString().Should().Be("\"Harry Potter\"");
	        logEvent.Properties["Address"].ToString().Should().Be("\"No. 4 Privet Drive\"");
	        logEvent.Properties["Town"].ToString().Should().Be("\"Little Whinging\"");
	        logEvent.Properties["County"].ToString().Should().Be("\"Surrey\"");
	        logEvent.Properties["Country"].ToString().Should().Be("\"England\"");
        }

        [Fact]
        public void ShouldPassAlongAdditionalContext()
        {
            var traceId = Guid.NewGuid();
            var spanId = Guid.NewGuid();
            var context1 = _loggingAdapter.ForContext("traceId", traceId);
            var context2 = context1.ForContext("spanId", spanId);

            _sink.Clear();
            AwaitCondition(() => _sink.Writes.Count == 0);

            context1.Info("hi");
            AwaitCondition(() => _sink.Writes.Count == 1);

            _sink.Writes.TryDequeue(out var logEvent).Should().BeTrue();
            logEvent.Level.Should().Be(LogEventLevel.Information);
            logEvent.Properties.ContainsKey("traceId").Should().BeTrue();
            logEvent.Properties["traceId"].ToString().Should().BeEquivalentTo(traceId.ToString());

            _sink.Clear();
            AwaitCondition(() => _sink.Writes.Count == 0);

            context2.Info("bye");

            AwaitCondition(() => _sink.Writes.Count == 1);

            _sink.Writes.TryDequeue(out var logEvent2).Should().BeTrue();

            logEvent2.Level.Should().Be(LogEventLevel.Information);

            // needs to still have the context from context1
            logEvent2.Properties.ContainsKey("traceId").Should().BeTrue();
            logEvent2.Properties["traceId"].ToString().Should().BeEquivalentTo(traceId.ToString());

            // and its own context from context2
            logEvent2.Properties.ContainsKey("spanId").Should().BeTrue();
            logEvent2.Properties["spanId"].ToString().Should().BeEquivalentTo(spanId.ToString());
        }

        [Fact]
        public void ShouldPassAlongAdditionalContextWorkaround()
        {
            var traceId = Guid.NewGuid();
            var spanId = Guid.NewGuid();
            var context1 = (SerilogLoggingAdapter)_loggingAdapter.ForContext("traceId", traceId);
            var context2 = (SerilogLoggingAdapter)context1.ForContext("spanId", spanId);

            _sink.Clear();
            AwaitCondition(() => _sink.Writes.Count == 0);

            context1.Info("hi");
            AwaitCondition(() => _sink.Writes.Count == 1);

            _sink.Writes.TryDequeue(out var logEvent).Should().BeTrue();
            logEvent.Level.Should().Be(LogEventLevel.Information);
            logEvent.Properties.ContainsKey("traceId").Should().BeTrue();
            logEvent.Properties["traceId"].ToString().Should().BeEquivalentTo(traceId.ToString());

            _sink.Clear();
            AwaitCondition(() => _sink.Writes.Count == 0);

            context2.Info("bye");

            AwaitCondition(() => _sink.Writes.Count == 1);

            _sink.Writes.TryDequeue(out var logEvent2).Should().BeTrue();

            logEvent2.Level.Should().Be(LogEventLevel.Information);

            // needs to still have the context from context1
            logEvent2.Properties.ContainsKey("traceId").Should().BeTrue();
            logEvent2.Properties["traceId"].ToString().Should().BeEquivalentTo(traceId.ToString());

            // and its own context from context2
            logEvent2.Properties.ContainsKey("spanId").Should().BeTrue();
            logEvent2.Properties["spanId"].ToString().Should().BeEquivalentTo(spanId.ToString());

        }

		[Fact]
		public void ShouldPassAlongClassNameAsSourceContext()
		{
			var context = _loggingAdapter;

			_sink.Clear();
			AwaitCondition(() => _sink.Writes.Count == 0);

			context.Info( "hi" );
			AwaitCondition(() => _sink.Writes.Count == 1);

			_sink.Writes.TryDequeue( out var logEvent ).Should().BeTrue();
			logEvent.Level.Should().Be(LogEventLevel.Information);
			logEvent.Properties.ContainsKey(Constants.SourceContextPropertyName).Should().BeTrue();
			logEvent.Properties[Constants.SourceContextPropertyName].ToString().Should().BeEquivalentTo($"\"{typeof(ActorSystem).FullName}\"");
		}

		[Fact]
		public void ShouldPassAlongActorPath()
		{
			var context = _loggingAdapter;

			_sink.Clear();
			AwaitCondition(() => _sink.Writes.Count == 0);

			context.Info( "hi" );
			AwaitCondition(() => _sink.Writes.Count == 1);

			_sink.Writes.TryDequeue( out var logEvent ).Should().BeTrue();
			logEvent.Level.Should().Be(LogEventLevel.Information);
			logEvent.Properties.ContainsKey("ActorPath").Should().BeTrue();
			logEvent.Properties["ActorPath"].ToString().Should().BeEquivalentTo($"\"{this.TestActor.Path}\"");
		}
	}
}



