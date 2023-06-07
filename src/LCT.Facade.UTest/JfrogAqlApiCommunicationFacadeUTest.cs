// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Interfaces;
using Moq;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace LCT.Facade.UTest
{
    [TestFixture]
    internal class JfrogAqlApiCommunicationFacadeUTest
    {
        [SetUp]
        public void Setup()
        {
            // to be implemented
        }

        [Test]
        public async Task GetInternalComponentDataByRepo_GetsRepoInfo_Successfully()
        {
            // Arange 
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            Mock<IJfrogAqlApiCommunication> mockJfrogAqlApiCommunicationFacade =
                new Mock<IJfrogAqlApiCommunication>();
            mockJfrogAqlApiCommunicationFacade.
                Setup(x => x.GetInternalComponentDataByRepo(It.IsAny<string>())).
                ReturnsAsync(httpResponseMessage);

            // Act
            JfrogAqlApiCommunicationFacade jfrogAqlApiCommunicationFacade =
                new JfrogAqlApiCommunicationFacade(mockJfrogAqlApiCommunicationFacade.Object);
            HttpResponseMessage actual =
                await jfrogAqlApiCommunicationFacade.GetInternalComponentDataByRepo("energy-dev-npm-egll");

            //Assert
            Assert.That(actual.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task GetInternalComponentDataByRepo_Returnswith_HttpCodeNoContent()
        {
            // Arange 
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.NoContent);
            Mock<IJfrogAqlApiCommunication> mockJfrogAqlApiCommunicationFacade =
                new Mock<IJfrogAqlApiCommunication>();
            mockJfrogAqlApiCommunicationFacade.
                Setup(x => x.GetInternalComponentDataByRepo(It.IsAny<string>())).
                ReturnsAsync(httpResponseMessage);

            // Act
            JfrogAqlApiCommunicationFacade jfrogAqlApiCommunicationFacade =
                new JfrogAqlApiCommunicationFacade(mockJfrogAqlApiCommunicationFacade.Object);
            HttpResponseMessage actual =
                await jfrogAqlApiCommunicationFacade.GetInternalComponentDataByRepo("energy-dev-npm-egll");

            //Assert
            Assert.That(actual.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }
    }
}
