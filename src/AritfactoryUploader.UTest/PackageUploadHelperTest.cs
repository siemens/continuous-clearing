// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common;
using CycloneDX.Models;
using LCT.APICommunications.Model;
using LCT.ArtifactoryUploader;
using LCT.Common.Constants;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnitTestUtilities;
using System.Threading.Tasks;
using System.Linq;
using LCT.ArtifactoryUploader.Model;
using LCT.APICommunications;

namespace AritfactoryUploader.UTest
{
    [TestFixture]
    public class PackageUploadHelperTest
    {

        [Test]
        public void GetComponentListFromComparisonBOM_GivenComparisonBOM_ReturnsComponentList()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string comparisonBOMPath = outFolder + @"\ArtifactoryUTTestFiles\CyclonedxBom.json";
            //Act
            Bom componentList = PackageUploadHelper.GetComponentListFromComparisonBOM(comparisonBOMPath);
            // Assert
            Assert.That(6, Is.EqualTo(componentList.Components.Count), "Checks for no of components");
        }

        [Test]
        public void GetComponentListFromComparisonBOM_GivenInvalidComparisonBOM_ReturnsException()
        {
            //Arrange
            string comparisonBOMPath = @"TestFiles\CCTComparisonBOM.json";

            //Act && Assert
            Assert.Throws<FileNotFoundException>(() => PackageUploadHelper.GetComponentListFromComparisonBOM(comparisonBOMPath));
        }
        [Test]
        public void GetComponentListFromComparisonBOM_GivenInvalidfile_ReturnsException()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string comparisonBOMPath = outFolder + @"\ArtifactoryUTTestFiles\ComparisonBOM.json";

