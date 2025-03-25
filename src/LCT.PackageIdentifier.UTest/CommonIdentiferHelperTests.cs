// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.APICommunications.Model.AQL;
using LCT.Common;
using LCT.Common.Constants;
using NUnit.Framework;
using System.Collections.Generic;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    public class CommonIdentiferHelperTests
    {
        private const string NotFoundInRepo = "Not Found in JFrogRepo";

        [Test]
        public void GetRepodetailsFromPerticularOrder_InputIsNull_ReturnsNotFound()
        {
            var result = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(null);
            Assert.AreEqual(NotFoundInRepo, result);
        }

        [Test]
        public void GetRepodetailsFromPerticularOrder_ContainsRelease_ReturnsReleaseRepo()
        {
            var aqlResults = new List<AqlResult>
            {
                new AqlResult { Repo = "release-repo" }
            };
            var result = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults);
            Assert.AreEqual("release-repo", result);
        }

        [Test]
        public void GetRepodetailsFromPerticularOrder_ContainsDevdep_ReturnsDevdepRepo()
        {
            var aqlResults = new List<AqlResult>
            {
                new AqlResult { Repo = "devdep-repo" }
            };
            var result = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults);
            Assert.AreEqual("devdep-repo", result);
        }

        [Test]
        public void GetRepodetailsFromPerticularOrder_ContainsDev_ReturnsDevRepo()
        {
            var aqlResults = new List<AqlResult>
            {
                new AqlResult { Repo = "dev-repo" }
            };
            var result = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults);
            Assert.AreEqual("dev-repo", result);
        }

        [Test]
        public void GetRepodetailsFromPerticularOrder_NoSpecificRepo_ReturnsFirstRepo()
        {
            var aqlResults = new List<AqlResult>
            {
                new AqlResult { Repo = "generic-repo" }
            };
            var result = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults);
            Assert.AreEqual("generic-repo", result);
        }
        [Test]
        public void GetBomFileName_WhenBasicSBOMIsFalse_ReturnsProjectNameBomFileName()
        {
            // Arrange
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360() { ProjectName = "TestProject" }
            };

            // Act
            string result = CommonIdentiferHelper.GetBomFileName(appSettings);

            // Assert
            Assert.AreEqual("TestProject_Bom.cdx.json", result);
        }

        [Test]
        public void GetBomFileName_WhenBasicSBOMIsTrue_ReturnsBasicSBOMNameBomFileName()
        {
            // Arrange
            var appSettings = new CommonAppSettings
            {

            };

            // Act
            string result = CommonIdentiferHelper.GetBomFileName(appSettings);

            // Assert
            Assert.AreEqual(FileConstant.basicSBOMName, result);
        }

        [Test]
        public void GetDefaultProjectName_WhenBasicSBOMIsFalse_ReturnsProjectName()
        {
            // Arrange
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360() { ProjectName = "TestProject" }
            };

            // Act
            string result = CommonIdentiferHelper.GetDefaultProjectName(appSettings);

            // Assert
            Assert.AreEqual("TestProject", result);
        }

        [Test]
        public void GetDefaultProjectName_WhenBasicSBOMIsTrue_ReturnsBasicSBOMName()
        {
            // Arrange
            var appSettings = new CommonAppSettings
            {
            };

            // Act
            string result = CommonIdentiferHelper.GetDefaultProjectName(appSettings);

            // Assert
            Assert.AreEqual(FileConstant.basicSBOMName, result);
        }
    }
}
