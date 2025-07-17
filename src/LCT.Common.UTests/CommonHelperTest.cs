// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.Common.Constants;
using LCT.Common.Interface;
using LCT.Common.Model;
using log4net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using File = System.IO.File;

namespace LCT.Common.UTest
{
    [TestFixture]
    public class CommonHelperTest
    {
        private string tempDir;
        private string tempLogFile;
        private CommonAppSettings appSettings;
        [SetUp]
        public void SetUp()
        {
            tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            System.IO.Directory.CreateDirectory(tempDir);

            tempLogFile = Path.Combine(tempDir, "catool.log");
            File.WriteAllText(tempLogFile, "test log content");            
            appSettings = new CommonAppSettings()
            {
                Directory = new Directory()
                {
                    LogFolder = tempDir // Mocked LogFolder
                }
            };

            // Set the static log path for the test
            Log4Net.CatoolLogPath = tempLogFile;
            CommonHelper.DefaultLogPath = "default";
        }

        [TearDown]
        public void TearDown()
        {
            LogManager.Shutdown();
            if (System.IO.Directory.Exists(tempDir))
                System.IO.Directory.Delete(tempDir, true);
        }
        [Test]
        public void LogFolderInitialisation_WhenLogFileExists_CopiesLogAndReturnsFolder()
        {
            // Act
            string result = CommonHelper.LogFolderInitialisation(appSettings, "catool.log", false);

            // Assert
            Assert.AreEqual(tempDir, result);
            Assert.IsTrue(File.Exists(tempLogFile));
        }
        [Test]
        public void LogFolderInitialisation_WhenLogFolderIsNull_ReturnsDefaultLogPath()
        {
            Log4Net.CatoolLogPath = "C:\\catool\\fds.log";

            // Act
            string result = CommonHelper.LogFolderInitialisation(appSettings, "catool.log", false);

            // Assert
            Assert.IsNotEmpty(result);
        }

        [Test]
        public void WriteComponentsNotLinkedListInConsole_PassingList_ReturnSuccess()
        {
            //Arrange
            List<Components> ComponentsNotLinked = new List<Components>();
            ComponentsNotLinked.Add(new Components());

            //Act
            CommonHelper.WriteComponentsNotLinkedListInConsole(ComponentsNotLinked);

            //Assert
            Assert.Pass();
        }

        [Test]
        public void RemoveExcludedComponents_PassingList_ReturnSuccess()
        {
            //Arrange
            List<Component> ComponentsForBom = new List<Component>();
            ComponentsForBom.Add(new Component() { Name = "Name", Version = "12" });
            int noOfExcludedComponents = 0;

            List<string> list = new List<string>();
            list.Add("Debian:Debian");

            //Act
            List<Component> result = CommonHelper.RemoveExcludedComponents(ComponentsForBom, list, ref noOfExcludedComponents);

            //Assert
            Assert.IsTrue(result.Count > 0);
        }

        [Test]
        public void RemoveMultipleExcludedComponents_ReturnSuccess()
        {
            //Arrange
            List<Component> ComponentsForBom = new List<Component>();
            ComponentsForBom.Add(new Component() { Name = "Debian", Version = "3.1.0", Purl = "pkg:npm/Debian@3.1.0", Properties = new List<Property>() });
            ComponentsForBom.Add(new Component() { Name = "Debian", Version = "3.1.1", Purl = "pkg:npm/Debian@3.1.1", Properties = new List<Property>() });
            ComponentsForBom.Add(new Component() { Name = "Debian", Version = "3.1.2", Purl = "pkg:npm/Debian@3.1.2", Properties = new List<Property>() });
            ComponentsForBom.Add(new Component() { Name = "Newton", Version = "3.1.3", Purl = "pkg:npm/Newton@3.1.3", Properties = new List<Property>() });
            ComponentsForBom.Add(new Component() { Name = "Log4t", Version = "3.1.4", Purl = "pkg:npm/Log4t@3.1.4", Properties = new List<Property>() });
            ComponentsForBom.Add(new Component() { Name = "Log4t", Version = "3.1.5", Purl = "pkg:npm/Log4t@3.1.5", Properties = new List<Property>() });

            int noOfExcludedComponents = 0;

            List<string> list = new List<string>();
            list.Add("Debian:*");
            list.Add("Newton:3.1.3");
            list.Add("pkg:npm/Log4t@3.1.5");

            //Act
            CommonHelper.RemoveExcludedComponents(ComponentsForBom, list, ref noOfExcludedComponents);

            //Assert            
            Assert.That(noOfExcludedComponents, Is.EqualTo(5), "Returns the count of excluded components");

        }

        [Test]
        public void GetDetailsforManuallyAdded_PassingList_ReturnSuccess()
        {
            //Arrange
            List<Component> componentsForBOM = new List<Component>();
            componentsForBOM.Add(new Component() { Name = "Component1", Version = "1.0" });
            componentsForBOM.Add(new Component() { Name = "Component2", Version = "2.0" });
            List<Component> listComponentForBOM = new List<Component>();
            string filePath = "";
            //Act
            CommonHelper.GetDetailsForManuallyAdded(componentsForBOM, listComponentForBOM, filePath);

            //Assert
            Assert.AreEqual(2, listComponentForBOM.Count);
            Assert.AreEqual("Component1", listComponentForBOM[0].Name);
            Assert.AreEqual("1.0", listComponentForBOM[0].Version);
            Assert.AreEqual("Component2", listComponentForBOM[1].Name);
            Assert.AreEqual("2.0", listComponentForBOM[1].Version);
        }

        [Test]
        public void CheckNullOrEmpty_WhenValueIsNull_ThrowsArgumentException()
        {
            // Arrange
            string name = "TestName";
            string value = null;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => CommonHelper.CheckNullOrEmpty(name, value));
        }

