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
using log4net.Appender;
using log4net.Config;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace LCT.Common.UTest
{
    [TestFixture]
    public class CommonHelperTest
    {
        private string tempDir;
        private string tempLogFile;
        private CommonAppSettings appSettings;
        private MemoryAppender memoryAppender;
        [SetUp]
        public void SetUp()
        {
            tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            System.IO.Directory.CreateDirectory(tempDir);

            tempLogFile = Path.Combine(tempDir, "catool.log");
            File.WriteAllText(tempLogFile, "test log content");
            IFolderAction folderAction = new FolderAction();
            IFileOperations fileOperations = new FileOperations();
            appSettings = new CommonAppSettings(folderAction, fileOperations)
            {
                Directory = new Directory(folderAction, fileOperations)
                {
                    LogFolder = tempDir // Mocked LogFolder
                }
            };

            // Set the static log path for the test
            Log4Net.CatoolLogPath = tempLogFile;
            CommonHelper.DefaultLogPath = "default";
            memoryAppender = new MemoryAppender();
            LogManager.GetRepository().ResetConfiguration();
            BasicConfigurator.Configure(memoryAppender);
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
            string result = CommonHelper.LogFolderInitialization(appSettings, "catool.log", false);

            // Assert
            Assert.AreEqual(tempDir, result);
            Assert.IsTrue(File.Exists(tempLogFile));
        }
        [Test]
        public void LogFolderInitialisation_WhenLogFolderIsNull_ReturnsDefaultLogPath()
        {
            Log4Net.CatoolLogPath = "C:\\catool\\fds.log";

            // Act
            string result = CommonHelper.LogFolderInitialization(appSettings, "catool.log", false);

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

            //Act
            CommonHelper.GetDetailsForManuallyAdded(componentsForBOM, listComponentForBOM);

            //Assert
            Assert.AreEqual(2, listComponentForBOM.Count);
            Assert.AreEqual("Component1", listComponentForBOM[0].Name);
            Assert.AreEqual("1.0", listComponentForBOM[0].Version);
            Assert.AreEqual("Component2", listComponentForBOM[1].Name);
            Assert.AreEqual("2.0", listComponentForBOM[1].Version);
        }

        [Test]
        public void CheckNullOrEmpty_WhenValueIsNull_LogsError()
        {
            // Arrange
            string name = "TestName";
            string value = null;

            // Act
            CommonHelper.CheckNullOrEmpty(name, value);

            // Assert
            var logEvents = memoryAppender.GetEvents();
            Assert.IsTrue(logEvents.Any(e => e.RenderedMessage.Contains($"The provided value for '{name}' is null, empty, or whitespace.")));
        }

        [Test]
        public void CheckNullOrEmpty_WhenValueIsEmpty_LogsError()
        {
            // Arrange
            string name = "TestName";
            string value = "";

            // Act
            CommonHelper.CheckNullOrEmpty(name, value);

            // Assert
            var logEvents = memoryAppender.GetEvents();
            Assert.IsTrue(logEvents.Any(e => e.RenderedMessage.Contains($"The provided value for '{name}' is null, empty, or whitespace.")));
        }

        [Test]
        public void CheckNullOrEmpty_WhenValueIsWhiteSpace_LogsError()
        {
            // Arrange
            string name = "TestName";
            string value = "   ";

            // Act
            CommonHelper.CheckNullOrEmpty(name, value);

            // Assert
            var logEvents = memoryAppender.GetEvents();
            Assert.IsTrue(logEvents.Any(e => e.RenderedMessage.Contains($"The provided value for '{name}' is null, empty, or whitespace.")));
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
            string runningLocation=Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Log4Net.CatoolCurrentDirectory = System.IO.Directory.GetParent(runningLocation).FullName;
            bool m_Verbose = false;
          

            // Act
            CommonHelper.DefaultLogFolderInitialization(logFileName, m_Verbose);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.AreEqual(FileConstant.LogFolder, CommonHelper.DefaultLogPath);
            }
            else
            {
                Assert.AreEqual("/var/log", CommonHelper.DefaultLogPath);
            }               
            
        }       
    }

    public class TestObject
    {
        [System.ComponentModel.DisplayName("Display Name 1")]
        public string Property1 { get; set; }

        public string Property2 { get; set; }
    }
}
