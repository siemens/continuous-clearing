// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common.Logging;
using log4net;
using log4net.Core;
using System;
using System.Globalization;

namespace LCT.Common
{
    /// <summary>
    /// Adapter that integrates SpectreLogger with log4net's ILog interface for enhanced console logging.
    /// </summary>
    public class SpectreLogAdapter : ILog
    {
        #region Fields

        private readonly SpectreLogger _logger;
        private readonly ILog _fileLogger;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SpectreLogAdapter"/> class.
        /// </summary>
        /// <param name="loggerName">The name of the logger.</param>
        public SpectreLogAdapter(string loggerName)
        {
            string resolvedLoggerName = loggerName ?? "Unknown";
            _logger = new SpectreLogger(resolvedLoggerName);
            _fileLogger = LogManager.GetLogger(resolvedLoggerName);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether debug logging is enabled.
        /// </summary>
        public bool IsDebugEnabled => true;

        /// <summary>
        /// Gets a value indicating whether info logging is enabled.
        /// </summary>
        public bool IsInfoEnabled => true;

        /// <summary>
        /// Gets a value indicating whether warn logging is enabled.
        /// </summary>
        public bool IsWarnEnabled => true;

        /// <summary>
        /// Gets a value indicating whether error logging is enabled.
        /// </summary>
        public bool IsErrorEnabled => true;

        /// <summary>
        /// Gets a value indicating whether fatal logging is enabled.
        /// </summary>
        public bool IsFatalEnabled => true;

        /// <summary>
        /// Gets the underlying logger instance.
        /// </summary>
        public ILogger Logger => _logger;

        #endregion

        #region Methods

        #region Debug Methods

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Debug(object message)
        {
            _fileLogger.Debug(message);
        }

        /// <summary>
        /// Logs a debug message with an exception.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        public void Debug(object message, Exception exception)
        {
            _fileLogger.Debug(message);
        }

        /// <summary>
        /// Logs a formatted debug message.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The format arguments.</param>
        public void DebugFormat(string format, params object[] args)
        {
            _logger.Log(null, Level.Debug, string.Format(CultureInfo.InvariantCulture, format, args), null);
        }

        /// <summary>
        /// Logs a formatted debug message with one argument.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first format argument.</param>
        public void DebugFormat(string format, object arg0)
        {
            _logger.Log(null, Level.Debug, string.Format(CultureInfo.InvariantCulture, format, arg0), null);
        }

        /// <summary>
        /// Logs a formatted debug message with two arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first format argument.</param>
        /// <param name="arg1">The second format argument.</param>
        public void DebugFormat(string format, object arg0, object arg1)
        {
            _logger.Log(null, Level.Debug, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1), null);
        }

        /// <summary>
        /// Logs a formatted debug message with three arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first format argument.</param>
        /// <param name="arg1">The second format argument.</param>
        /// <param name="arg2">The third format argument.</param>
        public void DebugFormat(string format, object arg0, object arg1, object arg2)
        {
            _logger.Log(null, Level.Debug, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2), null);
        }

        /// <summary>
        /// Logs a formatted debug message with a custom format provider.
        /// </summary>
        /// <param name="provider">The format provider.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The format arguments.</param>
        public void DebugFormat(IFormatProvider provider, string format, params object[] args)
        {
            _logger.Log(null, Level.Debug, string.Format(provider, format, args), null);
        }

        #endregion

        #region Info Methods

        /// <summary>
        /// Logs an info message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Info(object message)
        {
            _fileLogger.Info(message);
        }

        /// <summary>
        /// Logs an info message with an exception.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        public void Info(object message, Exception exception)
        {
            _fileLogger.Info(message);
        }

        /// <summary>
        /// Logs a formatted info message.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The format arguments.</param>
        public void InfoFormat(string format, params object[] args)
        {
            _logger.Log(null, Level.Info, string.Format(CultureInfo.InvariantCulture, format, args), null);
        }

        /// <summary>
        /// Logs a formatted info message with one argument.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first format argument.</param>
        public void InfoFormat(string format, object arg0)
        {
            _logger.Log(null, Level.Info, string.Format(CultureInfo.InvariantCulture, format, arg0), null);
        }

