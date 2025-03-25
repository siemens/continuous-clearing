// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using TestUtilities;

namespace SW360IntegrationTest.LoadTest
{
    public class ClearingToolLoadTest
    {
        private string OutFolderPath { get; set; }

        [SetUp]
        public void Setup()
        {
            OutFolderPath = TestHelper.OutFolder;
        }

        public void PerformanceTestFor130Components()
        {
            //Arrange
            string packageJsonPath = Path.GetFullPath(Path.Combine(OutFolderPath, "..", "..", "TestFiles", "NPMTestFile", "ProjectTestData", "PQAdviserCompact"));
            string bomPath = Path.GetFullPath(Path.Combine(OutFolderPath, "..", "BOMs"));
            string combomJsonPath = Path.GetFullPath(Path.Combine(OutFolderPath, "..", "BOMs", "Test_ComparisonBOM.json"));
            //Act
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            TestHelper.RunBOMCreatorExe(new string[] { packageJsonPath, bomPath });
            TestHelper.RunComponentCreatorExe(new string[] { combomJsonPath });

            stopwatch.Stop();
            Console.WriteLine($"PerformanceTestFor130Components():Total time taken : {stopwatch.Elapsed.Minutes}");

            //Assert
            Assert.AreEqual(60, stopwatch.Elapsed.Minutes, 200,
                $"The actual time taken : {stopwatch.Elapsed.Minutes} is not equal to expected,which is 200 min");
        }

        public void PerformanceTestFor70Components()
        {
            //Arrange
            string packageJsonPath = Path.GetFullPath(Path.Combine(OutFolderPath, "..", "..", "TestFiles", "NPMTestFile", "ProjectTestData", "SDBBackend"));
            string bomPath = Path.GetFullPath(Path.Combine(OutFolderPath, "..", "BOMs"));
            string combomJsonPath = Path.GetFullPath(Path.Combine(OutFolderPath, "..", "BOMs", "Test_ComparisonBOM.json"));

            //Act
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            TestHelper.RunBOMCreatorExe(new string[] { packageJsonPath, bomPath });
            TestHelper.RunComponentCreatorExe(new string[] { combomJsonPath });


            stopwatch.Stop();
            Console.WriteLine($"PerformanceTestFor70Components():Total time taken : {stopwatch.Elapsed.Minutes}");

            //Assert
            Assert.AreEqual(60, stopwatch.Elapsed.Minutes, 200,
                $"The actual time taken : {stopwatch.Elapsed.Minutes} is not equal to expected,which is 200 min");
        }
    }
}
