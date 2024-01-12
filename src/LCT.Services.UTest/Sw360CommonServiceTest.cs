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
    [Test]
    public async Task GetComponentDataByExternalId_PassComponentInfo_ReturnsComponentStatus()
    {
      // Arrange
      Self self = new();
      Links links = new();
      Sw360Components sw360Components = new();
      var componentList = new List<Sw360Components>();
      ComponentEmbedded componentEmbedded = new();

      self.Href = "http://localhost:8090/resource/api/components/uiweriwfoowefih87398r3ur093u0";
      links.Self = self;
      sw360Components.Name = "Zone.js";
      sw360Components.Links = links;
      sw360Components.ExternalIds = new ExternalIds() { Package_Url = "pkg:npm/zone.js" };
      componentList.Add(sw360Components);
      componentEmbedded.Sw360components = componentList;

      ComponentsModel componentsModel = new ComponentsModel();
      componentsModel.Embedded = componentEmbedded;

      var componentsModelSerialized = JsonConvert.SerializeObject(componentsModel);
      HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
      var content = new StringContent(componentsModelSerialized, Encoding.UTF8, "application/json");
      httpResponseMessage.Content = content;

      Mock<ISW360ApicommunicationFacade> sw360ApiCommunicationFacade = new();
      sw360ApiCommunicationFacade.Setup(
          x => x.GetComponentByExternalId(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(httpResponseMessage);

      // Act
      ISW360CommonService sW360CommonService = new SW360CommonService(sw360ApiCommunicationFacade.Object);
      var actual = await sW360CommonService.GetComponentDataByExternalId("zone.js", "pkg:npm/zone.js");

      // Assert
      Assert.That(actual, Is.Not.Null);
    }

    [Test]
    public async Task GetComponentDataByExternalId_PassComponentInfo_ThrowsHttpRequestException()
    {
      // Arrange
      Mock<ISW360ApicommunicationFacade> sw360ApiCommunicationFacade = new();
      sw360ApiCommunicationFacade.Setup(
          x => x.GetComponentByExternalId(It.IsAny<string>(), It.IsAny<string>())).Throws<HttpRequestException>();

      // Act
      ISW360CommonService sW360CommonService = new SW360CommonService(sw360ApiCommunicationFacade.Object);
      var actual = await sW360CommonService.GetComponentDataByExternalId("zone.js", "pkg:npm/zone.js");

      // Assert
      Assert.That(actual, Is.Not.Null);
    }

    [Test]
    public async Task GetComponentDataByExternalId_PassComponentInfo_ThrowsAggregateException()
    {
      // Arrange
      Mock<ISW360ApicommunicationFacade> sw360ApiCommunicationFacade = new();
      sw360ApiCommunicationFacade.Setup(
          x => x.GetComponentByExternalId(It.IsAny<string>(), It.IsAny<string>())).Throws<AggregateException>();

      // Act
      ISW360CommonService sW360CommonService = new SW360CommonService(sw360ApiCommunicationFacade.Object);
      var actual = await sW360CommonService.GetComponentDataByExternalId("zone.js", "pkg:npm/zone.js");

      // Assert
      Assert.That(actual, Is.Not.Null);
    }

    [Test]
    public async Task GetReleaseDataByExternalId_PassReleaseInfo_ReturnsReleaseStatus()
    {
      // Arrange
      Self self = new();
      Links links = new();
      Sw360Releases sw360Releases = new();
      var releaseList = new List<Sw360Releases>();
      ReleaseEmbedded releaseEmbedded = new();

      self.Href = "http://localhost:8090/resource/api/releases/uiweriwfoowefih87398r3ur093u0";
      links.Self = self;
      sw360Releases.Name = "Zone.js";
      sw360Releases.Version = "1.0.0";
      sw360Releases.Links = links;
      sw360Releases.ExternalIds = new ExternalIds() { Package_Url = "pkg:npm/zone.js@1.0.0" };
      releaseList.Add(sw360Releases);
      releaseEmbedded.Sw360Releases = releaseList;

      ComponentsRelease componentsRelease = new ComponentsRelease();
      componentsRelease.Embedded = releaseEmbedded;

      var componentsReleaseSerialized = JsonConvert.SerializeObject(componentsRelease);
      HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
      var content = new StringContent(componentsReleaseSerialized, Encoding.UTF8, "application/json");
      httpResponseMessage.Content = content;

      Mock<ISW360ApicommunicationFacade> sw360ApiCommunicationFacade = new();
      sw360ApiCommunicationFacade.Setup(
          x => x.GetReleaseByExternalId(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(httpResponseMessage);

      // Act
      ISW360CommonService sW360CommonService = new SW360CommonService(sw360ApiCommunicationFacade.Object);
      var actual = await sW360CommonService.GetReleaseDataByExternalId("zone.js", "1.0.0", "pkg:npm/zone.js@1.0.0");

      // Assert
      Assert.That(actual, Is.Not.Null);
    }

    [Test]
    public async Task GetReleaseDataByExternalId_PassReleaseInfo_ThrowsHttpRequestException()
    {
      // Arrange

      Mock<ISW360ApicommunicationFacade> sw360ApiCommunicationFacade = new();
      sw360ApiCommunicationFacade.Setup(
          x => x.GetReleaseByExternalId(It.IsAny<string>(), It.IsAny<string>())).Throws<HttpRequestException>();

      // Act
      ISW360CommonService sW360CommonService = new SW360CommonService(sw360ApiCommunicationFacade.Object);
      var actual = await sW360CommonService.GetReleaseDataByExternalId("zone.js", "1.0.0", "pkg:npm/zone.js@1.0.0");

      // Assert
      Assert.That(actual, Is.Not.Null);
    }

    [Test]
    public async Task GetReleaseDataByExternalId_PassReleaseInfo_ThrowsHttpAggregateException()
    {
      // Arrange

      Mock<ISW360ApicommunicationFacade> sw360ApiCommunicationFacade = new();
      sw360ApiCommunicationFacade.Setup(
          x => x.GetReleaseByExternalId(It.IsAny<string>(), It.IsAny<string>())).Throws<AggregateException>();

      // Act
      ISW360CommonService sW360CommonService = new SW360CommonService(sw360ApiCommunicationFacade.Object);
      var actual = await sW360CommonService.GetReleaseDataByExternalId("zone.js", "1.0.0", "pkg:npm/zone.js@1.0.0");

      // Assert
      Assert.That(actual, Is.Not.Null);
    }

    [Test]
    public async Task GetReleaseIdByComponentId_PassComponentId_ReturnsReleaseId()
    {
      ReleaseIdOfComponent releaseIdOfComponent = GetReleaseIdOfCompnentObject();
      var releaseIdOfComponentSerialized = JsonConvert.SerializeObject(releaseIdOfComponent);
      Mock<ISW360ApicommunicationFacade> sw360ApiCommunicationFacade = new();
      sw360ApiCommunicationFacade.Setup(
          x => x.GetReleaseOfComponentById(It.IsAny<string>())).ReturnsAsync(releaseIdOfComponentSerialized);

      // Act
      ISW360CommonService sW360CommonService = new SW360CommonService(sw360ApiCommunicationFacade.Object);
      var actual = await sW360CommonService.GetReleaseIdByComponentId("uiweriwfoowefih87398r3ur837489", "1.0.0");

      // Assert
      Assert.That("uiweriwfoowefih87398r3ur093u0", Is.EqualTo(actual));
    }

    [Test]
    public async Task GetReleaseIdByComponentId_PassComponentId_ThrowsHttpRequestException()
    {
      // Arrange
      Mock<ISW360ApicommunicationFacade> sw360ApiCommunicationFacade = new();
      sw360ApiCommunicationFacade.Setup(
          x => x.GetReleaseOfComponentById(It.IsAny<string>())).Throws<HttpRequestException>();

      // Act
      ISW360CommonService sW360CommonService = new SW360CommonService(sw360ApiCommunicationFacade.Object);
      var actual = await sW360CommonService.GetReleaseIdByComponentId("uiweriwfoowefih87398r3ur837489", "1.0.0");

      // Assert
      Assert.That("uiweriwfoowefih87398r3ur093u0", Is.Not.EqualTo(actual));
    }

    [Test]
    public async Task GetReleaseIdByComponentId_PassComponentId_ThrowsAggregateException()
    {
      // Arrange
      Mock<ISW360ApicommunicationFacade> sw360ApiCommunicationFacade = new();
      sw360ApiCommunicationFacade.Setup(
          x => x.GetReleaseOfComponentById(It.IsAny<string>())).Throws<AggregateException>();

      // Act
      ISW360CommonService sW360CommonService = new SW360CommonService(sw360ApiCommunicationFacade.Object);
      var actual = await sW360CommonService.GetReleaseIdByComponentId("uiweriwfoowefih87398r3ur837489", "1.0.0");

      // Assert
      Assert.That("uiweriwfoowefih87398r3ur093u0", Is.Not.EqualTo(actual));
    }

    private static ReleaseIdOfComponent GetReleaseIdOfCompnentObject()
    {
      // Arrange
      ReleaseIdOfComponent releaseIdOfComponent = new();

      Self self = new();
      Links links = new();
      Sw360Releases sw360Releases = new();
      var releaseList = new List<Sw360Releases>();
      ReleaseEmbedded releaseEmbedded = new();

      self.Href = "http://localhost:8090/resource/api/releases/uiweriwfoowefih87398r3ur093u0";
      links.Self = self;
      sw360Releases.Name = "Zone.js";
      sw360Releases.Version = "1.0.0";
      sw360Releases.Links = links;
      releaseList.Add(sw360Releases);
      releaseEmbedded.Sw360Releases = releaseList;
      releaseIdOfComponent.Embedded = releaseEmbedded;
      return releaseIdOfComponent;
    }
  }
}
