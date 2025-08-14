using LCT.Common;
using LCT.Common.Model;
using log4net.Appender;
using log4net.Config;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LCT.PackageIdentifier.UTest
{
    [TestFixture]
    public class DisplayInformationTests
    {
        private CommonAppSettings appSettings;
        private CatoolInfo caToolInformation;
        private MemoryAppender memoryAppender;

        [SetUp]
        public void Setup()
        {
            appSettings = new CommonAppSettings();
        }

        [Test]
        public void LogInputParameters_ShouldLogCorrectMessage_WhenBasicSBOMIsFalse()
        {
            // Arrange
            string listOfInternalRepoList = "repo1,repo2";
            string listOfInclude = "include1,include2";
            string listOfExclude = "exclude1,exclude2";
            string listOfExcludeComponents = "component1,component2";
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "NPM",
                SW360 = new SW360()
                {
                    URL = "http://sw360.url",
                    AuthTokenType = "Bearer",
                    ProjectName = "ProjectName",
                    ProjectID = "ProjectID",
                    ExcludeComponents = new List<string> { "component1", "component2" }
                },

                Directory = new LCT.Common.Directory()
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles")),
                    OutputFolder = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles"))
                }
            };
            caToolInformation = new CatoolInfo
            {
                CatoolVersion = "1.0.0",
                CatoolRunningLocation = "runningLocation"
            };

            memoryAppender = new MemoryAppender();
            BasicConfigurator.Configure(memoryAppender);
            // Act
            DisplayInformation.LogInputParameters(caToolInformation, appSettings, listOfInternalRepoList, listOfInclude, listOfExclude, listOfExcludeComponents);

            // Assert
            string expectedLogMessage = $"Input Parameters used in Package Identifier:\n\t" +
                $"CaToolVersion\t\t --> {caToolInformation.CatoolVersion}\n\t" +
                $"CaToolRunningPath\t --> {caToolInformation.CatoolRunningLocation}\n\t" +
                $"PackageFilePath\t\t --> {appSettings.Directory.InputFolder}\n\t" +
                $"BomFolderPath\t\t --> {appSettings.Directory.OutputFolder}\n\t" +
                $"SW360Url\t\t --> {appSettings.SW360.URL}\n\t" +
                $"SW360AuthTokenType\t --> {appSettings.SW360.AuthTokenType}\n\t" +
                $"SW360ProjectName\t --> {appSettings.SW360.ProjectName}\n\t" +
                $"SW360ProjectID\t\t --> {appSettings.SW360.ProjectID}\n\t" +
                $"ExcludeComponents\t --> {listOfExcludeComponents}\n\t" +
                $"ProjectType\t\t --> {appSettings.ProjectType}\n\t" +
                $"LogFolderPath\t\t --> {Log4Net.CatoolLogPath}\n\t" +
                $"Include\t\t\t --> {listOfInclude}\n\t" +
                $"Exclude\t\t\t --> {listOfExclude}\n";

            var logEvents = memoryAppender.GetEvents();
            Assert.IsNotEmpty(logEvents);
            var actualLogMessage = logEvents[0].RenderedMessage;
            Assert.AreEqual(expectedLogMessage, actualLogMessage);
        }
        [Test]
        public void LogInputParameters_ShouldLogCorrectMessage_WhenBasicSBOMIsTrue()
        {
            // Arrange            
            string listOfInternalRepoList = "repo1,repo2";
            string listOfInclude = "include1,include2";
            string listOfExclude = "exclude1,exclude2";
            string listOfExcludeComponents = "component1,component2";
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "NPM",
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles")),
                    OutputFolder = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles"))
                }

            };
            caToolInformation = new CatoolInfo
            {
                CatoolVersion = "1.0.0",
                CatoolRunningLocation = "runningLocation"
            };

            memoryAppender = new MemoryAppender();
            BasicConfigurator.Configure(memoryAppender);
            // Act
            DisplayInformation.LogInputParameters(caToolInformation, appSettings, listOfInternalRepoList, listOfInclude, listOfExclude, listOfExcludeComponents);

            // Assert
            string expectedLogMessage = $"Input Parameters used in Package Identifier:\n\t" +
                $"CaToolVersion\t\t --> {caToolInformation.CatoolVersion}\n\t" +
                $"CaToolRunningPath\t --> {caToolInformation.CatoolRunningLocation}\n\t" +
                $"PackageFilePath\t\t --> {appSettings.Directory.InputFolder}\n\t" +
                $"BomFolderPath\t\t --> {appSettings.Directory.OutputFolder}\n\t" +
                $"ProjectType\t\t --> {appSettings.ProjectType}\n\t" +
                $"LogFolderPath\t\t --> {Log4Net.CatoolLogPath}\n\t" +
                $"Include\t\t\t --> {listOfInclude}\n\t" +
                $"Exclude\t\t\t --> {listOfExclude}\n";

            var logEvents = memoryAppender.GetEvents();
            Assert.IsNotEmpty(logEvents);
            var actualLogMessage = logEvents[0].RenderedMessage;
            Assert.AreEqual(expectedLogMessage, actualLogMessage);
        }
        [Test]
        public void LogInputParameters_ShouldLogCorrectMessage_WhenJfrogIsTrue()
        {
            // Arrange            
            string listOfInternalRepoList = "repo1,repo2";
            string listOfInclude = "include1,include2";
            string listOfExclude = "exclude1,exclude2";
            string listOfExcludeComponents = "component1,component2";
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string OutFolder = Path.GetDirectoryName(exePath);

            CommonAppSettings appSettings = new CommonAppSettings()
            {
                ProjectType = "NPM",
                Jfrog = new Jfrog(),
                Directory = new LCT.Common.Directory()
                {
                    InputFolder = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles")),
                    OutputFolder = Path.GetFullPath(Path.Combine(OutFolder, "PackageIdentifierUTTestFiles"))
                }
            };
            caToolInformation = new CatoolInfo
            {
                CatoolVersion = "1.0.0",
                CatoolRunningLocation = "runningLocation"
            };

            memoryAppender = new MemoryAppender();
            BasicConfigurator.Configure(memoryAppender);
            // Act
            DisplayInformation.LogInputParameters(caToolInformation, appSettings, listOfInternalRepoList, listOfInclude, listOfExclude, listOfExcludeComponents);

            // Assert
            string expectedLogMessage = $"Input Parameters used in Package Identifier:\n\t" +
                $"CaToolVersion\t\t --> {caToolInformation.CatoolVersion}\n\t" +
                $"CaToolRunningPath\t --> {caToolInformation.CatoolRunningLocation}\n\t" +
                $"PackageFilePath\t\t --> {appSettings.Directory.InputFolder}\n\t" +
                $"BomFolderPath\t\t --> {appSettings.Directory.OutputFolder}\n\t" +
                $"InternalRepoList\t --> {listOfInternalRepoList}\n\t" +
                $"ProjectType\t\t --> {appSettings.ProjectType}\n\t" +
                $"LogFolderPath\t\t --> {Log4Net.CatoolLogPath}\n\t" +
                $"Include\t\t\t --> {listOfInclude}\n\t" +
                $"Exclude\t\t\t --> {listOfExclude}\n";

            var logEvents = memoryAppender.GetEvents();
            Assert.IsNotEmpty(logEvents);
            var actualLogMessage = logEvents[0].RenderedMessage;
            Assert.AreEqual(expectedLogMessage, actualLogMessage);
        }
        [TestCase("NPM", "p*-lock.json,*.cdx.json", "node_modules,Test", "Npm-Test")]
        [TestCase("NUGET", "p*-lock.json,*.cdx.json", "package,Test", "Nuget-Test")]
        [TestCase("MAVEN", "*.cdx.json", "package,Test", "Maven-Test")]
        [TestCase("DEBIAN", "*.cdx.json", "package,Test", "Debian-Test")]
        [TestCase("POETRY", "Poetry.lock,*.cdx.json", "package,Test", "Poetry-Test")]
        [TestCase("CONAN", "conan.lock,*.cdx.json", "package,Test", "Conan-Test")]
        [TestCase("ALPINE", "*.cdx.json", "package,Test", "Alpine-Test")]
        public void DisplayIncludeFiles_ValidProjectType_ReturnsIncludeFiles(string projectType, string expectedInclude, string expectedExclude, string expectedRepos)
        {
            SetupAppSettings(projectType, expectedInclude, expectedExclude, expectedRepos);
            string result = DisplayInformation.DisplayIncludeFiles(appSettings);
            Assert.AreEqual(expectedInclude, result);
        }

        [TestCase("NPM", "p*-lock.json,*.cdx.json", "node_modules,Test", "Npm-Test")]
        [TestCase("NUGET", "p*-lock.json,*.cdx.json", "package,Test", "Nuget-Test")]
        [TestCase("MAVEN", "*.cdx.json", "package,Test", "Maven-Test")]
        [TestCase("DEBIAN", "*.cdx.json", "package,Test", "Debian-Test")]
        [TestCase("POETRY", "Poetry.lock,*.cdx.json", "package,Test", "Poetry-Test")]
        [TestCase("CONAN", "conan.lock,*.cdx.json", "package,Test", "Conan-Test")]
        [TestCase("ALPINE", "*.cdx.json", "package,Test", "Alpine-Test")]
        public void DisplayExcludeFiles_ValidProjectType_ReturnsExcludeFiles(string projectType, string expectedInclude, string expectedExclude, string expectedRepos)
        {
            SetupAppSettings(projectType, expectedInclude, expectedExclude, expectedRepos);
            string result = DisplayInformation.DisplayExcludeFiles(appSettings);
            Assert.AreEqual(expectedExclude, result);
        }

        [TestCase("NPM", "p*-lock.json,*.cdx.json", "node_modules,Test", "Npm-Test")]
        [TestCase("NUGET", "p*-lock.json,*.cdx.json", "package,Test", "Nuget-Test")]
        [TestCase("MAVEN", "*.cdx.json", "package,Test", "Maven-Test")]
        [TestCase("DEBIAN", "*.cdx.json", "package,Test", "Debian-Test")]
        [TestCase("POETRY", "Poetry.lock,*.cdx.json", "package,Test", "Poetry-Test")]
        [TestCase("CONAN", "conan.lock,*.cdx.json", "package,Test", "Conan-Test")]
        [TestCase("ALPINE", "*.cdx.json", "package,Test", "Alpine-Test")]
        public void GetInternalRepolist_ValidProjectType_ReturnsInternalRepos(string projectType, string expectedInclude, string expectedExclude, string expectedRepos)
        {
            SetupAppSettings(projectType, expectedInclude, expectedExclude, expectedRepos);
            string result = DisplayInformation.GetInternalRepolist(appSettings);
            Assert.AreEqual(expectedRepos, result);
        }

        [Test]
        public void DisplayIncludeFiles_InvalidProjectType_ReturnsEmptyString()
        {
            appSettings.ProjectType = "INVALID";
            string result = DisplayInformation.DisplayIncludeFiles(appSettings);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void DisplayExcludeFiles_InvalidProjectType_ReturnsEmptyString()
        {
            appSettings.ProjectType = "INVALID";
            string result = DisplayInformation.DisplayExcludeFiles(appSettings);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void GetInternalRepolist_InvalidProjectType_ReturnsEmptyString()
        {
            appSettings.ProjectType = "INVALID";
            string result = DisplayInformation.GetInternalRepolist(appSettings);
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void DisplayExcludeComponents_ValidExcludeComponents_ReturnsExcludeComponents()
        {
            appSettings.SW360 = new SW360
            {
                ExcludeComponents = new List<string> { "component1", "component2" }
            };
            string result = DisplayInformation.DisplayExcludeComponents(appSettings);
            Assert.AreEqual("component1,component2", result);
        }

        [Test]
        public void DisplayExcludeComponents_NullExcludeComponents_ReturnsEmptyString()
        {
            appSettings.SW360 = new SW360
            {
                ExcludeComponents = null
            };
            string result = DisplayInformation.DisplayExcludeComponents(appSettings);
            Assert.AreEqual(string.Empty, result);
        }
        [Test]
        public void LogBomGenerationWarnings_ShouldLogWarning_WhenSW360AndJfrogAreNull()
        {
            // Arrange
            appSettings.SW360 = null;
            appSettings.Jfrog = null;
            memoryAppender = new MemoryAppender();
            BasicConfigurator.Configure(memoryAppender);
            // Act
            DisplayInformation.LogBomGenerationWarnings(appSettings);

            // Assert
            var logEvents = memoryAppender.GetEvents();
            Assert.IsNotEmpty(logEvents);
            var logMessage = logEvents.First().RenderedMessage;
            Assert.AreEqual("CycloneDX Bom file generated without using SW360 and Jfrog details.", logMessage);
        }

        [Test]
        public void LogBomGenerationWarnings_ShouldLogWarning_WhenSW360IsNull()
        {
            // Arrange
            appSettings.SW360 = null;
            appSettings.Jfrog = new Jfrog();
            memoryAppender = new MemoryAppender();
            BasicConfigurator.Configure(memoryAppender);
            // Act
            DisplayInformation.LogBomGenerationWarnings(appSettings);

            // Assert
            var logEvents = memoryAppender.GetEvents();
            Assert.IsNotEmpty(logEvents);
            var logMessage = logEvents.First().RenderedMessage;
            Assert.AreEqual("CycloneDX Bom file generated without using SW360 details.", logMessage);
        }

        [Test]
        public void LogBomGenerationWarnings_ShouldLogWarning_WhenJfrogIsNull()
        {
            // Arrange
            appSettings.SW360 = new SW360();
            appSettings.Jfrog = null;
            memoryAppender = new MemoryAppender();
            BasicConfigurator.Configure(memoryAppender);
            // Act
            DisplayInformation.LogBomGenerationWarnings(appSettings);

            // Assert
            var logEvents = memoryAppender.GetEvents();
            Assert.IsNotEmpty(logEvents);
            var logMessage = logEvents.First().RenderedMessage;
            Assert.AreEqual("CycloneDX Bom file generated without using Jfrog details.", logMessage);
        }

        [Test]
        public void LogBomGenerationWarnings_ShouldNotLogWarning_WhenSW360AndJfrogAreNotNull()
        {
            // Arrange
            appSettings.SW360 = new SW360();
            appSettings.Jfrog = new Jfrog();
            memoryAppender = new MemoryAppender();
            BasicConfigurator.Configure(memoryAppender);
            // Act
            DisplayInformation.LogBomGenerationWarnings(appSettings);

            // Assert
            var logEvents = memoryAppender.GetEvents();
            Assert.IsEmpty(logEvents);
        }

        private void SetupAppSettings(string projectType, string include, string exclude, string repos)
        {
            appSettings.ProjectType = projectType;
            var includeList = include.Split(',');
            var excludeList = exclude.Split(',');
            var repoList = repos.Split(',');

            switch (projectType)
            {
                case "NPM":
                    appSettings.Npm = new Config { Include = includeList, Exclude = excludeList, Artifactory = new Artifactory { InternalRepos = repoList } };
                    break;
                case "NUGET":
                    appSettings.Nuget = new Config { Include = includeList, Exclude = excludeList, Artifactory = new Artifactory { InternalRepos = repoList } };
                    break;
                case "MAVEN":
                    appSettings.Maven = new Config { Include = includeList, Exclude = excludeList, Artifactory = new Artifactory { InternalRepos = repoList } };
                    break;
                case "DEBIAN":
                    appSettings.Debian = new Config { Include = includeList, Exclude = excludeList, Artifactory = new Artifactory { InternalRepos = repoList } };
                    break;
                case "POETRY":
                    appSettings.Poetry = new Config { Include = includeList, Exclude = excludeList, Artifactory = new Artifactory { InternalRepos = repoList } };
                    break;
                case "CONAN":
                    appSettings.Conan = new Config { Include = includeList, Exclude = excludeList, Artifactory = new Artifactory { InternalRepos = repoList } };
                    break;
                case "ALPINE":
                    appSettings.Alpine = new Config { Include = includeList, Exclude = excludeList, Artifactory = new Artifactory { InternalRepos = repoList } };
                    break;
            }
        }
    }
}