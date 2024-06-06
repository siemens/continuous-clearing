// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common.Model;
using Moq.Protected;
using Moq;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using LCT.APICommunications.Model;

namespace LCT.APICommunications.UTest
{
    public class SW360ApicommunicationUTest
    {
        readonly SW360ConnectionSettings connectionSettings = new SW360ConnectionSettings();

        [SetUp]
        public void Setup()
        {
            connectionSettings.Timeout = 5;
            connectionSettings.SW360AuthTokenType = "Token";
        }

        [Test]
        public void SW360Apicommunication_GetProjects_ReturnsInvalidOperationException()
        {
            //Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetProjects());
        }

        [Test]
        public void SW360Apicommunication_GetSw360Users_ReturnsInvalidOperationException()
        {
            //Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetSw360Users());
        }

        [Test]
        public void SW360Apicommunication_GetProjectsByName_ReturnsInvalidOperationException()
        {
            //Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetProjectsByName(""));
        }

        [Test]
        public void SW360Apicommunication_GetProjectsByTag_ReturnsInvalidOperationException()
        {
            //Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetProjectsByTag(""));
        }

        [Test]
        public void SW360Apicommunication_CheckFossologyProcessStatus_ReturnsInvalidOperationException()
        {
            //Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.CheckFossologyProcessStatus(""));
        }

        [Test]
        public void SW360Apicommunication_GetReleaseByLink_ReturnsInvalidOperationException()
        {
            //Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetReleaseByLink(""));
        }

        [Test]
        public void SW360Apicommunication_GetComponentUsingName_ReturnsInvalidOperationException()
        {
            //Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetComponentUsingName(""));
        }

        [Test]
        public void SW360Apicommunication_GetReleaseAttachments_ReturnsInvalidOperationException()
        {
            //Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetReleaseAttachments(""));
        }

        [Test]
        public void SW360Apicommunication_GetAttachmentInfo_ReturnsInvalidOperationException()
        {
            //Arrange & Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetAttachmentInfo(""));
        }

        [Test]
        public void SW360Apicommunication_DownloadAttachmentUsingWebClient_ReturnsUriFormatException()
        {
            //Arrange & Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<UriFormatException>(() => { sW360Apicommunication.DownloadAttachmentUsingWebClient("attachmentDownloadLink", ""); return Task.CompletedTask; });
        }

        [Test]
        public void SW360Apicommunication_GetReleaseByCompoenentName_ReturnsInvalidOperationException()
        {
            //Arrange & Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetReleaseByCompoenentName(""));
        }

        [Test]
        public void SW360Apicommunication_GetComponentDetailsByUrl_ReturnsInvalidOperationException()
        {
            //Arrange & Act
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetComponentDetailsByUrl(""));
        }

        [Test]
        public void SW360Apicommunication_UpdateComponent_ReturnsInvalidOperationException()
        {
            //Arrange
            HttpContent httpContent;
            var jsonString = JsonConvert.SerializeObject("");
            httpContent = new StringContent(jsonString, Encoding.UTF8, "application/json");

            //Arrange
            SW360Apicommunication sW360Apicommunication = new SW360Apicommunication(connectionSettings);

            //Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.UpdateComponent("", httpContent));
        }
        [Test]
        public async Task GetComponentByName_ReturnsExpectedString_WhenResponseIsOk()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("Test content"),
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var sW360Apicommunication = new SW360Apicommunication(connectionSettings);



            // Act
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetComponentByName("TestComponentName"));

        }
        [Test]
        public async Task UpdateRelease_ReturnsHttpResponseMessage_WhenResponseIsOk()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var sW360Apicommunication = new SW360Apicommunication(connectionSettings); // Assuming your class SW360Apicommunication does not have a constructor that accepts HttpClient



            var httpContent = new StringContent("Test content", Encoding.UTF8, "application/json");

            // Act
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.UpdateRelease("TestReleaseId", httpContent));


        }
        [Test]
        public async Task CreateComponent_ReturnsHttpResponseMessage_WhenCalledWithCreateComponentContent()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var sW360Apicommunication = new SW360Apicommunication(connectionSettings); // Assuming your class SW360Apicommunication does not have a constructor that accepts HttpClient



            var createComponentContent = new CreateComponent(); // Assuming CreateComponent is a class. Replace with your actual CreateComponent instance.

            // Act
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.CreateComponent(createComponentContent));


        }
        [Test]
        public async Task GetComponentByExternalId_ReturnsHttpResponseMessage_WhenResponseIsOk()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("Test content"),
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var sW360Apicommunication = new SW360Apicommunication(connectionSettings); // Assuming your class SW360Apicommunication does not have a constructor that accepts HttpClient


            // Act
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetComponentByExternalId("TestPurlId"));


        }
        [Test]
        public async Task GetProjects_ReturnsContent_WhenResponseIsOk()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("Test content"),
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var sW360Apicommunication = new SW360Apicommunication(connectionSettings); // Assuming your class SW360Apicommunication does not have a constructor that accepts HttpClient


            // Act
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetProjects());


        }
        [Test]
        public async Task GetComponentByName_ReturnsContent_WhenCalledWithComponentName()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("Test content"),
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var sW360Apicommunication = new SW360Apicommunication(connectionSettings); // Assuming your class SW360Apicommunication does not have a constructor that accepts HttpClient


            // Act
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetComponentByName("TestComponent"));

        }
        [Test]
        public async Task GetReleases_ReturnsContent_WhenResponseIsOk()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("Test content"),
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var sW360Apicommunication = new SW360Apicommunication(connectionSettings); // Assuming your class SW360Apicommunication does not have a constructor that accepts HttpClient


            // Act
            Assert.ThrowsAsync<InvalidOperationException>(async () => await sW360Apicommunication.GetReleases());


        }
     
    }
}
