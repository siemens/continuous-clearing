// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model.AQL;
using LCT.Facade.Interfaces;
using LCT.Services.Interface;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LCT.Services.UTest
{
    [TestFixture]
    internal class JFrogServiceUTest
    {
        [Test]
        public async Task GetInternalComponentDataByRepo_GetsRepoInfo_Successfully()
        {
            // Arrange

            AqlResult aqlResult = new AqlResult()
            {
                Name = "saap-api-node-2.26.3-LicenseClearing.16.sha-058fada.tgz",
                Path = "@siemens-gds/saap-api-node/-/@siemens-gds",
                Repo = "energy-dev-npm-egll"
            };

            IList<AqlResult> results = new List<AqlResult>();
            results.Add(aqlResult);

            AqlResponse aqlResponse = new AqlResponse();
            aqlResponse.Results = results;

            var aqlResponseSerialized = JsonConvert.SerializeObject(aqlResponse);
            var content = new StringContent(aqlResponseSerialized, Encoding.UTF8, "application/json");
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            httpResponseMessage.Content = content;


            Mock<IJfrogAqlApiCommunicationFacade> mockJfrogApiComFacade =
                new Mock<IJfrogAqlApiCommunicationFacade>();
            mockJfrogApiComFacade
                .Setup(x => x.GetInternalComponentDataByRepo(It.IsAny<string>()))
                .ReturnsAsync(httpResponseMessage);

            // Act
            IJFrogService jFrogService = new JFrogService(mockJfrogApiComFacade.Object);
            IList<AqlResult> actual = await jFrogService.GetInternalComponentDataByRepo("energy-dev-npm-egll");

            // Assert
            Assert.That(actual.Count, Is.GreaterThan(0));
        }

        [Test]
        public async Task GetInternalComponentDataByRepo_ResultsWith_NoContent()
        {
            // Arrange

            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.NoContent);

            Mock<IJfrogAqlApiCommunicationFacade> mockJfrogApiComFacade =
                new Mock<IJfrogAqlApiCommunicationFacade>();
            mockJfrogApiComFacade
                .Setup(x => x.GetInternalComponentDataByRepo(It.IsAny<string>()))
                .ReturnsAsync(httpResponseMessage);

            // Act
            IJFrogService jFrogService = new JFrogService(mockJfrogApiComFacade.Object);
            IList<AqlResult> actual = await jFrogService.GetInternalComponentDataByRepo("energy-dev-npm-egll");

            // Assert
            Assert.That(actual.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetInternalComponentDataByRepo_ResultsWith_EmptyHttpResponse()
        {
            // Arrange
            HttpResponseMessage httpResponseMessage = null;
            Mock<IJfrogAqlApiCommunicationFacade> mockJfrogApiComFacade =
                new Mock<IJfrogAqlApiCommunicationFacade>();
            mockJfrogApiComFacade
                .Setup(x => x.GetInternalComponentDataByRepo(It.IsAny<string>()))
                .ReturnsAsync(httpResponseMessage);

            // Act
            IJFrogService jFrogService = new JFrogService(mockJfrogApiComFacade.Object);
            IList<AqlResult> actual = await jFrogService.GetInternalComponentDataByRepo("energy-dev-npm-egll");

            // Assert
            Assert.That(actual.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetInternalComponentDataByRepo_ResultsWith_HttpRequestException()
        {
            // Arrange
            Mock<IJfrogAqlApiCommunicationFacade> mockJfrogApiComFacade =
                new Mock<IJfrogAqlApiCommunicationFacade>();
            mockJfrogApiComFacade
                .Setup(x => x.GetInternalComponentDataByRepo(It.IsAny<string>())).
                Throws<HttpRequestException>();

            // Act
            IJFrogService jFrogService = new JFrogService(mockJfrogApiComFacade.Object);
            IList<AqlResult> actual = await jFrogService.GetInternalComponentDataByRepo("energy-dev-npm-egll");

            // Assert
            Assert.That(actual.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetInternalComponentDataByRepo_ResultsWith_InvalidOperationException()
        {
            // Arrange
            Mock<IJfrogAqlApiCommunicationFacade> mockJfrogApiComFacade =
                new Mock<IJfrogAqlApiCommunicationFacade>();
            mockJfrogApiComFacade
                .Setup(x => x.GetInternalComponentDataByRepo(It.IsAny<string>())).
                Throws<InvalidOperationException>();

            // Act
            IJFrogService jFrogService = new JFrogService(mockJfrogApiComFacade.Object);
            IList<AqlResult> actual = await jFrogService.GetInternalComponentDataByRepo("energy-dev-npm-egll");

            // Assert
            Assert.That(actual.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetInternalComponentDataByRepo_ResultsWith_TaskCanceledException()
        {
            // Arrange
            Mock<IJfrogAqlApiCommunicationFacade> mockJfrogApiComFacade =
                new Mock<IJfrogAqlApiCommunicationFacade>();
            mockJfrogApiComFacade
                .Setup(x => x.GetInternalComponentDataByRepo(It.IsAny<string>())).
                Throws<TaskCanceledException>();

            // Act
            IJFrogService jFrogService = new JFrogService(mockJfrogApiComFacade.Object);
            IList<AqlResult> actual = await jFrogService.GetInternalComponentDataByRepo("energy-dev-npm-egll");

            // Assert
            Assert.That(actual.Count, Is.EqualTo(0));
        }
    }
}
