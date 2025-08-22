using LCT.Common.Model;
using log4net;
using log4net.Core;
using log4net.Repository;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using Color = Spectre.Console.Color;

namespace LCT.Common
{
    public class SpectreLogAdapter : ILog
    {
        private readonly SpectreLogger _logger;
        private readonly ILog _fileLogger;

        public SpectreLogAdapter(string loggerName)
        {
            string resolvedLoggerName = loggerName ?? "Unknown";
            _logger = new SpectreLogger(resolvedLoggerName);
            _fileLogger = LogManager.GetLogger(resolvedLoggerName);
        }

        public bool IsDebugEnabled => true;
        public bool IsInfoEnabled => true;
        public bool IsWarnEnabled => true;
        public bool IsErrorEnabled => true;
        public bool IsFatalEnabled => true;

        // Return our own implementation of ILogger
        public ILogger Logger => _logger;

        #region Debug Methods
        public void Debug(object message)
        {
            _fileLogger.Debug(message);
        }

        public void Debug(object message, Exception exception)
        {
            _fileLogger.Debug(message);
        }

        public void DebugFormat(string format, params object[] args)
        {
            _logger.Log(null, Level.Debug, string.Format(CultureInfo.InvariantCulture, format, args), null);
        }

        public void DebugFormat(string format, object arg0)
        {
            _logger.Log(null, Level.Debug, string.Format(CultureInfo.InvariantCulture, format, arg0), null);
        }

        public void DebugFormat(string format, object arg0, object arg1)
        {
            _logger.Log(null, Level.Debug, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1), null);
        }

        public void DebugFormat(string format, object arg0, object arg1, object arg2)
        {
            _logger.Log(null, Level.Debug, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2), null);
        }

        public void DebugFormat(IFormatProvider provider, string format, params object[] args)
        {
            _logger.Log(null, Level.Debug, string.Format(provider, format, args), null);
        }
        #endregion

        #region Info Methods
        public void Info(object message)
        {
            _fileLogger.Info(message);
        }

        public void Info(object message, Exception exception)
        {
            _fileLogger.Info(message);
        }

        public void InfoFormat(string format, params object[] args)
        {
            _logger.Log(null, Level.Info, string.Format(CultureInfo.InvariantCulture, format, args), null);
        }

        public void InfoFormat(string format, object arg0)
        {
            _logger.Log(null, Level.Info, string.Format(CultureInfo.InvariantCulture, format, arg0), null);
        }

        public void InfoFormat(string format, object arg0, object arg1)
        {
            _logger.Log(null, Level.Info, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1), null);
        }

        public void InfoFormat(string format, object arg0, object arg1, object arg2)
        {
            _logger.Log(null, Level.Info, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2), null);
        }

        public void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
            _logger.Log(null, Level.Info, string.Format(provider, format, args), null);
        }
        #endregion

        #region Warn Methods
        public void Warn(object message)
        {
            _fileLogger.Warn(message);
        }

        public void Warn(object message, Exception exception)
        {
            _fileLogger.Warn(message);
        }

        public void WarnFormat(string format, params object[] args)
        {
            _logger.Log(null, Level.Warn, string.Format(CultureInfo.InvariantCulture, format, args), null);
        }

        public void WarnFormat(string format, object arg0)
        {
            _logger.Log(null, Level.Warn, string.Format(CultureInfo.InvariantCulture, format, arg0), null);
        }

        public void WarnFormat(string format, object arg0, object arg1)
        {
            _logger.Log(null, Level.Warn, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1), null);
        }

        public void WarnFormat(string format, object arg0, object arg1, object arg2)
        {
            _logger.Log(null, Level.Warn, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2), null);
        }

        public void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
            _logger.Log(null, Level.Warn, string.Format(provider, format, args), null);
        }
        #endregion

        #region Error Methods
        public void Error(object message)
        {
            _fileLogger.Error(message);
        }

        public void Error(object message, Exception exception)
        {
            _fileLogger.Error(message);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            _logger.Log(null, Level.Error, string.Format(CultureInfo.InvariantCulture, format, args), null);
        }

        public void ErrorFormat(string format, object arg0)
        {
            _logger.Log(null, Level.Error, string.Format(CultureInfo.InvariantCulture, format, arg0), null);
        }

        public void ErrorFormat(string format, object arg0, object arg1)
        {
            _logger.Log(null, Level.Error, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1), null);
        }

        public void ErrorFormat(string format, object arg0, object arg1, object arg2)
        {
            _logger.Log(null, Level.Error, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2), null);
        }

        public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
            _logger.Log(null, Level.Error, string.Format(provider, format, args), null);
        }
        
        #endregion

        #region Fatal Methods
        public void Fatal(object message)
        {
            _fileLogger.Fatal(message);
        }

        public void Fatal(object message, Exception exception)
        {
            _fileLogger.Fatal(message);
        }

        public void FatalFormat(string format, params object[] args)
        {
            _logger.Log(null, Level.Fatal, string.Format(CultureInfo.InvariantCulture, format, args), null);
        }

        public void FatalFormat(string format, object arg0)
        {
            _logger.Log(null, Level.Fatal, string.Format(CultureInfo.InvariantCulture, format, arg0), null);
        }

        public void FatalFormat(string format, object arg0, object arg1)
        {
            _logger.Log(null, Level.Fatal, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1), null);
        }

        public void FatalFormat(string format, object arg0, object arg1, object arg2)
        {
            _logger.Log(null, Level.Fatal, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2), null);
        }

        public void FatalFormat(IFormatProvider provider, string format, params object[] args)
        {
            _logger.Log(null, Level.Fatal, string.Format(provider, format, args), null);
        }
        #endregion
    }

    // Implement the ILogger interface to handle direct Logger access
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
            return true; // Always enabled for all log levels
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
                    AnsiConsole.MarkupLine($"[white] {message}[/]");
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
                case "CRITICAL":
                    fileLogger.Fatal(message, exception);
                    break;
                case "EMERGENCY":
                    fileLogger.Fatal(message, exception);
                    break;
                default:
                    fileLogger.Info(message, exception);
                    break;
            }

            // Handle exception if present
            if (exception != null)
            {
                AnsiConsole.WriteException(exception, new ExceptionSettings
                {
                    Format = ExceptionFormats.ShortenEverything,
                    Style = new ExceptionStyle
                    {
                        Exception = new Style().Foreground(Color.Red),
                        Message = new Style().Foreground(Color.Red),
                        NonEmphasized = new Style().Foreground(Color.White),
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