            //Act && Assert
            Assert.Throws<System.Text.Json.JsonException>(() => PackageUploadHelper.GetComponentListFromComparisonBOM(comparisonBOMPath));
        }


        [Test]
        public async Task GetComponentsToBeUploadedToArtifactory_GivenFewApprovedComponentList_ReturnsUploadList()
        {
            //Arrange
            List<Component> componentLists = GetComponentList();
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            DisplayPackagesInfo displayPackagesInfo = PackageUploadHelper.GetComponentsToBePackages();
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ArtifactoryUploadApiKey = "wfwfwfwfwegwgweg",
                ArtifactoryUploadUser = "user@account.com",
                Npm = new LCT.Common.Model.Config { JfrogThirdPartyDestRepoName = "npm-test" },
                Nuget = new LCT.Common.Model.Config { JfrogThirdPartyDestRepoName = "nuget-test" },
                JfrogNpmSrcRepo = "remote-cache",
                JFrogApi = UTParams.JFrogURL,
                LogFolderPath = outFolder
            };

            //Act
            List<ComponentsToArtifactory> uploadList = await PackageUploadHelper.GetComponentsToBeUploadedToArtifactory(componentLists, appSettings, displayPackagesInfo);
            // Assert
            Assert.That(3, Is.EqualTo(uploadList.Count), "Checks for 2  no of components to upload");
        }


        [Test]
        public async Task GetComponentsToBeUploadedToArtifactory_GivenAllApprovedComponentList_ReturnsUploadList()
        {
            //Arrange
            List<Component> componentLists = GetComponentList();
            DisplayPackagesInfo displayPackagesInfo = PackageUploadHelper.GetComponentsToBePackages();
            foreach (var component in componentLists)
            {
                if (component.Name == "@angular/core")
                {
                    component.Properties[1].Value = "APPROVED";
                }

            }
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ArtifactoryUploadApiKey = "wfwfwfwfwegwgweg",
                ArtifactoryUploadUser = "user@account.com",
                Npm = new LCT.Common.Model.Config { JfrogThirdPartyDestRepoName = "npm-test" },
                Nuget = new LCT.Common.Model.Config { JfrogThirdPartyDestRepoName = "nuget-test" },
                JFrogApi = UTParams.JFrogURL,
                LogFolderPath = outFolder
            };

            //Act
            List<ComponentsToArtifactory> uploadList = await PackageUploadHelper.GetComponentsToBeUploadedToArtifactory(componentLists, appSettings, displayPackagesInfo);

            // Assert
            Assert.That(4, Is.EqualTo(uploadList.Count), "Checks for 3 no of components to upload");
        }

        [Test]
        public async Task GetComponentsToBeUploadedToArtifactory_GivenNotApprovedComponentList_ReturnsUploadList()
        {
            //Arrange
            List<Component> componentLists = GetComponentList();
            DisplayPackagesInfo displayPackagesInfo = PackageUploadHelper.GetComponentsToBePackages();
            foreach (var component in componentLists)
            {
                component.Properties[1].Value = "NEW_CLEARING";
            }

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ArtifactoryUploadApiKey = "wfwfwfwfwegwgweg",
                ArtifactoryUploadUser = "user@account.com",
                Npm = new LCT.Common.Model.Config
                {
                    JfrogThirdPartyDestRepoName = "nuget-test",
                },
                JFrogApi = UTParams.JFrogURL
            };

            //Act
            List<ComponentsToArtifactory> uploadList = await PackageUploadHelper.GetComponentsToBeUploadedToArtifactory(componentLists, appSettings, displayPackagesInfo);

            // Assert
            Assert.That(0, Is.EqualTo(uploadList.Count), "Checks for components to upload to be zero");
        }

        [Test]
        public void UpdateBomArtifactoryRepoUrl_GivenBomAndComponentsUploadedToArtifactory_UpdatesBom()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string comparisonBOMPath = outFolder + @"\ArtifactoryUTTestFiles\CyclonedxBom.json";
            Bom bom = PackageUploadHelper.GetComponentListFromComparisonBOM(comparisonBOMPath);
            List<ComponentsToArtifactory> components = new List<ComponentsToArtifactory>()
            {
                new ComponentsToArtifactory()
                {
                    Purl = "pkg:npm/rxjs@6.5.4",
                    DestRepoName = "org1-npmjs-npm-remote",
                    DryRun = false,
                }
            };

            //Act
            PackageUploadHelper.UpdateBomArtifactoryRepoUrl(ref bom, components);

            //Assert
            var repoUrl = bom.Components.First(x => x.Properties[3].Name == "internal:siemens:clearing:jfrog-repo-name").Properties[3].Value;
            Assert.AreEqual("org1-npmjs-npm-remote", repoUrl);
        }

        [Test]
        public void UpdateBomArtifactoryRepoUrl_GivenBomAndComponentsUploadedToArtifactoryDryRun_NoUpdateBom()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string comparisonBOMPath = outFolder + @"\ArtifactoryUTTestFiles\CyclonedxBom.json";
            Bom bom = PackageUploadHelper.GetComponentListFromComparisonBOM(comparisonBOMPath);
            List<ComponentsToArtifactory> components = new List<ComponentsToArtifactory>()
            {
                new ComponentsToArtifactory()
                {
                    Purl = "pkg:npm/rxjs@6.5.4",
                    DestRepoName = "org1-npmjs-npm-remote",
                }
            };

            //Act
            PackageUploadHelper.UpdateBomArtifactoryRepoUrl(ref bom, components);

            //Assert
            var repoUrl = bom.Components.First(x => x.Properties[3].Name == "internal:siemens:clearing:jfrog-repo-name").Properties[3].Value;
            Assert.AreNotEqual("org1-npmjs-npm-remote", repoUrl);
        }

        [Test]
        public void DisplayErrorForJfrogPackages_GivenJfrogNotFoundPackages_ResultsSucess()
        {
            // Arrange
            ComponentsToArtifactory componentsToArtifactory = new ComponentsToArtifactory();
            componentsToArtifactory.Name = "Test";
            componentsToArtifactory.Version = "0.12.3";
            List<ComponentsToArtifactory> JfrogNotFoundPackages = new() { componentsToArtifactory };
            // Act

            PackageUploadHelper.DisplayErrorForJfrogPackages(JfrogNotFoundPackages);

            // Assert
        }

        [Test]
        public void DisplayErrorForJfrogFoundPackages_GivenJfrogNotFoundPackages_ResultsSucess()
        {
            // Arrange
            ComponentsToArtifactory componentsToArtifactory = new ComponentsToArtifactory();
            componentsToArtifactory.Name = "Test";
            componentsToArtifactory.Version = "0.12.3";
            componentsToArtifactory.ResponseMessage = new System.Net.Http.HttpResponseMessage()
            { ReasonPhrase = ApiConstant.ErrorInUpload };

            ComponentsToArtifactory componentsToArtifactory2 = new ComponentsToArtifactory();
            componentsToArtifactory2.Name = "Test2";
            componentsToArtifactory2.Version = "0.12.32";
            componentsToArtifactory2.ResponseMessage = new System.Net.Http.HttpResponseMessage()
            { ReasonPhrase = ApiConstant.PackageNotFound };

            ComponentsToArtifactory componentsToArtifactory3 = new ComponentsToArtifactory();
            componentsToArtifactory3.Name = "Test3";
            componentsToArtifactory3.Version = "0.12.33";
            componentsToArtifactory3.ResponseMessage = new System.Net.Http.HttpResponseMessage()
            { ReasonPhrase = "error" };

            List<ComponentsToArtifactory> JfrogNotFoundPackages = new() {
                componentsToArtifactory, componentsToArtifactory2, componentsToArtifactory3 };
            // Act

            PackageUploadHelper.DisplayErrorForJfrogFoundPackages(JfrogNotFoundPackages);

            // Assert
        }

        private static List<Component> GetComponentList()
        {
            List<Component> componentLists = new List<Component>();
            Property propinternal = new Property
            {
                Name = Dataconstant.Cdx_IsInternal,
                Value = "false"
            };
            Property prop = new Property
            {
                Name = Dataconstant.Cdx_ClearingState,
                Value = "APPROVED"
            };
            Component comp1 = new Component
            {
                Name = "@angular/animations",
                Version = "11.2.3",
                Purl = "pkg:npm/%40angular/animations@11.0.4",
                Properties = new List<Property>()
            };
            comp1.Properties.Add(propinternal);
            comp1.Properties.Add(prop);
            componentLists.Add(comp1);

            Property prop1 = new Property
            {
                Name = Dataconstant.Cdx_ClearingState,
                Value = "NEW_CLEARING"
            };
            Component comp2 = new Component
            {
                Name = "@angular/core",
                Version = "11.2.3",
                Purl = "pkg:npm/%40angular/core@11.0.4",
                Properties = new List<Property>()
            };
            comp2.Properties.Add(propinternal);
            comp2.Properties.Add(prop1);
            componentLists.Add(comp2);

            Property prop2 = new Property
            {
                Name = Dataconstant.Cdx_ClearingState,
                Value = "APPROVED"
            };
            Component comp3 = new Component
            {
                Name = "NewtonsoftJson",
                Version = "11.9.3",
                Purl = "pkg:nuget/NewtonsoftJson@11.9.3",
                Properties = new List<Property>()
            };
            comp3.Properties.Add(propinternal);
            comp3.Properties.Add(prop2);
            componentLists.Add(comp3);

            Property prop3 = new Property
            {
                Name = Dataconstant.Cdx_ClearingState,
                Value = "APPROVED"
            };
            Component comp4 = new Component
            {
                Name = "adduser",
                Version = "11.9.3",
                Purl = "pkg:deb/adduser@11.9.3",
                Properties = new List<Property>()
            };
            comp4.Properties.Add(propinternal);
            comp4.Properties.Add(prop3);
            componentLists.Add(comp4);
            return componentLists;
        }
    }
}
