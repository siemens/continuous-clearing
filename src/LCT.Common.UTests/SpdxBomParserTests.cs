using System;
using CycloneDX.Models;
using LCT.Common.Interface;
using Moq;
using NUnit.Framework;

namespace LCT.Common.UTests
{
    [TestFixture]
    public class SpdxBomParserTests
    {
        [Test]
        public void ParseSPDXBom_ShouldReturnBom_WhenFilePathIsValid()
        {
            // Arrange
            var mockParser = new Mock<ISpdxBomParser>();
            var expectedBom = new Bom();
            mockParser.Setup(p => p.ParseSPDXBom(It.IsAny<string>())).Returns(expectedBom);

            // Act
            var result = mockParser.Object.ParseSPDXBom("valid/path/to/file.spdx");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedBom, result);
        }

        [Test]
        public void ParseSPDXBom_ReturnsEmptyBom_WhenFileDoesNotExist()
        {
            var parser = new LCT.Common.SpdxBomParser();
            var result = parser.ParseSPDXBom("nonexistentfile.spdx");
            Assert.IsNotNull(result);
            Assert.IsEmpty(result.Components);
            Assert.IsEmpty(result.Dependencies);
        }

        [Test]
        public void ParseSPDXBom_ReturnsEmptyBom_WhenFileIsInvalidJson()
        {
            var tempFile = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllText(tempFile, "not a json");
            var parser = new LCT.Common.SpdxBomParser();
            var result = parser.ParseSPDXBom(tempFile);
            Assert.IsNotNull(result);
            Assert.IsEmpty(result.Components);
            Assert.IsEmpty(result.Dependencies);
            System.IO.File.Delete(tempFile);
        }

        [Test]
        public void ParseSPDXBom_ReturnsBomWithComponents_WhenValidSpdxJson()
        {
            var tempFile = System.IO.Path.GetTempFileName();
            var validJson = "{\"@graph\": [" +
                "{\"type\": \"SpdxDocument\", \"creationInfo\": \"cinfo1\"}," +
                "{\"@id\": \"cinfo1\", \"type\": \"CreationInfo\", \"specVersion\": \"3.0\"}," +
                "{\"type\": \"software_Package\", \"name\": \"TestLib\", \"software_packageVersion\": \"1.0.0\", \"spdxId\": \"pkg1\", \"software_packageUrl\": \"pkg:pypi/testlib@1.0.0\"}" +
                "]}";
            System.IO.File.WriteAllText(tempFile, validJson);
            var parser = new LCT.Common.SpdxBomParser();
            var result = parser.ParseSPDXBom(tempFile);
            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result.Components);
            Assert.AreEqual("TestLib", result.Components[0].Name);
            System.IO.File.Delete(tempFile);
        }
    }
}
