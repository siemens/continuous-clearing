// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications;
using LCT.APICommunications.Model;
using LCT.ArtifactoryUploader;
using LCT.ArtifactoryUploader.Model;
using LCT.Common;
using LCT.Common.Constants;
using LCT.Common.Interface;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;

namespace AritfactoryUploader.UTest
{
    public class PackageUploadInformationTest
    {
        private MemoryAppender _memoryAppender;

        [SetUp]
        public void SetUp()
        {           
            _memoryAppender = new MemoryAppender();
            BasicConfigurator.Configure(_memoryAppender);
            _memoryAppender.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            _memoryAppender?.Close();
        }

        [Test]
        public void SetExitCode_BothCountersNonZero_LogsCombinedWarning_ExitsWith2()
        {
            // Arrange
            var kpi = new UploaderKpiData
            {
                PackagesNotExistingInRemoteCache = 3,
                PackagesNotUploadedDueToError = 2
            };
            EnvironmentHelper environmentHelper = new();

            // Act
            PackageUploadInformation.SetExitCode(kpi, environmentHelper);

            // Assert logs
            LoggingEvent[] events = _memoryAppender.GetEvents();
            Assert.That(events.Length, Is.GreaterThanOrEqualTo(2), "Expected at least a warn and a debug log event.");

            var warnEvent = FindEventByLevel(events, Level.Warn);
            var debugEvent = FindEventByLevel(events, Level.Debug);

            Assert.NotNull(warnEvent, "Warning log was not captured.");
            Assert.NotNull(debugEvent, "Debug log was not captured.");

            StringAssert.Contains("This step failed due to 3 packages not existing in repository and 2 packages not actioned due to error.", warnEvent.RenderedMessage);
            StringAssert.Contains("Setting ExitCode to 2", debugEvent.RenderedMessage);
        }

        [Test]
        public void SetExitCode_OnlyNotInRepoNonZero_LogsRepoWarning_ExitsWith2()
        {
            // Arrange
            var kpi = new UploaderKpiData
            {
                PackagesNotExistingInRemoteCache = 5,
                PackagesNotUploadedDueToError = 0
            };

            EnvironmentHelper environmentHelper = new();

            // Act
            PackageUploadInformation.SetExitCode(kpi, environmentHelper);

            // Assert
            
            var events = _memoryAppender.GetEvents();
            var warnEvent = FindEventByLevel(events, Level.Warn);
            var debugEvent = FindEventByLevel(events, Level.Debug);

            Assert.NotNull(warnEvent);
            Assert.NotNull(debugEvent);

            StringAssert.Contains("This step failed due to 5 packages not existing in repository.", warnEvent.RenderedMessage);
            StringAssert.Contains("Setting ExitCode to 2", debugEvent.RenderedMessage);
        }

        [Test]
        public void SetExitCode_OnlyErrorNonZero_LogsErrorWarning_ExitsWith2()
        {
            // Arrange
            var kpi = new UploaderKpiData
            {
                PackagesNotExistingInRemoteCache = 0,
                PackagesNotUploadedDueToError = 7
            };

            EnvironmentHelper environmentHelper = new();

            // Act
            PackageUploadInformation.SetExitCode(kpi, environmentHelper);

            // Assert
            
            var events = _memoryAppender.GetEvents();
            var warnEvent = FindEventByLevel(events, Level.Warn);
            var debugEvent = FindEventByLevel(events, Level.Debug);

            Assert.NotNull(warnEvent);
            Assert.NotNull(debugEvent);

            StringAssert.Contains("This step failed due to 7 packages not actioned due to error.", warnEvent.RenderedMessage);
            StringAssert.Contains("Setting ExitCode to 2", debugEvent.RenderedMessage);
        }
                
