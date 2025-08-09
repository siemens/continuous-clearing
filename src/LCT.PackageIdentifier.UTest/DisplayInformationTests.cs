using LCT.Common;
using LCT.Common.Interface;
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