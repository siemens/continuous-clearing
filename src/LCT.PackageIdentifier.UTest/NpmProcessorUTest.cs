﻿// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications.Model.AQL;
using LCT.Common;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using LCT.Services.Interface;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    internal class NpmProcessorUTest
    {
        [Test]
        public async Task IdentificationOfInternalComponents_ReturnsComponentData_Successfully()
        {
            // Arrange
            Component component1 = new Component();
            component1.Name = "animations";
            component1.Group = "";
            component1.Description = string.Empty;
            component1.Version = "1.0.0";
            var components = new List<Component>() { component1 };
            ComponentIdentification component = new() { comparisonBOMData = components };
            string[] reooListArr = { "energy-dev-npm-egll", "energy-release-npm-egll" };
            CommonAppSettings appSettings = new() { InternalRepoList = reooListArr };

            AqlResult aqlResult = new()
            {
                Name = "animations-1.0.0.tgz",
                Path = "@siemens-gds/saap-api-node/-/@siemens-gds",
                Repo = "energy-dev-npm-egll"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };
            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations");

            // Act
            NpmProcessor npmProcessor = new NpmProcessor();
            var actual = await npmProcessor.IdentificationOfInternalComponents(component, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task IdentificationOfInternalComponents_ReturnsComponentData2_Successfully()
        {
            // Arrange
            Component component1 = new Component();
            component1.Name = "animations";
            component1.Group = "";
            component1.Description = string.Empty;
            component1.Version = "1.0.0";
            var components = new List<Component>() { component1 };
            ComponentIdentification component = new() { comparisonBOMData = components };
            string[] reooListArr = { "energy-dev-npm-egll", "energy-release-npm-egll" };
            CommonAppSettings appSettings = new() { InternalRepoList = reooListArr };

            AqlResult aqlResult = new()
            {
                Name = "animations-common_license-1.0.0.tgz",
                Path = "@siemens-gds/saap-api-node/-/@siemens-gds",
                Repo = "energy-dev-npm-egll"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };
            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations");

            // Act
            NpmProcessor npmProcessor = new NpmProcessor();
            var actual = await npmProcessor.IdentificationOfInternalComponents(component, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task IdentificationOfInternalComponents_ReturnsComponentData3_Successfully()
        {
            // Arrange
            Component component1 = new Component
            {
                Name = "animations",
                Group = "common",
                Description = string.Empty,
                Version = "1.0.0"
            };
            var components = new List<Component>() { component1 };
            ComponentIdentification componentIdentification = new() { comparisonBOMData = components };
            string[] reooListArr = { "energy-dev-npm-egll", "energy-release-npm-egll" };
            CommonAppSettings appSettings = new() { InternalRepoList = reooListArr };

            AqlResult aqlResult = new()
            {
                Name = "animations-common-1.0.0.tgz",
                Path = "@siemens-gds/saap-api-node/-/@siemens-gds",
                Repo = "energy-dev-npm-egll"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };
            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations/common");

            // Act
            NpmProcessor npmProcessor = new NpmProcessor();
            var actual = await npmProcessor.IdentificationOfInternalComponents(
                componentIdentification, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task GetJfrogRepoDetailsOfAComponent_ReturnsWithData_SuccessFully()
        {
            // Arrange
            Component component1 = new Component
            {
                Name = "animations",
                Group = "common",
                Description = string.Empty,
                Version = "1.0.0"
            };
            var components = new List<Component>() { component1 };
            string[] reooListArr = { "siparty-release-npm-egll", "org1-npmjs-npm-remote-cache" };
            CommonAppSettings appSettings = new();
            appSettings.Npm = new Common.Model.Config() { JfrogNpmRepoList = reooListArr };
            AqlResult aqlResult = new()
            {
                Name = "animations-common-1.0.0.tgz",
                Path = "@siemens-gds/saap-api-node/-/@siemens-gds",
                Repo = "siparty-release-npm-egll"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations/common");
           
            // Act
            NpmProcessor npmProcessor = new NpmProcessor();
            var actual = await npmProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }

        [Test]
        public async Task GetJfrogRepoDetailsOfAComponent_ReturnsWithData2_SuccessFully()
        {
            // Arrange
            Component component1 = new Component
            {
                Name = "animations",
                Group = "",
                Description = string.Empty,
                Version = "1.0.0"
            };
            var components = new List<Component>() { component1 };
            string[] reooListArr = { "siparty-release-npm-egll", "org1-npmjs-npm-remote-cache" };
            CommonAppSettings appSettings = new();
            appSettings.Npm = new Common.Model.Config() { JfrogNpmRepoList = reooListArr };
            AqlResult aqlResult = new()
            {
                Name = "animations-common-1.0.0.tgz",
                Path = "@siemens-gds/saap-api-node/-/@siemens-gds",
                Repo = "siparty-release-npm-egll"
            };

            List<AqlResult> results = new List<AqlResult>() { aqlResult };

            Mock<IJFrogService> mockJfrogService = new Mock<IJFrogService>();
            Mock<IBomHelper> mockBomHelper = new Mock<IBomHelper>();
            mockBomHelper.Setup(m => m.GetListOfComponentsFromRepo(It.IsAny<string[]>(), It.IsAny<IJFrogService>()))
                .ReturnsAsync(results);
            mockBomHelper.Setup(m => m.GetFullNameOfComponent(It.IsAny<Component>())).Returns("animations");

            // Act
            NpmProcessor npmProcessor = new NpmProcessor();
            var actual = await npmProcessor.GetJfrogRepoDetailsOfAComponent(
                components, appSettings, mockJfrogService.Object, mockBomHelper.Object);

            // Assert
            Assert.That(actual, Is.Not.Null);
        }
    }
}