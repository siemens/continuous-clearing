// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2026 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using NUnit.Framework;
using SIT.APICommunications.Model.AQL;
using SIT.Common;
using SIT.Common.Constants;
using SIT.Common.Model;
using System.Collections.Generic;
using System.Linq;

namespace SIT.Scan.UTest
{
    [TestFixture]
    public class CommonIdentiferHelperTests
    {
        private const string NotFoundInRepo = "Not Found in JFrogRepo";

        private static Component CreateDevComponent() => new Component
        {
            Properties = new List<Property>
            {
                new Property { Name = Dataconstant.Cdx_IsDevelopment, Value = "true" }
            }
        };

        [Test]
        public void GetRepodetailsFromPerticularOrder_InputIsNull_ReturnsNotFound()
        {
            var result = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(null, null);
            Assert.AreEqual(NotFoundInRepo, result);
        }

        [Test]
        public void GetRepodetailsFromPerticularOrder_ContainsRelease_ReturnsReleaseRepo()
        {
            var aqlResults = new List<AqlResult>
            {
                new AqlResult { Repo = "release-repo" }
            };
            var result = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults, null);
            Assert.AreEqual("release-repo", result);
        }

        [Test]
        public void GetRepodetailsFromPerticularOrder_ContainsDevdep_ReturnsDevdepRepo()
        {
            var aqlResults = new List<AqlResult>
            {
                new AqlResult { Repo = "devdep-repo" }
            };
            var result = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults, null);
            Assert.AreEqual("devdep-repo", result);
        }

        [Test]
        public void GetRepodetailsFromPerticularOrder_ContainsDev_ReturnsDevRepo()
        {
            var aqlResults = new List<AqlResult>
            {
                new AqlResult { Repo = "dev-repo" }
            };
            var result = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults, null);
            Assert.AreEqual("dev-repo", result);
        }

        [Test]
        public void GetRepodetailsFromPerticularOrder_NoSpecificRepo_ReturnsFirstRepo()
        {
            var aqlResults = new List<AqlResult>
            {
                new AqlResult { Repo = "generic-repo" }
            };
            var result = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults, null);
            Assert.AreEqual("generic-repo", result);
        }

        [Test]
        public void GetRepodetailsFromPerticularOrder_NonDevComponent_PrefersReleaseOverDevdep()
        {
            var aqlResults = new List<AqlResult>
            {
                new AqlResult { Repo = "devdep-repo" },
                new AqlResult { Repo = "release-repo" }
            };
            var result = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults, null);
            Assert.AreEqual("release-repo", result);
        }

        [Test]
        public void GetRepodetailsFromPerticularOrder_DevComponent_InputIsNull_ReturnsNotFound()
        {
            var result = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(null, CreateDevComponent());
            Assert.AreEqual(NotFoundInRepo, result);
        }

        [Test]
        public void GetRepodetailsFromPerticularOrder_DevComponent_PrefersDevdepOverRelease()
        {
            var aqlResults = new List<AqlResult>
            {
                new AqlResult { Repo = "release-repo" },
                new AqlResult { Repo = "devdep-repo" }
            };
            var result = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults, CreateDevComponent());
            Assert.AreEqual("devdep-repo", result);
        }

        [Test]
        public void GetRepodetailsFromPerticularOrder_DevComponent_NoDevdep_ReturnsReleaseRepo()
        {
            var aqlResults = new List<AqlResult>
            {
                new AqlResult { Repo = "release-repo" }
            };
            var result = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults, CreateDevComponent());
            Assert.AreEqual("release-repo", result);
        }

        [Test]
        public void GetRepodetailsFromPerticularOrder_DevComponent_OnlyDevRepo_ReturnsDevRepo()
        {
            var aqlResults = new List<AqlResult>
            {
                new AqlResult { Repo = "dev-repo" }
            };
            var result = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults, CreateDevComponent());
            Assert.AreEqual("dev-repo", result);
        }

        [Test]
        public void GetRepodetailsFromPerticularOrder_DevComponent_NoSpecificRepo_ReturnsFirstRepo()
        {
            var aqlResults = new List<AqlResult>
            {
                new AqlResult { Repo = "generic-repo" }
            };
            var result = CommonIdentiferHelper.GetRepodetailsFromPerticularOrder(aqlResults, CreateDevComponent());
            Assert.AreEqual("generic-repo", result);
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
            var appComponent = new Component { Name = "app", Version = "1.0", Type = Component.Classification.Application, Purl = "pkg:npm/app@1.0.0" };
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

        #region IsComponentInternal

        [Test]
        public void IsComponentInternal_NullComponent_ReturnsFalse()
        {
            Assert.IsFalse(CommonIdentiferHelper.IsComponentInternal(null));
        }

        [Test]
        public void IsComponentInternal_NullProperties_ReturnsFalse()
        {
            var component = new Component { Name = "a", Version = "1.0" };
            Assert.IsFalse(CommonIdentiferHelper.IsComponentInternal(component));
        }

        [Test]
        public void IsComponentInternal_PropertyMissing_ReturnsFalse()
        {
            var component = new Component
            {
                Properties = new List<Property>
                {
                    new Property { Name = Dataconstant.Cdx_IsDevelopment, Value = "true" }
                }
            };
            Assert.IsFalse(CommonIdentiferHelper.IsComponentInternal(component));
        }

        [Test]
        public void IsComponentInternal_PropertyFalse_ReturnsFalse()
        {
            var component = new Component
            {
                Properties = new List<Property>
                {
                    new Property { Name = Dataconstant.Cdx_IsInternal, Value = "false" }
                }
            };
            Assert.IsFalse(CommonIdentiferHelper.IsComponentInternal(component));
        }

        [Test]
        public void IsComponentInternal_PropertyTrue_ReturnsTrue()
        {
            var component = new Component
            {
                Properties = new List<Property>
                {
                    new Property { Name = Dataconstant.Cdx_IsInternal, Value = "true" }
                }
            };
            Assert.IsTrue(CommonIdentiferHelper.IsComponentInternal(component));
        }

        #endregion

        #region GetInternalRepos

        [Test]
        public void GetInternalRepos_NullAppSettings_ReturnsEmpty()
        {
            var result = CommonIdentiferHelper.GetInternalRepos(null);
            Assert.IsNotNull(result);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetInternalRepos_EmptyProjectType_ReturnsEmpty()
        {
            var appSettings = new CommonAppSettings { ProjectType = string.Empty };
            var result = CommonIdentiferHelper.GetInternalRepos(appSettings);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetInternalRepos_UnknownProjectType_ReturnsEmpty()
        {
            var appSettings = new CommonAppSettings { ProjectType = "UNKNOWN" };
            var result = CommonIdentiferHelper.GetInternalRepos(appSettings);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetInternalRepos_NpmWithInternalRepos_ReturnsRepos()
        {
            var appSettings = new CommonAppSettings
            {
                ProjectType = "NPM",
                Npm = new Config
                {
                    Artifactory = new Artifactory
                    {
                        InternalRepos = new[] { "npm-internal", "npm-shared", string.Empty }
                    }
                }
            };
            var result = CommonIdentiferHelper.GetInternalRepos(appSettings);
            Assert.That(result, Is.EquivalentTo(new[] { "npm-internal", "npm-shared" }));
        }

        [Test]
        public void GetInternalRepos_ConfigWithoutArtifactory_ReturnsEmpty()
        {
            var appSettings = new CommonAppSettings
            {
                ProjectType = "NUGET",
                Nuget = new Config()
            };
            var result = CommonIdentiferHelper.GetInternalRepos(appSettings);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetInternalRepos_ProjectTypeIsCaseInsensitive()
        {
            var appSettings = new CommonAppSettings
            {
                ProjectType = "npm",
                Npm = new Config
                {
                    Artifactory = new Artifactory
                    {
                        InternalRepos = new[] { "npm-internal" }
                    }
                }
            };
            var result = CommonIdentiferHelper.GetInternalRepos(appSettings);
            Assert.That(result, Is.EquivalentTo(new[] { "npm-internal" }));
        }

        #endregion

        #region FilterAqlResultsByRepos

        [Test]
        public void FilterAqlResultsByRepos_NullAqlList_ReturnsEmpty()
        {
            var result = CommonIdentiferHelper.FilterAqlResultsByRepos(null, new[] { "repo" });
            Assert.IsNotNull(result);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void FilterAqlResultsByRepos_NullReposList_ReturnsEmpty()
        {
            var aql = new List<AqlResult> { new AqlResult { Repo = "any" } };
            var result = CommonIdentiferHelper.FilterAqlResultsByRepos(aql, null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void FilterAqlResultsByRepos_FiltersOnlyMatchingReposCaseInsensitive()
        {
            var aql = new List<AqlResult>
            {
                new AqlResult { Repo = "npm-internal", Name = "a" },
                new AqlResult { Repo = "NPM-Release", Name = "b" },
                new AqlResult { Repo = "npm-dev", Name = "c" },
                new AqlResult { Repo = null, Name = "d" }
            };

            var result = CommonIdentiferHelper.FilterAqlResultsByRepos(aql, new[] { "npm-internal", "npm-release" });

            Assert.That(result.Count, Is.EqualTo(2));
            CollectionAssert.AreEquivalent(new[] { "a", "b" }, result.Select(x => x.Name).ToList());
        }

        [Test]
        public void FilterAqlResultsByRepos_NoMatches_ReturnsEmpty()
        {
            var aql = new List<AqlResult>
            {
                new AqlResult { Repo = "some-repo", Name = "a" }
            };
            var result = CommonIdentiferHelper.FilterAqlResultsByRepos(aql, new[] { "other-repo" });
            Assert.That(result.Count, Is.EqualTo(0));
        }

        #endregion

        #region GetAqlResultsForComponent

        [Test]
        public void GetAqlResultsForComponent_InternalComponent_ReturnsInternalList()
        {
            var full = new List<AqlResult> { new AqlResult { Repo = "full" } };
            var internalList = new List<AqlResult> { new AqlResult { Repo = "internal" } };
            var component = new Component
            {
                Properties = new List<Property>
                {
                    new Property { Name = Dataconstant.Cdx_IsInternal, Value = "true" }
                }
            };

            var result = CommonIdentiferHelper.GetAqlResultsForComponent(component, full, internalList);

            Assert.AreSame(internalList, result);
        }

        [Test]
        public void GetAqlResultsForComponent_NonInternalComponent_ReturnsFullList()
        {
            var full = new List<AqlResult> { new AqlResult { Repo = "full" } };
            var internalList = new List<AqlResult> { new AqlResult { Repo = "internal" } };
            var component = new Component
            {
                Properties = new List<Property>
                {
                    new Property { Name = Dataconstant.Cdx_IsInternal, Value = "false" }
                }
            };

            var result = CommonIdentiferHelper.GetAqlResultsForComponent(component, full, internalList);

            Assert.AreSame(full, result);
        }

        [Test]
        public void GetAqlResultsForComponent_NoInternalProperty_ReturnsFullList()
        {
            var full = new List<AqlResult>();
            var internalList = new List<AqlResult>();
            var component = new Component { Name = "a" };

            var result = CommonIdentiferHelper.GetAqlResultsForComponent(component, full, internalList);

            Assert.AreSame(full, result);
        }

        #endregion
    }
}