        [Test]
        public void CheckNullOrEmpty_WhenValueIsEmpty_ThrowsArgumentException()
        {
            // Arrange
            string name = "TestName";
            string value = "";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => CommonHelper.CheckNullOrEmpty(name, value));
        }

        [Test]
        public void CheckNullOrEmpty_WhenValueIsWhiteSpace_ThrowsArgumentException()
        {
            // Arrange
            string name = "TestName";
            string value = "   ";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => CommonHelper.CheckNullOrEmpty(name, value));
        }

        [Test]
        public void CheckNullOrEmpty_WhenValueIsNotNullOrEmpty_DoesNotThrowException()
        {
            // Arrange
            string name = "TestName";
            string value = "TestValue";

            // Act & Assert
            Assert.DoesNotThrow(() => CommonHelper.CheckNullOrEmpty(name, value));
        }
        [Test]
        public void Convert_WhenAttributeExists_ReturnsDisplayName()
        {
            // Arrange
            var objectValue = new TestObject();
            var nameOfProperty = "Property1";

            // Act
            var result = CommonHelper.Convert(objectValue, nameOfProperty);

            // Assert
            Assert.AreEqual("Display Name 1", result);
        }

        [Test]
        public void Convert_WhenAttributeDoesNotExist_ReturnsEmptyString()
        {
            // Arrange
            var objectValue = new TestObject();
            var nameOfProperty = "Property2";

            // Act
            var result = CommonHelper.Convert(objectValue, nameOfProperty);

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void TrimEndOfString_WhenSuffixToRemoveIsNull_ReturnsInputString()
        {
            // Arrange
            string input = "TestString";
            string suffixToRemove = null;

            // Act
            string result = CommonHelper.TrimEndOfString(input, suffixToRemove);

            // Assert
            Assert.AreEqual(input, result);
        }

        [Test]
        public void TrimEndOfString_WhenSuffixToRemoveIsEmpty_ReturnsInputString()
        {
            // Arrange
            string input = "TestString";
            string suffixToRemove = "";

            // Act
            string result = CommonHelper.TrimEndOfString(input, suffixToRemove);

            // Assert
            Assert.AreEqual(input, result);
        }

        [Test]
        public void TrimEndOfString_WhenSuffixToRemoveDoesNotMatch_ReturnsInputString()
        {
            // Arrange
            string input = "TestString";
            string suffixToRemove = "Suffix";

            // Act
            string result = CommonHelper.TrimEndOfString(input, suffixToRemove);

            // Assert
            Assert.AreEqual(input, result);
        }

        [Test]
        public void TrimEndOfString_WhenSuffixToRemoveMatches_ReturnsTrimmedString()
        {
            // Arrange
            string input = "TestStringSuffix";
            string suffixToRemove = "Suffix";
            string expected = "TestString";

            // Act
            string result = CommonHelper.TrimEndOfString(input, suffixToRemove);

            // Assert
            Assert.AreEqual(expected, result);
        }
        [Test]
        public void IsAzureDevOpsDebugEnabled_WhenSystemDebugEnabledIsTrue_ReturnsTrue()
        {
            // Arrange
            System.Environment.SetEnvironmentVariable("System.Debug", "true");

            // Act
            bool result = CommonHelper.IsAzureDevOpsDebugEnabled();

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsAzureDevOpsDebugEnabled_WhenSystemDebugEnabledIsFalse_ReturnsFalse()
        {
            // Arrange
            System.Environment.SetEnvironmentVariable("System.Debug", "false");

            // Act
            bool result = CommonHelper.IsAzureDevOpsDebugEnabled();

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsAzureDevOpsDebugEnabled_WhenSystemDebugEnabledIsNotSet_ReturnsFalse()
        {
            // Arrange
            System.Environment.SetEnvironmentVariable("System.Debug", null);

            // Act
            bool result = CommonHelper.IsAzureDevOpsDebugEnabled();

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void RemoveExcludedComponents_WhenExcludedComponentMatches_ReturnsExcludedComponents()
        {
            // Arrange
            List<Component> componentList = new List<Component>
            {
                new Component { Name = "Component1", Version = "1.0", Properties = new List<Property>() },
                new Component { Name = "Component2", Version = "2.0", Properties = new List<Property>() },
                new Component { Name = "Component3", Version = "3.0", Properties = new List<Property>() }
            };
            List<string> excludedComponents = new List<string> { "Component1:*", "Component2:2.0" };
            int noOfExcludedComponents = 0;

            // Act
            List<Component> result = CommonHelper.RemoveExcludedComponents(componentList, excludedComponents, ref noOfExcludedComponents);

            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(2, noOfExcludedComponents);
        }

        [Test]
        public void RemoveExcludedComponents_WhenExcludedComponentDoesNotMatch_ReturnsOriginalComponentList()
        {
            // Arrange
            List<Component> componentList = new List<Component>
            {
                new Component { Name = "Component1", Version = "1.0" },
                new Component { Name = "Component2", Version = "2.0" },
                new Component { Name = "Component3", Version = "3.0" }
            };
            List<string> excludedComponents = new List<string> { "Component4:*", "Component2:3.0" };
            int noOfExcludedComponents = 0;

            // Act
            List<Component> result = CommonHelper.RemoveExcludedComponents(componentList, excludedComponents, ref noOfExcludedComponents);

            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Any(c => c.Name == "Component1" && c.Version == "1.0"));
            Assert.IsTrue(result.Any(c => c.Name == "Component2" && c.Version == "2.0"));
            Assert.IsTrue(result.Any(c => c.Name == "Component3" && c.Version == "3.0"));
            Assert.AreEqual(0, noOfExcludedComponents);
        }
        [Test]
        public void ValidateSw360Project_WithEmptyProjectName_ThrowsInvalidDataException()
        {
            // Arrange
            string sw360ProjectName = string.Empty;
            string clearingState = "OPEN";
            string name = "ValidProjectName";
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360 { ProjectID = "12345" }
            };

            // Act & Assert
            var ex = Assert.Throws<InvalidDataException>(() =>
                CommonHelper.ValidateSw360Project(sw360ProjectName, clearingState, name, appSettings));
            Assert.AreEqual("Invalid Project Id - 12345", ex.Message);
        }

        [Test]
        public void ValidateSw360Project_WithInvalidCharacters_ReturnsMinusOne()
        {
            // Arrange
            string sw360ProjectName = "ValidProjectName";
            string clearingState = "OPEN";
            string name = "Invalid/Project\\Name.";
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360 { ProjectID = "12345" }
            };

            // Act
            int result = CommonHelper.ValidateSw360Project(sw360ProjectName, clearingState, name, appSettings);

            // Assert
            Assert.AreEqual(-1, result, "Expected -1 when project name contains invalid characters.");
        }

        [Test]
        public void ValidateSw360Project_WithClosedClearingState_ReturnsMinusOne()
        {
            // Arrange
            string sw360ProjectName = "ValidProjectName";
            string clearingState = "CLOSED";
            string name = "ValidProjectName";
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360 { ProjectID = "12345" }
            };

            // Act
            int result = CommonHelper.ValidateSw360Project(sw360ProjectName, clearingState, name, appSettings);

            // Assert
            Assert.AreEqual(-1, result, "Expected -1 when clearing state is CLOSED.");
        }

        [Test]
        public void ValidateSw360Project_WithValidInputs_ReturnsZero()
        {
            // Arrange
            string sw360ProjectName = "ValidProjectName";
            string clearingState = "OPEN";
            string name = "ValidProjectName";
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360 { ProjectID = "12345" }
            };

            // Act
            int result = CommonHelper.ValidateSw360Project(sw360ProjectName, clearingState, name, appSettings);

            // Assert
            Assert.AreEqual(0, result, "Expected 0 when all inputs are valid.");
        }
        [Test]
        public void MaskSensitiveArguments_WithSensitiveTokens_MasksTokens()
        {
            // Arrange
            string[] args = "--SW360:Token asdfghj --Jfrog:Token abcdefgh --Directory:InputFolder /mnt/Input --Directory:OutputFolder /mnt/Output"
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Act
            string[] result = CommonHelper.MaskSensitiveArguments(args);

            // Assert
            Assert.AreEqual("--SW360:Token", result[0]);
            Assert.AreEqual("******", result[1]);
            Assert.AreEqual("--Jfrog:Token", result[2]);
            Assert.AreEqual("******", result[3]);
            Assert.AreEqual("--Directory:InputFolder", result[4]);
            Assert.AreEqual("/mnt/Input", result[5]);
            Assert.AreEqual("--Directory:OutputFolder", result[6]);
            Assert.AreEqual("/mnt/Output", result[7]);
        }

        [Test]
        public void MaskSensitiveArguments_WithoutSensitiveTokens_ReturnsUnchangedArguments()
        {
            // Arrange
            string[] args = "--Directory:InputFolder /mnt/Input --Directory:OutputFolder /mnt/Output --ProjectType alpine"
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Act
            string[] result = CommonHelper.MaskSensitiveArguments(args);

            // Assert
            Assert.AreEqual("--Directory:InputFolder", result[0]);
            Assert.AreEqual("/mnt/Input", result[1]);
            Assert.AreEqual("--Directory:OutputFolder", result[2]);
            Assert.AreEqual("/mnt/Output", result[3]);
            Assert.AreEqual("--ProjectType", result[4]);
            Assert.AreEqual("alpine", result[5]);
        }

        [Test]
        public void MaskSensitiveArguments_WithEmptyArguments_ReturnsEmptyArray()
        {
            // Arrange
            string[] args = new string[] { };

            // Act
            string[] result = CommonHelper.MaskSensitiveArguments(args);

            // Assert
            Assert.IsEmpty(result, "Empty input should return an empty array.");
        }

        [Test]
        public void MaskSensitiveArguments_WithNullArguments_ReturnsEmptyArrayAndLogsWarning()
        {
            // Arrange
            string[] args = null;

            // Act
            string[] result = CommonHelper.MaskSensitiveArguments(args);

            // Assert
            Assert.IsEmpty(result, "Null input should return an empty array.");
        }
        [Test]
        public void DefaultLogFolderInitialisation_SetsDefaultLogPath_Windows()
        {
            // Arrange
            string logFileName = FileConstant.BomCreatorLog;
            string runningLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Log4Net.CatoolCurrentDirectory = System.IO.Directory.GetParent(runningLocation).FullName;
            bool m_Verbose = false;


            // Act
            CommonHelper.DefaultLogFolderInitialisation(logFileName, m_Verbose);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.AreEqual(FileConstant.LogFolder, CommonHelper.DefaultLogPath);
            }
            else
            {
                Assert.AreEqual("/var/log", CommonHelper.DefaultLogPath);
            }

        }

        [Test]
        public void RemoveExcludedComponentsFromBom_WithNullAppSettings_ReturnsUnchangedBom()
        {
            // Arrange
            var bom = new Bom
            {
                Components = new List<Component>
                {
                    new Component { Name = "Component1", Version = "1.0", Properties = new List<Property>() },
                    new Component { Name = "Component2", Version = "2.0", Properties = new List<Property>() }
                },
                Dependencies = new List<Dependency>
                {
                    new Dependency { Ref = "ref1" },
                    new Dependency { Ref = "ref2" }
                }
            };
            CommonAppSettings appSettings = null;

            // Act
            var result = CommonHelper.RemoveExcludedComponentsFromBom(appSettings, bom);

            // Assert
            Assert.AreEqual(2, result.Components.Count);
            Assert.AreEqual(2, result.Dependencies.Count);
            Assert.AreEqual("Component1", result.Components.First().Name);
            Assert.AreEqual("Component2", result.Components.Last().Name);
        }

        [Test]
        public void RemoveExcludedComponentsFromBom_WithNullSW360_ReturnsUnchangedBom()
        {
            // Arrange
            var bom = new Bom
            {
                Components = new List<Component>
                {
                    new Component { Name = "Component1", Version = "1.0", Properties = new List<Property>() },
                    new Component { Name = "Component2", Version = "2.0", Properties = new List<Property>() }
                },
                Dependencies = new List<Dependency>
                {
                    new Dependency { Ref = "ref1" },
                    new Dependency { Ref = "ref2" }
                }
            };
            var appSettings = new CommonAppSettings { SW360 = null };

            // Act
            var result = CommonHelper.RemoveExcludedComponentsFromBom(appSettings, bom);

            // Assert
            Assert.AreEqual(2, result.Components.Count);
            Assert.AreEqual(2, result.Dependencies.Count);
        }

        [Test]
        public void RemoveExcludedComponentsFromBom_WithNullExcludeComponents_ReturnsUnchangedBom()
        {
            // Arrange
            var bom = new Bom
            {
                Components = new List<Component>
                {
                    new Component { Name = "Component1", Version = "1.0", Properties = new List<Property>() },
                    new Component { Name = "Component2", Version = "2.0", Properties = new List<Property>() }
                },
                Dependencies = new List<Dependency>
                {
                    new Dependency { Ref = "ref1" },
                    new Dependency { Ref = "ref2" }
                }
            };
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360 { ExcludeComponents = null }
            };

            // Act
            var result = CommonHelper.RemoveExcludedComponentsFromBom(appSettings, bom);

            // Assert
            Assert.AreEqual(2, result.Components.Count);
            Assert.AreEqual(2, result.Dependencies.Count);
        }

        [Test]
        public void RemoveExcludedComponentsFromBom_WithExcludedComponents_RemovesComponentsAndCallsCallback()
        {
            // Arrange
            var bom = new Bom
            {
                Components = new List<Component>
                {
                    new Component { Name = "Component1", Version = "1.0", Properties = new List<Property>() },
                    new Component { Name = "Component2", Version = "2.0", Properties = new List<Property>() },
                    new Component { Name = "Component3", Version = "3.0", Properties = new List<Property>() }
                },
                Dependencies = new List<Dependency>
                {
                    new Dependency { Ref = "ref1" },
                    new Dependency { Ref = "ref2" }
                }
            };
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360
                {
                    ExcludeComponents = new List<string> { "Component1:*", "Component2:2.0" }
                }
            };
            int callbackInvokedWith = -1;

            // Act
            var result = CommonHelper.RemoveExcludedComponentsFromBom(appSettings, bom, 
                count => callbackInvokedWith = count);

            // Assert
            Assert.AreEqual(3, result.Components.Count);
            Assert.AreEqual(2, callbackInvokedWith);
        }

        [Test]
        public void RemoveExcludedComponentsFromBom_WithNullDependencies_HandlesNullDependencies()
        {
            // Arrange
            var bom = new Bom
            {
                Components = new List<Component>
                {
                    new Component { Name = "Component1", Version = "1.0", Properties = new List<Property>() }
                },
                Dependencies = null
            };
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360
                {
                    ExcludeComponents = new List<string> { "Component1:*" }
                }
            };

            // Act
            var result = CommonHelper.RemoveExcludedComponentsFromBom(appSettings, bom);

            // Assert
            Assert.AreEqual(1, result.Components.Count);
            Assert.IsNotNull(result.Dependencies);
            Assert.AreEqual(0, result.Dependencies.Count);
        }

        [Test]
        public void RemoveExcludedComponentsFromBom_WithoutCallback_DoesNotThrowException()
        {
            // Arrange
            var bom = new Bom
            {
                Components = new List<Component>
                {
                    new Component { Name = "Component1", Version = "1.0", Properties = new List<Property>() }
                },
                Dependencies = new List<Dependency>()
            };
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360
                {
                    ExcludeComponents = new List<string> { "Component1:*" }
                }
            };

            // Act & Assert
            Assert.DoesNotThrow(() => 
                CommonHelper.RemoveExcludedComponentsFromBom(appSettings, bom, null));
        }

        [Test]
        public void RemoveExcludedComponentsFromBom_WithValidComponentsAndDependencies_RemovesInvalidDependencies()
        {
            // Arrange
            var bom = new Bom
            {
                Components = new List<Component>
                {
                    new Component { Name = "Component1", Version = "1.0", BomRef = "ref1", Properties = new List<Property>() },
                    new Component { Name = "Component2", Version = "2.0", BomRef = "ref2", Properties = new List<Property>() }
                },
                Dependencies = new List<Dependency>
                {
                    new Dependency { Ref = "ref1" }, // Valid - component exists
                    new Dependency { Ref = "ref2" }, // Valid - component exists
                    new Dependency { Ref = "ref3" }  // Invalid - component doesn't exist
                }
            };
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360
                {
                    ExcludeComponents = new List<string>() // No exclusions
                }
            };

            // Act
            var result = CommonHelper.RemoveExcludedComponentsFromBom(appSettings, bom);

            // Assert
            Assert.AreEqual(2, result.Components.Count);
            Assert.AreEqual(2, result.Dependencies.Count); // Invalid dependency should be removed
            Assert.IsTrue(result.Dependencies.All(d => d.Ref == "ref1" || d.Ref == "ref2"));
        }

        [Test]
        public void RemoveExcludedComponentsFromBom_WithEmptyComponents_ReturnsEmptyComponents()
        {
            // Arrange
            var bom = new Bom
            {
                Components = new List<Component>(),
                Dependencies = new List<Dependency>
                {
                    new Dependency { Ref = "ref1" }
                }
            };
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360
                {
                    ExcludeComponents = new List<string> { "Component1:*" }
                }
            };
            int callbackInvokedWith = -1;

            // Act
            var result = CommonHelper.RemoveExcludedComponentsFromBom(appSettings, bom, 
                count => callbackInvokedWith = count);

            // Assert
            Assert.AreEqual(0, result.Components.Count);
            Assert.AreEqual(0, result.Dependencies.Count); // Dependencies removed because no valid components
            Assert.AreEqual(0, callbackInvokedWith); // No components to exclude
        }

        [Test]
        public void RemoveExcludedComponentsFromBom_WithPurlBasedExclusion_ExcludesCorrectComponents()
        {
            // Arrange
            var bom = new Bom
            {
                Components = new List<Component>
                {
                    new Component { Name = "Component1", Version = "1.0", Purl = "pkg:npm/Component1@1.0", Properties = new List<Property>() },
                    new Component { Name = "Component2", Version = "2.0", Purl = "pkg:npm/Component2@2.0", Properties = new List<Property>() }
                },
                Dependencies = new List<Dependency>()
            };
            var appSettings = new CommonAppSettings
            {
                SW360 = new SW360
                {
                    ExcludeComponents = new List<string> { "pkg:npm/Component1@1.0" }
                }
            };
            int callbackInvokedWith = -1;

            // Act
            var result = CommonHelper.RemoveExcludedComponentsFromBom(appSettings, bom, 
                count => callbackInvokedWith = count);

            // Assert
            Assert.AreEqual(2, result.Components.Count);
            Assert.AreEqual(1, callbackInvokedWith);
            // Verify that the excluded component has the exclusion property
            var excludedComponent = result.Components.First(c => c.Name == "Component1");
            Assert.IsTrue(excludedComponent.Properties.Any(p => p.Name == Dataconstant.Cdx_ExcludeComponent && p.Value == "true"));
        }        

        [Test]
        public void ProcessInternalComponentIdentification_WithEmptyComponents_ReturnsEmptyLists()
        {
            // Arrange
            List<Component> components = new List<Component>();
            Func<Component, bool> predicate = component => true;

            // Act
            var (processedComponents, internalComponents) = CommonHelper.ProcessInternalComponentIdentification(components, predicate);

            // Assert
            Assert.AreEqual(0, processedComponents.Count);
            Assert.AreEqual(0, internalComponents.Count);
        }        

        [Test]
        public void ProcessInternalComponentIdentification_WithAllInternalComponents_ReturnsAllAsInternal()
        {
            // Arrange
            List<Component> components = new List<Component>
            {
                new Component { Name = "Component1", Version = "1.0", Properties = new List<Property>() },
                new Component { Name = "Component2", Version = "2.0", Properties = new List<Property>() }
            };
            Func<Component, bool> predicate = component => true; // All are internal

            // Act
            var (processedComponents, internalComponents) = CommonHelper.ProcessInternalComponentIdentification(components, predicate);

            // Assert
            Assert.AreEqual(2, processedComponents.Count);
            Assert.AreEqual(2, internalComponents.Count);
            
            // Verify all components have the internal property set to true
            foreach (var component in processedComponents)
            {
                var internalProperty = component.Properties.FirstOrDefault(p => p.Name == Dataconstant.Cdx_IsInternal);
                Assert.IsNotNull(internalProperty);
                Assert.AreEqual("true", internalProperty.Value);
            }
        }

        [Test]
        public void ProcessInternalComponentIdentification_WithNoInternalComponents_ReturnsNoneAsInternal()
        {
            // Arrange
            List<Component> components = new List<Component>
            {
                new Component { Name = "Component1", Version = "1.0", Properties = new List<Property>() },
                new Component { Name = "Component2", Version = "2.0", Properties = new List<Property>() }
            };
            Func<Component, bool> predicate = component => false; // None are internal

            // Act
            var (processedComponents, internalComponents) = CommonHelper.ProcessInternalComponentIdentification(components, predicate);

            // Assert
            Assert.AreEqual(2, processedComponents.Count);
            Assert.AreEqual(0, internalComponents.Count);
            
            // Verify all components have the internal property set to false
            foreach (var component in processedComponents)
            {
                var internalProperty = component.Properties.FirstOrDefault(p => p.Name == Dataconstant.Cdx_IsInternal);
                Assert.IsNotNull(internalProperty);
                Assert.AreEqual("false", internalProperty.Value);
            }
        }

        [Test]
        public void ProcessInternalComponentIdentification_WithMixedComponents_ReturnsMixedResults()
        {
            // Arrange
            List<Component> components = new List<Component>
            {
                new Component { Name = "InternalComponent", Version = "1.0", Properties = new List<Property>() },
                new Component { Name = "ExternalComponent", Version = "2.0", Properties = new List<Property>() },
                new Component { Name = "AnotherInternal", Version = "3.0", Properties = new List<Property>() }
            };
            Func<Component, bool> predicate = component => component.Name.Contains("Internal");

            // Act
            var (processedComponents, internalComponents) = CommonHelper.ProcessInternalComponentIdentification(components, predicate);

            // Assert
            Assert.AreEqual(3, processedComponents.Count);
            Assert.AreEqual(2, internalComponents.Count);
            
            // Verify internal components
            Assert.IsTrue(internalComponents.All(c => c.Name.Contains("Internal")));
            
            // Verify properties are set correctly
            var internalComp1 = processedComponents.FirstOrDefault(c => c.Name == "InternalComponent");
            var externalComp = processedComponents.FirstOrDefault(c => c.Name == "ExternalComponent");
            var internalComp2 = processedComponents.FirstOrDefault(c => c.Name == "AnotherInternal");
            
            Assert.AreEqual("true", internalComp1.Properties.FirstOrDefault(p => p.Name == Dataconstant.Cdx_IsInternal)?.Value);
            Assert.AreEqual("false", externalComp.Properties.FirstOrDefault(p => p.Name == Dataconstant.Cdx_IsInternal)?.Value);
            Assert.AreEqual("true", internalComp2.Properties.FirstOrDefault(p => p.Name == Dataconstant.Cdx_IsInternal)?.Value);
        }

        [Test]
        public void ProcessInternalComponentIdentification_WithNullProperties_InitializesProperties()
        {
            // Arrange
            List<Component> components = new List<Component>
            {
                new Component { Name = "Component1", Version = "1.0", Properties = null },
                new Component { Name = "Component2", Version = "2.0" } // Properties not set
            };
            Func<Component, bool> predicate = component => component.Name == "Component1";

            // Act
            var (processedComponents, internalComponents) = CommonHelper.ProcessInternalComponentIdentification(components, predicate);

            // Assert
            Assert.AreEqual(2, processedComponents.Count);
            Assert.AreEqual(1, internalComponents.Count);
            
            // Verify properties were initialized and set correctly
            foreach (var component in processedComponents)
            {
                Assert.IsNotNull(component.Properties);
                Assert.IsTrue(component.Properties.Count > 0);
                
                var internalProperty = component.Properties.FirstOrDefault(p => p.Name == Dataconstant.Cdx_IsInternal);
                Assert.IsNotNull(internalProperty);
                
                if (component.Name == "Component1")
                {
                    Assert.AreEqual("true", internalProperty.Value);
                }
                else
                {
                    Assert.AreEqual("false", internalProperty.Value);
                }
            }
        }

        [Test]
        public void ProcessInternalComponentIdentification_WithExistingProperties_AddsInternalProperty()
        {
            // Arrange
            var existingProperty = new Property { Name = "ExistingProperty", Value = "ExistingValue" };
            List<Component> components = new List<Component>
            {
                new Component 
                { 
                    Name = "Component1", 
                    Version = "1.0", 
                    Properties = new List<Property> { existingProperty } 
                }
            };
            Func<Component, bool> predicate = component => true;

            // Act
            var (processedComponents, internalComponents) = CommonHelper.ProcessInternalComponentIdentification(components, predicate);

            // Assert
            Assert.AreEqual(1, processedComponents.Count);
            Assert.AreEqual(1, internalComponents.Count);
            
            var component = processedComponents.First();
            Assert.AreEqual(2, component.Properties.Count);
            
            // Verify existing property is preserved
            var existingProp = component.Properties.FirstOrDefault(p => p.Name == "ExistingProperty");
            Assert.IsNotNull(existingProp);
            Assert.AreEqual("ExistingValue", existingProp.Value);
            
            // Verify internal property is added
            var internalProp = component.Properties.FirstOrDefault(p => p.Name == Dataconstant.Cdx_IsInternal);
            Assert.IsNotNull(internalProp);
            Assert.AreEqual("true", internalProp.Value);
        }

        [Test]
        public void ProcessInternalComponentIdentification_WithEmptyPropertiesList_AddsInternalProperty()
        {
            // Arrange
            List<Component> components = new List<Component>
            {
                new Component 
                { 
                    Name = "Component1", 
                    Version = "1.0", 
                    Properties = new List<Property>() // Empty but not null
                }
            };
            Func<Component, bool> predicate = component => false;

            // Act
            var (processedComponents, internalComponents) = CommonHelper.ProcessInternalComponentIdentification(components, predicate);

            // Assert
            Assert.AreEqual(1, processedComponents.Count);
            Assert.AreEqual(0, internalComponents.Count);
            
            var component = processedComponents.First();
            Assert.AreEqual(1, component.Properties.Count);
            
            var internalProp = component.Properties.FirstOrDefault(p => p.Name == Dataconstant.Cdx_IsInternal);
            Assert.IsNotNull(internalProp);
            Assert.AreEqual("false", internalProp.Value);
        }

        [Test]
        public void ProcessInternalComponentIdentification_WithComplexPredicate_WorksCorrectly()
        {
            // Arrange
            List<Component> components = new List<Component>
            {
                new Component { Name = "Component1", Version = "1.0", Properties = new List<Property>() },
                new Component { Name = "Component2", Version = "2.0", Properties = new List<Property>() },
                new Component { Name = "Component3", Version = "1.5", Properties = new List<Property>() }
            };
            // Complex predicate: internal if version starts with "1"
            Func<Component, bool> predicate = component => component.Version.StartsWith("1");

            // Act
            var (processedComponents, internalComponents) = CommonHelper.ProcessInternalComponentIdentification(components, predicate);

            // Assert
            Assert.AreEqual(3, processedComponents.Count);
            Assert.AreEqual(2, internalComponents.Count);
            
            // Verify the correct components are identified as internal
            Assert.IsTrue(internalComponents.Any(c => c.Name == "Component1"));
            Assert.IsTrue(internalComponents.Any(c => c.Name == "Component3"));
            Assert.IsFalse(internalComponents.Any(c => c.Name == "Component2"));
        }

        [Test]
        public void ProcessInternalComponentIdentification_PreservesOriginalComponentData()
        {
            // Arrange
            List<Component> components = new List<Component>
            {
                new Component 
                { 
                    Name = "Component1", 
                    Version = "1.0", 
                    BomRef = "ref1",
                    Purl = "pkg:npm/component1@1.0",
                    Properties = new List<Property>() 
                }
            };
            Func<Component, bool> predicate = component => true;

            // Act
            var (processedComponents, internalComponents) = CommonHelper.ProcessInternalComponentIdentification(components, predicate);

            // Assert
            var processedComponent = processedComponents.First();
            Assert.AreEqual("Component1", processedComponent.Name);
            Assert.AreEqual("1.0", processedComponent.Version);
            Assert.AreEqual("ref1", processedComponent.BomRef);
            Assert.AreEqual("pkg:npm/component1@1.0", processedComponent.Purl);
            
            // Should have added one property (internal) to the existing empty list
            Assert.AreEqual(1, processedComponent.Properties.Count);
        }
        [Test]
        public void AddSpdxSBomFileNameProperty_ValidBomWithComponents_AddsFileNamePropertyToAllComponents()
        {
            // Arrange
            var testBom = new Bom
            {
                Components = new List<Component>
        {
            new Component
            {
                Name = "ComponentWithProperties",
                Properties = new List<Property>
                {
                    new Property { Name = "ExistingProperty", Value = "ExistingValue" }
                }
            },
            new Component
            {
                Name = "ComponentWithNullProperties",
                Properties = null
            },
            new Component
            {
                Name = "ComponentWithEmptyProperties",
                Properties = new List<Property>()
            }
        }
            };
            var testFilePath = Path.Combine("TestDirectory", "TestFile.spdx.json");

            // Act
            CommonHelper.AddSpdxSBomFileNameProperty(ref testBom, testFilePath);

            // Assert - All components should have the SPDX filename property
            Assert.AreEqual(3, testBom.Components.Count);
            foreach (var component in testBom.Components)
            {
                Assert.IsNotNull(component.Properties);
                var spdxProperty = component.Properties.FirstOrDefault(p => p.Name == Dataconstant.Cdx_SpdxFileName);
                Assert.IsNotNull(spdxProperty);
                Assert.AreEqual("TestFile.spdx.json", spdxProperty.Value);
            }

            // Assert - Existing properties are preserved
            var componentWithExisting = testBom.Components.First();
            Assert.AreEqual(3, componentWithExisting.Properties.Count);
            Assert.IsTrue(componentWithExisting.Properties.Any(p => p.Name == "ExistingProperty"));
        }

        [Test]
        public void AddSpdxSBomFileNameProperty_NullAndEmptyScenarios_HandlesGracefully()
        {
            // Test null BOM
            Bom nullBom = null;
            var testFilePath = Path.Combine("TestDirectory", "TestFile.spdx.json");
            Assert.DoesNotThrow(() => CommonHelper.AddSpdxSBomFileNameProperty(ref nullBom, testFilePath));

            // Test BOM with null components
            var bomWithNullComponents = new Bom { Components = null };
            Assert.DoesNotThrow(() => CommonHelper.AddSpdxSBomFileNameProperty(ref bomWithNullComponents, testFilePath));

            // Test empty components list
            var bomWithEmptyComponents = new Bom { Components = new List<Component>() };
            CommonHelper.AddSpdxSBomFileNameProperty(ref bomWithEmptyComponents, testFilePath);
            Assert.AreEqual(0, bomWithEmptyComponents.Components.Count);
        }

        [Test]
        public void AddSpdxSBomFileNameProperty_VariousFilePathFormats_ExtractsCorrectFileName()
        {
            var testCases = new[]
            {
        (Path.Combine("C:", "Complex", "Path", "ComplexFile.spdx.json"), "ComplexFile.spdx.json"),
        ("SimpleFile.json", "SimpleFile.json"),
        (Path.Combine("Folder", "FileWithoutExtension"), "FileWithoutExtension"),
        ("", ""),
        (null, null)
    };

            foreach (var (filePath, expectedFileName) in testCases)
            {
                var testBom = new Bom
                {
                    Components = new List<Component>
            {
                new Component { Name = "TestComponent", Properties = new List<Property>() }
            }
                };

                if (filePath == null)
                {
                    Assert.DoesNotThrow(() => CommonHelper.AddSpdxSBomFileNameProperty(ref testBom, filePath));
                }
                else
                {
                    CommonHelper.AddSpdxSBomFileNameProperty(ref testBom, filePath);
                    var property = testBom.Components.First().Properties.FirstOrDefault(p => p.Name == Dataconstant.Cdx_SpdxFileName);
                    Assert.IsNotNull(property);
                    Assert.AreEqual(expectedFileName, property.Value);
                }
            }
        }

        [Test]
        public void AddSpdxSBomFileNameProperty_BomComponentsReference_PreservesOriginalReference()
        {
            // Arrange
            var testBom = new Bom
            {
                Components = new List<Component>
        {
            new Component { Name = "Component1", Properties = new List<Property>() },
            new Component { Name = "Component2", Properties = new List<Property>() },
            new Component { Name = "Component3", Properties = new List<Property>() }
        }
            };
            var originalComponentsList = testBom.Components;
            var testFilePath = Path.Combine("TestDirectory", "TestFile.spdx.json");

            // Act
            CommonHelper.AddSpdxSBomFileNameProperty(ref testBom, testFilePath);

            // Assert - The BOM should still reference the same components list
            Assert.AreSame(originalComponentsList, testBom.Components);

            // Assert - bomComponentsList assignment works correctly (line coverage)
            Assert.IsNotNull(testBom.Components);
            Assert.AreEqual(3, testBom.Components.Count);
        }

        [Test]
        public void AddSpdxSBomFileNameProperty_PropertyInitialization_WorksForNullProperties()
        {
            // Arrange - Component with null properties
            var bomWithNullProps = new Bom
            {
                Components = new List<Component>
        {
            new Component { Name = "TestComponent", Properties = null }
        }
            };
            var testFilePath = Path.Combine("TestDirectory", "TestFile.spdx.json");

            // Act
            CommonHelper.AddSpdxSBomFileNameProperty(ref bomWithNullProps, testFilePath);

            // Assert - Properties should be initialized and property added
            var component = bomWithNullProps.Components.First();
            Assert.IsNotNull(component.Properties);
            Assert.AreEqual(2, component.Properties.Count);
            Assert.AreEqual(Dataconstant.Cdx_SpdxFileName, component.Properties[0].Name);
            Assert.AreEqual("TestFile.spdx.json", component.Properties[0].Value);
        }

        [Test]
        public void AddSpdxSBomFileNameProperty_FilePathWithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var testBom = new Bom
            {
                Components = new List<Component>
        {
            new Component { Name = "TestComponent", Properties = new List<Property>() }
        }
            };
            var specialCharPath = Path.Combine("Test Folder", "File-Name_With@Special#Characters.spdx.json");

            // Act
            CommonHelper.AddSpdxSBomFileNameProperty(ref testBom, specialCharPath);

            // Assert
            var component = testBom.Components.First();
            var spdxFileNameProperty = component.Properties.FirstOrDefault(p => p.Name == Dataconstant.Cdx_SpdxFileName);
            Assert.IsNotNull(spdxFileNameProperty);
            Assert.AreEqual("File-Name_With@Special#Characters.spdx.json", spdxFileNameProperty.Value);
        }

        [Test]
        public void AddSpdxSBomFileNameProperty_EmptyFilePath_HandlesCorrectly()
        {
            // Arrange
            var testBom = new Bom
            {
                Components = new List<Component>
        {
            new Component { Name = "TestComponent", Properties = new List<Property>() }
        }
            };
            var emptyFilePath = string.Empty;

            // Act
            CommonHelper.AddSpdxSBomFileNameProperty(ref testBom, emptyFilePath);

            // Assert
            var component = testBom.Components.First();
            var spdxFileNameProperty = component.Properties.FirstOrDefault(p => p.Name == Dataconstant.Cdx_SpdxFileName);
            Assert.IsNotNull(spdxFileNameProperty);
            Assert.AreEqual(string.Empty, spdxFileNameProperty.Value);
        }

        [Test]
        public void AddSpdxSBomFileNameProperty_MultipleCallsWithSameFile_AddsPropertyEachTime()
        {
            // Arrange
            var testBom = new Bom
            {
                Components = new List<Component>
        {
            new Component { Name = "TestComponent", Properties = new List<Property>() }
        }
            };
            var testFilePath = Path.Combine("TestDirectory", "TestFile.spdx.json");

            // Act
            CommonHelper.AddSpdxSBomFileNameProperty(ref testBom, testFilePath);
            CommonHelper.AddSpdxSBomFileNameProperty(ref testBom, testFilePath);

            // Assert
            var component = testBom.Components.First();
            var spdxFileNameProperties = component.Properties.Where(p => p.Name == Dataconstant.Cdx_SpdxFileName);
            Assert.AreEqual(2, spdxFileNameProperties.Count()); // Method adds property each time it's called
        }

        [Test]
        public void AddSpdxSBomFileNameProperty_LargeNumberOfComponents_ProcessesAllComponents()
        {
            // Arrange
            var largeComponentList = new List<Component>();
            for (int i = 0; i < 100; i++)
            {
                largeComponentList.Add(new Component
                {
                    Name = $"Component{i}",
                    Version = "1.0.0",
                    Properties = new List<Property>()
                });
            }

            var largeBom = new Bom { Components = largeComponentList };
            var testFilePath = Path.Combine("TestDirectory", "TestFile.spdx.json");

            // Act
            CommonHelper.AddSpdxSBomFileNameProperty(ref largeBom, testFilePath);

            // Assert
            Assert.AreEqual(100, largeBom.Components.Count);
            foreach (var component in largeBom.Components)
            {
                var spdxFileNameProperty = component.Properties.FirstOrDefault(p => p.Name == Dataconstant.Cdx_SpdxFileName);
                Assert.IsNotNull(spdxFileNameProperty);
                Assert.AreEqual("TestFile.spdx.json", spdxFileNameProperty.Value);
            }
        }


        #region SetComponentPropertiesAndHashes Tests

        [Test]
        public void SetComponentPropertiesAndHashes_WithNullProperties_InitializesPropertiesList()
        {
            // Arrange
            var component = new Component 
            { 
                Name = "TestComponent", 
                Version = "1.0", 
                Properties = null,
                Description = "Original Description"
            };
            var artifactoryRepo = new Property { Name = Dataconstant.Cdx_ArtifactoryRepoName, Value = "test-repo" };
            var projectType = new Property { Name = Dataconstant.Cdx_ProjectType, Value = "npm" };
            var siemensFileName = new Property { Name = Dataconstant.Cdx_Siemensfilename, Value = "test-file.tgz" };
            var jfrogRepoPath = new Property { Name = Dataconstant.Cdx_JfrogRepoPath, Value = "test-repo/path/file.tgz" };

            // Act
            CommonHelper.SetComponentPropertiesAndHashes(component, artifactoryRepo, projectType, siemensFileName, jfrogRepoPath);

            // Assert
            Assert.IsNotNull(component.Properties);
            Assert.AreEqual(4, component.Properties.Count);
            Assert.IsNull(component.Description);
            Assert.IsNull(component.Hashes);
        }

        [Test]
        public void SetComponentPropertiesAndHashes_WithEmptyProperties_AddsStandardProperties()
        {
            // Arrange
            var component = new Component 
            { 
                Name = "TestComponent", 
                Version = "1.0", 
                Properties = new List<Property>(),
                Description = "Original Description"
            };
            var artifactoryRepo = new Property { Name = Dataconstant.Cdx_ArtifactoryRepoName, Value = "test-repo" };
            var projectType = new Property { Name = Dataconstant.Cdx_ProjectType, Value = "maven" };
            var siemensFileName = new Property { Name = Dataconstant.Cdx_Siemensfilename, Value = "test-file.jar" };
            var jfrogRepoPath = new Property { Name = Dataconstant.Cdx_JfrogRepoPath, Value = "test-repo/path/file.jar" };

            // Act
            CommonHelper.SetComponentPropertiesAndHashes(component, artifactoryRepo, projectType, siemensFileName, jfrogRepoPath);

            // Assert
            Assert.IsNotNull(component.Properties);
            Assert.AreEqual(4, component.Properties.Count);
            Assert.AreEqual("test-repo", component.Properties.FirstOrDefault(p => p.Name == Dataconstant.Cdx_ArtifactoryRepoName)?.Value);
            Assert.AreEqual("maven", component.Properties.FirstOrDefault(p => p.Name == Dataconstant.Cdx_ProjectType)?.Value);
            Assert.AreEqual("test-file.jar", component.Properties.FirstOrDefault(p => p.Name == Dataconstant.Cdx_Siemensfilename)?.Value);
            Assert.AreEqual("test-repo/path/file.jar", component.Properties.FirstOrDefault(p => p.Name == Dataconstant.Cdx_JfrogRepoPath)?.Value);
            Assert.IsNull(component.Description);
        }

        [Test]
        public void SetComponentPropertiesAndHashes_WithExistingProperties_PreservesAndAddsProperties()
        {
            // Arrange
            var existingProperty = new Property { Name = "ExistingProperty", Value = "ExistingValue" };
            var component = new Component 
            { 
                Name = "TestComponent", 
                Version = "1.0", 
                Properties = new List<Property> { existingProperty },
                Description = "Original Description"
            };
            var artifactoryRepo = new Property { Name = Dataconstant.Cdx_ArtifactoryRepoName, Value = "python-repo" };
            var projectType = new Property { Name = Dataconstant.Cdx_ProjectType, Value = "python" };
            var siemensFileName = new Property { Name = Dataconstant.Cdx_Siemensfilename, Value = "test-package.whl" };
            var jfrogRepoPath = new Property { Name = Dataconstant.Cdx_JfrogRepoPath, Value = "python-repo/path/package.whl" };

            // Act
            CommonHelper.SetComponentPropertiesAndHashes(component, artifactoryRepo, projectType, siemensFileName, jfrogRepoPath);

            // Assert
            Assert.IsNotNull(component.Properties);
            Assert.AreEqual(5, component.Properties.Count);
            
            // Verify existing property is preserved
            Assert.IsTrue(component.Properties.Any(p => p.Name == "ExistingProperty" && p.Value == "ExistingValue"));
            
            // Verify new properties are added
            Assert.IsTrue(component.Properties.Any(p => p.Name == Dataconstant.Cdx_ArtifactoryRepoName && p.Value == "python-repo"));
            Assert.IsTrue(component.Properties.Any(p => p.Name == Dataconstant.Cdx_ProjectType && p.Value == "python"));
            Assert.IsTrue(component.Properties.Any(p => p.Name == Dataconstant.Cdx_Siemensfilename && p.Value == "test-package.whl"));
            Assert.IsTrue(component.Properties.Any(p => p.Name == Dataconstant.Cdx_JfrogRepoPath && p.Value == "python-repo/path/package.whl"));
            
            Assert.IsNull(component.Description);
        }

        [Test]
        public void SetComponentPropertiesAndHashes_WithValidHashes_AddsHashesToComponent()
        {
            // Arrange
            var component = new Component 
            { 
                Name = "TestComponent", 
                Version = "1.0", 
                Properties = new List<Property>(),
                Description = "Original Description"
            };
            var artifactoryRepo = new Property { Name = Dataconstant.Cdx_ArtifactoryRepoName, Value = "test-repo" };
            var projectType = new Property { Name = Dataconstant.Cdx_ProjectType, Value = "nuget" };
            var siemensFileName = new Property { Name = Dataconstant.Cdx_Siemensfilename, Value = "test-package.nupkg" };
            var jfrogRepoPath = new Property { Name = Dataconstant.Cdx_JfrogRepoPath, Value = "nuget-repo/path/package.nupkg" };
            
            // Create a test hash object that matches the expected structure
            var hashes = new TestHashObject
            {
                MD5 = "5d41402abc4b2a76b9719d911017c592",
                SHA1 = "aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d",
                SHA256 = "2cf24dba4f21d4288074e297f7719e34d3e7e0cbfb1c4a1b8d1f8e8b7a6e9c9d"
            };

            // Act
            CommonHelper.SetComponentPropertiesAndHashes(component, artifactoryRepo, projectType, siemensFileName, jfrogRepoPath, hashes);

            // Assert
            Assert.IsNotNull(component.Properties);
            Assert.AreEqual(4, component.Properties.Count);
            Assert.IsNull(component.Description);
            
            // Verify hashes are set correctly
            Assert.IsNotNull(component.Hashes);
            Assert.AreEqual(3, component.Hashes.Count);
            
            var md5Hash = component.Hashes.FirstOrDefault(h => h.Alg == Hash.HashAlgorithm.MD5);
            Assert.IsNotNull(md5Hash);
            Assert.AreEqual("5d41402abc4b2a76b9719d911017c592", md5Hash.Content);
            
            var sha1Hash = component.Hashes.FirstOrDefault(h => h.Alg == Hash.HashAlgorithm.SHA_1);
            Assert.IsNotNull(sha1Hash);
            Assert.AreEqual("aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d", sha1Hash.Content);
            
            var sha256Hash = component.Hashes.FirstOrDefault(h => h.Alg == Hash.HashAlgorithm.SHA_256);
            Assert.IsNotNull(sha256Hash);
            Assert.AreEqual("2cf24dba4f21d4288074e297f7719e34d3e7e0cbfb1c4a1b8d1f8e8b7a6e9c9d", sha256Hash.Content);
        }

        [Test]
        public void SetComponentPropertiesAndHashes_WithNullHashes_DoesNotSetHashes()
        {
            // Arrange
            var component = new Component 
            { 
                Name = "TestComponent", 
                Version = "1.0", 
                Properties = new List<Property>(),
                Description = "Original Description"
            };
            var artifactoryRepo = new Property { Name = Dataconstant.Cdx_ArtifactoryRepoName, Value = "debian-repo" };
            var projectType = new Property { Name = Dataconstant.Cdx_ProjectType, Value = "debian" };
            var siemensFileName = new Property { Name = Dataconstant.Cdx_Siemensfilename, Value = "test-package.deb" };
            var jfrogRepoPath = new Property { Name = Dataconstant.Cdx_JfrogRepoPath, Value = "debian-repo/path/package.deb" };

            // Act
            CommonHelper.SetComponentPropertiesAndHashes(component, artifactoryRepo, projectType, siemensFileName, jfrogRepoPath, null);

            // Assert
            Assert.IsNotNull(component.Properties);
            Assert.AreEqual(4, component.Properties.Count);
            Assert.IsNull(component.Description);
            Assert.IsNull(component.Hashes);
        }

        [Test]
        public void SetComponentPropertiesAndHashes_WithExistingHashes_ReplacesHashes()
        {
            // Arrange
            var existingHash = new Hash { Alg = Hash.HashAlgorithm.MD5, Content = "oldmd5hash" };
            var component = new Component 
            { 
                Name = "TestComponent", 
                Version = "1.0", 
                Properties = new List<Property>(),
                Description = "Original Description",
                Hashes = new List<Hash> { existingHash }
            };
            var artifactoryRepo = new Property { Name = Dataconstant.Cdx_ArtifactoryRepoName, Value = "test-repo" };
            var projectType = new Property { Name = Dataconstant.Cdx_ProjectType, Value = "conan" };
            var siemensFileName = new Property { Name = Dataconstant.Cdx_Siemensfilename, Value = "test-package.tgz" };
            var jfrogRepoPath = new Property { Name = Dataconstant.Cdx_JfrogRepoPath, Value = "conan-repo/path/package.tgz" };
            
            // Create a test hash object that matches the expected structure
            var newHashes = new TestHashObject
            {
                MD5 = "newmd5hash",
                SHA1 = "newsha1hash",
                SHA256 = "newsha256hash"
            };

            // Act
            CommonHelper.SetComponentPropertiesAndHashes(component, artifactoryRepo, projectType, siemensFileName, jfrogRepoPath, newHashes);

            // Assert
            Assert.IsNotNull(component.Properties);
            Assert.AreEqual(4, component.Properties.Count);
            Assert.IsNull(component.Description);
            
            // Verify hashes are replaced, not appended
            Assert.IsNotNull(component.Hashes);
            Assert.AreEqual(3, component.Hashes.Count);
            Assert.IsFalse(component.Hashes.Any(h => h.Content == "oldmd5hash"));
            Assert.IsTrue(component.Hashes.Any(h => h.Content == "newmd5hash"));
        }

        [Test]
        public void SetComponentPropertiesAndHashes_AlwaysClearsDescription()
        {
            // Arrange
            var component = new Component 
            { 
                Name = "TestComponent", 
                Version = "1.0", 
                Properties = new List<Property>(),
                Description = "This description should be cleared"
            };
            var artifactoryRepo = new Property { Name = Dataconstant.Cdx_ArtifactoryRepoName, Value = "test-repo" };
            var projectType = new Property { Name = Dataconstant.Cdx_ProjectType, Value = "alpine" };
            var siemensFileName = new Property { Name = Dataconstant.Cdx_Siemensfilename, Value = "test-package.apk" };
            var jfrogRepoPath = new Property { Name = Dataconstant.Cdx_JfrogRepoPath, Value = "alpine-repo/path/package.apk" };

            // Act
            CommonHelper.SetComponentPropertiesAndHashes(component, artifactoryRepo, projectType, siemensFileName, jfrogRepoPath);

            // Assert
            Assert.IsNull(component.Description);
        }

        [Test]
        public void SetComponentPropertiesAndHashes_WithAllParametersNull_HandlesGracefully()
        {
            // Arrange
            var component = new Component 
            { 
                Name = "TestComponent", 
                Version = "1.0", 
                Properties = new List<Property>(),
                Description = "Original Description"
            };

            // Act & Assert - Should not throw exception
            Assert.DoesNotThrow(() => 
                CommonHelper.SetComponentPropertiesAndHashes(component, null, null, null, null, null));
                
            // Verify basic behavior
            Assert.IsNotNull(component.Properties);
            Assert.AreEqual(4, component.Properties.Count); // 4 null properties added
            Assert.IsNull(component.Description);
            Assert.IsNull(component.Hashes);
        }

        [Test]
        public void SetComponentPropertiesAndHashes_WithPartialHashData_HandlesPartialHashes()
        {
            // Arrange
            var component = new Component 
            { 
                Name = "TestComponent", 
                Version = "1.0", 
                Properties = new List<Property>(),
                Description = "Original Description"
            };
            var artifactoryRepo = new Property { Name = Dataconstant.Cdx_ArtifactoryRepoName, Value = "test-repo" };
            var projectType = new Property { Name = Dataconstant.Cdx_ProjectType, Value = "npm" };
            var siemensFileName = new Property { Name = Dataconstant.Cdx_Siemensfilename, Value = "test-package.tgz" };
            var jfrogRepoPath = new Property { Name = Dataconstant.Cdx_JfrogRepoPath, Value = "npm-repo/path/package.tgz" };
            
            // Hashes with some null values
            var partialHashes = new TestHashObject
            {
                MD5 = "validmd5hash",
                SHA1 = null,
                SHA256 = "validsha256hash"
            };

            // Act
            CommonHelper.SetComponentPropertiesAndHashes(component, artifactoryRepo, projectType, siemensFileName, jfrogRepoPath, partialHashes);

            // Assert
            Assert.IsNotNull(component.Properties);
            Assert.AreEqual(4, component.Properties.Count);
            Assert.IsNull(component.Description);
            
            // Verify hashes are set even with null values
            Assert.IsNotNull(component.Hashes);
            Assert.AreEqual(3, component.Hashes.Count);
            
            var md5Hash = component.Hashes.FirstOrDefault(h => h.Alg == Hash.HashAlgorithm.MD5);
            Assert.IsNotNull(md5Hash);
            Assert.AreEqual("validmd5hash", md5Hash.Content);
            
            var sha1Hash = component.Hashes.FirstOrDefault(h => h.Alg == Hash.HashAlgorithm.SHA_1);
            Assert.IsNotNull(sha1Hash);
            Assert.IsNull(sha1Hash.Content);
            
            var sha256Hash = component.Hashes.FirstOrDefault(h => h.Alg == Hash.HashAlgorithm.SHA_256);
            Assert.IsNotNull(sha256Hash);
            Assert.AreEqual("validsha256hash", sha256Hash.Content);
        }

        #endregion

        // ...existing code...
    }

    public class TestObject
    {
        [System.ComponentModel.DisplayName("Display Name 1")]
        public string Property1 { get; set; }

        public string Property2 { get; set; }
    }

    public class TestHashObject
    {
        public string MD5 { get; set; }
        public string SHA1 { get; set; }
        public string SHA256 { get; set; }
    }
}
