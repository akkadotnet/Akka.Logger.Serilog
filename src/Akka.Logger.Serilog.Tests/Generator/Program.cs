// -----------------------------------------------------------------------
//   <copyright file="Program.cs" company="Akka.NET Project">
//     Copyright (C) 2013-2023 Akka.NET project <https://github.com/akkadotnet/akka.net>
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Serilog;

namespace Akka.Logger.Serilog.Tests.Generator
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var version = args.Length > 0 ? args[0] : Assembly.GetAssembly(typeof(Log))?.GetName().Version?.ToString() ?? Guid.NewGuid().ToString("N");
        
            var sink = new TestSink();
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.Sink(sink)
                .MinimumLevel.Debug()
                .CreateLogger();
        
            sink.Clear();

            using (var fileWriter = new StreamWriter($"./TestFiles/v{version}.tf"))
            {
                foreach (var i in Enumerable.Range(0, TestData.MessageFormats.Length))
                {
                    var messageFormat = TestData.MessageFormats[i];
                    var arg = TestData.Args[i];
            
                    Log.Logger.Information(messageFormat, arg);

                    var count = 0;
                    while (sink.Writes.Count < 1 && count < 50)
                    {
                        await Task.Delay(100);
                        count++;
                    }

                    if (!sink.Writes.TryDequeue(out var logEvent))
                        throw new Exception($"Failed to log test data [{messageFormat}] with data [{arg}]");

                    await fileWriter.WriteLineAsync(logEvent.RenderMessage());
                }
            }
        }
    }
}

