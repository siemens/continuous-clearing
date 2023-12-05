// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT

// -------------------------------------------------------------------------------------------------------------------- 
using NUnit.Framework;
using System;
using System.Diagnostics;
using TestUtilities;

namespace SW360IntegrationTest.LoadTest
{
    [Ignore("Load test need to run separatly")]
    [TestFixture, Order(11)]
    public class ClearingToolLoadTest
    {
        private string OutFolderPath { get; set; }

        [SetUp]
        public void Setup()
        {
            OutFolderPath = TestHelper.OutFolder;
        }

        [Ignore("Load test need to run separatly")]
        [Test, Order(1)]
        public void PerformanceTestFor130Components()
        {
            //Arrange
            string packageJsonPath = $"{OutFolderPath}\\..\\..\\TestFiles\\NPMTestFile\\ProjectTestData\\PQAdviserCompact";
            string bomPath = $"{OutFolderPath}\\..\\BOMs";
            string combomJsonPath = $"{OutFolderPath}\\..\\BOMs\\Test_ComparisonBOM.json";
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

        [Ignore("Load test need to run separatly")]
        [Test, Order(2)]
        public void PerformanceTestFor70Components()
        {
            //Arrange
            string packageJsonPath = $"{OutFolderPath}\\..\\..\\TestFiles\\NPMTestFile\\ProjectTestData\\SDBBackend";
            string bomPath = $"{OutFolderPath}\\..\\BOMs";
            string combomJsonPath = $"{OutFolderPath}\\..\\BOMs\\Test_ComparisonBOM.json";

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
