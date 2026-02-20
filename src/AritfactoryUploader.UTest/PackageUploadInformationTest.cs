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

namespace LCT.ArtifactoryUploader.UTest
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

            StringAssert.Contains("Artifactory uploader exited with warning, due to 3 packages not found in repository and 2 packages not actioned due to error.", warnEvent.RenderedMessage);
            StringAssert.Contains("For more detailed packages information, check the above tables.", warnEvent.RenderedMessage);
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

            StringAssert.Contains("Artifactory uploader exited with warning, due to 5 packages not found in repository.", warnEvent.RenderedMessage);
            StringAssert.Contains("For more detailed packages information, check the above tables.", warnEvent.RenderedMessage);
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

            StringAssert.Contains("Artifactory uploader exited with warning, due to 7 packages not actioned due to error.", warnEvent.RenderedMessage);
            StringAssert.Contains("For more detailed packages information, check the above tables.", warnEvent.RenderedMessage);
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
        public void GetNotApprovedNpmPackages_FileDoesNotExist_ShouldCreateNewNpmComponents()
        {
            // Arrange
            var unknownPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory { Name = "react", Version = "18.2.0" },
                new ComponentsToArtifactory { Name = "lodash", Version = "4.17.21" }
            };
            var projectResponse = new ProjectResponse();
            var mockFileOperations = new Mock<IFileOperations>();
            var filepath = Path.GetTempPath();
            var filename = Path.Combine(filepath, "nonexistent_npm_file.json");

            // Ensure file doesn't exist
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            // Act
            var method = typeof(PackageUploadInformation).GetMethod("GetNotApprovedNpmPackages",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Invoke(null, new object[] { unknownPackages, projectResponse, mockFileOperations.Object, filepath, filename });

            // Assert
            mockFileOperations.Verify(m => m.WriteContentToReportNotApprovedFile(It.Is<ProjectResponse>(pr =>
                pr.Npm != null &&
                pr.Npm.Count == 2 &&
                pr.Npm[0].Name == "react" &&
                pr.Npm[0].Version == "18.2.0" &&
                pr.Npm[1].Name == "lodash" &&
                pr.Npm[1].Version == "4.17.21"
            ), filepath, FileConstant.artifactoryReportNotApproved, "Artifactory"), Times.Once);
        }

        [Test]
        public void GetNotApprovedNpmPackages_FileExists_ShouldUpdateNpmComponents()
        {
            // Arrange
            var unknownPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory { Name = "vue", Version = "3.3.0" }
            };
            var projectResponse = new ProjectResponse();
            var mockFileOperations = new Mock<IFileOperations>();
            var filepath = Path.GetTempPath();
            var filename = Path.Combine(filepath, $"test_npm_{FileConstant.artifactoryReportNotApproved}");

            var existingProjectResponse = new ProjectResponse
            {
                Npm = new List<JsonComponents>
                {
                    new JsonComponents { Name = "ExistingNpmPackage", Version = "1.0.0" }
                }
            };
            var json = JsonConvert.SerializeObject(existingProjectResponse);
            File.WriteAllText(filename, json);

            // Act
            var method = typeof(PackageUploadInformation).GetMethod("GetNotApprovedNpmPackages",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Invoke(null, new object[] { unknownPackages, projectResponse, mockFileOperations.Object, filepath, filename });

            // Assert
            mockFileOperations.Verify(m => m.WriteContentToReportNotApprovedFile(It.Is<ProjectResponse>(pr =>
                pr.Npm != null &&
                pr.Npm.Count == 1 &&
                pr.Npm[0].Name == "vue" &&
                pr.Npm[0].Version == "3.3.0"
            ), filepath, FileConstant.artifactoryReportNotApproved, "Artifactory"), Times.Once);

            // Cleanup
            File.Delete(filename);
        }

        [Test]
        public void GetNotApprovedNugetPackages_FileDoesNotExist_ShouldCreateNewNugetComponents()
        {
            // Arrange
            var unknownPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory { Name = "Newtonsoft.Json", Version = "13.0.3" },
                new ComponentsToArtifactory { Name = "AutoMapper", Version = "12.0.1" }
            };
            var projectResponse = new ProjectResponse();
            var mockFileOperations = new Mock<IFileOperations>();
            var filepath = Path.GetTempPath();
            var filename = Path.Combine(filepath, "nonexistent_nuget_file.json");

            // Ensure file doesn't exist
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            // Act
            var method = typeof(PackageUploadInformation).GetMethod("GetNotApprovedNugetPackages",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Invoke(null, new object[] { unknownPackages, projectResponse, mockFileOperations.Object, filepath, filename });

            // Assert
            mockFileOperations.Verify(m => m.WriteContentToReportNotApprovedFile(It.Is<ProjectResponse>(pr =>
                pr.Nuget != null &&
                pr.Nuget.Count == 2 &&
                pr.Nuget[0].Name == "Newtonsoft.Json" &&
                pr.Nuget[0].Version == "13.0.3" &&
                pr.Nuget[1].Name == "AutoMapper" &&
                pr.Nuget[1].Version == "12.0.1"
            ), filepath, FileConstant.artifactoryReportNotApproved, "Artifactory"), Times.Once);
        }

        [Test]
        public void GetNotApprovedNugetPackages_FileExists_ShouldUpdateNugetComponents()
        {
            // Arrange
            var unknownPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory { Name = "Serilog", Version = "3.1.0" }
            };
            var projectResponse = new ProjectResponse();
            var mockFileOperations = new Mock<IFileOperations>();
            var filepath = Path.GetTempPath();
            var filename = Path.Combine(filepath, $"test_nuget_{FileConstant.artifactoryReportNotApproved}");

            var existingProjectResponse = new ProjectResponse
            {
                Nuget = new List<JsonComponents>
                {
                    new JsonComponents { Name = "ExistingNugetPackage", Version = "2.0.0" }
                }
            };
            var json = JsonConvert.SerializeObject(existingProjectResponse);
            File.WriteAllText(filename, json);

            // Act
            var method = typeof(PackageUploadInformation).GetMethod("GetNotApprovedNugetPackages",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Invoke(null, new object[] { unknownPackages, projectResponse, mockFileOperations.Object, filepath, filename });

            // Assert
            mockFileOperations.Verify(m => m.WriteContentToReportNotApprovedFile(It.Is<ProjectResponse>(pr =>
                pr.Nuget != null &&
                pr.Nuget.Count == 1 &&
                pr.Nuget[0].Name == "Serilog" &&
                pr.Nuget[0].Version == "3.1.0"
            ), filepath, FileConstant.artifactoryReportNotApproved, "Artifactory"), Times.Once);

            // Cleanup
            File.Delete(filename);
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
        public void DisplayErrorForSucessfullPackages_WithEmptyList_ShouldNotLog()
        {
            // Arrange
            List<ComponentsToArtifactory> emptySuccessfulPackages = new List<ComponentsToArtifactory>();
            _memoryAppender.Clear();

            // Act
            var method = typeof(PackageUploadInformation).GetMethod("DisplayErrorForSucessfullPackages",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Invoke(null, new object[] { emptySuccessfulPackages });

            // Assert
            var events = _memoryAppender.GetEvents();
            Assert.AreEqual(0, events.Length, "Expected no log events for empty successful packages list");
        }

        [Test]
        public void DisplayErrorForSucessfullPackages_WithPackages_ShouldLogAllPackages()
        {
            // Arrange
            var successfulPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory { Name = "Package1", Version = "1.0.0" },
                new ComponentsToArtifactory { Name = "Package2", Version = "2.0.0" },
                new ComponentsToArtifactory { Name = "Package3", Version = "3.0.0" }
            };
            _memoryAppender.Clear();

            // Act
            var method = typeof(PackageUploadInformation).GetMethod("DisplayErrorForSucessfullPackages",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.Invoke(null, new object[] { successfulPackages });

            // Assert
            var events = _memoryAppender.GetEvents();
            Assert.That(events.Length, Is.GreaterThanOrEqualTo(3), "Expected at least 3 log events");

            // Verify all packages are logged
            var package1Event = FindEventContaining(events, "Package1-1.0.0");
            var package2Event = FindEventContaining(events, "Package2-2.0.0");
            var package3Event = FindEventContaining(events, "Package3-3.0.0");

            Assert.IsNotNull(package1Event, "Package1 should be logged");
            Assert.IsNotNull(package2Event, "Package2 should be logged");
            Assert.IsNotNull(package3Event, "Package3 should be logged");

            StringAssert.Contains("already uploaded", package1Event.RenderedMessage);
            StringAssert.Contains("already uploaded", package2Event.RenderedMessage);
            StringAssert.Contains("already uploaded", package3Event.RenderedMessage);
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

        [Test]
        public void DisplayWithLogger_WithAllPackageTypes_ShouldLogAllInformation()
        {
            // Arrange
            var unknownPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory { Name = "UnknownPkg", Version = "1.0.0" }
            };

            var jfrogNotFoundPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory { Name = "NotFoundPkg", Version = "2.0.0" }
            };

            var successfulPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory { Name = "SuccessPkg", Version = "3.0.0" }
            };

            var jfrogFoundPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory 
                { 
                    Name = "FoundPkg", 
                    Version = "4.0.0",
                    ResponseMessage = new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        ReasonPhrase = "OK"
                    },
                    OperationType = "Upload",
                    SrcRepoName = "SourceRepo",
                    DestRepoName = "DestRepo",
                    DryRunSuffix = ""
                }
            };

            var filepath = Path.GetTempPath();

            // Use reflection to call the private method
            var method = typeof(PackageUploadInformation).GetMethod("DisplayWithLogger",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            method.Invoke(null, new object[] 
            { 
                unknownPackages, 
                jfrogNotFoundPackages, 
                successfulPackages, 
                jfrogFoundPackages, 
                "TestPackageType", 
                filepath 
            });

            // Assert
            var events = _memoryAppender.GetEvents();
            Assert.IsTrue(events.Length > 0, "Expected log events to be captured");
            
            var infoEvent = FindEventByLevel(events, Level.Info);
            Assert.IsNotNull(infoEvent, "Expected Info level log event");
            StringAssert.Contains("TestPackageType", infoEvent.RenderedMessage);
        }

        [Test]
        public void DisplayWithLogger_WithEmptyPackageLists_ShouldLogName()
        {
            // Arrange
            var emptyList = new List<ComponentsToArtifactory>();
            var filepath = Path.GetTempPath();

            var method = typeof(PackageUploadInformation).GetMethod("DisplayWithLogger",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            method.Invoke(null, new object[] 
            { 
                emptyList, 
                emptyList, 
                emptyList, 
                emptyList, 
                "EmptyPackageType", 
                filepath 
            });

            // Assert
            var events = _memoryAppender.GetEvents();
            Assert.IsTrue(events.Length > 0, "Expected at least one log event");
            
            var infoEvent = FindEventByLevel(events, Level.Info);
            Assert.IsNotNull(infoEvent, "Expected Info level log event");
            StringAssert.Contains("EmptyPackageType", infoEvent.RenderedMessage);
        }

        [Test]
        public void DisplayWithLogger_WithJfrogFoundPackagesErrorInUpload_ShouldLogError()
        {
            // Arrange
            var jfrogFoundPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory 
                { 
                    Name = "FailedPkg", 
                    Version = "1.0.0",
                    ResponseMessage = new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.InternalServerError,
                        ReasonPhrase = ApiConstant.ErrorInUpload
                    },
                    OperationType = "Upload",
                    SrcRepoName = "SourceRepo",
                    DestRepoName = "DestRepo"
                }
            };

            var emptyList = new List<ComponentsToArtifactory>();
            var filepath = Path.GetTempPath();

            var method = typeof(PackageUploadInformation).GetMethod("DisplayWithLogger",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            method.Invoke(null, new object[] 
            { 
                emptyList, 
                emptyList, 
                emptyList, 
                jfrogFoundPackages, 
                "npm", 
                filepath 
            });

            // Assert
            var events = _memoryAppender.GetEvents();
            var errorEvent = FindEventByLevel(events, Level.Error);
            Assert.IsNotNull(errorEvent, "Expected Error level log event");
            StringAssert.Contains("FailedPkg", errorEvent.RenderedMessage);
            StringAssert.Contains("Failed", errorEvent.RenderedMessage);
        }

        [Test]
        public void DisplayWithLogger_WithJfrogNotFoundPackages_ShouldLogWarning()
        {
            // Arrange
            var jfrogNotFoundPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory { Name = "MissingPkg", Version = "5.0.0" }
            };

            var emptyList = new List<ComponentsToArtifactory>();
            var filepath = Path.GetTempPath();

            var method = typeof(PackageUploadInformation).GetMethod("DisplayWithLogger",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            method.Invoke(null, new object[] 
            { 
                emptyList, 
                jfrogNotFoundPackages, 
                emptyList, 
                emptyList, 
                "NuGet", 
                filepath 
            });

            // Assert
            var events = _memoryAppender.GetEvents();
            var warnEvent = FindEventByLevel(events, Level.Warn);
            Assert.IsNotNull(warnEvent, "Expected Warn level log event");
            StringAssert.Contains("MissingPkg", warnEvent.RenderedMessage);
            StringAssert.Contains("not found in jfrog", warnEvent.RenderedMessage);
        }

        [Test]
        public void DisplayWithLogger_WithSuccessfulPackages_ShouldLogInfo()
        {
            // Arrange
            var successfulPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory { Name = "UploadedPkg", Version = "6.0.0" }
            };

            var emptyList = new List<ComponentsToArtifactory>();
            var filepath = Path.GetTempPath();

            var method = typeof(PackageUploadInformation).GetMethod("DisplayWithLogger",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            method.Invoke(null, new object[] 
            { 
                emptyList, 
                emptyList, 
                successfulPackages, 
                emptyList, 
                "Maven", 
                filepath 
            });

            // Assert
            var events = _memoryAppender.GetEvents();
            Assert.IsTrue(events.Length >= 2, "Expected at least 2 log events");
            
            // Check if any event contains the package name and success message
            var packageEvent = FindEventContaining(events, "UploadedPkg");
            Assert.IsNotNull(packageEvent, "Expected log event containing package name 'UploadedPkg'");
            StringAssert.Contains("already uploaded", packageEvent.RenderedMessage);
        }

        [Test]
        public void DisplayWithLogger_WithMultiplePackageTypes_ShouldCallAllDisplayMethods()
        {
            // Arrange
            var unknownPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory { Name = "UnknownPkg", Version = "1.0.0" }
            };

            var jfrogNotFoundPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory { Name = "NotFoundPkg", Version = "2.0.0" }
            };

            var successfulPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory { Name = "SuccessPkg", Version = "3.0.0" }
            };

            var jfrogFoundPackages = new List<ComponentsToArtifactory>
            {
                new ComponentsToArtifactory 
                { 
                    Name = "FoundPkg", 
                    Version = "4.0.0",
                    ResponseMessage = new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        ReasonPhrase = "Success"
                    },
                    OperationType = "Copy",
                    SrcRepoName = "SourceRepo",
                    DestRepoName = "DestRepo",
                    DryRunSuffix = " (Dry Run)"
                }
            };

            var filepath = Path.GetTempPath();

            var method = typeof(PackageUploadInformation).GetMethod("DisplayWithLogger",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            method.Invoke(null, new object[] 
            { 
                unknownPackages, 
                jfrogNotFoundPackages, 
                successfulPackages, 
                jfrogFoundPackages, 
                "Python", 
                filepath 
            });

            // Assert
            var events = _memoryAppender.GetEvents();
            Assert.IsTrue(events.Length >= 4, "Expected at least 4 log events (one for each package type)");
            
            // Verify package type name is logged
            var packageTypeEvent = FindEventContaining(events, "Python");
            Assert.IsNotNull(packageTypeEvent, "Expected Python package type to be logged");
            
            // Verify warning event for not found packages
            var warnEvent = FindEventByLevel(events, Level.Warn);
            Assert.IsNotNull(warnEvent, "Expected warning for not found packages");
        }

        private static LoggingEvent FindEventContaining(LoggingEvent[] events, string text)
        {
            foreach (var e in events)
            {
                if (e.RenderedMessage.Contains(text))
                    return e;
            }
            return null;
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
