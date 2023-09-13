// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.Common;
using CycloneDX.Models;
using LCT.APICommunications.Model;
using LCT.ArtifactoryUploader;
using LCT.Common.Constants;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnitTestUtilities;

namespace AritfactoryUploader.UTest
{
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
            Assert.That(11, Is.EqualTo(componentList.Components.Count), "Checks for no of components");
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
            Assert.Throws<JsonReaderException>(() => PackageUploadHelper.GetComponentListFromComparisonBOM(comparisonBOMPath));
        }


        [Test]
        public void GetComponentsToBeUploadedToArtifactory_GivenFewApprovedComponentList_ReturnsUploadList()
        {
            //Arrange
            List<Component> componentLists = GetComponentList();
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ArtifactoryUploadApiKey = "wfwfwfwfwegwgweg",
                ArtifactoryUploadUser = "user@account.com",
                JfrogNpmDestRepoName = "npm-test",
                JfrogNpmSrcRepo = "remote-cache",
                JFrogApi = UTParams.JFrogURL,
                LogFolderPath = outFolder
            };

            //Act
            List<ComponentsToArtifactory> uploadList = PackageUploadHelper.GetComponentsToBeUploadedToArtifactory(componentLists, appSettings);
            // Assert
            Assert.That(3, Is.EqualTo(uploadList.Count), "Checks for 2  no of components to upload");
        }


        [Test]
        public void GetComponentsToBeUploadedToArtifactory_GivenAllApprovedComponentList_ReturnsUploadList()
        {
            //Arrange
            List<Component> componentLists = GetComponentList();
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
                JfrogNpmDestRepoName = "npm-test",
                JfrogNpmSrcRepo = "remote-cache",
                JFrogApi = UTParams.JFrogURL,
                LogFolderPath= outFolder
            };
            string LogfolderPath = appSettings.LogFolderPath;

            //Act
            List<ComponentsToArtifactory> uploadList = PackageUploadHelper.GetComponentsToBeUploadedToArtifactory(componentLists, appSettings);

            // Assert
            Assert.That(4, Is.EqualTo(uploadList.Count), "Checks for 3 no of components to upload");
        }
        [Test]
        public void GetComponentsToBeUploadedToArtifactory_GivenNotApprovedComponentList_ReturnsUploadList()
        {
            //Arrange
            List<Component> componentLists = GetComponentList();
            foreach (var component in componentLists)
            {
                component.Properties[1].Value = "NEW_CLEARING";
            }

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ArtifactoryUploadApiKey = "wfwfwfwfwegwgweg",
                ArtifactoryUploadUser = "user@account.com",
                JfrogNugetDestRepoName = "nuget-test",
                JfrogNugetSrcRepo = "remote-cache",
                JFrogApi = UTParams.JFrogURL
            };
        
            //Act
            List<ComponentsToArtifactory> uploadList = PackageUploadHelper.GetComponentsToBeUploadedToArtifactory(componentLists, appSettings);

            // Assert
            Assert.That(0, Is.EqualTo(uploadList.Count), "Checks for components to upload to be zero");
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
