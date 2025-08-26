using NUnit.Framework;
using LCT.Common.Logging;
using LCT.Common.Model;
using CycloneDX.Models;
using System.Collections.Generic;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace LCT.Common.Tests.Logging
{
    [TestFixture]
    public class LoggerHelperTests
    {
        private CatoolInfo _catoolInfo;
        private CommonAppSettings _appSettings;
        private StringWriter _consoleOutput;

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
                LoggerHelper.WriteToConsoleTable(printData, printTimingData, "http://summary.link", "TestExeType")
            );
        }

        [Test]
        public void WriteToSpectreConsoleTable_CoversAllBranches()
        {
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
                LoggerHelper.WriteToSpectreConsoleTable(printData, printTimingData, "http://summary.link", "TestExeType")
            );
        }

        [Test]
        public void GetColorForItem_CoversAllBranches()
        {
            var method = typeof(LoggerHelper).GetMethod("GetColorForItem", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.AreEqual("red", method.Invoke(null, new object[] { "Packages Not Uploaded Due To Error", 1 }));
            Assert.AreEqual("green", method.Invoke(null, new object[] { "Packages Not Uploaded Due To Error", 0 }));
            Assert.AreEqual("red", method.Invoke(null, new object[] { "Packages Not Existing in Remote Cache", 2 }));
            Assert.AreEqual("green", method.Invoke(null, new object[] { "Packages Not Existing in Remote Cache", 0 }));
            Assert.That(method.Invoke(null, new object[] { "FeatureX", 5 }), Is.Not.Null);
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
                LoggerHelper.WriteComponentsWithoutDownloadURLByUseingSpectreToKpi(componentInfo, lstReleaseNotCreated, "http://sw360/")
            );
            Assert.DoesNotThrow(() =>
                LoggerHelper.WriteComponentsWithoutDownloadURLByUseingSpectreToKpi(new List<ComparisonBomData>(), new List<Components>(), "http://sw360/")
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
                LoggerHelper.WriteComponentsWithoutDownloadURLToKpi(componentInfo, lstReleaseNotCreated, "http://sw360/")
            );
            Assert.DoesNotThrow(() =>
                LoggerHelper.WriteComponentsWithoutDownloadURLToKpi(new List<ComparisonBomData>(), new List<Components>(), "http://sw360/")
            );
        }

        [Test]
        public void Sw360URL_ReturnsExpectedFormat()
        {
            var method = typeof(LoggerHelper).GetMethod("Sw360URL", BindingFlags.NonPublic | BindingFlags.Static);
            string url = (string)method.Invoke(null, new object[] { "http://sw360", "rel1" });
            Assert.That(url, Does.Contain("rel1"));
        }
    }
}