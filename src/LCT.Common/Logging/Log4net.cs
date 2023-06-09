// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using System.IO;
using log4net;
using System.Reflection;
using log4net.Config;
using log4net.Repository;
using LCT.Common.Runtime;
using log4net.Repository.Hierarchy;
using log4net.Core;
using log4net.Appender;
using System;

namespace LCT.Common
{
    /// <summary>
    /// Class to Log errors to a text file
    /// </summary>
    public static class Log4Net
    {
        public static ILoggerRepository LoggerRepository { get; set; }
        public static void Init(string logFileName, string logFolder, bool verbose)
        {
            LoggerRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(LoggerRepository, new FileInfo(GetDefaultLogConfigFile()));

            string logPath = Path.Combine(logFolder, logFileName);

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

        private static void ActivateFileAppender(bool verbose, string logPath, IAppender[] appenders)
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
    }
}

       