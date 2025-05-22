using CycloneDX.Models;
using LCT.Common.Constants;
using LCT.Common.Model;
using Moq;
using NUnit.Framework;
using System;
using log4net;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace LCT.Common.UTest
{
    public class LogHandlingHelperTests
    {
        private Mock<ILog> _mockLogger;

        [SetUp]
        public void SetUp()
        {
            // Set up a mock logger
            _mockLogger = new Mock<ILog>();
            LogHandlingHelper.Logger = _mockLogger.Object;
        }

        [Test]
        public void ExceptionErrorHandling_ShouldLogErrorDetails()
        {
            // Arrange
            string context = "TestContext";
            string details = "TestDetails";
            Exception ex = new Exception("TestException");
            string additionalDetails = "AdditionalTestDetails";

            // Act
            LogHandlingHelper.ExceptionErrorHandling(context, details, ex, additionalDetails);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("TestContext") &&
                log.Contains("TestDetails") &&
                log.Contains("TestException") &&
                log.Contains("AdditionalTestDetails"))), Times.Once);
        }
        [Test]
        public void ExceptionErrorHandling_ShouldLogInnerExceptionDetails()
        {
            // Arrange
            string context = "TestContext";
            string details = "TestDetails";
            string additionalDetails = "AdditionalTestDetails";

            // Create an exception with an inner exception
            var innerException = new Exception("InnerExceptionMessage");
            var exception = new Exception("OuterExceptionMessage", innerException);

            // Act
            LogHandlingHelper.ExceptionErrorHandling(context, details, exception, additionalDetails);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("TestContext") &&
                log.Contains("TestDetails") &&
                log.Contains("AdditionalTestDetails") &&
                log.Contains("OuterExceptionMessage") &&
                log.Contains("InnerExceptionMessage") &&
                log.Contains("INNER EXCEPTION DETAILS"))), Times.Once);
        }
        [Test]
        public void BasicErrorHandling_ShouldLogBasicErrorDetails()
        {
            // Arrange
            string context = "TestContext";
            string details = "TestDetails";
            string message = "TestMessage";
            string additional = "TestAdditional";

            // Act
            LogHandlingHelper.BasicErrorHandling(context, details, message, additional);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("TestContext") &&
                log.Contains("TestDetails") &&
                log.Contains("TestMessage") &&
                log.Contains("TestAdditional"))), Times.Once);
        }

        [Test]
        public void HttpResponseErrorHandling_ShouldLogHttpResponseDetails()
        {
            // Arrange
            string context = "TestContext";
            string details = "TestDetails";
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                ReasonPhrase = "Bad Request",
                Content = new StringContent("Test response content")
            };
            response.Headers.Add("Authorization", "Bearer token");
            string additionalDetails = "AdditionalTestDetails";

            // Act
            LogHandlingHelper.HttpResponseErrorHandling(context, details, response, additionalDetails);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("TestContext") &&
                log.Contains("TestDetails") &&
                log.Contains("Bad Request") &&
                log.Contains("Test response content") &&
                log.Contains("AdditionalTestDetails"))), Times.Once);
        }

        [Test]
        public void HttpRequestHandling_ShouldLogRequestDetails()
        {
            // Arrange
            string context = "TestContext";
            string details = "TestDetails";
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer token");
            string url = "https://example.com/api";
            var httpContent = new StringContent("Test request content");

            // Act
            LogHandlingHelper.HttpRequestHandling(context, details, httpClient, url, httpContent);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("TestContext") &&
                log.Contains("TestDetails") &&
                log.Contains("https://example.com/api") &&
                log.Contains("Test request content"))), Times.Once);
        }
        [Test]
        public void HttpResponseHandling_ShouldLogRequestAndResponseDetails()
        {
            // Arrange
            string context = "TestContext";
            string details = "TestDetails";
            string additionalDetails = "AdditionalTestDetails";

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                ReasonPhrase = "OK",
                Content = new StringContent("Test response content")
            };
            response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api");
            response.RequestMessage.Headers.Add("Authorization", "Bearer token");

            // Act
            LogHandlingHelper.HttpResponseHandling(context, details, response, additionalDetails);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("TestContext") &&
                log.Contains("TestDetails") &&
                log.Contains("AdditionalTestDetails") &&
                log.Contains("GET") &&
                log.Contains("https://example.com/api") &&
                log.Contains("Authorization: *****") &&
                log.Contains("Status Code: OK") &&
                log.Contains("Test response content"))), Times.Once);
        }

        [Test]
        public void HttpResponseHandling_ShouldHandleEmptyResponseContent()
        {
            // Arrange
            string context = "TestContext";
            string details = "TestDetails";
            string additionalDetails = "AdditionalTestDetails";

            var response = new HttpResponseMessage(HttpStatusCode.NoContent)
            {
                ReasonPhrase = "No Content",
                Content = null
            };
            response.RequestMessage = new HttpRequestMessage(HttpMethod.Post, "https://example.com/api");

            // Act
            LogHandlingHelper.HttpResponseHandling(context, details, response, additionalDetails);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("TestContext") &&
                log.Contains("TestDetails") &&
                log.Contains("AdditionalTestDetails") &&
                log.Contains("POST") &&
                log.Contains("https://example.com/api") &&
                log.Contains("Status Code: NoContent") &&
                log.Contains("No Content"))), Times.Once);
        }

        [Test]
        public void HttpResponseHandling_ShouldHandleNullResponse()
        {
            // Arrange
            string context = "TestContext";
            string details = "TestDetails";
            string additionalDetails = "AdditionalTestDetails";

            // Act
            LogHandlingHelper.HttpResponseHandling(context, details, null, additionalDetails);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("TestContext") &&
                log.Contains("TestDetails") &&
                log.Contains("AdditionalTestDetails") &&
                log.Contains("Status Code:") &&
                log.Contains("Reason Phrase: No Reason Phrase"))), Times.Once);
        }

        [Test]
        public void HttpResponseHandling_ShouldTruncateContentWhenVerboseIsFalse()
        {
            // Arrange
            string context = "TestContext";
            string details = "TestDetails";
            string additionalDetails = "AdditionalTestDetails";

            var longContent = new StringBuilder();
            for (int i = 0; i < 1500; i++)
            {
                longContent.AppendLine($"Line {i + 1}");
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                ReasonPhrase = "OK",
                Content = new StringContent(longContent.ToString())
            };
            response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api");

            // Simulate non-verbose mode
            Log4Net.Verbose = false;

            // Act
            LogHandlingHelper.HttpResponseHandling(context, details, response, additionalDetails);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("TestContext") &&
                log.Contains("TestDetails") &&
                log.Contains("AdditionalTestDetails") &&
                log.Contains("GET") &&
                log.Contains("https://example.com/api") &&
                log.Contains("Status Code: OK") &&
                log.Contains("... [Content truncated. Showing first 1000 lines. Enable verbose mode to see full content.]"))), Times.Once);
        }
        [Test]
        public void IdentifierComponentsData_ShouldLogTableWithComponents()
        {
            // Arrange
            var allComponents = new List<Component>
            {
                new Component
                {
                    Name = "Component1",
                    Version = "1.0.0",
                    Properties = new List<Property>
                    {
                        new Property { Name = Dataconstant.Cdx_ArtifactoryRepoName, Value = "Repo1" }
                    }
                },
                new Component
                {
                    Name = "Component2",
                    Version = "2.0.0",
                    Properties = new List<Property>
                    {
                        new Property { Name = Dataconstant.Cdx_ArtifactoryRepoName, Value = "Repo2" }
                    }
                }
            };

            var internalComponents = new List<Component> { allComponents[0] };

            // Act
            LogHandlingHelper.IdentifierComponentsData(allComponents, internalComponents);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("Consolidated Component Table") &&
                log.Contains("Component1") &&
                log.Contains("1.0.0") &&
                log.Contains("Repo1") &&
                log.Contains("Yes") &&
                log.Contains("Component2") &&
                log.Contains("2.0.0") &&
                log.Contains("Repo2") &&
                log.Contains("No"))), Times.Once);
        }

        [Test]
        public void IdentifierComponentsData_ShouldLogEmptyTableWhenNoComponents()
        {
            // Arrange
            var allComponents = new List<Component>();
            var internalComponents = new List<Component>();

            // Act
            LogHandlingHelper.IdentifierComponentsData(allComponents, internalComponents);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("Consolidated Component Table"))), Times.Once);
        }

        [Test]
        public void IdentifierComponentsData_ShouldHandleNullProperties()
        {
            // Arrange
            var allComponents = new List<Component>
            {
                new Component
                {
                    Name = "Component1",
                    Version = "1.0.0",
                    Properties = null // No properties
                }
            };

            var internalComponents = new List<Component> { allComponents[0] };

            // Act
            LogHandlingHelper.IdentifierComponentsData(allComponents, internalComponents);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("Consolidated Component Table") &&
                log.Contains("Component1") &&
                log.Contains("1.0.0") &&
                log.Contains('-') && // Default value for missing repo name
                log.Contains("Yes"))), Times.Once);
        }
        [Test]
        public void IdentifierInputfileComponents_ShouldLogTableWithComponents()
        {
            // Arrange
            string filepath = "test-file.json";
            var components = new List<Component>
            {
                new Component
                {
                    Name = "Component1",
                    Version = "1.0.0",
                    Purl = "pkg:example/component1@1.0.0",
                    Properties = new List<Property>
                    {
                        new Property { Name = Dataconstant.Cdx_IsDevelopment, Value = "true" }
                    }
                },
                new Component
                {
                    Name = "Component2",
                    Version = "2.0.0",
                    Purl = "pkg:example/component2@2.0.0",
                    Properties = new List<Property>
                    {
                        new Property { Name = Dataconstant.Cdx_IsDevelopment, Value = "false" }
                    }
                }
            };

            // Act
            LogHandlingHelper.IdentifierInputfileComponents(filepath, components);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("COMPONENTS FOUND IN FILE: test-file.json") &&
                log.Contains("Component1") &&
                log.Contains("1.0.0") &&
                log.Contains("pkg:example/component1@1.0.0") &&
                log.Contains("true") &&
                log.Contains("Component2") &&
                log.Contains("2.0.0") &&
                log.Contains("pkg:example/component2@2.0.0") &&
                log.Contains("false"))), Times.Once);
        }

        [Test]
        public void IdentifierInputfileComponents_ShouldLogMessageWhenNoComponents()
        {
            // Arrange
            string filepath = "test-file.json";
            var components = new List<Component>(); // Empty list

            // Act
            LogHandlingHelper.IdentifierInputfileComponents(filepath, components);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("No components were found in the file: test-file.json"))), Times.Once);
        }

        [Test]
        public void IdentifierInputfileComponents_ShouldHandleNullComponents()
        {
            // Arrange
            string filepath = "test-file.json";
            List<Component> components = null; // Null list

            // Act
            LogHandlingHelper.IdentifierInputfileComponents(filepath, components);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("No components were found in the file: test-file.json"))), Times.Once);
        }

        [Test]
        public void IdentifierInputfileComponents_ShouldHandleMissingProperties()
        {
            // Arrange
            string filepath = "test-file.json";
            var components = new List<Component>
            {
                new Component
                {
                    Name = "Component1",
                    Version = "1.0.0",
                    Purl = "pkg:example/component1@1.0.0",
                    Properties = null // No properties
                }
            };

            // Act
            LogHandlingHelper.IdentifierInputfileComponents(filepath, components);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("COMPONENTS FOUND IN FILE: test-file.json") &&
                log.Contains("Component1") &&
                log.Contains("1.0.0") &&
                log.Contains("pkg:example/component1@1.0.0") &&
                log.Contains("false"))), Times.Once);
        }
        [Test]
        public void ComponentsList_ShouldLogTableWithComponents()
        {
            // Arrange
            string filepath = "test-file.json";
            var components = new List<Component>
            {
                new Component
                {
                    Name = "Component1",
                    Version = "1.0.0",
                    Purl = "pkg:example/component1@1.0.0"
                },
                new Component
                {
                    Name = "Component2",
                    Version = "2.0.0",
                    Purl = "pkg:example/component2@2.0.0"
                }
            };

            // Act
            LogHandlingHelper.ComponentsList(filepath, components);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("Components Foung In File: test-file.json") &&
                log.Contains("Component1") &&
                log.Contains("1.0.0") &&
                log.Contains("pkg:example/component1@1.0.0") &&
                log.Contains("Component2") &&
                log.Contains("2.0.0") &&
                log.Contains("pkg:example/component2@2.0.0"))), Times.Once);
        }

        [Test]
        public void ComponentsList_ShouldLogMessageWhenNoComponents()
        {
            // Arrange
            string filepath = "test-file.json";
            var components = new List<Component>(); // Empty list

            // Act
            LogHandlingHelper.ComponentsList(filepath, components);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("No components were found in the file: test-file.json"))), Times.Once);
        }

        [Test]
        public void SW360AvailableComponentsList_ShouldLogTableWithComponents()
        {
            // Arrange
            var components = new List<Components>
            {
                new Components
                {
                    Name = "Component1",
                    Version = "1.0.0",
                    ReleaseLink = "https://example.com/release1",
                    ReleaseExternalId = "ReleaseExtId1",
                    ComponentExternalId = "ComponentExtId1"
                },
                new Components
                {
                    Name = "Component2",
                    Version = "2.0.0",
                    ReleaseLink = "https://example.com/release2",
                    ReleaseExternalId = "ReleaseExtId2",
                    ComponentExternalId = "ComponentExtId2"
                }
            };

            // Act
            LogHandlingHelper.SW360AvailableComponentsList(components);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("Available SW360 releases data") &&
                log.Contains("Component1") &&
                log.Contains("1.0.0") &&
                log.Contains("https://example.com/release1") &&
                log.Contains("ReleaseExtId1") &&
                log.Contains("ComponentExtId1") &&
                log.Contains("Component2") &&
                log.Contains("2.0.0") &&
                log.Contains("https://example.com/release2") &&
                log.Contains("ReleaseExtId2") &&
                log.Contains("ComponentExtId2"))), Times.Once);
        }

        [Test]
        public void SW360AvailableComponentsList_ShouldLogMessageWhenNoComponents()
        {
            // Arrange
            var components = new List<Components>(); // Empty list

            // Act
            LogHandlingHelper.SW360AvailableComponentsList(components);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("No components were found in the list"))), Times.Once);
        }
        [Test]
        public void SW360AvailableComponentsData_ShouldLogTableWithComponents()
        {
            // Arrange
            var components = new List<ComparisonBomData>
            {
                new ComparisonBomData
                {
                    Name = "Component1",
                    Group = "Group1",
                    Version = "1.0.0",
                    ComponentExternalId = "CompExtId1",
                    ReleaseExternalId = "RelExtId1",
                    SourceUrl = "https://example.com/source1",
                    DownloadUrl = "https://example.com/download1",
                    ComponentStatus = "Active",
                    ReleaseStatus = "Released",
                    ApprovedStatus = "Approved",
                    IsComponentCreated = "true",
                    IsReleaseCreated = "true",
                    FossologyUploadStatus = "Uploaded",
                    ReleaseLink = "https://example.com/release1",
                    ReleaseID = "RelID1",
                    AlpineSource = "Alpine1",
                    PatchURls = new[] { "https://example.com/patch1", "https://example.com/patch2" }
                },
                new ComparisonBomData
                {
                    Name = "Component2",
                    Group = "Group2",
                    Version = "2.0.0",
                    ComponentExternalId = "CompExtId2",
                    ReleaseExternalId = "RelExtId2",
                    SourceUrl = "https://example.com/source2",
                    DownloadUrl = "https://example.com/download2",
                    ComponentStatus = "Inactive",
                    ReleaseStatus = "Draft",
                    ApprovedStatus = "Pending",
                    IsComponentCreated = "false",
                    IsReleaseCreated = "false",
                    FossologyUploadStatus = "Pending",
                    ReleaseLink = "https://example.com/release2",
                    ReleaseID = "RelID2",
                    AlpineSource = "Alpine2",
                    PatchURls = new[] { "https://example.com/patch3" }
                }
            };

            // Act
            LogHandlingHelper.SW360AvailableComponentsData(components);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("Identified component releases data") &&
                log.Contains("Component1") &&
                log.Contains("Group1") &&
                log.Contains("1.0.0") &&
                log.Contains("CompExtId1") &&
                log.Contains("RelExtId1") &&
                log.Contains("https://example.com/source1") &&
                log.Contains("https://example.com/download1") &&
                log.Contains("Active") &&
                log.Contains("Released") &&
                log.Contains("Approved") &&
                log.Contains("true") &&
                log.Contains("Uploaded") &&
                log.Contains("https://example.com/release1") &&
                log.Contains("RelID1") &&
                log.Contains("Alpine1") &&
                log.Contains("https://example.com/patch1, https://example.com/patch2") &&
                log.Contains("Component2") &&
                log.Contains("Group2") &&
                log.Contains("2.0.0") &&
                log.Contains("CompExtId2") &&
                log.Contains("RelExtId2") &&
                log.Contains("https://example.com/source2") &&
                log.Contains("https://example.com/download2") &&
                log.Contains("Inactive") &&
                log.Contains("Draft") &&
                log.Contains("Pending") &&
                log.Contains("false") &&
                log.Contains("Pending") &&
                log.Contains("https://example.com/release2") &&
                log.Contains("RelID2") &&
                log.Contains("Alpine2") &&
                log.Contains("https://example.com/patch3"))), Times.Once);
        }

        [Test]
        public void SW360AvailableComponentsData_ShouldLogMessageWhenNoComponents()
        {
            // Arrange
            var components = new List<ComparisonBomData>(); // Empty list

            // Act
            LogHandlingHelper.SW360AvailableComponentsData(components);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("No components were found in the list"))), Times.Once);
        }

        [Test]
        public void SW360AvailableComponentsData_ShouldHandleNullComponents()
        {
            // Arrange
            List<ComparisonBomData> components = null; // Null list

            // Act
            LogHandlingHelper.SW360AvailableComponentsData(components);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("No components were found in the list"))), Times.Once);
        }
        [Test]
        public void ListOfBomFileComponents_ShouldLogTableWithComponents()
        {
            // Arrange
            string bomFilePath = "test-bom.json";
            var components = new List<Component>
        {
            new Component
            {
                Name = "Component1",
                Version = "1.0.0",
                Purl = "pkg:example/component1@1.0.0",
                Properties = new List<Property>
                {
                    new Property { Name = Dataconstant.Cdx_IsDevelopment, Value = "true" },
                    new Property { Name = Dataconstant.Cdx_ArtifactoryRepoName, Value = "Repo1" }
                }
            }
        };

            // Act
            LogHandlingHelper.ListOfBomFileComponents(bomFilePath, components);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("Components from BOM File: test-bom.json") &&
                log.Contains("Component1") &&
                log.Contains("1.0.0") &&
                log.Contains("pkg:example/component1@1.0.0") &&
                log.Contains("true") &&
                log.Contains("Repo1"))), Times.Once);
        }

        [Test]
        public void ListOfBomFileComponents_ShouldLogWarningWhenNoComponents()
        {
            // Arrange
            string bomFilePath = "test-bom.json";
            var components = new List<Component>();

            // Act
            LogHandlingHelper.ListOfBomFileComponents(bomFilePath, components);

            // Assert
            _mockLogger.Verify(logger => logger.Warn(It.Is<string>(log =>
                log.Contains("No components found in the BOM file: test-bom.json"))), Times.Once);
        }
        [Test]
        public void HttpResponseOfStringContent_ShouldLogResponseBody()
        {
            // Arrange
            string context = "TestContext";
            string details = "TestDetails";
            string responseBody = "This is a test response body.";
            string additionalDetails = "Additional details.";

            // Act
            LogHandlingHelper.HttpResponseOfStringContent(context, details, responseBody, additionalDetails);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("HTTP API RESPONSE DETAILS") &&
                log.Contains("TestContext") &&
                log.Contains("TestDetails") &&
                log.Contains("This is a test response body.") &&
                log.Contains("Additional details."))), Times.Once);
        }

        [Test]
        public void HttpResponseOfStringContent_ShouldTruncateResponseBodyWhenVerboseIsFalse()
        {
            // Arrange
            Log4Net.Verbose = false;
            string context = "TestContext";
            string details = "TestDetails";
            string responseBody = string.Join("\n", Enumerable.Range(1, 1500).Select(i => $"Line {i}"));

            // Act
            LogHandlingHelper.HttpResponseOfStringContent(context, details, responseBody);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("HTTP API RESPONSE DETAILS") &&
                log.Contains("TestContext") &&
                log.Contains("TestDetails") &&
                log.Contains("... [Content truncated. Showing first 1000 lines. Enable verbose mode to see full content.]"))), Times.Once);
        }
        [Test]
        public void ComponentDataForLogTable_ShouldLogComparisonTable()
        {
            // Arrange
            string methodName = "TestMethod";
            var initialItem = new ComparisonBomData
            {
                Name = "InitialComponent",
                Group = "InitialGroup",
                Version = "1.0.0",
                ComponentExternalId = "InitialCompExtId",
                ReleaseExternalId = "InitialRelExtId",
                PackageUrl = "pkg:example/initial@1.0.0",
                SourceUrl = "https://example.com/initial",
                DownloadUrl = "https://example.com/download/initial",
                PatchURls = new[] { "https://example.com/patch1" },
                ComponentStatus = "Active",
                ReleaseStatus = "Released",
                ApprovedStatus = "Approved",
                IsComponentCreated = "true",
                IsReleaseCreated = "true"
            };

            var updatedItem = new ComparisonBomData
            {
                Name = "UpdatedComponent",
                Group = "UpdatedGroup",
                Version = "2.0.0",
                ComponentExternalId = "UpdatedCompExtId",
                ReleaseExternalId = "UpdatedRelExtId",
                PackageUrl = "pkg:example/updated@2.0.0",
                SourceUrl = "https://example.com/updated",
                DownloadUrl = "https://example.com/download/updated",
                PatchURls = new[] { "https://example.com/patch2" },
                ComponentStatus = "Inactive",
                ReleaseStatus = "Draft",
                ApprovedStatus = "Pending",
                IsComponentCreated = "false",
                IsReleaseCreated = "false"
            };

            // Act
            LogHandlingHelper.ComponentDataForLogTable(methodName, initialItem, updatedItem);

            // Assert
            _mockLogger.Verify(logger => logger.Debug(It.Is<string>(log =>
                log.Contains("Method: TestMethod") &&
                log.Contains("InitialComponent") &&
                log.Contains("UpdatedComponent") &&
                log.Contains("InitialGroup") &&
                log.Contains("UpdatedGroup") &&
                log.Contains("1.0.0") &&
                log.Contains("2.0.0") &&
                log.Contains("InitialCompExtId") &&
                log.Contains("UpdatedCompExtId") &&
                log.Contains("Active") &&
                log.Contains("Inactive"))), Times.Once);
        }
    }
}
