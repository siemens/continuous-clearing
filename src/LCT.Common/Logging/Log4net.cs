// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common.Runtime;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using System;
using System.IO;
using System.Reflection;

namespace LCT.Common
{
    /// <summary>
    /// Class to Log errors to a text file
    /// </summary>
    public static class Log4Net
    {
        #region Fields
        // No fields present.
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the logger repository.
        /// </summary>
        public static ILoggerRepository LoggerRepository { get; set; }

        /// <summary>
        /// Gets or sets the path to the log file.
        /// </summary>
        public static string CatoolLogPath { get; set; }

        /// <summary>
        /// Gets or sets the current directory for the tool.
        /// </summary>
        public static string CatoolCurrentDirectory { get; set; }
        public static bool Verbose { get; set; }

        #endregion Properties

        #region Constructors
        // No constructors present.
        #endregion

        #region Methods
        /// <summary>
        /// Initializes the log4net logger with the specified log file name, folder, and verbosity.
        /// </summary>
        /// <param name="logFileName">The name of the log file.</param>
        /// <param name="logFolder">The folder where the log file will be stored.</param>
        /// <param name="verbose">If true, sets verbose logging level.</param>
        /// <returns>void.</returns>
        public static void Init(string logFileName, string logFolder, bool verbose)
        {
            LoggerRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(LoggerRepository, new FileInfo(GetDefaultLogConfigFile()));
            string logPath = CatoolLogPath = Path.IsPathRooted(logFolder)
                                                ? Path.Combine(logFolder, logFileName)
                                                : Path.Combine(CatoolCurrentDirectory, logFolder, logFileName);

            if (LoggerRepository is Hierarchy rootRepo)
            {
                rootRepo.Root.Level = Level.All;
                LoggerRepository.Threshold = Level.All;
                IAppender[] appenders = LoggerRepository.GetAppenders();
                if (appenders != null)
                {
                    ActivateFileAppender(verbose, logPath, appenders);
                }

                rootRepo.RaiseConfigurationChanged(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Activates the file appender for logging, setting verbosity and log path.
        /// </summary>
        /// <param name="verbose">If true, sets verbose logging level.</param>
        /// <param name="logPath">The path to the log file.</param>
        /// <param name="appenders">The array of appenders to configure.</param>
        /// <returns>void.</returns>
        public static void ActivateFileAppender(bool verbose, string logPath, IAppender[] appenders)
        {
            foreach (IAppender appender in appenders)
            {
                if (verbose && appender is AppenderSkeleton appenderSkeleton)
                {
                    appenderSkeleton.Threshold = Level.All;
                }

                if (!string.IsNullOrWhiteSpace(logPath) && appender is RollingFileAppender fileAppender)
                {
                    fileAppender.File = logPath;
                    fileAppender.ActivateOptions();
                }
            }
        }

        /// <summary>
        /// Gets the default log4net configuration file path based on the environment.
        /// </summary>
        /// <returns>The path to the default log4net config file.</returns>
        public static string GetDefaultLogConfigFile()
        {
            EnvironmentType envType = RuntimeEnvironment.GetEnvironment();
            string appFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (envType == EnvironmentType.AzurePipeline)
            {
                return Path.Combine(appFolder, "log4net.ansi.config");
            }

            return Path.Combine(appFolder, "log4net.color.config");
        }
        public static void AppendVerboseValue(CommonAppSettings appSettings)
        {
            if (appSettings.Verbose || CommonHelper.IsAzureDevOpsDebugEnabled())
            {
                Verbose = true;
            }
            else
            {
                Verbose = false;
            }
        }

        #endregion Methods
    }
}

