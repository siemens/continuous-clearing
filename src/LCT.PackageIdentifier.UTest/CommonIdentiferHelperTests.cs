// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
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

        [Test]
        public void GetCdxGenBomData_ReturnsNull_WhenNoDependencyFile()
        {
            // Arrange
            var configFiles = new List<string> { "somefile.txt", "another.json" };
            var appSettings = new CommonAppSettings { ProjectType = "NPM" };

            // Act
            var bom = CommonIdentiferHelper.GetCdxGenBomData(configFiles, appSettings, _ => new Bom { Components = new List<Component> { new Component() } });

            // Assert
            Assert.IsNull(bom);
        }

        [Test]
        public void GetCdxGenBomData_ReturnsNull_WhenOnlyDependencyFiles()
        {
            // Arrange: only dependency files present
            var dep1 = $"file1{FileConstant.DependencyFileExtension}";
            var dep2 = $"file2{FileConstant.DependencyFileExtension}";
            var configFiles = new List<string> { dep1, dep2 };
            var appSettings = new CommonAppSettings { ProjectType = "NPM" };

            // Act
            var bom = CommonIdentiferHelper.GetCdxGenBomData(configFiles, appSettings, _ => new Bom { Components = new List<Component> { new Component() } });

            // Assert
            Assert.IsNull(bom);
        }

        [Test]
        public void GetCdxGenBomData_FiltersOutApplicationComponents_AndReturnsBom()
        {
            // Arrange: include one dependency file and another non-dependency file to trigger parsing
            var dep = $"deps{FileConstant.DependencyFileExtension}";
            var other = "other.txt";
            var configFiles = new List<string> { dep, other };
            var appSettings = new CommonAppSettings { ProjectType = "NPM" };

            // Create BOM with both Application and Library components
            var appComponent = new Component { Name = "app", Version = "1.0", Type = Component.Classification.Application,Purl= "pkg:npm/app@1.0.0" };
            var libComponent = new Component { Name = "lib", Version = "1.0", Type = Component.Classification.Library, Purl = "pkg:npm/lib@1.0.0" };

            Bom Parse(string path) => new Bom { Components = new List<Component> { appComponent, libComponent } };

            // Act
            var bom = CommonIdentiferHelper.GetCdxGenBomData(configFiles, appSettings, Parse);

            // Assert
            Assert.IsNotNull(bom, "Expected BOM to be returned");
            Assert.IsNotNull(bom.Components);
            Assert.That(bom.Components.Count, Is.EqualTo(1));
            Assert.That(bom.Components[0].Name, Is.EqualTo("lib"));
            Assert.That(bom.Components[0].Type, Is.EqualTo(Component.Classification.Library));
        }
    }
}