        /// <summary>
        /// Logs a formatted info message with two arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first format argument.</param>
        /// <param name="arg1">The second format argument.</param>
        public void InfoFormat(string format, object arg0, object arg1)
        {
            _logger.Log(null, Level.Info, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1), null);
        }

        /// <summary>
        /// Logs a formatted info message with three arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first format argument.</param>
        /// <param name="arg1">The second format argument.</param>
        /// <param name="arg2">The third format argument.</param>
        public void InfoFormat(string format, object arg0, object arg1, object arg2)
        {
            _logger.Log(null, Level.Info, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2), null);
        }

        /// <summary>
        /// Logs a formatted info message with a custom format provider.
        /// </summary>
        /// <param name="provider">The format provider.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The format arguments.</param>
        public void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
            _logger.Log(null, Level.Info, string.Format(provider, format, args), null);
        }

        #endregion

        #region Warn Methods

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Warn(object message)
        {
            _fileLogger.Warn(message);
        }

        /// <summary>
        /// Logs a warning message with an exception.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        public void Warn(object message, Exception exception)
        {
            _fileLogger.Warn(message);
        }

        /// <summary>
        /// Logs a formatted warning message.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The format arguments.</param>
        public void WarnFormat(string format, params object[] args)
        {
            _logger.Log(null, Level.Warn, string.Format(CultureInfo.InvariantCulture, format, args), null);
        }

        /// <summary>
        /// Logs a formatted warning message with one argument.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first format argument.</param>
        public void WarnFormat(string format, object arg0)
        {
            _logger.Log(null, Level.Warn, string.Format(CultureInfo.InvariantCulture, format, arg0), null);
        }

        /// <summary>
        /// Logs a formatted warning message with two arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first format argument.</param>
        /// <param name="arg1">The second format argument.</param>
        public void WarnFormat(string format, object arg0, object arg1)
        {
            _logger.Log(null, Level.Warn, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1), null);
        }

        /// <summary>
        /// Logs a formatted warning message with three arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first format argument.</param>
        /// <param name="arg1">The second format argument.</param>
        /// <param name="arg2">The third format argument.</param>
        public void WarnFormat(string format, object arg0, object arg1, object arg2)
        {
            _logger.Log(null, Level.Warn, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2), null);
        }

        /// <summary>
        /// Logs a formatted warning message with a custom format provider.
        /// </summary>
        /// <param name="provider">The format provider.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The format arguments.</param>
        public void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
            _logger.Log(null, Level.Warn, string.Format(provider, format, args), null);
        }

        #endregion

        #region Error Methods

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Error(object message)
        {
            _fileLogger.Error(message);
        }

        /// <summary>
        /// Logs an error message with an exception.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        public void Error(object message, Exception exception)
        {
            _fileLogger.Error(message);
        }

        /// <summary>
        /// Logs a formatted error message.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The format arguments.</param>
        public void ErrorFormat(string format, params object[] args)
        {
            _logger.Log(null, Level.Error, string.Format(CultureInfo.InvariantCulture, format, args), null);
        }

        /// <summary>
        /// Logs a formatted error message with one argument.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first format argument.</param>
        public void ErrorFormat(string format, object arg0)
        {
            _logger.Log(null, Level.Error, string.Format(CultureInfo.InvariantCulture, format, arg0), null);
        }

        /// <summary>
        /// Logs a formatted error message with two arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first format argument.</param>
        /// <param name="arg1">The second format argument.</param>
        public void ErrorFormat(string format, object arg0, object arg1)
        {
            _logger.Log(null, Level.Error, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1), null);
        }

        /// <summary>
        /// Logs a formatted error message with three arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first format argument.</param>
        /// <param name="arg1">The second format argument.</param>
        /// <param name="arg2">The third format argument.</param>
        public void ErrorFormat(string format, object arg0, object arg1, object arg2)
        {
            _logger.Log(null, Level.Error, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2), null);
        }

        /// <summary>
        /// Logs a formatted error message with a custom format provider.
        /// </summary>
        /// <param name="provider">The format provider.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The format arguments.</param>
        public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
            _logger.Log(null, Level.Error, string.Format(provider, format, args), null);
        }

        #endregion

        #region Fatal Methods

        /// <summary>
        /// Logs a fatal message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Fatal(object message)
        {
            _fileLogger.Fatal(message);
        }

        /// <summary>
        /// Logs a fatal message with an exception.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        public void Fatal(object message, Exception exception)
        {
            _fileLogger.Fatal(message);
        }

        /// <summary>
        /// Logs a formatted fatal message.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="args">The format arguments.</param>
        public void FatalFormat(string format, params object[] args)
        {
            _logger.Log(null, Level.Fatal, string.Format(CultureInfo.InvariantCulture, format, args), null);
        }

        /// <summary>
        /// Logs a formatted fatal message with one argument.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first format argument.</param>
        public void FatalFormat(string format, object arg0)
        {
            _logger.Log(null, Level.Fatal, string.Format(CultureInfo.InvariantCulture, format, arg0), null);
        }

        /// <summary>
        /// Logs a formatted fatal message with two arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first format argument.</param>
        /// <param name="arg1">The second format argument.</param>
        public void FatalFormat(string format, object arg0, object arg1)
        {
            _logger.Log(null, Level.Fatal, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1), null);
        }

        /// <summary>
        /// Logs a formatted fatal message with three arguments.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="arg0">The first format argument.</param>
        /// <param name="arg1">The second format argument.</param>
        /// <param name="arg2">The third format argument.</param>
        public void FatalFormat(string format, object arg0, object arg1, object arg2)
        {
            _logger.Log(null, Level.Fatal, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2), null);
        }

        /// <summary>
        /// Logs a formatted fatal message with a custom format provider.
        /// </summary>
        /// <param name="provider">The format provider.</param>
        /// <param name="format">The format string.</param>
        /// <param name="args">The format arguments.</param>
        public void FatalFormat(IFormatProvider provider, string format, params object[] args)
        {
            _logger.Log(null, Level.Fatal, string.Format(provider, format, args), null);
        }

        #endregion

        #endregion
    }

}