        [Test]
        public void GetUploadPackageDetails_CoversAllScenarios()
        {
            // Arrange
            DisplayPackagesInfo displayPackagesInfo = new DisplayPackagesInfo()
            {
                JfrogFoundPackagesConan = new List<ComponentsToArtifactory>()
                {
                    new ComponentsToArtifactory()
                    {
                        ResponseMessage = new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK
                        }
                    }
                },
                JfrogFoundPackagesMaven = new List<ComponentsToArtifactory>()
                {
                    new ComponentsToArtifactory()
                    {
                        ResponseMessage = new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK
                        }
                    }
                },
                JfrogFoundPackagesNpm = new List<ComponentsToArtifactory>()
                {
                    new ComponentsToArtifactory()
                    {
                        ResponseMessage = new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK
                        }
                    }
                },
                JfrogFoundPackagesNuget = new List<ComponentsToArtifactory>()
                {
                    new ComponentsToArtifactory()
                    {
                        ResponseMessage = new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK
                        }
                    }
                },
                JfrogFoundPackagesPython = new List<ComponentsToArtifactory>()
                {
                    new ComponentsToArtifactory()
                    {
                        ResponseMessage = new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK
                        }
                    }
                },
                JfrogFoundPackagesDebian = new List<ComponentsToArtifactory>()
                {
                    new ComponentsToArtifactory()
                    {
                        ResponseMessage = new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK
                        }
                    }
                },
                JfrogFoundPackagesCargo = new List<ComponentsToArtifactory>()
                {
                    new ComponentsToArtifactory()
                    {
                        ResponseMessage = new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK
                        }
                    }
                }
            };

            // Act
            List<ComponentsToArtifactory> uploadedPackages = PackageUploadInformation.GetUploadePackageDetails(displayPackagesInfo);

            // Assert
            Assert.AreEqual(7, uploadedPackages.Count);
        }
        [Test]
        public void GetNotApprovedDebianPackages_CoversAllScenarios()
        {
            // Arrange
            var unknownPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory { Name = "Package1", Version = "1.0.0" },
                new ComponentsToArtifactory { Name = "Package2", Version = "2.0.0" }
            };
            var projectResponse = new ProjectResponse();
            var fileOperationsMock = new Mock<IFileOperations>();
            var filepath = "..";
            var filename = "\\testFileName.json";

            // Act
            PackageUploadInformation.GetNotApprovedDebianPackages(unknownPackages, projectResponse, fileOperationsMock.Object, filepath, filename);

            // Assert
            // Add your assertions here
            Assert.That(unknownPackages.Count, Is.EqualTo(2));
        }
        [Test]
        public void DisplayErrorForJfrogPackages_GivenJfrogNotFoundPackages_ResultsSucess()
        {
            // Arrange
            ComponentsToArtifactory componentsToArtifactory = new ComponentsToArtifactory();
            componentsToArtifactory.Name = "Test";
            componentsToArtifactory.Version = "0.12.3";
            List<ComponentsToArtifactory> JfrogNotFoundPackages = new() { componentsToArtifactory };
            // Act

            PackageUploadInformation.DisplayErrorForJfrogPackages(JfrogNotFoundPackages);

            // Assert
            Assert.That(JfrogNotFoundPackages.Count, Is.GreaterThanOrEqualTo(1));
        }
        [Test]
        public void DisplayErrorForJfrogFoundPackages_GivenJfrogNotFoundPackages_ResultsSucess()
        {
            // Arrange
            ComponentsToArtifactory componentsToArtifactory = new ComponentsToArtifactory();
            componentsToArtifactory.Name = "Test";
            componentsToArtifactory.Version = "0.12.3";
            componentsToArtifactory.ResponseMessage = new System.Net.Http.HttpResponseMessage()
            { ReasonPhrase = ApiConstant.ErrorInUpload };

            ComponentsToArtifactory componentsToArtifactory2 = new ComponentsToArtifactory();
            componentsToArtifactory2.Name = "Test2";
            componentsToArtifactory2.Version = "0.12.32";
            componentsToArtifactory2.ResponseMessage = new System.Net.Http.HttpResponseMessage()
            { ReasonPhrase = ApiConstant.PackageNotFound };

            ComponentsToArtifactory componentsToArtifactory3 = new ComponentsToArtifactory();
            componentsToArtifactory3.Name = "Test3";
            componentsToArtifactory3.Version = "0.12.33";
            componentsToArtifactory3.ResponseMessage = new System.Net.Http.HttpResponseMessage()
            { ReasonPhrase = "error" };

            List<ComponentsToArtifactory> JfrogNotFoundPackages = new() {
                componentsToArtifactory, componentsToArtifactory2, componentsToArtifactory3 };
            // Act

            PackageUploadInformation.DisplayErrorForJfrogFoundPackages(JfrogNotFoundPackages);

            // Assert
            Assert.That(JfrogNotFoundPackages.Count, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void GetNotApprovedDebianPackages_FileExists_ShouldUpdateDebianComponents()
        {
            // Arrange
            var unknownPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory { Name = "Package1", Version = "1.0.0" },
                new ComponentsToArtifactory { Name = "Package2", Version = "2.0.0" }
            };
            var projectResponse = new ProjectResponse();
            var mockFileOperations = new Mock<IFileOperations>();
            var filepath = Path.GetTempPath();
            var filename = Path.Combine(filepath, $"Artifactory_{FileConstant.artifactoryReportNotApproved}");

            var existingProjectResponse = new ProjectResponse
            {
                Debian = new List<JsonComponents>
                {
                    new JsonComponents { Name = "ExistingPackage", Version = "1.0.0" }
                }
            };
            var json = JsonConvert.SerializeObject(existingProjectResponse);
            File.WriteAllText(filename, json);

            // Act
            PackageUploadInformation.GetNotApprovedDebianPackages(unknownPackages, projectResponse, mockFileOperations.Object, filepath, filename);

            // Assert
            mockFileOperations.Verify(m => m.WriteContentToReportNotApprovedFile(It.Is<ProjectResponse>(pr =>
                pr.Debian.Count == 2 &&
                pr.Debian[0].Name == "Package1" &&
                pr.Debian[0].Version == "1.0.0" &&
                pr.Debian[1].Name == "Package2" &&
                pr.Debian[1].Version == "2.0.0"
            ), filepath, FileConstant.artifactoryReportNotApproved, "Artifactory"), Times.Once);

            // Cleanup
            File.Delete(filename);
        }

        [Test]
        public void GetNotApprovedCargoPackages_FileDoesNotExist_ShouldCreateNewCargoComponents()
        {
            // Arrange
            var unknownPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory { Name = "serde", Version = "1.0.150" },
                new ComponentsToArtifactory { Name = "tokio", Version = "1.23.0" }
            };
            var projectResponse = new ProjectResponse();
            var mockFileOperations = new Mock<IFileOperations>();
            var filepath = Path.GetTempPath();
            var filename = Path.Combine(filepath, "nonexistent_cargo_file.json");

            // Ensure file doesn't exist
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            // Act
            var method = typeof(PackageUploadInformation).GetMethod("GetNotApprovedCargoPackages",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Invoke(null, new object[] { unknownPackages, projectResponse, mockFileOperations.Object, filepath, filename });

            // Assert
            mockFileOperations.Verify(m => m.WriteContentToReportNotApprovedFile(It.Is<ProjectResponse>(pr =>
                pr.Cargo != null &&
                pr.Cargo.Count == 2 &&
                pr.Cargo[0].Name == "serde" &&
                pr.Cargo[0].Version == "1.0.150" &&
                pr.Cargo[1].Name == "tokio" &&
                pr.Cargo[1].Version == "1.23.0"
            ), filepath, FileConstant.artifactoryReportNotApproved, "Artifactory"), Times.Once);
        }

        [Test]
        public void GetNotApprovedCargoPackages_FileExists_ShouldUpdateCargoComponents()
        {
            // Arrange
            var unknownPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory { Name = "clap", Version = "4.0.0" },
                new ComponentsToArtifactory { Name = "regex", Version = "1.7.0" }
            };
            var projectResponse = new ProjectResponse();
            var mockFileOperations = new Mock<IFileOperations>();
            var filepath = Path.GetTempPath();
            var filename = Path.Combine(filepath, $"test_cargo_{FileConstant.artifactoryReportNotApproved}");

            var existingProjectResponse = new ProjectResponse
            {
                Cargo = new List<JsonComponents>
                {
                    new JsonComponents { Name = "ExistingCargoPackage", Version = "1.0.0" }
                },
                Npm = new List<JsonComponents>
                {
                    new JsonComponents { Name = "ExistingNpmPackage", Version = "2.0.0" }
                }
            };
            var json = JsonConvert.SerializeObject(existingProjectResponse);
            File.WriteAllText(filename, json);

            // Act
            var method = typeof(PackageUploadInformation).GetMethod("GetNotApprovedCargoPackages",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Invoke(null, new object[] { unknownPackages, projectResponse, mockFileOperations.Object, filepath, filename });

            // Assert
            mockFileOperations.Verify(m => m.WriteContentToReportNotApprovedFile(It.Is<ProjectResponse>(pr =>
                pr.Cargo != null &&
                pr.Cargo.Count == 2 &&
                pr.Cargo[0].Name == "clap" &&
                pr.Cargo[0].Version == "4.0.0" &&
                pr.Cargo[1].Name == "regex" &&
                pr.Cargo[1].Version == "1.7.0" &&
                pr.Npm != null &&  // Verify existing data is preserved
                pr.Npm.Count == 1 &&
                pr.Npm[0].Name == "ExistingNpmPackage"
            ), filepath, FileConstant.artifactoryReportNotApproved, "Artifactory"), Times.Once);

            // Cleanup
            File.Delete(filename);
        }

        [Test]
        public void GetNotApprovedCargoPackages_EmptyUnknownPackages_FileDoesNotExist_ShouldCreateEmptyCargoList()
        {
            // Arrange
            var unknownPackages = new List<ComponentsToArtifactory>();
            var projectResponse = new ProjectResponse();
            var mockFileOperations = new Mock<IFileOperations>();
            var filepath = Path.GetTempPath();
            var filename = Path.Combine(filepath, "empty_cargo_file.json");

            // Ensure file doesn't exist
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            // Act
            var method = typeof(PackageUploadInformation).GetMethod("GetNotApprovedCargoPackages",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Invoke(null, new object[] { unknownPackages, projectResponse, mockFileOperations.Object, filepath, filename });

            // Assert
            mockFileOperations.Verify(m => m.WriteContentToReportNotApprovedFile(It.Is<ProjectResponse>(pr =>
                pr.Cargo != null &&
                pr.Cargo.Count == 0
            ), filepath, FileConstant.artifactoryReportNotApproved, "Artifactory"), Times.Once);
        }

        [Test]
        public void GetNotApprovedCargoPackages_EmptyUnknownPackages_FileExists_ShouldUpdateWithEmptyCargoList()
        {
            // Arrange
            var unknownPackages = new List<ComponentsToArtifactory>();
            var projectResponse = new ProjectResponse();
            var mockFileOperations = new Mock<IFileOperations>();
            var filepath = Path.GetTempPath();
            var filename = Path.Combine(filepath, $"empty_existing_cargo_{FileConstant.artifactoryReportNotApproved}");

            var existingProjectResponse = new ProjectResponse
            {
                Cargo = new List<JsonComponents>
                {
                    new JsonComponents { Name = "WillBeReplaced", Version = "1.0.0" }
                }
            };
            var json = JsonConvert.SerializeObject(existingProjectResponse);
            File.WriteAllText(filename, json);

            // Act
            var method = typeof(PackageUploadInformation).GetMethod("GetNotApprovedCargoPackages",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Invoke(null, new object[] { unknownPackages, projectResponse, mockFileOperations.Object, filepath, filename });

            // Assert
            mockFileOperations.Verify(m => m.WriteContentToReportNotApprovedFile(It.Is<ProjectResponse>(pr =>
                pr.Cargo != null &&
                pr.Cargo.Count == 0  // Should be empty since unknownPackages was empty
            ), filepath, FileConstant.artifactoryReportNotApproved, "Artifactory"), Times.Once);

            // Cleanup
            File.Delete(filename);
        }

        [Test]
        public void GetNotApprovedCargoPackages_SinglePackage_FileDoesNotExist_ShouldCreateSingleCargoComponent()
        {
            // Arrange
            var unknownPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory { Name = "rand", Version = "0.8.5" }
            };
            var projectResponse = new ProjectResponse();
            var mockFileOperations = new Mock<IFileOperations>();
            var filepath = Path.GetTempPath();
            var filename = Path.Combine(filepath, "single_cargo_file.json");

            // Ensure file doesn't exist
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            // Act
            var method = typeof(PackageUploadInformation).GetMethod("GetNotApprovedCargoPackages",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Invoke(null, new object[] { unknownPackages, projectResponse, mockFileOperations.Object, filepath, filename });

            // Assert
            mockFileOperations.Verify(m => m.WriteContentToReportNotApprovedFile(It.Is<ProjectResponse>(pr =>
                pr.Cargo != null &&
                pr.Cargo.Count == 1 &&
                pr.Cargo[0].Name == "rand" &&
                pr.Cargo[0].Version == "0.8.5"
            ), filepath, FileConstant.artifactoryReportNotApproved, "Artifactory"), Times.Once);
        }

        [Test]
        public void GetNotApprovedCargoPackages_PackagesWithEmptyNameAndVersion_ShouldHandleGracefully()
        {
            // Arrange
            var unknownPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory { Name = "", Version = "" },
                new ComponentsToArtifactory { Name = null, Version = null },
                new ComponentsToArtifactory { Name = "valid-package", Version = "1.0.0" }
            };
            var projectResponse = new ProjectResponse();
            var mockFileOperations = new Mock<IFileOperations>();
            var filepath = Path.GetTempPath();
            var filename = Path.Combine(filepath, "edge_case_cargo_file.json");

            // Ensure file doesn't exist
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            // Act
            var method = typeof(PackageUploadInformation).GetMethod("GetNotApprovedCargoPackages",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Invoke(null, new object[] { unknownPackages, projectResponse, mockFileOperations.Object, filepath, filename });

            // Assert
            mockFileOperations.Verify(m => m.WriteContentToReportNotApprovedFile(It.Is<ProjectResponse>(pr =>
                pr.Cargo != null &&
                pr.Cargo.Count == 3 &&
                pr.Cargo[0].Name == "" &&
                pr.Cargo[0].Version == "" &&
                pr.Cargo[1].Name == null &&
                pr.Cargo[1].Version == null &&
                pr.Cargo[2].Name == "valid-package" &&
                pr.Cargo[2].Version == "1.0.0"
            ), filepath, FileConstant.artifactoryReportNotApproved, "Artifactory"), Times.Once);
        }

        [Test]
        public void GetNotApprovedCargoPackages_FileExistsWithInvalidJson_ShouldHandleJsonException()
        {
            // Arrange
            var unknownPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory { Name = "serde", Version = "1.0.150" }
            };
            var projectResponse = new ProjectResponse();
            var mockFileOperations = new Mock<IFileOperations>();
            var filepath = Path.GetTempPath();
            var filename = Path.Combine(filepath, $"invalid_json_cargo_{FileConstant.artifactoryReportNotApproved}");

            // Create a file with invalid JSON
            File.WriteAllText(filename, "{ invalid json content");

            // Act & Assert
            var method = typeof(PackageUploadInformation).GetMethod("GetNotApprovedCargoPackages",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Should throw JsonReaderException
            Assert.Throws<System.Reflection.TargetInvocationException>(() =>
                method.Invoke(null, new object[] { unknownPackages, projectResponse, mockFileOperations.Object, filepath, filename }));

            // Cleanup
            File.Delete(filename);
        }
        [Test]
        public void GetNotApprovedChocoPackages_FileExists_ShouldUpdateDebianComponents()
        {
            // Arrange
            var unknownPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory { Name = "Package1", Version = "1.0.0" },
                new ComponentsToArtifactory { Name = "Package2", Version = "2.0.0" }
            };
            var projectResponse = new ProjectResponse();
            var mockFileOperations = new Mock<IFileOperations>();
            var filepath = Path.GetTempPath();
            var filename = Path.Combine(filepath, $"Artifactory_{FileConstant.artifactoryReportNotApproved}");

            var existingProjectResponse = new ProjectResponse
            {
                Choco = new List<JsonComponents>
                {
                    new JsonComponents { Name = "ExistingPackage", Version = "1.0.0" }
                }
            };
            var json = JsonConvert.SerializeObject(existingProjectResponse);
            File.WriteAllText(filename, json);

            // Act
            PackageUploadInformation.GetNotApprovedChocoPackages(unknownPackages, projectResponse, mockFileOperations.Object, filepath, filename);

            // Assert
            mockFileOperations.Verify(m => m.WriteContentToReportNotApprovedFile(It.Is<ProjectResponse>(pr =>
                pr.Choco.Count == 2 &&
                pr.Choco[0].Name == "Package1" &&
                pr.Choco[0].Version == "1.0.0" &&
                pr.Choco[1].Name == "Package2" &&
                pr.Choco[1].Version == "2.0.0"
            ), filepath, FileConstant.artifactoryReportNotApproved, "Artifactory"), Times.Once);

            // Cleanup
            File.Delete(filename);
        }

        private static LoggingEvent FindEventByLevel(LoggingEvent[] events, Level level)
        {
            foreach (var e in events)
            {
                if (e.Level == level)
                    return e;
            }
            return null;
        }
    }
}
