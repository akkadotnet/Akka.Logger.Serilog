// -----------------------------------------------------------------------
//  <copyright file="SerilogAkkaHostingExtensions.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2024 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Hosting;

namespace Akka.Logger.Serilog;

/// <summary>
/// Extension methods for configuring Serilog as the default logger for Akka.NET.
/// </summary>
public static class SerilogAkkaHostingExtensions
{
    /// <summary>
    /// Adds Serilog one of the default loggers for the Akka.NET actor system and enables
    /// Serilog-style semantic logging formatting for all log messages.
    /// </summary>
    /// <param name="configBuilder">The Akka.Hosting <see cref="LoggerConfigBuilder"/> - call <see cref="AkkaConfigurationBuilder.ConfigureLoggers"/></param>
    /// <param name="enableSerilogFormatter">Defaults to <c>true</c> - enables the <see cref="SerilogLogMessageFormatter"/> to be used by default.</param>
    /// <returns></returns>
    public static LoggerConfigBuilder AddSerilogLogging(this LoggerConfigBuilder configBuilder, bool enableSerilogFormatter = true)
    {
        configBuilder.AddLogger<SerilogLogger>();
        
        if(enableSerilogFormatter)
            configBuilder.WithDefaultLogMessageFormatter<SerilogLogMessageFormatter>();
        return configBuilder;
    }
}