// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2024 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace LCT.Common.UTest
{
    [TestFixture]
    public class FileOperationsTest
    {
        [SetUp]
        public void Setup()
        {           
            // Implement
        }
        [Test]
        public void ValidateFilePath_WhenPathIsNotProper_ThrowsArgumentException()
        {
            var fileOperations = new FileOperations();
            Assert.Throws<System.ArgumentException>(() => fileOperations.ValidateFilePath(""));
        }

        [Test]
        public void ValidateFilePath_WhenFileNotAvailable_ThrowsFileNotFoundException()
        {
            var fileOperations = new FileOperations();
            Assert.Throws<FileNotFoundException>(() => fileOperations.ValidateFilePath("test"));
        }
        [Test]
        public void BackupTheGivenFile_WhenFilePathIsNull_ThrowsArgumentNullExceptionn()
        {
            var fileOperations = new FileOperations();
            Assert.Throws<System.ArgumentNullException>(() => fileOperations.WriteContentToFile<string>(null, null, null, null));
        }
        [Test]
        public void WriteContentToFile_WhenFilepathIsWrong_ReturnsFailure()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filePath = outFolder + @"\LCT.Common.UTests\Source";
            var fileOperations = new FileOperations();

            //Act
            string actual = fileOperations.WriteContentToFile<string>("move the file", filePath, ".txt", "test");

            //Assert
            Assert.AreEqual("failure", actual);
        }

        [Test]
        public void WriteContentToCycloneDXFile_WhenFilepathIsWrong_ReturnsFailure()
        {
            //Arrange
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filePath = outFolder + @"\LCT.Common.UTests\Source";
            string fileFolder = outFolder + @"\LCT.Common.UTests\Source\";
            var fileOperations = new FileOperations();

            //Act
            string actual = fileOperations.WriteContentToCycloneDXFile<string>("move the file", filePath, fileFolder);

            //Assert
            Assert.AreEqual("failure", actual);
        }

        [Test]
        public void CombineComponentsFromExistingBOM_WhenFilepathIsWrong_ReturnsSuccess()
        {
            //Arrange
            Bom bom = new Bom();
            bom.Components = new List<Component>();
            string filePath = $"{Path.GetTempPath()}\\";
            File.WriteAllText(filePath + "output.json", "{\"BomFormat\":\"CycloneDX\",\"SpecVersion\":4,\"SpecVersionString\":\"1.4\",\"SerialNumber\":null,\"Version\":null,\"Components\":[{\"Type\":0,\"MimeType\":null,\"BomRef\":\"\",\"Supplier\":null,\"Author\":null,\"Publisher\":null,\"Group\":null,\"Name\":\"cef.redist.x64\",\"Version\":\"100.0.14\",\"Description\":\"\",\"Scope\":null,\"Hashes\":null,\"Licenses\":null,\"Copyright\":null,\"Cpe\":null,\"Purl\":\"\",\"Swid\":null,\"Modified\":null,\"Pedigree\":null,\"Components\":null,\"Properties\":[{\"Name\":\"internal:siemens:clearing:is-internal\",\"Value\":\"false\"},{\"Name\":\"internal:siemens:clearing:repo-url\",\"Value\":\"org1-nuget-nuget-remote-cache\"},{\"Name\":\"internal:siemens:clearing:project-type\",\"Value\":\"NUGET\"}],\"Evidence\":null}],\"Compositions\":null}");
            var fileOperations = new FileOperations();

            //Act
            Bom comparisonData = fileOperations.CombineComponentsFromExistingBOM(bom, filePath + "output.json");

            //Assert
            Assert.AreEqual(1, comparisonData.Components.Count);
        }

       
        [Test]
        public void CombineComponentsFromExistingBOM_WhenFilepathIsWrong_ReturnsFailure()
        {
            //Arrange
            Bom bom = new Bom();
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            string filePath = outFolder + @"\LCT.Common.UTests\Source";
            var fileOperations = new FileOperations();

            //Act
            Bom comparisonData = fileOperations.CombineComponentsFromExistingBOM(bom, filePath);

            //Assert
            Assert.AreEqual(null, comparisonData.Components);
        }
    }
}
