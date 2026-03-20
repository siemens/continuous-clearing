// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using CycloneDX.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security;

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

        [Test]
        public void ExecuteFileOperation_WhenActionThrowsUnauthorizedAccessException_ReturnsFailure()
        {
            //Arrange
            string tempDir = Path.GetTempPath();
            string projectName = "ROProject";
            string fileName = "ro_exec_test.txt";
            string fullPath = Path.Combine(tempDir, $"{projectName}_{fileName}");

            // Pre-create the target file and mark it read-only so File.WriteAllText throws
            // UnauthorizedAccessException. WriteContentToReportNotApprovedFile is used because
            // it writes directly without a backup step, giving a clean trigger path.
            File.WriteAllText(fullPath, "existing content");
            File.SetAttributes(fullPath, FileAttributes.ReadOnly);

            var fileOperations = new FileOperations();

            try
            {
                //Act
                string result = fileOperations.WriteContentToReportNotApprovedFile(
                    "new data", tempDir, fileName, projectName);

                //Assert
                Assert.AreEqual("failure", result);
            }
            finally
            {
                File.SetAttributes(fullPath, FileAttributes.Normal);
                File.Delete(fullPath);
            }
        }

        [Test]
        public void ExecuteFileOperation_WhenActionThrowsSecurityException_ReturnsFailure()
        {
            //Arrange
            // SecurityException cannot be raised via normal file I/O on .NET 8, so the private
            // static method is invoked directly through reflection with a throwing delegate.
            var method = typeof(FileOperations).GetMethod(
                "ExecuteFileOperation",
                BindingFlags.Static | BindingFlags.NonPublic);

            Action throwingAction = () => throw new SecurityException("Access denied");

            //Act
            var result = method.Invoke(null,
                new object[] { "description", "methodName", "context", throwingAction });

            //Assert
            Assert.AreEqual("failure", result);
        }

        #region UpdateCompositions Tests

        [Test]
        public void UpdateCompositions_WhenSourceCompositionsIsNull_TargetCompositionsUnchanged()
        {
            //Arrange
            var targetComposition = new Composition { Assemblies = new List<string> { "asm1" } };
            Bom components = new Bom { Compositions = null };
            Bom comparisonData = new Bom { Compositions = new List<Composition> { targetComposition } };

            //Act
            InvokeUpdateCompositions(components, comparisonData);

            //Assert  early return: target is not touched
            Assert.AreEqual(1, comparisonData.Compositions.Count);
            Assert.AreEqual("asm1", comparisonData.Compositions[0].Assemblies[0]);
        }

        [Test]
        public void UpdateCompositions_WhenSourceCompositionsIsEmpty_TargetCompositionsUnchanged()
        {
            //Arrange
            var targetComposition = new Composition { Assemblies = new List<string> { "asm1" } };
            Bom components = new Bom { Compositions = new List<Composition>() };
            Bom comparisonData = new Bom { Compositions = new List<Composition> { targetComposition } };

            //Act
            InvokeUpdateCompositions(components, comparisonData);

            //Assert  early return: target is not touched
            Assert.AreEqual(1, comparisonData.Compositions.Count);
        }

        [Test]
        public void UpdateCompositions_WhenTargetCompositionsIsNull_AssignsSourceCompositions()
        {
            //Arrange
            var sourceComposition = new Composition { Assemblies = new List<string> { "asm1" } };
            Bom components = new Bom { Compositions = new List<Composition> { sourceComposition } };
            Bom comparisonData = new Bom { Compositions = null };

            //Act
            InvokeUpdateCompositions(components, comparisonData);

            //Assert
            Assert.AreEqual(1, comparisonData.Compositions.Count);
            Assert.AreEqual("asm1", comparisonData.Compositions[0].Assemblies[0]);
        }

        [Test]
        public void UpdateCompositions_WhenTargetCompositionsIsEmpty_AssignsSourceCompositions()
        {
            //Arrange
            var sourceComposition = new Composition { Assemblies = new List<string> { "asm1" } };
            Bom components = new Bom { Compositions = new List<Composition> { sourceComposition } };
            Bom comparisonData = new Bom { Compositions = new List<Composition>() };

            //Act
            InvokeUpdateCompositions(components, comparisonData);

            //Assert
            Assert.AreEqual(1, comparisonData.Compositions.Count);
            Assert.AreEqual("asm1", comparisonData.Compositions[0].Assemblies[0]);
        }

        [Test]
        public void UpdateCompositions_WhenMatchingCompositionFound_MergesNewDependenciesIntoTarget()
        {
            //Arrange  same assembly key  MergeDependencies path
            var sourceComposition = new Composition
            {
                Assemblies = new List<string> { "asm1" },
                Dependencies = new List<string> { "dep2" }
            };
            var targetComposition = new Composition
            {
                Assemblies = new List<string> { "asm1" },
                Dependencies = new List<string> { "dep1" }
            };
            Bom components = new Bom { Compositions = new List<Composition> { sourceComposition } };
            Bom comparisonData = new Bom { Compositions = new List<Composition> { targetComposition } };

            //Act
            InvokeUpdateCompositions(components, comparisonData);

            //Assert  both deps present in the matched target composition
            Assert.AreEqual(1, comparisonData.Compositions.Count);
            Assert.That(comparisonData.Compositions[0].Dependencies, Does.Contain("dep1"));
            Assert.That(comparisonData.Compositions[0].Dependencies, Does.Contain("dep2"));
        }

        [Test]
        public void UpdateCompositions_WhenNoMatchingCompositionFound_AddsSourceCompositionToTarget()
        {
            //Arrange  different assembly keys  no match  add path
            var sourceComposition = new Composition { Assemblies = new List<string> { "asm2" } };
            var targetComposition = new Composition { Assemblies = new List<string> { "asm1" } };
            Bom components = new Bom { Compositions = new List<Composition> { sourceComposition } };
            Bom comparisonData = new Bom { Compositions = new List<Composition> { targetComposition } };

            //Act
            InvokeUpdateCompositions(components, comparisonData);

            //Assert  source composition appended
            Assert.AreEqual(2, comparisonData.Compositions.Count);
        }

        #endregion

        #region FindMatchingComposition Tests

        [Test]
        public void FindMatchingComposition_WhenAssembliesMatch_ReturnsMatchingComposition()
        {
            //Arrange
            var findMethod = GetPrivateStaticMethod("FindMatchingComposition");
            var target = new Composition { Assemblies = new List<string> { "asm1" } };
            var source = new Composition { Assemblies = new List<string> { "asm1" } };

            //Act
            var result = findMethod.Invoke(null, new object[] { new List<Composition> { target }, source }) as Composition;

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("asm1", result.Assemblies[0]);
        }

        [Test]
        public void FindMatchingComposition_WhenAssembliesDoNotMatch_ReturnsNull()
        {
            //Arrange
            var findMethod = GetPrivateStaticMethod("FindMatchingComposition");
            var target = new Composition { Assemblies = new List<string> { "asm1" } };
            var source = new Composition { Assemblies = new List<string> { "asm2" } };

            //Act
            var result = findMethod.Invoke(null, new object[] { new List<Composition> { target }, source }) as Composition;

            //Assert
            Assert.IsNull(result);
        }

        [Test]
        public void FindMatchingComposition_WhenTargetAssembliesIsNull_ReturnsNull()
        {
            //Arrange  c.Assemblies != null is false  predicate short-circuits
            var findMethod = GetPrivateStaticMethod("FindMatchingComposition");
            var target = new Composition { Assemblies = null };
            var source = new Composition { Assemblies = new List<string> { "asm1" } };

            //Act
            var result = findMethod.Invoke(null, new object[] { new List<Composition> { target }, source }) as Composition;

            //Assert
            Assert.IsNull(result);
        }

        [Test]
        public void FindMatchingComposition_WhenSourceAssembliesIsNull_ReturnsNull()
        {
            //Arrange  sourceComposition.Assemblies != null is false  predicate short-circuits
            var findMethod = GetPrivateStaticMethod("FindMatchingComposition");
            var target = new Composition { Assemblies = new List<string> { "asm1" } };
            var source = new Composition { Assemblies = null };

            //Act
            var result = findMethod.Invoke(null, new object[] { new List<Composition> { target }, source }) as Composition;

            //Assert
            Assert.IsNull(result);
        }

        #endregion

        #region MergeDependencies Tests

        [Test]
        public void MergeDependencies_WhenSourceDependenciesIsNull_TargetDependenciesUnchanged()
        {
            //Arrange
            var mergeMethod = GetPrivateStaticMethod("MergeDependencies");
            var source = new Composition { Dependencies = null };
            var target = new Composition { Dependencies = new List<string> { "dep1" } };

            //Act
            mergeMethod.Invoke(null, new object[] { source, target });

            //Assert  early return: target unchanged
            Assert.AreEqual(1, target.Dependencies.Count);
            Assert.AreEqual("dep1", target.Dependencies[0]);
        }

        [Test]
        public void MergeDependencies_WhenSourceDependenciesIsEmpty_TargetDependenciesUnchanged()
        {
            //Arrange
            var mergeMethod = GetPrivateStaticMethod("MergeDependencies");
            var source = new Composition { Dependencies = new List<string>() };
            var target = new Composition { Dependencies = new List<string> { "dep1" } };

            //Act
            mergeMethod.Invoke(null, new object[] { source, target });

            //Assert  early return: target unchanged
            Assert.AreEqual(1, target.Dependencies.Count);
            Assert.AreEqual("dep1", target.Dependencies[0]);
        }

        [Test]
        public void MergeDependencies_WhenTargetDependenciesIsNull_InitializesListAndAddsAll()
        {
            //Arrange  target.Dependencies ??= new List<string>() path
            var mergeMethod = GetPrivateStaticMethod("MergeDependencies");
            var source = new Composition { Dependencies = new List<string> { "dep1", "dep2" } };
            var target = new Composition { Dependencies = null };

            //Act
            mergeMethod.Invoke(null, new object[] { source, target });

            //Assert
            Assert.IsNotNull(target.Dependencies);
            Assert.AreEqual(2, target.Dependencies.Count);
            Assert.That(target.Dependencies, Does.Contain("dep1"));
            Assert.That(target.Dependencies, Does.Contain("dep2"));
        }

        [Test]
        public void MergeDependencies_WhenDuplicateDependenciesExist_AddsOnlyUnique()
        {
            //Arrange  dep1 already in target; only dep2 is new
            var mergeMethod = GetPrivateStaticMethod("MergeDependencies");
            var source = new Composition { Dependencies = new List<string> { "dep1", "dep2" } };
            var target = new Composition { Dependencies = new List<string> { "dep1" } };

            //Act
            mergeMethod.Invoke(null, new object[] { source, target });

            //Assert  dep1 not duplicated
            Assert.AreEqual(2, target.Dependencies.Count);
            Assert.That(target.Dependencies, Does.Contain("dep1"));
            Assert.That(target.Dependencies, Does.Contain("dep2"));
        }

        [Test]
        public void MergeDependencies_WhenAllDependenciesAreNew_AddsAllToTarget()
        {
            //Arrange  no overlap between source and target
            var mergeMethod = GetPrivateStaticMethod("MergeDependencies");
            var source = new Composition { Dependencies = new List<string> { "dep2", "dep3" } };
            var target = new Composition { Dependencies = new List<string> { "dep1" } };

            //Act
            mergeMethod.Invoke(null, new object[] { source, target });

            //Assert
            Assert.AreEqual(3, target.Dependencies.Count);
            Assert.That(target.Dependencies, Does.Contain("dep1"));
            Assert.That(target.Dependencies, Does.Contain("dep2"));
            Assert.That(target.Dependencies, Does.Contain("dep3"));
        }

        #endregion

        //  helpers 

        /// <summary>
        /// Resolves any private static method on <see cref="FileOperations"/> by name.
        /// </summary>
        private static MethodInfo GetPrivateStaticMethod(string name) =>
            typeof(FileOperations).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;

        /// <summary>
        /// Invokes the private static <c>UpdateCompositions</c> method via reflection.
        /// The method uses <c>ref</c> parameters but only modifies object properties
        /// (never reassigns the references), so callers can inspect the original objects directly.
        /// </summary>
        private static void InvokeUpdateCompositions(Bom components, Bom comparisonData)
        {
            var method = GetPrivateStaticMethod("UpdateCompositions");
            var parameters = new object[] { components, comparisonData };
            method.Invoke(null, parameters);
        }
    }
}