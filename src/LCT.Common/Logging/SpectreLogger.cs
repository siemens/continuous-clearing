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
    /// <summary>
    /// Implements a custom logger that integrates Spectre.Console for enhanced console output with log4net.
    /// </summary>
    public class SpectreLogger : ILogger
    {
        #region Fields

        private readonly string _loggerName;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SpectreLogger"/> class.
        /// </summary>
        /// <param name="loggerName">The name of the logger.</param>
        public SpectreLogger(string loggerName)
        {
            _loggerName = loggerName;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the name of the logger.
        /// </summary>
        public string Name => _loggerName;

        /// <summary>
        /// Gets the logger repository (not implemented).
        /// </summary>
        ILoggerRepository ILogger.Repository => throw new NotImplementedException();

        #endregion

        #region Methods

        /// <summary>
        /// Determines whether logging is enabled for the specified level.
        /// </summary>
        /// <param name="level">The logging level.</param>
        /// <returns>True if logging is enabled for the level; otherwise, false.</returns>
        public bool IsEnabledFor(Level level)
        {
            return true;
        }

        /// <summary>
        /// Logs a message with the specified level and exception using both file and console output.
        /// </summary>
        /// <param name="callerStackBoundaryDeclaringType">The type declaring the caller stack boundary.</param>
        /// <param name="level">The logging level.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
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
                    LoggerHelper.ConsoleInstance.MarkupLine($"[white]{message}[/]");
                    fileLogger.Debug(message, exception);
                    break;
                case "WARN":
                    fileLogger.Warn(message, exception);
                    break;
                case "ERROR":
                    fileLogger.Error(message, exception);
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

        /// <summary>
        /// Logs a logging event (not implemented).
        /// </summary>
        /// <param name="logEvent">The logging event.</param>
        void ILogger.Log(LoggingEvent logEvent)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
