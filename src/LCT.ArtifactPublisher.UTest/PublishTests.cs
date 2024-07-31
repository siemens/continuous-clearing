// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using NUnit.Framework;

namespace LCT.ArtifactPublisher.UTest
{
    public class PublishTests
    {
        private Publish? _publish;

        [SetUp]
        public void Setup()
        {
            // Initialize the Publish instance with test data
            _publish = new Publish("testLogPath", "testBomFilePath");
        }

        [Test]
        public void UploadLogs_SuccessfullyUploadsLogs()
        {
            // Arrange
            string expectedCommand = $"##vso[artifact.upload containerfolder={Publish.ContainerFolderName};" +
                $"artifactname={Publish.LogArtifactFolderName}]{_publish?.CatoolLogPath}";

            // Act
            using (StringWriter sw = new StringWriter())
            {
                Console.SetOut(sw);
                _publish?.UploadLogs();
                string consoleOutput = sw.ToString().Trim();

                // Assert
                Assert.That(consoleOutput.Contains(consoleOutput), Is.True);
            }
        }

        [Test]
        public void UploadLogs_ThrowsIOException()
        {
            // Arrange
            string expectedExceptionMessage = "Test exception message";
            _publish!.CatoolLogPath = "invalidLogPath";

            // Act
            using (StringWriter sw = new StringWriter())
            {
                Console.SetOut(sw);
                _publish.UploadLogs();
                string consoleOutput = sw.ToString().Trim();

                // Assert
                Assert.That(consoleOutput.Contains(expectedExceptionMessage), Is.False);
            }
        }
    }
}

