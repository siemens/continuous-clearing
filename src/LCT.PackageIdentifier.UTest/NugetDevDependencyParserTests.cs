// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using LCT.PackageIdentifier.Model.NugetModel;
using NuGet.ProjectModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    public class NugetDevDependencyParserTests
    {
        [SetUp]
        public void SetUp()
        {
            NugetDevDependencyParser.ClearDirectDependencies();
        }

        [Test]
        public void NugetDirectDependencies_InitiallyEmpty()
        {
            Assert.IsNotNull(NugetDevDependencyParser.NugetDirectDependencies);
            Assert.IsEmpty(NugetDevDependencyParser.NugetDirectDependencies);
        }

        [Test]
        public void AddDirectDependency_AddsDependency()
        {
            NugetDevDependencyParser.AddDirectDependency("Test.Dependency 1.0.0");
            Assert.Contains("Test.Dependency 1.0.0", (System.Collections.ICollection)NugetDevDependencyParser.NugetDirectDependencies);
        }

        [Test]
        public void AddDirectDependency_DoesNotAddDuplicate()
        {
            NugetDevDependencyParser.AddDirectDependency("Test.Dependency 1.0.0");
            NugetDevDependencyParser.AddDirectDependency("Test.Dependency 1.0.0");
            Assert.AreEqual(1, NugetDevDependencyParser.NugetDirectDependencies.Count);
        }

        [Test]
        public void AddRangeDirectDependencies_AddsMultiple()
        {
            var deps = new List<string> { "Dep1 1.0.0", "Dep2 2.0.0" };
            NugetDevDependencyParser.AddRangeDirectDependencies(deps);
            CollectionAssert.IsSubsetOf(deps, NugetDevDependencyParser.NugetDirectDependencies);
        }

        [Test]
        public void ClearDirectDependencies_RemovesAll()
        {
            NugetDevDependencyParser.AddDirectDependency("Dep1 1.0.0");
            NugetDevDependencyParser.ClearDirectDependencies();
            Assert.IsEmpty(NugetDevDependencyParser.NugetDirectDependencies);
        }

        [Test]
        public void SetDirectDependencies_SetsDependencies()
        {
            var deps = new List<string> { "DepA 1.0.0", "DepB 2.0.0" };
            NugetDevDependencyParser.SetDirectDependencies(deps);
            CollectionAssert.AreEqual(deps, NugetDevDependencyParser.NugetDirectDependencies);
        }

        [Test]
        public void Instance_ReturnsSingleton()
        {
            var instance1 = NugetDevDependencyParser.Instance;
            var instance2 = NugetDevDependencyParser.Instance;
            Assert.IsNotNull(instance1);
            Assert.AreSame(instance1, instance2);
        }

        [Test]
        public void IsDevDependecy_ReturnsTrueIfAllCollectionsEmpty()
        {
            var library = new LockFileTargetLibrary
            {
                Name = "Test",
                Version = new NuGet.Versioning.NuGetVersion("1.0.0"),
                CompileTimeAssemblies = { },
                ContentFiles = { },
                EmbedAssemblies = { },
                FrameworkAssemblies = { },
                NativeLibraries = { },
                ResourceAssemblies = { },
                ToolsAssemblies = { }
            };
            var method = typeof(NugetDevDependencyParser).GetMethod("IsDevDependecy", BindingFlags.NonPublic | BindingFlags.Static);
            bool result = (bool)method.Invoke(null, new object[] { library });
            Assert.IsTrue(result);
        }

        [Test]
        public void IsDevDependecy_ReturnsFalseIfAnyCollectionNotEmpty()
        {
            var library = new LockFileTargetLibrary
            {
                Name = "Test",
                Version = new NuGet.Versioning.NuGetVersion("1.0.0"),
                CompileTimeAssemblies = { new NuGet.ProjectModel.LockFileItem("a.dll") }
            };
            var method = typeof(NugetDevDependencyParser).GetMethod("IsDevDependecy", BindingFlags.NonPublic | BindingFlags.Static);
            bool result = (bool)method.Invoke(null, new object[] { library });
            Assert.IsFalse(result);
        }

        [Test]
        public void IsTestProject_ReturnsFalseOnInvalidFile()
        {
            var method = typeof(NugetDevDependencyParser).GetMethod("IsTestProject", BindingFlags.NonPublic | BindingFlags.Static);
            bool result = (bool)method.Invoke(null, new object[] { "nonexistent.csproj" });
            Assert.IsFalse(result);
        }

        [Test]
        public void IsTestProject_ReturnsTrueIfTestContainerPresent()
        {
            string tempFile = Path.GetTempFileName();
            string csprojContent = "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><ProjectCapability Include=\"TestContainer\" /></ItemGroup></Project>";
            File.WriteAllText(tempFile, csprojContent);
            var method = typeof(NugetDevDependencyParser).GetMethod("IsTestProject", BindingFlags.NonPublic | BindingFlags.Static);
            bool result = (bool)method.Invoke(null, new object[] { tempFile });
            Assert.IsTrue(result);
            File.Delete(tempFile);
        }

        [Test]
        public void IsTestProject_ReturnsFalseIfTestContainerNotPresent()
        {
            string tempFile = Path.GetTempFileName();
            string csprojContent = "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><ProjectCapability Include=\"NotTestContainer\" /></ItemGroup></Project>";
            File.WriteAllText(tempFile, csprojContent);
            var method = typeof(NugetDevDependencyParser).GetMethod("IsTestProject", BindingFlags.NonPublic | BindingFlags.Static);
            bool result = (bool)method.Invoke(null, new object[] { tempFile });
            Assert.IsFalse(result);
            File.Delete(tempFile);
        }

        [Test]
        public void GetDirectDependencies_AddsDependenciesFromJson()
        {
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "{\"project\":{\"frameworks\":{\"net8.0\":{\"dependencies\":{\"Dep1\":{\"version\":\"1.0.0\"}}}}}}");
            var method = typeof(NugetDevDependencyParser).GetMethod("GetDirectDependencies", BindingFlags.NonPublic | BindingFlags.Static);
            method.Invoke(null, new object[] { tempFile });
            Assert.Contains("Dep1 1.0.0", (System.Collections.ICollection)NugetDevDependencyParser.NugetDirectDependencies);
            File.Delete(tempFile);
        }

        [Test]
        public void GetDirectDependencies_DoesNothingIfNoFrameworks()
        {
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "{\"project\":{}}\n");
            var method = typeof(NugetDevDependencyParser).GetMethod("GetDirectDependencies", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.DoesNotThrow(() => method.Invoke(null, new object[] { tempFile }));
            File.Delete(tempFile);
        }

        [Test]
        public void ParseJsonInContainer_ReturnsFalseIfNoCsproj()
        {
            var container = new Container();
            var method = typeof(NugetDevDependencyParser).GetMethod("ParseJsonInContainer", BindingFlags.NonPublic | BindingFlags.Static);
            bool result = (bool)method.Invoke(null, new object[] { "nonexistent.json", container });
            Assert.IsFalse(result);
        }

        [Test]
        public void ParseJsonInContainer_ReturnsTrueIfCsprojPresent()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            string csprojPath = Path.Combine(tempDir, "test.csproj");
            File.WriteAllText(csprojPath, "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><ProjectCapability Include=\"TestContainer\" /></ItemGroup></Project>");
            string fakeJson = Path.Combine(tempDir, "obj", "project.assets.json");
            Directory.CreateDirectory(Path.GetDirectoryName(fakeJson));
            File.WriteAllText(fakeJson, "{}\n");
            var container = new Container();
            var method = typeof(NugetDevDependencyParser).GetMethod("ParseJsonInContainer", BindingFlags.NonPublic | BindingFlags.Static);
            bool result = (bool)method.Invoke(null, new object[] { fakeJson, container });
            Assert.IsTrue(result);
            Assert.AreEqual("test.csproj", container.Name);
            Directory.Delete(tempDir, true);
        }

        [Test]
        public void GetFileHash_ReturnsNullIfFileNotExists()
        {
            var method = typeof(NugetDevDependencyParser).GetMethod("GetFileHash", BindingFlags.NonPublic | BindingFlags.Static);
            string result = (string)method.Invoke(null, new object[] { "nonexistent.nupkg", SHA256.Create() });
            Assert.IsNull(result);
        }

        [Test]
        public void GetFileHash_ReturnsHashIfFileExists()
        {
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "test content");
            var method = typeof(NugetDevDependencyParser).GetMethod("GetFileHash", BindingFlags.NonPublic | BindingFlags.Static);
            string hash = (string)method.Invoke(null, new object[] { tempFile, SHA256.Create() });
            Assert.IsNotNull(hash);
            Assert.AreEqual(64, hash.Length); // SHA256 hash length in hex
            File.Delete(tempFile);
        }
    }
}
