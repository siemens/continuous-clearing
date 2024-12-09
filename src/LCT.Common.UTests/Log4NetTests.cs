// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using log4net;
using log4net.Appender;
using log4net.Core;
using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;

namespace LCT.Common.UTest
{
    [TestFixture]
    internal class Log4NetTests
    {
        public Log4NetTests()
        {
            Log4Net.CatoolCurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
        }

        [Test]
        public void Init_ShouldConfigureLoggerRepositoryWithEntryAssembly()
        {
            // Arrange
            string logFileName = "test.log";
            string logFolder = "logs";
            bool verbose = true;

            // Act
            Log4Net.Init(logFileName, logFolder, verbose);

            // Assert
            Assert.That(Log4Net.LoggerRepository, Is.EqualTo(LogManager.GetRepository(Assembly.GetEntryAssembly())));
        }

        [Test]
        public void Init_ShouldConfigureXmlConfiguratorWithDefaultLogConfigFile()
        {
            // Arrange
            string logFileName = "test.log";
            string logFolder = "logs";
            bool verbose = true;

            // Act
            Log4Net.Init(logFileName, logFolder, verbose);

            // Assert
            Assert.That(Log4Net.LoggerRepository, Is.Not.Null);
            Assert.That(Log4Net.LoggerRepository!.Configured, Is.True);
        }

        [Test]
        public void Init_ShouldSetLogPath()
        {
            // Arrange
            string logFileName = "test.log";
            string logFolder = "logs";
            bool verbose = true;
            Log4Net.CatoolCurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;

            // Act
            Log4Net.Init(logFileName, logFolder, verbose);

            // Assert
            string expectedLogPath = Path.Combine(Log4Net.CatoolCurrentDirectory, logFolder, logFileName);
            Assert.That(Log4Net.CatoolLogPath, Is.EqualTo(expectedLogPath));
        }

        [Test]
        public void Init_ShouldActivateFileAppenderWithVerboseLogging()
        {
            // Arrange
            string logFileName = "test.log";
            string logFolder = "logs";
            bool verbose = true;

            // Act
            Log4Net.Init(logFileName, logFolder, verbose);

            // Assert
            IAppender[] appenders = Log4Net.LoggerRepository!.GetAppenders();
            Assert.That(appenders, Is.Not.Null);
            foreach (IAppender appender in appenders)
            {
                if (appender is AppenderSkeleton appenderSkeleton)
                {
                    Assert.That(appenderSkeleton.Threshold, Is.EqualTo(Level.All));
                }
            }
        }

        [Test]
        public void Init_ShouldActivateFileAppenderWithoutVerboseLogging()
        {
            // Arrange
            string logFileName = "test.log";
            string logFolder = "logs";
            bool verbose = false;

            // Act
            Log4Net.Init(logFileName, logFolder, verbose);

            // Assert
            IAppender[] appenders = Log4Net.LoggerRepository!.GetAppenders();
            Assert.That(appenders, Is.Not.Null);
            foreach (IAppender appender in appenders)
            {
                if (appender is AppenderSkeleton appenderSkeleton)
                {
                    Assert.That(appenderSkeleton.Threshold, Is.Not.EqualTo(Level.All));
                }
            }
        }

        [Test]
        public void GetDefaultLogConfigFile_WhenEnvironmentIsAzurePipeline_ShouldReturnLog4NetAnsiConfigPath()
        {
            // Arrange
            // Set the environment variable as AzurePipeline
            Environment.SetEnvironmentVariable("envType", "AzurePipeline");

            string appFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            string expectedPath = Path.Combine(appFolder, "log4net.ansi.config");

            // Act
            _= Log4Net.GetDefaultLogConfigFile();

            // Assert
            Assert.That(expectedPath, Is.EqualTo(expectedPath));
        }

        [Test]
        public void GetDefaultLogConfigFile_WhenEnvironmentIsNotAzurePipeline_ShouldReturnLog4NetColorConfigPath()
        {
            // Arrange
            string appFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            string expectedPath = Path.Combine(appFolder, "log4net.color.config");

            // Act
            _= Log4Net.GetDefaultLogConfigFile();

            // Assert
            Assert.That(expectedPath, Is.Not.Null);
        }
        [Test]
        public void ActivateFileAppender_VerboseIsTrueAndAppenderIsAppenderSkeleton_ShouldSetThresholdToAll()
        {
            // Arrange
            bool verbose = true;
            string logPath = "test.log";
            IAppender[] appenders = new IAppender[] { new ConsoleAppender() };

            // Act
            Log4Net.ActivateFileAppender(verbose, logPath, appenders);

            // Assert
            Assert.That(((ConsoleAppender)appenders[0]).Threshold, Is.EqualTo(Level.All));
        }

        [Test]
        public void ActivateFileAppender_VerboseIsFalseAndAppenderIsAppenderSkeleton_ShouldNotSetThresholdToAll()
        {
            // Arrange
            bool verbose = false;
            string logPath = "test.log";
            IAppender[] appenders = [new ConsoleAppender()];

            // Act
            Log4Net.ActivateFileAppender(verbose, logPath, appenders);

            // Assert
            Assert.That(((ConsoleAppender)appenders[0]).Threshold, Is.Not.EqualTo(Level.All));
        }

        [Test]
        public void ActivateFileAppender_LogPathIsNotNullOrWhiteSpaceAndAppenderIsRollingFileAppender_ShouldSetFileAndActivateOptions()
        {
            // Arrange
            bool verbose = true;
            string logPath = "test.log";
            IAppender[] appenders = [new RollingFileAppender()];

            // Act
            Log4Net.ActivateFileAppender(verbose, logPath, appenders);

            // Assert
            Assert.That(((RollingFileAppender)appenders[0]).File.EndsWith("test.log"), Is.EqualTo(true));
        }

        [Test]
        public void ActivateFileAppender_LogPathIsNull_ShouldNotSetFileAndNotActivateOptions()
        {
            // Arrange
            bool verbose = true;
            string? logPath = null;
            IAppender[] appenders = [new RollingFileAppender()];

            // Act
            Log4Net.ActivateFileAppender(verbose, logPath!, appenders);

            // Assert
            Assert.That(((RollingFileAppender)appenders[0]).File, Is.EqualTo(null));
        }

        [Test]
        public void ActivateFileAppender_LogPathIsWhiteSpace_ShouldNotSetFileAndNotActivateOptions()
        {
            // Arrange
            bool verbose = true;
            string logPath = " ";
            IAppender[] appenders = [new RollingFileAppender()];

            // Act
            Log4Net.ActivateFileAppender(verbose, logPath, appenders);

            // Assert
            Assert.That(((RollingFileAppender)appenders[0]).File, Is.Null);
        }
    }
}
