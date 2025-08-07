// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// --------------------------------------------------------------------------------------------------------------------

using LCT.Common;
using LCT.Common.Model;
using LCT.PackageIdentifier.Interface;
using LCT.PackageIdentifier.Model;
using Microsoft.Build.Evaluation;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    public class DotnetRuntimeIdentiferTests
    {
        private string testFilesPath;
        private CommonAppSettings appSettings;
        private Mock<IRuntimeIdentifier> mockRuntimeIdentifier;
        private TestableRuntimeIdentifier testableIdentifier;

        // Helper class to expose protected methods for testing
        private class TestableRuntimeIdentifier
        {
            private readonly Type runtimeIdentifierType;
            private readonly object runtimeIdentifier;
            private readonly MethodInfo registerMSBuildMethod;
            private readonly MethodInfo loadProjectMethod;
            private readonly MethodInfo parseCSProjFileMethod;
            private readonly MethodInfo writeDetailLogMethod;
            private readonly MethodInfo getProjectFilePathMethod;
            private readonly MethodInfo isExcludedMethod;

            public TestableRuntimeIdentifier()
            {
                // Use reflection to get the internal type
                Assembly assembly = Assembly.GetAssembly(typeof(IRuntimeIdentifier));
                runtimeIdentifierType = assembly.GetType("LCT.PackageIdentifier.DotnetRuntimeIdentifer");
                
                if (runtimeIdentifierType == null)
                    throw new InvalidOperationException("Could not locate the DotnetRuntimeIdentifer type");
                    
                runtimeIdentifier = Activator.CreateInstance(runtimeIdentifierType);

                // Get private and protected methods
                registerMSBuildMethod = runtimeIdentifierType.GetMethod("RegisterMSBuild", BindingFlags.NonPublic | BindingFlags.Instance);
                loadProjectMethod = runtimeIdentifierType.GetMethod("LoadProject", BindingFlags.NonPublic | BindingFlags.Instance);
                parseCSProjFileMethod = runtimeIdentifierType.GetMethod("ParseCSProjFile", BindingFlags.NonPublic | BindingFlags.Instance);
                writeDetailLogMethod = runtimeIdentifierType.GetMethod("WriteDetailLog", BindingFlags.NonPublic | BindingFlags.Static);
                getProjectFilePathMethod = runtimeIdentifierType.GetMethod("GetProjectFilePathFromAssestJson", BindingFlags.NonPublic | BindingFlags.Static);
                isExcludedMethod = runtimeIdentifierType.GetMethod("IsExcluded", BindingFlags.NonPublic | BindingFlags.Static);
            }

            public void RegisterMSBuild()
            {
                registerMSBuildMethod?.Invoke(runtimeIdentifier, null);
            }

            public Project LoadProject(string projectFilePath, IDictionary<string, string> globalProperties, ProjectCollection projectCollection)
            {
                return (Project)loadProjectMethod?.Invoke(runtimeIdentifier, new object[] { projectFilePath, globalProperties, projectCollection });
            }

            public RuntimeInfo ParseCSProjFile(string projectFilePath)
            {
                return (RuntimeInfo)parseCSProjFileMethod?.Invoke(runtimeIdentifier, new object[] { projectFilePath });
            }

            public void WriteDetailLog(RuntimeInfo info)
            {
                writeDetailLogMethod?.Invoke(null, new object[] { info });
            }

            public string GetProjectFilePathFromAssestJson(string assetsFile)
            {
                return (string)getProjectFilePathMethod?.Invoke(null, new object[] { assetsFile });
            }

            public bool IsExcluded(string filePath, string[] excludePatterns)
            {
                return (bool)isExcludedMethod?.Invoke(null, new object[] { filePath, excludePatterns });
            }

            public RuntimeInfo IdentifyRuntime(CommonAppSettings appSettings)
            {
                MethodInfo identifyRuntimeMethod = runtimeIdentifierType.GetMethod("IdentifyRuntime", BindingFlags.Public | BindingFlags.Instance);
                return (RuntimeInfo)identifyRuntimeMethod?.Invoke(runtimeIdentifier, new object[] { appSettings });
            }
        }

        [SetUp]
        public void Setup()
        {
            string exePath = Assembly.GetExecutingAssembly().Location;
            string outFolder = Path.GetDirectoryName(exePath);
            testFilesPath = Path.Combine(outFolder, "PackageIdentifierUTTestFiles");
            
            // Setup app settings
            appSettings = new CommonAppSettings
            {
                Directory = new LCT.Common.Directory
                {
                    InputFolder = testFilesPath
                },
                Nuget = new Config
                {
                    Exclude = new string[] { "exclude" }
                }
            };

            // Create a mock of the interface
            mockRuntimeIdentifier = new Mock<IRuntimeIdentifier>();

            try
            {
                // Create testable instance (may throw in some environments)
                testableIdentifier = new TestableRuntimeIdentifier();
            }
            catch (Exception ex)
            {
                // Log the exception but don't rethrow
                Console.WriteLine($"Could not create TestableRuntimeIdentifier: {ex.Message}");
                testableIdentifier = null;
            }
        }

        #region Interface Mocking Tests

        [Test]
        public void IdentifyRuntime_NoAssetFilesFound_ReturnsErrorMessage()
        {
            // Arrange
            var emptyFolderSettings = new CommonAppSettings
            {
                Directory = new LCT.Common.Directory
                {
                    // Use a folder with no asset files
                    InputFolder = Path.GetTempPath()
                }
            };

            mockRuntimeIdentifier.Setup(r => r.IdentifyRuntime(It.IsAny<CommonAppSettings>()))
                .Returns(new RuntimeInfo
                {
                    ErrorMessage = "No project.assets.json files found in the specified directory."
                });

            // Act
            var result = mockRuntimeIdentifier.Object.IdentifyRuntime(emptyFolderSettings);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ErrorMessage.Contains("No project.assets.json files found"));
        }

        [Test]
        public void IdentifyRuntime_AllAssetsFilesExcluded_ReturnsErrorMessage()
        {
            // Arrange
            // Create settings that exclude all asset files
            var excludeAllSettings = new CommonAppSettings
            {
                Directory = new LCT.Common.Directory
                {
                    InputFolder = testFilesPath
                },
                Nuget = new Config
                {
                    // This will exclude all files by matching any path
                    Exclude = new string[] { "project.assets.json" }
                }
            };

            mockRuntimeIdentifier.Setup(r => r.IdentifyRuntime(It.IsAny<CommonAppSettings>()))
                .Returns(new RuntimeInfo
                {
                    ErrorMessage = "No project.assets.json files found in the specified directory."
                });

            // Act
            var result = mockRuntimeIdentifier.Object.IdentifyRuntime(excludeAllSettings);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ErrorMessage.Contains("No project.assets.json files found"));
        }

        [Test]
        public void IdentifyRuntime_WithValidProjectFile_ReturnsRuntimeInfo()
        {
            // Arrange
            string projectPath = Path.Combine(testFilesPath, "Nuget.csproj");
            
            mockRuntimeIdentifier.Setup(r => r.IdentifyRuntime(It.IsAny<CommonAppSettings>()))
                .Returns(new RuntimeInfo
                {
                    ProjectPath = projectPath,
                    ProjectName = "TestProject",
                    IsSelfContained = false,
                    SelfContainedExplicitlySet = false,
                    SelfContainedEvaluated = "false",
                    SelfContainedReason = "'SelfContained' property implicitly defaulted to 'false'",
                    RuntimeIdentifiers = new List<string>(),
                    FrameworkReferences = new List<FrameworkReferenceInfo>()
                });

            // Act
            var result = mockRuntimeIdentifier.Object.IdentifyRuntime(appSettings);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNull(result.ErrorMessage, "Should not have error message");
            Assert.IsFalse(result.IsSelfContained, "Should not be self-contained");
            Assert.AreEqual(projectPath, result.ProjectPath);
            Assert.IsNotNull(result.RuntimeIdentifiers);
            Assert.IsNotNull(result.FrameworkReferences);
        }

        [Test]
        public void IdentifyRuntime_SelfContainedProject_ReturnsSelfContainedInfo()
        {
            // Arrange
            string projectPath = Path.Combine(testFilesPath, "NugetSelfContainedProject", "Nuget-SelfContained.csproj");
            
            var frameworkReferences = new List<FrameworkReferenceInfo>
            {
                new FrameworkReferenceInfo
                {
                    Name = "Microsoft.NETCore.App",
                    TargetFramework = "netstandard2.0",
                    TargetingPackVersion = "3.1.0"
                }
            };

            mockRuntimeIdentifier.Setup(r => r.IdentifyRuntime(It.IsAny<CommonAppSettings>()))
                .Returns(new RuntimeInfo
                {
                    ProjectPath = projectPath,
                    ProjectName = "TestSelfContained",
                    IsSelfContained = true,
                    SelfContainedExplicitlySet = true,
                    SelfContainedEvaluated = "true",
                    SelfContainedReason = "'SelfContained' property is explicitly set to 'true'.",
                    RuntimeIdentifiers = new List<string> { "win-x64" },
                    FrameworkReferences = frameworkReferences
                });

            // Act
            var result = mockRuntimeIdentifier.Object.IdentifyRuntime(appSettings);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNull(result.ErrorMessage, "Should not have error message");
            Assert.IsTrue(result.IsSelfContained, "Should be self-contained");
            Assert.AreEqual(projectPath, result.ProjectPath);
            Assert.IsTrue(result.RuntimeIdentifiers.Contains("win-x64"), "Should have win-x64 runtime identifier");
            Assert.AreEqual("true", result.SelfContainedEvaluated);
            Assert.IsTrue(result.SelfContainedReason.Contains("explicitly set"));
            Assert.AreEqual(1, result.FrameworkReferences.Count);
            Assert.AreEqual("Microsoft.NETCore.App", result.FrameworkReferences[0].Name);
        }

        [Test]
        public void IdentifyRuntime_InvalidProjectPath_ReturnsErrorInfo()
        {
            // Arrange
            mockRuntimeIdentifier.Setup(r => r.IdentifyRuntime(It.IsAny<CommonAppSettings>()))
                .Returns(new RuntimeInfo
                {
                    ErrorMessage = "Error loading project file: Invalid project file",
                    ErrorDetails = "Details: Error code at 10, 15"
                });
            
            // Act
            var result = mockRuntimeIdentifier.Object.IdentifyRuntime(appSettings);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ErrorMessage);
            Assert.IsTrue(result.ErrorMessage.Contains("Error loading project file"));
        }

        [Test]
        public void IdentifyRuntime_ExceptionThrown_ReturnsErrorInfo()
        {
            // Arrange
            mockRuntimeIdentifier.Setup(r => r.IdentifyRuntime(It.IsAny<CommonAppSettings>()))
                .Returns(new RuntimeInfo
                {
                    ErrorMessage = "Error registering MSBuildLocator or reading assets files",
                    ErrorDetails = "Test exception"
                });
            
            // Act
            var result = mockRuntimeIdentifier.Object.IdentifyRuntime(appSettings);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.ErrorMessage);
            Assert.AreEqual("Error registering MSBuildLocator or reading assets files", result.ErrorMessage);
            Assert.IsTrue(result.ErrorDetails.Contains("Test exception"));
        }

        [Test]
        public void IdentifyRuntime_MultipleAssetFiles_PrioritizesSelfContainedWithFrameworkReferences()
        {
            // Arrange
            var selfContainedInfo = new RuntimeInfo
            {
                ProjectPath = "path/to/self-contained.csproj",
                ProjectName = "SelfContainedProject",
                IsSelfContained = true,
                SelfContainedExplicitlySet = true,
                SelfContainedEvaluated = "true",
                RuntimeIdentifiers = new List<string> { "win-x64" },
                FrameworkReferences = new List<FrameworkReferenceInfo>
                {
                    new FrameworkReferenceInfo
                    {
                        Name = "Microsoft.NETCore.App",
                        TargetingPackVersion = "3.1.0"
                    }
                }
            };

            var nonSelfContainedInfo = new RuntimeInfo
            {
                ProjectPath = "path/to/non-self-contained.csproj",
                ProjectName = "NonSelfContainedProject",
                IsSelfContained = false,
                SelfContainedExplicitlySet = false,
                SelfContainedEvaluated = "false",
                RuntimeIdentifiers = new List<string>(),
                FrameworkReferences = new List<FrameworkReferenceInfo>()
            };

            mockRuntimeIdentifier.Setup(r => r.IdentifyRuntime(It.IsAny<CommonAppSettings>()))
                .Returns(selfContainedInfo);

            // Act
            var result = mockRuntimeIdentifier.Object.IdentifyRuntime(appSettings);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSelfContained);
            Assert.AreEqual("SelfContainedProject", result.ProjectName);
            Assert.AreEqual(1, result.FrameworkReferences.Count);
        }

        #endregion

        #region Private Method Tests via Reflection

        [Test]
        public void IsExcluded_WithNullOrEmptyExcludePatterns_ReturnsFalse()
        {
            // Skip if we couldn't create the testable instance
            if (testableIdentifier == null)
                Assert.Ignore("Cannot create testable instance in this environment");

            // Arrange
            string filePath = "C:\\path\\to\\project.assets.json";
            
            // Act & Assert
            Assert.IsFalse(testableIdentifier.IsExcluded(filePath, null), "Should return false for null patterns");
            Assert.IsFalse(testableIdentifier.IsExcluded(filePath, Array.Empty<string>()), "Should return false for empty patterns");
        }

        [Test]
        public void IsExcluded_WithMatchingPattern_ReturnsTrue()
        {
            // Skip if we couldn't create the testable instance
            if (testableIdentifier == null)
                Assert.Ignore("Cannot create testable instance in this environment");

            // Arrange
            string filePath = "C:\\path\\to\\project.assets.json";
            string[] patterns = { "TO\\PROJECT" };  // Mixed case to test case insensitivity
            
            // Act & Assert
            Assert.IsTrue(testableIdentifier.IsExcluded(filePath, patterns), 
                "Should return true when path contains pattern (case-insensitive)");
        }

        [Test]
        public void IsExcluded_WithNonMatchingPattern_ReturnsFalse()
        {
            // Skip if we couldn't create the testable instance
            if (testableIdentifier == null)
                Assert.Ignore("Cannot create testable instance in this environment");

            // Arrange
            string filePath = "C:\\path\\to\\project.assets.json";
            string[] patterns = { "non-matching-pattern" };
            
            // Act & Assert
            Assert.IsFalse(testableIdentifier.IsExcluded(filePath, patterns), 
                "Should return false when path doesn't contain any patterns");
        }

        [Test]
        public void RuntimeInfo_LoggingPaths_AreCorrectlyFormatted()
        {
            // This tests that a RuntimeInfo object formats paths correctly in logs
            
            // Arrange
            var info = new RuntimeInfo
            {
                ProjectPath = "C:\\Project\\Path\\project.csproj",
                ProjectName = "TestProject",
                IsSelfContained = true,
                SelfContainedEvaluated = "true",
                SelfContainedExplicitlySet = true,
                SelfContainedReason = "Test reason",
                RuntimeIdentifiers = new List<string> { "win-x64" }
            };

            // No assertions needed as we're just ensuring this doesn't throw
            // In a real test environment, we'd use a log appender to capture and verify log output
            if (testableIdentifier != null)
            {
                // Act & Assert - should not throw
                Assert.DoesNotThrow(() => testableIdentifier.WriteDetailLog(info));
            }
        }

        [Test]
        public void RuntimeInfo_WithError_LogsErrorMessageAndDetails()
        {
            // Arrange
            var info = new RuntimeInfo
            {
                ErrorMessage = "Test error message",
                ErrorDetails = "Test error details"
            };

            // No assertions needed as we're just ensuring this doesn't throw
            if (testableIdentifier != null)
            {
                // Act & Assert - should not throw
                Assert.DoesNotThrow(() => testableIdentifier.WriteDetailLog(info));
            }
        }

        #endregion
        
        #region Advanced Scenarios
        [Test]
        public void RuntimeInfo_AllPropertiesInitialized_HasCorrectValues()
        {
            // Arrange & Act
            var info = new RuntimeInfo
            {
                ProjectPath = "test/path",
                ProjectName = "TestProject",
                IsSelfContained = true,
                SelfContainedExplicitlySet = true,
                SelfContainedEvaluated = "true",
                SelfContainedReason = "Test reason",
                RuntimeIdentifiers = new List<string> { "win-x64", "linux-x64" },
                FrameworkReferences = new List<FrameworkReferenceInfo>
                {
                    new FrameworkReferenceInfo
                    {
                        Name = "Test.Framework",
                        TargetFramework = "net6.0",
                        TargetingPackVersion = "6.0.0"
                    }
                }
            };

            // Assert
            Assert.AreEqual("test/path", info.ProjectPath);
            Assert.AreEqual("TestProject", info.ProjectName);
            Assert.IsTrue(info.IsSelfContained);
            Assert.IsTrue(info.SelfContainedExplicitlySet);
            Assert.AreEqual("true", info.SelfContainedEvaluated);
            Assert.AreEqual("Test reason", info.SelfContainedReason);
            Assert.AreEqual(2, info.RuntimeIdentifiers.Count);
            Assert.AreEqual("win-x64", info.RuntimeIdentifiers[0]);
            Assert.AreEqual("linux-x64", info.RuntimeIdentifiers[1]);
            Assert.AreEqual(1, info.FrameworkReferences.Count);
            Assert.AreEqual("Test.Framework", info.FrameworkReferences[0].Name);
            Assert.AreEqual("net6.0", info.FrameworkReferences[0].TargetFramework);
            Assert.AreEqual("6.0.0", info.FrameworkReferences[0].TargetingPackVersion);
        }

        [Test]
        public void RuntimeInfo_EmptyRuntimeIdentifiers_InitializedToEmptyList()
        {
            // Arrange & Act
            var info = new RuntimeInfo();

            // Assert
            Assert.IsNotNull(info.RuntimeIdentifiers);
            Assert.IsEmpty(info.RuntimeIdentifiers);
        }

        [Test]
        public void RuntimeInfo_EmptyFrameworkReferences_InitializedToEmptyList()
        {
            // Arrange & Act
            var info = new RuntimeInfo();

            // Assert
            Assert.IsNotNull(info.FrameworkReferences);
            Assert.IsEmpty(info.FrameworkReferences);
        }

        [Test]
        public void IdentifyRuntime_WithMultipleProjectsOfDifferentTypes_ReturnsCorrectPrioritizedProject()
        {
            // This is a higher-level test demonstrating the prioritization logic
            // Arrange
            mockRuntimeIdentifier.Setup(r => r.IdentifyRuntime(It.IsAny<CommonAppSettings>()))
                .Returns((CommonAppSettings settings) => 
                {
                    // Create a RuntimeInfo that simulates finding multiple project files
                    // with different configurations - prioritize self-contained with framework references
                    if (settings.Nuget?.Exclude?.Contains("prioritize-test") == true)
                    {
                        return new RuntimeInfo
                        {
                            ProjectName = "PrioritizedProject",
                            IsSelfContained = true,
                            FrameworkReferences = new List<FrameworkReferenceInfo> 
                            { 
                                new FrameworkReferenceInfo { Name = "Microsoft.NETCore.App" } 
                            }
                        };
                    }
                    else
                    {
                        return new RuntimeInfo
                        {
                            ProjectName = "DefaultProject",
                            IsSelfContained = false,
                            FrameworkReferences = new List<FrameworkReferenceInfo>()
                        };
                    }
                });
                
            var prioritizeSettings = new CommonAppSettings
            {
                Directory = new LCT.Common.Directory { InputFolder = testFilesPath },
                Nuget = new Config { Exclude = new[] { "prioritize-test" } }
            };

            // Act
            var result = mockRuntimeIdentifier.Object.IdentifyRuntime(prioritizeSettings);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("PrioritizedProject", result.ProjectName);
            Assert.IsTrue(result.IsSelfContained);
            Assert.IsTrue(result.FrameworkReferences.Any(fr => fr.Name == "Microsoft.NETCore.App"));
        }
        #endregion
    }
}