using CycloneDX.Models;
using LCT.Common;
using LCT.Common.Constants;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    public class SBomTemplateTests
    {


        [Test]
        public void GetFilePathForTemplate_WithSingleFile_ReturnsFilePath()
        {
            // Arrange
            var filePaths = new List<string> { "path1" };

            // Act
            var result = SbomTemplate.GetFilePathForTemplate(filePaths);

            // Assert
            Assert.AreEqual("path1", result);
        }

        [Test]
        public void GetFilePathForTemplate_WithNoFiles_ReturnsEmptyString()
        {
            // Arrange
            var filePaths = new List<string>();

            // Act
            var result = SbomTemplate.GetFilePathForTemplate(filePaths);

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void ProcessTemplateFile_WithInvalidTemplateFile_DoesNotAddComponentDetails()
        {
            // Arrange
            var templateFilePath = "invalidTemplateFilePath";
            var cycloneDXBomParserMock = new Mock<ICycloneDXBomParser>();
            var componentsForBOM = new List<Component>();
            var projectType = "projectType";

            // Act
            SbomTemplate.ProcessTemplateFile(templateFilePath, cycloneDXBomParserMock.Object, componentsForBOM, projectType);

            // Assert
            Assert.AreEqual(0, componentsForBOM.Count);
        }

        [Test]
        public void AddComponentDetails_InputTemplateDetailsWithoutProperty_ReturnsBomWithPropertyAdded()
        {
            //Arrange
            List<Component> componentsForBOM = new List<Component>();
            Bom templateDetails = new Bom();
            Component component1 = new Component();
            component1.Name = "animations";
            component1.Group = "";
            component1.Description = string.Empty;
            component1.Version = "1.0.0";
            var components = new List<Component>() { component1 };
            templateDetails.Components = components;

            //Act
            SbomTemplate.AddComponentDetails(componentsForBOM, templateDetails);
            bool isUpdated = componentsForBOM.Exists(x => x.Properties != null && x.Properties.Exists(x => x.Name == Dataconstant.Cdx_IdentifierType && x.Value == Dataconstant.TemplateAdded));
            //Assert
            Assert.IsTrue(isUpdated, "Checks For Updated Property In List ");
        }
        [Test]
        public void AddComponentDetails_InputTemplateDetailsWithProperties_ReturnsBomWithPropertyAdded()
        {
            //Arrange
            List<Component> componentsForBOM = new List<Component>();
            Bom templateDetails = new Bom();
            Component component1 = new Component();
            component1.Name = "animations";
            component1.Group = "";
            component1.Description = string.Empty;
            component1.Version = "1.0.0";
            component1.Properties = new List<Property>();

            var components = new List<Component>() { component1 };
            templateDetails.Components = components;

            //Act
            SbomTemplate.AddComponentDetails(componentsForBOM, templateDetails);
            bool isUpdated = componentsForBOM.Exists(x => x.Properties != null && x.Properties.Exists(x => x.Name == Dataconstant.Cdx_IdentifierType && x.Value == Dataconstant.TemplateAdded));
            //Assert
            Assert.IsTrue(isUpdated, "Checks For Updated Property In List ");
        }


        [Test]
        public void AddComponentDetails_InputTemplateDetailsWithoutComponents_ReturnsNull()
        {
            //Arrange
            List<Component> componentsForBOM = new List<Component>();
            Bom templateDetails = new Bom();


            //Act
            SbomTemplate.AddComponentDetails(componentsForBOM, templateDetails);
            bool isUpdated = componentsForBOM.Exists(x => x.Properties != null && x.Properties.Exists(x => x.Name == Dataconstant.Cdx_IdentifierType && x.Value == Dataconstant.TemplateAdded));
            //Assert
            Assert.IsFalse(isUpdated, "Checks For Updated Property In List ");
        }


        [Test]
        public void AddComponentDetails_InputTemplateDetails_ReturnsTemplateWithNoDetailsupdated()
        {
            //Arrange
            List<Component> componentsForBOM = new List<Component>();
            Bom templateDetails = new Bom();
            Component component1 = new Component();
            component1.Name = "animations";
            component1.Group = "";
            component1.Description = string.Empty;
            component1.Version = "1.0.0";
            component1.Properties = new List<Property>();

            var components = new List<Component>() { component1 };
            templateDetails.Components = components;
            Component component = new Component();
            component.Name = "animations";
            component.Group = "";
            component.Description = string.Empty;
            component.Version = "1.0.0";
            componentsForBOM.Add(component);

            //Act
            SbomTemplate.AddComponentDetails(componentsForBOM, templateDetails);
            bool isUpdated = componentsForBOM.Exists(x => x.Properties != null && x.Properties.Exists(x => x.Name == Dataconstant.Cdx_IdentifierType && x.Value == Dataconstant.TemplateAdded));
            //Assert
            Assert.IsFalse(isUpdated, "Checks For Updated Property In List ");
        }

        [Test]
        public void AddComponentDetails_InputTemplateDetails_ReturnsTemplateWithDetailsupdated()
        {
            //Arrange
            List<Component> componentsForBOM = new List<Component>();
            Bom templateDetails = new Bom();
            Component component1 = new Component();
            component1.Name = "animations";
            component1.Group = "";
            component1.Description = string.Empty;
            component1.Version = "1.0.0";
            component1.Properties = new List<Property>()
            {
                new(){Name=Dataconstant.Cdx_IdentifierType,Value=Dataconstant.TemplateAdded}
            };
            component1.Licenses = new List<LicenseChoice> {
             new() {License=new() }};

            var components = new List<Component>() { component1 };
            templateDetails.Components = components;
            Component component = new Component();
            component.Name = "animations";
            component.Group = "";
            component.Description = string.Empty;
            component.Version = "1.0.0";
            component.Properties = new List<Property>()
                  {
                new(){Name=Dataconstant.Cdx_IdentifierType,Value=Dataconstant.TemplateAdded}
            };
            component.Licenses = new List<LicenseChoice> {
             new() {License=new() }};

            componentsForBOM.Add(component);
            BomCreator.bomKpiData.ComponentsUpdatedFromSBOMTemplateFile = 0;
            //Act
            SbomTemplate.AddComponentDetails(componentsForBOM, templateDetails);

            //Assert
            Assert.That(BomCreator.bomKpiData.ComponentsUpdatedFromSBOMTemplateFile, Is.EqualTo(1));
        }
    }
}
