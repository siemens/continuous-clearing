// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using NUnit.Framework;
using System;
using System.IO;

namespace LCT.Common.UTest
{
    [TestFixture]
    public class PipelineArtifactUploaderTests
    {
        private StringWriter consoleOutput;

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
            Environment.SetEnvironmentVariable("Build_BuildId", null);
            Environment.SetEnvironmentVariable("CI_JOB_ID", null);
            Environment.SetEnvironmentVariable("Release_ReleaseId", null);
        }

        [Test]
        public void UploadLogs_ShouldUpload_WhenInAzurePipeline_AndLogFileExists()
        {
            // Arrange
            Environment.SetEnvironmentVariable("Build_BuildId", "1234"); // Simulate Azure Pipeline
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
            Environment.SetEnvironmentVariable("Build_BuildId", null); // No pipeline detected

            // Act
            PipelineArtifactUploader.UploadLogs();
            string output = consoleOutput.ToString();

            // Assert
            Assert.That(output, Is.Empty);
        }

        [Test]
        public void UploadBom_ShouldUpload_WhenInAzurePipeline_AndBomFileExists()
        {
            // Arrange
            Environment.SetEnvironmentVariable("Build_BuildId", "1234"); // Simulate Azure Pipeline
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
            Environment.SetEnvironmentVariable("Build_BuildId", null); // No pipeline detected

            // Act
            PipelineArtifactUploader.UploadBom();
            string output = consoleOutput.ToString().Trim();

            // Assert
            Assert.AreEqual("Uploading of SBOM is not supported.", output);
        }
    }
}
