// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.APICommunications.Model.AQL;
using LCT.Common;
using LCT.PackageIdentifier.Interface;
using LCT.Services.Interface;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    public class ConanProcessorTests
    {
        private ConanProcessor _conanProcessor;
        private Mock<IJFrogService> _mockJFrogService;
        private Mock<IBomHelper> _mockBomHelper;
        private Mock<ICycloneDXBomParser> _mockCycloneDxBomParser;

        [SetUp]
        public void Setup()
        {
            _mockJFrogService = new Mock<IJFrogService>();
            _mockBomHelper = new Mock<IBomHelper>();
            _mockCycloneDxBomParser = new Mock<ICycloneDXBomParser>();
            _conanProcessor = new ConanProcessor(_mockCycloneDxBomParser.Object);
        }

        [Test]
        public void GetArtifactoryRepoName_Returns_RepoName()
        {
            // Arrange
            var aqlResultList = new List<AqlResult>
            {
                new AqlResult
                {
                    Path = "org/package/1.0.0",
                    Repo = "repo1",
                    Name = "package.tgz"
                },
                new AqlResult
                {
                    Path = "org/package/2.0.0",
                    Repo = "repo2",
                    Name = "package.tgz"
                }
            };
            var component = new Component
            {
                Name = "package",
                Version = "1.0.0"
            };            

            // Act
            var repoName = ConanProcessor.GetArtifactoryRepoName(aqlResultList, component, out string jfrogRepoPath);

            // Assert
            Assert.AreEqual("repo1", repoName);
            Assert.AreEqual("repo1/org/package/1.0.0/package.tgz;", jfrogRepoPath);
        }
    }
}
