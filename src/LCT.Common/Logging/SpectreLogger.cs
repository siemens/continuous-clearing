// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 
using log4net;
using log4net.Core;
using log4net.Repository;
using Spectre.Console;
using System;

namespace LCT.Common.Logging
{
    public class SpectreLogger : ILogger
    {
        private readonly string _loggerName;

        public SpectreLogger(string loggerName)
        {
            _loggerName = loggerName;
        }

        public string Name => _loggerName;

        ILoggerRepository ILogger.Repository => throw new NotImplementedException();

        public bool IsEnabledFor(Level level)
        {
            return true;
        }

        public void Log(Type callerStackBoundaryDeclaringType, Level level, object message, Exception exception)
        {
            var fileLogger = LogManager.GetLogger(_loggerName);
            switch (level.Name.ToUpperInvariant())
            {
                case "DEBUG":
                    fileLogger.Debug(message, exception);
                    break;
                case "INFO":
                    fileLogger.Info(message, exception);
                    break;
                case "NOTICE":
                    LoggerHelper.ConsoleInstance.MarkupLine($"[white] {message}[/]");
                    fileLogger.Debug(message, exception);
                    break;
                case "WARN":
                    fileLogger.Warn($" {message}", exception);
                    break;
                case "ERROR":
                    fileLogger.Error($" {message}", exception);
                    break;
                case "FATAL":
                    fileLogger.Fatal(message, exception);
                    break;
                case "ALERT":
                    fileLogger.Warn(message, exception);
                    break;
                default:
                    fileLogger.Info(message, exception);
                    break;
            }

            if (exception != null)
            {
                AnsiConsole.WriteException(exception, new ExceptionSettings
                {
                    Format = ExceptionFormats.ShortenEverything,
                    Style = new ExceptionStyle
                    {
                        Exception = new Style().Foreground(Color.Red),
                        Message = new Style().Foreground(Color.Red),
                        Method = new Style().Foreground(Color.Green),
                        ParameterName = new Style().Foreground(Color.Cyan1),
                        ParameterType = new Style().Foreground(Color.Blue),
                        Path = new Style().Foreground(Color.Magenta1),
                        LineNumber = new Style().Foreground(Color.Yellow)
                    }
                });
            }
        }
        void ILogger.Log(LoggingEvent logEvent)
        {
            throw new NotImplementedException();
        }
    }
}
