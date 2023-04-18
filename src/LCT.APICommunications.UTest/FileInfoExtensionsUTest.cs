// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 

using NUnit.Framework;

namespace LCT.APICommunications.UTest
{
    public class FileInfoExtensionsUTest
    {
        [SetUp]
        public void Setup()
        {
            // Method intentionally left empty.
        }

        [Test]
        public void WriteMultipartFormData_InputEmptyFile_ReturnsFileNotFoundException()
        {
            //Arrange
            FileInfo file = new FileInfo("Empty");
            string mimeBoundary = "";
            string mimeType = "";
            string formKey = "";

            //Act & Assert
            Assert.ThrowsAsync<FileNotFoundException>(() => { FileInfoExtensions.WriteMultipartFormData(file, null, mimeBoundary, mimeType, formKey); return Task.CompletedTask; });
        }

        [Test]
        public void WriteMultipartFormData_InputFile_ReturnsArgumentNullException()
        {
            //Arrange
            string filename = GetFileName() + "Sample.txt";
            string filePath = $"{Path.GetTempPath()}\\";
            File.Create(filePath + filename);
            FileInfo file = new FileInfo(filePath + filename);
            string mimeBoundary = "";
            string mimeType = "";
            string formKey = "";

            //Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(() => { FileInfoExtensions.WriteMultipartFormData(file, null, mimeBoundary, mimeType, formKey); return Task.CompletedTask; });
        }

        [Test]
        public void WriteMultipartFormData_InputEmptymimeBoundary_ReturnsArgumentException()
        {
            //Arrange
            string filename = GetFileName() + "Sample1.txt";
            string filePath = $"{Path.GetTempPath()}\\";
            File.Create(filePath + filename);
            FileInfo file = new FileInfo(filePath + filename);
            Stream stream = new MemoryStream();
            string mimeBoundary = "";
            string mimeType = "";
            string formKey = "";

            //Act & Assert
            Assert.ThrowsAsync<ArgumentException>(() => { FileInfoExtensions.WriteMultipartFormData(file, stream, mimeBoundary, mimeType, formKey); return Task.CompletedTask; });
        }

        [Test]
        public void WriteMultipartFormData_InputEmptymimeType_ReturnsArgumentException()
        {
            //Arrange
            string filename = GetFileName() + "Sample2.txt";
            string filePath = $"{Path.GetTempPath()}\\";
            File.Create(filePath + filename);
            FileInfo file = new FileInfo(filePath + filename);
            Stream stream = new MemoryStream();
            string mimeBoundary = "mimeBoundary";
            string mimeType = "";
            string formKey = "";

            //Act & Assert
            Assert.ThrowsAsync<ArgumentException>(() => { FileInfoExtensions.WriteMultipartFormData(file, stream, mimeBoundary, mimeType, formKey); return Task.CompletedTask; });
        }

        [Test]
        public void WriteMultipartFormData_InputEmptyformKey_ReturnsArgumentException()
        {
            //Arrange
            string filename = GetFileName() + "Sample3.txt";
            string filePath = $"{Path.GetTempPath()}\\";
            File.Create(filePath + filename);
            FileInfo file = new FileInfo(filePath + filename);
            Stream stream = new MemoryStream();
            string mimeBoundary = "mimeBoundary";
            string mimeType = "mimeType";
            string formKey = "";

            //Act & Assert
            Assert.ThrowsAsync<ArgumentException>(() => { FileInfoExtensions.WriteMultipartFormData(file, stream, mimeBoundary, mimeType, formKey); return Task.CompletedTask; });
        }

        private static string GetFileName()
        {
            return DateTime.Now.ToString("yyyyMMddTHHmmss");
        }
    }
}
