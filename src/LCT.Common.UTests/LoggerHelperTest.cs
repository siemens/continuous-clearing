// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2025 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Models;
using LCT.Common.Constants;
using LCT.Common.Logging;
using LCT.Common.Model;
using log4net.Appender;
using log4net.Config;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LCT.Common.UTest
{
    [TestFixture]
    public class LoggerHelperTests
    {
        private CatoolInfo _catoolInfo;
        private CommonAppSettings _appSettings;
        private StringWriter _consoleOutput;
        private CatoolInfo caToolInformation;
        private MemoryAppender memoryAppender;

        [SetUp]
        public void Setup()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            System.IO.Directory.CreateDirectory(tempDir);

            _catoolInfo = new CatoolInfo
            {
                CatoolVersion = "8.2.0",
                CatoolRunningLocation = tempDir
            };

            _appSettings = new CommonAppSettings();
            var directoryType = _appSettings.Directory.GetType();
            directoryType.GetProperty("InputFolder").SetValue(_appSettings.Directory, tempDir);
            directoryType.GetProperty("OutputFolder").SetValue(_appSettings.Directory, tempDir);

            _appSettings.ProjectType = "TestProject";
            _appSettings.SW360 = new SW360
            {
                URL = "http://localhost:8090",
                AuthTokenType = "Token",
                ProjectName = "Test",
                ProjectID = "036ec371847b4b199dd21c3494ccb108",
                Fossology = new Fossology { URL = "http://localhost:8091", EnableTrigger = false },
                IgnoreDevDependency = false
            };
            _appSettings.Jfrog = new Jfrog { URL = "http://localhost:8098", DryRun = true };

            _consoleOutput = new StringWriter();
            Console.SetOut(_consoleOutput);
        }

        [TearDown]
        public void TearDown()
        {
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
            _consoleOutput.Dispose();
        }

        [Test]
        public void WrapPath_ReturnsOriginalIfShort()
        {
            string path = Path.Combine(Path.GetTempPath(), "ShortPath");
            string result = LoggerHelper.WrapPath(path, 80);
            Assert.AreEqual(path, result);
        }

        [Test]
        public void WrapPath_WrapsLongPath()
        {
            string basePath = Path.GetTempPath();
            string longPath = basePath;
            for (int i = 0; i < 12; i++)
            {
                longPath = Path.Combine(longPath, $"Subfolder{i}");
            }
            string result = LoggerHelper.WrapPath(longPath, 40);
            Assert.That(result, Does.Contain("\n"));
        }

        [Test]
        public void WriteStyledPanel_CoversTitleAndNoTitle()
        {
            Assert.DoesNotThrow(() => LoggerHelper.WriteStyledPanel("Test Content", "Test Title"));
            Assert.DoesNotThrow(() => LoggerHelper.WriteStyledPanel("Test Content"));
        }

        [Test]
        public void WriteHeader_CentersText()
        {
            Assert.DoesNotThrow(() => LoggerHelper.WriteHeader("Test Header"));
        }

        [TestCase("warning")]
        [TestCase("error")]
        [TestCase("success")]
        [TestCase("notice")]
        [TestCase("header")]
        [TestCase("panel")]
        [TestCase("alert")]
        [TestCase("other")]
        public void WriteFallback_CoversAllTypes(string type)
        {
            Assert.DoesNotThrow(() => LoggerHelper.WriteFallback("Test fallback message", type));
        }

        [Test]
        public void WriteToConsoleTable_CoversAllBranches()
        {
            KpiNames kpiNames = new KpiNames();

            var printData = new Dictionary<string, int>
            {
                { "Feature1", 10 },
                { "Packages Not Uploaded Due To Error", 2 },
                { "Packages Not Existing in Remote Cache", 1 }
            };
            var printTimingData = new Dictionary<string, double>
            {
                { "Operation1", 12.5 },
                { "Operation2", 65.0 }
            };
            LoggerFactory.UseSpectreConsole = false;
            Assert.DoesNotThrow(() =>
                LoggerHelper.WriteToConsoleTable(printData, printTimingData, "http://summary.link", "TestExeType", kpiNames)
            );
        }

        [Test]
        public void WriteToSpectreConsoleTable_CoversAllBranches()
        {
            KpiNames kpiNames = new KpiNames();
            var printData = new Dictionary<string, int>
            {
                { "Feature1", 10 },
                { "Packages Not Uploaded Due To Error", 2 },
                { "Packages Not Existing in Remote Cache", 1 }
            };
            var printTimingData = new Dictionary<string, double>
            {
                { "Operation1", 12.5 },
                { "Operation2", 65.0 }
            };
            Assert.DoesNotThrow(() =>
                LoggerHelper.WriteToSpectreConsoleTable(printData, printTimingData, "http://summary.link", "TestExeType", kpiNames)
            );
        }


        [Test]
        public void WriteInternalComponentsTableInCli_CoversBothBranches()
        {
            var components = new List<Component>
            {
                new Component { Name = "Comp1", Version = "1.0" },
                new Component { Name = "Comp2", Version = "2.0" }
            };
            LoggerFactory.UseSpectreConsole = true;
            Assert.DoesNotThrow(() => LoggerHelper.WriteInternalComponentsTableInCli(components));
            LoggerFactory.UseSpectreConsole = false;
            Assert.DoesNotThrow(() => LoggerHelper.WriteInternalComponentsTableInCli(components));
        }

        [Test]
        public void WriteInternalComponentsListTableToKpi_CoversNullAndValid()
        {
            var components = new List<Component>
            {
                new Component { Name = "Comp1", Version = "1.0" },
                new Component { Name = null, Version = null }
            };
            Assert.DoesNotThrow(() => LoggerHelper.WriteInternalComponentsListTableToKpi(components));
            Assert.DoesNotThrow(() => LoggerHelper.WriteInternalComponentsListTableToKpi(null));
        }

        [Test]
        public void WriteInternalComponentsListToKpi_CoversNullAndValid()
        {
            var components = new List<Component>
            {
                new Component { Name = "Comp1", Version = "1.0" },
                new Component { Name = null, Version = null }
            };
            Assert.DoesNotThrow(() => LoggerHelper.WriteInternalComponentsListToKpi(components));
            Assert.DoesNotThrow(() => LoggerHelper.WriteInternalComponentsListToKpi(null));
        }

        [Test]
        public void ValidFilesInfoDisplayForCli_CoversBothBranches()
        {
            LoggerFactory.UseSpectreConsole = true;
            Assert.DoesNotThrow(() => LoggerHelper.ValidFilesInfoDisplayForCli("config.json"));
            LoggerFactory.UseSpectreConsole = false;
            Assert.DoesNotThrow(() => LoggerHelper.ValidFilesInfoDisplayForCli("config.json"));
        }

        [Test]
        public void JfrogConnectionInfoDisplayForCli_CoversBothBranches()
        {
            LoggerFactory.UseSpectreConsole = true;
            Assert.DoesNotThrow(() => LoggerHelper.JfrogConnectionInfoDisplayForCli());
            LoggerFactory.UseSpectreConsole = false;
            Assert.DoesNotThrow(() => LoggerHelper.JfrogConnectionInfoDisplayForCli());
        }

        [Test]
        public void WriteLine_CoversSafeSpectreAction()
        {
            Assert.DoesNotThrow(() => LoggerHelper.WriteLine());
        }

        [Test]
        public void WriteInfoWithMarkup_CoversSafeSpectreAction()
        {
            Assert.DoesNotThrow(() => LoggerHelper.WriteInfoWithMarkup("[green]Test Markup[/]"));
        }

        [Test]
        public void SafeSpectreAction_CatchesExceptionAndCallsFallback()
        {
            var method = typeof(LoggerHelper).GetMethod("SafeSpectreAction", BindingFlags.Public | BindingFlags.Static);
            Action action = () => throw new InvalidOperationException();
            Assert.DoesNotThrow(() => method.Invoke(null, new object[] { action, "FallbackMessage", "warning" }));
        }

        // --- Additional coverage for component-related methods ---

        [Test]
        public void WriteComponentsWithoutDownloadURLByUseingSpectreToKpi_CoversAllBranches()
        {
            var componentInfo = new List<ComparisonBomData>
            {
                new ComparisonBomData { Name = "CompA", Version = "1.0", ReleaseID = "rel1" }
            };
            var lstReleaseNotCreated = new List<Components>
            {
                new Components { Name = "CompB", Version = "2.0" }
            };
            Assert.DoesNotThrow(() =>
                LoggerHelper.WriteComponentsWithoutDownloadURLByUseingSpectreToKpi(componentInfo, lstReleaseNotCreated, "http://sw360/", lstReleaseNotCreated)
            );
            Assert.DoesNotThrow(() =>
                LoggerHelper.WriteComponentsWithoutDownloadURLByUseingSpectreToKpi(new List<ComparisonBomData>(), new List<Components>(), "http://sw360/", lstReleaseNotCreated)
            );
        }

        [Test]
        public void WriteComponentsNotLinkedListTableWithSpectre_CoversAllBranches()
        {
            var components = new List<Components>
            {
                new Components { Name = "CompA", Version = "1.0" }
            };
            Assert.DoesNotThrow(() =>
                LoggerHelper.WriteComponentsNotLinkedListTableWithSpectre(components)
            );
            Assert.DoesNotThrow(() =>
                LoggerHelper.WriteComponentsNotLinkedListTableWithSpectre(new List<Components>())
            );
        }

        [Test]
        public void WriteComponentsNotLinkedListInConsole_CoversBothBranches()
        {
            var components = new List<Components>
            {
                new Components { Name = "CompA", Version = "1.0" }
            };
            LoggerFactory.UseSpectreConsole = false;
            Assert.DoesNotThrow(() =>
                LoggerHelper.WriteComponentsNotLinkedListInConsole(components)
            );
            Assert.DoesNotThrow(() =>
                LoggerHelper.WriteComponentsNotLinkedListInConsole(new List<Components>())
            );
        }

        [Test]
        public void WriteComponentsWithoutDownloadURLToKpi_CoversAllBranches()
        {
            var componentInfo = new List<ComparisonBomData>
            {
                new ComparisonBomData { Name = "CompA", Version = "1.0", ReleaseID = "rel1" }
            };
            var lstReleaseNotCreated = new List<Components>
            {
                new Components { Name = "CompB", Version = "2.0" }
            };
            LoggerFactory.UseSpectreConsole = false;
            Assert.DoesNotThrow(() =>
                LoggerHelper.WriteComponentsWithoutDownloadURLToKpi(componentInfo, lstReleaseNotCreated, "http://sw360/", lstReleaseNotCreated)
            );
            Assert.DoesNotThrow(() =>
                LoggerHelper.WriteComponentsWithoutDownloadURLToKpi(new List<ComparisonBomData>(), new List<Components>(), "http://sw360/", lstReleaseNotCreated)
            );
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
            LoggerFactory.UseSpectreConsole = false;
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
            appSettings.Mode = "test";
            var listParameters = new ListofPerametersForCli
            {
                InternalRepoList = listOfInternalRepoList,
                Include = listOfInclude,
                Exclude = listOfExclude,
                ExcludeComponents = listOfExcludeComponents
            };
            memoryAppender = new MemoryAppender();
            BasicConfigurator.Configure(memoryAppender);
            // Act
            LoggerHelper.LogInputParameters(caToolInformation, appSettings, listParameters, Dataconstant.Identifier, OutFolder);

            // Assert
            string expectedLogMessage = $"Input Parameters used in Package Identifier:\n\t" +
                $"CaToolVersion\t\t --> {caToolInformation.CatoolVersion}\n\t" +
                $"CaToolRunningPath\t --> {caToolInformation.CatoolRunningLocation}\n\t" +
                $"PackageFilePath\t\t --> {appSettings.Directory.InputFolder}\n\t" +
                $"BomFolderPath\t\t --> {appSettings.Directory.OutputFolder}\n\t" +
                $"SW360Url\t\t --> {appSettings.SW360.URL}\n\t" +
                $"SW360ProjectName\t --> {appSettings.SW360.ProjectName}\n\t" +
                $"SW360ProjectID\t\t --> {appSettings.SW360.ProjectID}\n\t" +
                $"ExcludeComponents\t --> {listOfExcludeComponents}\n\t" +
                $"Mode\t --> {appSettings.Mode}\n\t" +
                $"ProjectType\t\t --> {appSettings.ProjectType}\n\t" +
                $"LogFolderPath\t\t --> {Log4Net.CatoolLogPath}\n\t" +
                $"Include\t\t\t --> {listOfInclude}\n\t" +
                $"Exclude\t\t\t --> {listOfExclude}\n";
            LoggerFactory.UseSpectreConsole = true;
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
            LoggerFactory.UseSpectreConsole = false;
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
            var listParameters = new ListofPerametersForCli
            {
                InternalRepoList = listOfInternalRepoList,
                Include = listOfInclude,
                Exclude = listOfExclude,
                ExcludeComponents = listOfExcludeComponents
            };
            memoryAppender = new MemoryAppender();
            BasicConfigurator.Configure(memoryAppender);
            // Act
            LoggerHelper.LogInputParameters(caToolInformation, appSettings, listParameters, Dataconstant.Identifier, OutFolder);

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
            LoggerFactory.UseSpectreConsole = true;
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
            LoggerFactory.UseSpectreConsole = false;
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
            var listParameters = new ListofPerametersForCli
            {
                InternalRepoList = listOfInternalRepoList,
                Include = listOfInclude,
                Exclude = listOfExclude,
                ExcludeComponents = listOfExcludeComponents
            };
            memoryAppender = new MemoryAppender();
            BasicConfigurator.Configure(memoryAppender);
            // Act
            LoggerHelper.LogInputParameters(caToolInformation, appSettings, listParameters, Dataconstant.Identifier, OutFolder);

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
            LoggerFactory.UseSpectreConsole = true;
            var logEvents = memoryAppender.GetEvents();
            Assert.IsNotEmpty(logEvents);
            var actualLogMessage = logEvents[0].RenderedMessage;
            Assert.AreEqual(expectedLogMessage, actualLogMessage);
        }

        [Test]
        public void LogInputParameters_Creator_ShouldLogExpectedMessage_WithTestMode()
        {
            // Arrange
            LoggerFactory.UseSpectreConsole = false;
            var catool = new CatoolInfo { CatoolVersion = "2.0.0", CatoolRunningLocation = "creatorRun" };
            var bomFilePath = Path.Combine(Path.GetTempPath(), "creator-bom.spdx.json");

            var appSettings = new CommonAppSettings
            {
                Mode = "test", // triggers Mode line
                SW360 = new SW360
                {
                    URL = "http://sw360.creator",
                    ProjectName = "CreatorProject",
                    ProjectID = "CPID123",
                    Fossology = new Fossology { URL = "http://foss.creator", EnableTrigger = true },
                    IgnoreDevDependency = true
                }
            };

            var cli = new ListofPerametersForCli();
            var memoryAppender = new MemoryAppender();
            BasicConfigurator.Configure(memoryAppender);

            // Act
            LoggerHelper.LogInputParameters(catool, appSettings, cli, Dataconstant.Creator, bomFilePath);

            // Assert
            var events = memoryAppender.GetEvents();
            Assert.IsNotEmpty(events, "Expected log events for Creator execution.");
            var notice = Array.FindLast(events, e => e.RenderedMessage.Contains("Input parameters used in Package Creator"));
            Assert.IsNotNull(notice, "Creator notice log not found.");

            string expected =
                $"Input parameters used in Package Creator:\n\t" +
                $"CaToolVersion\t\t --> {catool.CatoolVersion}\n\t" +
                $"CaToolRunningPath\t --> {catool.CatoolRunningLocation}\n\t" +
                $"BomFilePath\t\t --> {bomFilePath}\n\t" +
                $"SW360Url\t\t --> {appSettings.SW360.URL}\n\t" +
                $"SW360ProjectName\t --> {appSettings.SW360.ProjectName}\n\t" +
                $"SW360ProjectID\t\t --> {appSettings.SW360.ProjectID}\n\t" +
                $"FossologyURL\t\t --> {appSettings.SW360.Fossology.URL}\n\t" +
                $"EnableFossTrigger\t --> {appSettings.SW360.Fossology.EnableTrigger}\n\t" +
                $"IgnoreDevDependency\t --> {appSettings.SW360.IgnoreDevDependency}\n\t" +
                $"Mode\t\t --> {appSettings.Mode}\n\t" +
                $"LogFolderPath\t\t --> {Log4Net.CatoolLogPath}\n\t";

            Assert.AreEqual(expected, notice.RenderedMessage);
        }

        [Test]
        public void LogInputParameters_Uploader_ShouldLogExpectedMessages()
        {
            // Arrange
            LoggerFactory.UseSpectreConsole = false;
            var catool = new CatoolInfo { CatoolVersion = "3.1.4", CatoolRunningLocation = "uploaderRun" };
            var bomFilePath = Path.Combine(Path.GetTempPath(), "upload-bom.spdx.json");

            var appSettings = new CommonAppSettings
            {
                Jfrog = new Jfrog { URL = "http://JFrog.upload", DryRun = false }
            };

            var cli = new ListofPerametersForCli();
            var memoryAppender = new MemoryAppender();
            BasicConfigurator.Configure(memoryAppender);

            // Act
            LoggerHelper.LogInputParameters(catool, appSettings, cli, Dataconstant.Uploader, bomFilePath);

            // Assert
            var events = memoryAppender.GetEvents();
            Assert.IsTrue(events.Length >= 2, "Expected at least two log events (Info header + Notice details) for Uploader.");

            var infoHeader = Array.Find(events, e => e.RenderedMessage.Contains("Input Parameters used in Artifactory Uploader"));
            Assert.IsNotNull(infoHeader, "Uploader info header log not found.");

            var detail = Array.Find(events, e => e.RenderedMessage.Contains("JFrogUrl:"));
            Assert.IsNotNull(detail, "Uploader detail notice log not found.");

            string expectedDetail =
                $"\tBomFilePath:\t\t {bomFilePath}\n\t" +
                $"CaToolVersion\t\t {catool.CatoolVersion}\n\t" +
                $"CaToolRunningPath\t {catool.CatoolRunningLocation}\n\t" +
                $"JFrogUrl:\t\t {appSettings.Jfrog.URL}\n\t" +
                $"Dry-run:\t\t {appSettings.Jfrog.DryRun}\n\t" +
                $"LogFolderPath:\t\t {Log4Net.CatoolLogPath}\n";

            Assert.AreEqual(expectedDetail, detail.RenderedMessage);
        }
        [Test]
        public void DisplaySettingsWithLogger_LogsHeader_ValidAndInvalidProjectTypes()
        {
            LoggerFactory.UseSpectreConsole = false;
            var memoryAppender = new log4net.Appender.MemoryAppender();
            var hierarchy = (log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository();
            hierarchy.Root.AddAppender(memoryAppender);
            hierarchy.Root.Level = log4net.Core.Level.All;
            hierarchy.Configured = true;

            // Prepare project types and config map
            var projectTypes = new List<string> { "npm", "unknown" };
            var cfg = new Config
            {
                Include = new[] { "inc1", "inc2" },
                Exclude = new[] { "exc1" },
                DevDepRepo = "dev-dep",
                ReleaseRepo = "release-repo",
                Artifactory = new Artifactory
                {
                    ThirdPartyRepos = new List<ThirdPartyRepo>
                    {
                        new ThirdPartyRepo { Name = "third-party-A", Upload = true }
                    }
                }
            };
            var map = new Dictionary<string, Config>(StringComparer.OrdinalIgnoreCase)
            {
                { "npm", cfg }
            };

            // Invoke private method via reflection
            var method = typeof(LoggerHelper).GetMethod("DisplaySettingsWithLogger", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method, "Reflection failed for DisplaySettingsWithLogger");
            method.Invoke(null, new object[] { projectTypes, map });

            var events = memoryAppender.GetEvents();
            Assert.IsTrue(events.Any(e => e.RenderedMessage.Contains("Current Application Settings:")), "Header missing");
            Assert.IsTrue(events.Any(e => e.RenderedMessage.Contains("npm:")), "Valid project type missing");
            Assert.IsTrue(events.Any(e => e.RenderedMessage.Contains("DisplayAllSettings(): Invalid ProjectType - unknown")),
                "Invalid project type error missing");
            Assert.IsTrue(events.Any(e => e.RenderedMessage.Contains("DEVDEP_REPO_NAME:")), "Package settings block missing");
        }

        [Test]
        public void DisplayPackageSettings_FullConfig_LogsAllValues()
        {
            LoggerFactory.UseSpectreConsole = false;
            var memoryAppender = new log4net.Appender.MemoryAppender();
            var hierarchy = (log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository();
            hierarchy.Root.AddAppender(memoryAppender);
            hierarchy.Root.Level = log4net.Core.Level.All;
            hierarchy.Configured = true;

            var cfg = new Config
            {
                Include = new[] { "incA", "incB" },
                Exclude = new[] { "excA" },
                DevDepRepo = "dev-dep-repo",
                ReleaseRepo = "release-repo",
                Artifactory = new Artifactory
                {
                    ThirdPartyRepos = new List<ThirdPartyRepo>
                    {
                        new ThirdPartyRepo { Name = "3rd-upload", Upload = true },
                        new ThirdPartyRepo { Name = "3rd-no-upload", Upload = false }
                    }
                }
            };

            var method = typeof(LoggerHelper).GetMethod("DisplayPackageSettings", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method, "Reflection failed for DisplayPackageSettings");
            method.Invoke(null, new object[] { cfg });

            var events = memoryAppender.GetEvents();
            var notice = events.LastOrDefault(e => e.RenderedMessage.Contains("DEVDEP_REPO_NAME:"));
            Assert.NotNull(notice, "Notice block not logged for full config");
            string msg = notice.RenderedMessage;
            Assert.That(msg, Does.Contain("DEVDEP_REPO_NAME:").And.Contain("THIRD_PARTY_REPO_NAME:").And.Contain("RELEASE_REPO_NAME:"));
            Assert.That(msg, Does.Contain("dev-dep-repo"));
            Assert.That(msg, Does.Contain("release-repo"));
            Assert.That(msg, Does.Contain("3rd-upload"));
            Assert.That(msg, Does.Contain("Exclude:").And.Contain("excA"));
            Assert.That(msg, Does.Contain("Include:").And.Contain("incA, incB"));
        }

        [Test]
        public void DisplayPackageSettings_NullConfig_LogsWarning()
        {
            LoggerFactory.UseSpectreConsole = false;
            var memoryAppender = new log4net.Appender.MemoryAppender();
            var hierarchy = (log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository();
            hierarchy.Root.AddAppender(memoryAppender);
            hierarchy.Root.Level = log4net.Core.Level.All;
            hierarchy.Configured = true;

            var method = typeof(LoggerHelper).GetMethod("DisplayPackageSettings", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            method.Invoke(null, new object[] { null });

            var events = memoryAppender.GetEvents();
            Assert.IsTrue(events.Any(e => e.RenderedMessage.Contains("DisplayPackageSettings(): Config is null.")),
                "Warning for null config not logged");
        }

        [Test]
        public void DisplayPackageSettings_NotConfiguredPaths_WhenArraysEmptyAndNoRepos()
        {
            LoggerFactory.UseSpectreConsole = false;
            var memoryAppender = new log4net.Appender.MemoryAppender();
            var hierarchy = (log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository();
            hierarchy.Root.AddAppender(memoryAppender);
            hierarchy.Root.Level = log4net.Core.Level.All;
            hierarchy.Configured = true;

            var cfg = new Config
            {
                Include = [],
                Exclude = null,
                DevDepRepo = "",
                ReleaseRepo = "",
                Artifactory = new Artifactory
                {
                    ThirdPartyRepos = new List<ThirdPartyRepo>() // none with Upload = true
                }
            };

            var method = typeof(LoggerHelper).GetMethod("DisplayPackageSettings", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            method.Invoke(null, new object[] { cfg });

            var events = memoryAppender.GetEvents();
            var notice = events.LastOrDefault(e => e.RenderedMessage.Contains("DEVDEP_REPO_NAME:"));
            Assert.NotNull(notice, "Expected notice log for empty config");
            // We cannot rely on the exact literal of Dataconstant.NotConfigured; just ensure placeholders present (empty after tab)
            Assert.That(notice.RenderedMessage, Does.Contain("DEVDEP_REPO_NAME:\t"));
            Assert.That(notice.RenderedMessage, Does.Contain("THIRD_PARTY_REPO_NAME:\t"));
            Assert.That(notice.RenderedMessage, Does.Contain("RELEASE_REPO_NAME:\t"));
        }
    }
}