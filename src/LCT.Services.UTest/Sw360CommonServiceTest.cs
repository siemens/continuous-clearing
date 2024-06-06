// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using LCT.APICommunications.Model;
using LCT.Facade.Interfaces;
using LCT.Services.Interface;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LCT.Services.UTest
{
    /// <summary>
    /// The Sw360CommonServiceTest class
    /// </summary>
    [TestFixture]
    internal class Sw360CommonServiceTest
    {
        private Mock<ISW360ApicommunicationFacade> sw360ApiCommunicationFacade;
        private ISW360CommonService sW360CommonService;

        [SetUp]
        public void SetUp()
        {
            sw360ApiCommunicationFacade = new Mock<ISW360ApicommunicationFacade>();
            sW360CommonService = new SW360CommonService(sw360ApiCommunicationFacade.Object);
        }

        [Test]
        public async Task GetComponentDataByExternalId_PassComponentInfo_ReturnsComponentStatus()
        {
            // Arrange
            var componentList = CreateComponentList();
            var componentsModel = new ComponentsModel { Embedded = new ComponentEmbedded { Sw360components = componentList } };
            var componentsModelSerialized = JsonConvert.SerializeObject(componentsModel);
            var httpResponseMessage = CreateHttpResponseMessage(componentsModelSerialized);

            sw360ApiCommunicationFacade.Setup(
                x => x.GetComponentByExternalId(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(httpResponseMessage);

            // Act
            var actual = await sW360CommonService.GetComponentDataByExternalId("zone.js", "pkg:npm/zone.js");

            // Assert
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.isComponentExist, Is.True);
            Assert.That(actual.Sw360components.Name, Is.EqualTo("Zone.js"));
        }

        [Test]
        public async Task GetComponentDataByExternalId_PassComponentInfo_ThrowsHttpRequestException()
        {
            // Arrange
            sw360ApiCommunicationFacade.Setup(
                x => x.GetComponentByExternalId(It.IsAny<string>(), It.IsAny<string>())).Throws<HttpRequestException>();

            // Act
            var actual = await sW360CommonService.GetComponentDataByExternalId("zone.js", "pkg:npm/zone.js");

            // Assert
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.isComponentExist, Is.False);
        }

        [Test]
        public async Task GetComponentDataByExternalId_PassComponentInfo_ThrowsAggregateException()
        {
            // Arrange
            sw360ApiCommunicationFacade.Setup(
                x => x.GetComponentByExternalId(It.IsAny<string>(), It.IsAny<string>())).Throws<AggregateException>();

            // Act
            var actual = await sW360CommonService.GetComponentDataByExternalId("zone.js", "pkg:npm/zone.js");

            // Assert
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.isComponentExist, Is.False);
        }

        [Test]
        public async Task GetReleaseDataByExternalId_PassReleaseInfo_ReturnsReleaseStatus()
        {
            // Arrange
            var releaseList = CreateReleaseList();
            var componentsRelease = new ComponentsRelease { Embedded = new ReleaseEmbedded { Sw360Releases = releaseList } };
            var componentsReleaseSerialized = JsonConvert.SerializeObject(componentsRelease);
            var httpResponseMessage = CreateHttpResponseMessage(componentsReleaseSerialized);

            sw360ApiCommunicationFacade.Setup(
                x => x.GetReleaseByExternalId(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(httpResponseMessage);

            // Act
            var actual = await sW360CommonService.GetReleaseDataByExternalId("zone.js", "1.0.0", "pkg:npm/zone.js@1.0.0");

            // Assert
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.isReleaseExist, Is.True);
            Assert.That(actual.sw360Releases.Name, Is.EqualTo("Zone.js"));
            Assert.That(actual.sw360Releases.Version, Is.EqualTo("1.0.0"));
        }

        [Test]
        public async Task GetReleaseDataByExternalId_PassReleaseInfo_ThrowsHttpRequestException()
        {
            // Arrange
            sw360ApiCommunicationFacade.Setup(
                x => x.GetReleaseByExternalId(It.IsAny<string>(), It.IsAny<string>())).Throws<HttpRequestException>();

            // Act
            var actual = await sW360CommonService.GetReleaseDataByExternalId("zone.js", "1.0.0", "pkg:npm/zone.js@1.0.0");

            // Assert
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.isReleaseExist, Is.False);
        }

        [Test]
        public async Task GetReleaseDataByExternalId_PassReleaseInfo_ThrowsAggregateException()
        {
            // Arrange
            sw360ApiCommunicationFacade.Setup(
                x => x.GetReleaseByExternalId(It.IsAny<string>(), It.IsAny<string>())).Throws<AggregateException>();

            // Act
            var actual = await sW360CommonService.GetReleaseDataByExternalId("zone.js", "1.0.0", "pkg:npm/zone.js@1.0.0");

            // Assert
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.isReleaseExist, Is.False);
        }

        //[Test]
        //public async Task GetReleaseIdByComponentId_PassComponentId_ReturnsReleaseId()
        //{
        //    // Arrange
        //    var releaseIdOfComponent = GetReleaseIdOfCompnentObject();
        //    var releaseIdOfComponentSerialized = JsonConvert.SerializeObject(releaseIdOfComponent);
        //    sw360ApiCommunicationFacade.Setup(
        //        x => x.GetReleaseOfComponentById(It.IsAny<string>())).ReturnsAsync(releaseIdOfComponentSerialized);

        //    // Act
        //    var actual = await sW360CommonService.GetReleaseIdByComponentId("uiweriwfoowefih87398r3ur837489", "1.0.0");

        //    // Assert
        //    Assert.That(actual, Is.EqualTo("uiweriwfoowefih87398r3ur093u0"));
        //}

        [Test]
        public async Task GetReleaseIdByComponentId_PassComponentId_ThrowsHttpRequestException()
        {
            // Arrange
            sw360ApiCommunicationFacade.Setup(
                x => x.GetReleaseOfComponentById(It.IsAny<string>())).Throws<HttpRequestException>();

            // Act
            var actual = await sW360CommonService.GetReleaseIdByComponentId("uiweriwfoowefih87398r3ur837489", "1.0.0");

            // Assert
            Assert.That(actual, Is.Empty);
        }

        [Test]
        public async Task GetReleaseIdByComponentId_PassComponentId_ThrowsAggregateException()
        {
            // Arrange
            sw360ApiCommunicationFacade.Setup(
                x => x.GetReleaseOfComponentById(It.IsAny<string>())).Throws<AggregateException>();

            // Act
            var actual = await sW360CommonService.GetReleaseIdByComponentId("uiweriwfoowefih87398r3ur837489", "1.0.0");

            // Assert
            Assert.That(actual, Is.Empty);
        }

        private static List<Sw360Components> CreateComponentList()
        {
            var self = new Self { Href = "http://localhost:8090/resource/api/components/uiweriwfoowefih87398r3ur093u0" };
            var links = new Links { Self = self };
            var sw360Components = new Sw360Components
            {
                Name = "Zone.js",
                Links = links,
                ExternalIds = new ExternalIds { Package_Url = "pkg:npm/zone.js" }
            };
            return new List<Sw360Components> { sw360Components };
        }

        private static List<Sw360Releases> CreateReleaseList()
        {
            var self = new Self { Href = "http://localhost:8090/resource/api/releases/uiweriwfoowefih87398r3ur093u0" };
            var links = new Links { Self = self };
            var sw360Releases = new Sw360Releases
            {
                Name = "Zone.js",
                Version = "1.0.0",
                Links = links,
                ExternalIds = new ExternalIds { Package_Url = "pkg:npm/zone.js@1.0.0" }
            };
            return new List<Sw360Releases> { sw360Releases };
        }

        private static HttpResponseMessage CreateHttpResponseMessage(string content)
        {
            return new HttpResponseMessage
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };
        }

        private static Dictionary<string, List<string>> GetReleaseIdOfCompnentObject()
        {
            return new Dictionary<string, List<string>> {
                { "uiweriwfoowefih87398r3ur093u0", new List<string> { "1.0.0" } }
            };
        }
    }
}