﻿// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using log4net.Appender;
using log4net.Config;
using NUnit.Framework;
using System;
using System.IO;

namespace LCT.Common.UTest
{
    [TestFixture]
    public class PipelineArtifactUploaderTests
    {
        private StringWriter consoleOutput;
        private MemoryAppender memoryAppender;

        [SetUp]
        public void SetUp()
        {
            consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);
        }

        [TearDown]
        public void TearDown()
        {
            consoleOutput.Dispose();
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });

            // Cleanup environment variables
            Environment.SetEnvironmentVariable("BUILD_BUILDID", null);
            Environment.SetEnvironmentVariable("CI_JOB_ID", null);
            Environment.SetEnvironmentVariable("RELEASE_RELEASEID", null);
        }

        [Test]
        public void UploadLogs_ShouldUpload_WhenInAzurePipeline_AndLogFileExists()
        {
            // Arrange
            Environment.SetEnvironmentVariable("BUILD_BUILDID", "1234"); // Simulate Azure Pipeline
            Log4Net.CatoolLogPath = "mockLogPath.log";
            File.WriteAllText(Log4Net.CatoolLogPath, "Test Log Content");

            // Act
            PipelineArtifactUploader.UploadLogs();
            string output = consoleOutput.ToString();

            // Assert
            Assert.That(output, Does.Contain($"##vso[artifact.upload containerfolder={PipelineArtifactUploader.LogContainerFolderName};artifactname={PipelineArtifactUploader.LogArtifactFolderName}]{Log4Net.CatoolLogPath}"));

            // Cleanup
            File.Delete(Log4Net.CatoolLogPath);
        }

        [Test]
        public void UploadLogs_ShouldNotUpload_WhenInUnknownEnvironment()
        {
            // Arrange
            Environment.SetEnvironmentVariable("BUILD_BUILDID", null); // No pipeline detected
            memoryAppender = new MemoryAppender();
            BasicConfigurator.Configure(memoryAppender);
            // Act
            PipelineArtifactUploader.UploadLogs();

            string expectedlogmessage = "Uploading of logs is not supported.";

            var logEvents = memoryAppender.GetEvents();

            // Assert
            Assert.IsNotEmpty(logEvents);
            var actualLogMessage = logEvents[0].RenderedMessage;
            Assert.AreEqual(expectedlogmessage, actualLogMessage);
        }

        [Test]
        public void UploadBom_ShouldUpload_WhenInAzurePipeline_AndBomFileExists()
        {
            // Arrange
            Environment.SetEnvironmentVariable("BUILD_BUILDID", "1234"); // Simulate Azure Pipeline
            FileOperations.CatoolBomFilePath = "mockBomFile.json";
            File.WriteAllText(FileOperations.CatoolBomFilePath, "{}"); // Simulate BOM file

            // Act
            PipelineArtifactUploader.UploadBom();
            string output = consoleOutput.ToString();

            // Assert
            Assert.That(output, Does.Contain($"##vso[artifact.upload containerfolder={PipelineArtifactUploader.BomContainerFolderName};artifactname={PipelineArtifactUploader.BomArtifactFolderName}]{FileOperations.CatoolBomFilePath}"));

            // Cleanup
            File.Delete(FileOperations.CatoolBomFilePath);
        }

        [Test]
        public void UploadBom_ShouldNotUpload_WhenInUnknownEnvironment()
        {
            // Arrange
            Environment.SetEnvironmentVariable("BUILD_BUILDID", null); // No pipeline detected
            memoryAppender = new MemoryAppender();
            BasicConfigurator.Configure(memoryAppender);
            // Act
            PipelineArtifactUploader.UploadBom();
            string expectedlogmessage = "Uploading of SBOM is not supported.";

            var logEvents = memoryAppender.GetEvents();

            // Assert
            Assert.IsNotEmpty(logEvents);
            var actualLogMessage = logEvents[0].RenderedMessage;
            Assert.AreEqual(expectedlogmessage, actualLogMessage);
        }
    }
}
