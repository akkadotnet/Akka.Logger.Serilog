using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster;
using Akka.Configuration;
using Akka.Event;
using FluentAssertions;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Logger.Serilog.Tests
{
    public class Bugfix4579Spec : TestKit.Xunit2.TestKit
    {
        public Bugfix4579Spec(ITestOutputHelper output) : base(GetConfig(), output: output)
        {
            global::Serilog.Log.Logger = new LoggerConfiguration()
                .WriteTo.Sink(_sink)
                .MinimumLevel.Information()
                .CreateLogger();

            var logSource = Sys.Name;
            var logClass = typeof(ActorSystem);

        }

        private readonly TestSink _sink = new TestSink();

        public static Config GetConfig()
        {
            return @"akka.actor.provider = cluster
                    akka.remote.dot-netty.tcp.hostname = localhost
                    akka.remote.dot-netty.tcp.port = 5110
                    akka.cluster.seed-nodes = [""akka.tcp://test@localhost:5110""]
                    akka.loglevel = DEBUG
                    akka.loggers=[""Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog""]";
        }

        private class LoggerActor : UntypedActor
        {
            private readonly ILoggingAdapter _logger = Context.GetLogger<SerilogLoggingAdapter>(); // correct

            protected override void OnReceive(object message)
            {
                _logger.ForContext("semantic", true);
                _logger.Info("My boss makes me use {msg} logging", message);
            }
        }

        /// <summary>
        /// Reproduction of https://github.com/akkadotnet/akka.net/issues/4579
        /// </summary>
        [Fact]
        public async Task SerilogShouldNotCrashWhileAkkaRemoteIsBound()
        {
            var upProbe = CreateTestProbe();
            var semanticLogger = Sys.ActorOf(Props.Create(() => new LoggerActor()));
            Sys.EventStream.Subscribe(TestActor, typeof(Info));
            Cluster.Cluster.Get(Sys).Subscribe(upProbe, ClusterEvent.SubscriptionInitialStateMode.InitialStateAsEvents, typeof(ClusterEvent.MemberUp));
            upProbe.FishForMessage(f => f is ClusterEvent.MemberUp);
            Sys.Log.Info("Foo");
            Sys.Log.Info("Foo");
            semanticLogger.Tell("hit");
            semanticLogger.Tell("hit");
            var logs = ReceiveN(4); // receive all 4 logs
        }

    }
}
